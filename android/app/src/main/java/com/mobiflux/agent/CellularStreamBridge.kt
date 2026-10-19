package com.mobiflux.agent

import java.io.BufferedInputStream
import java.net.Socket
import kotlin.concurrent.thread

internal object CellularStreamBridge {
    internal fun bridge(local: Socket, remote: Socket, input: BufferedInputStream) {
        val failure = java.util.concurrent.atomic.AtomicReference<Exception?>()
        fun pump(source: java.io.InputStream, destination: Socket) {
            try {
                source.copyTo(destination.getOutputStream())
                destination.shutdownOutput()
            } catch (error: Exception) {
                failure.compareAndSet(null, error)
                try { local.close() } finally { remote.close() }
            }
        }
        val up = thread(name = "mobiflux-upload", isDaemon = true) {
            pump(input, remote)
        }
        try {
            pump(remote.getInputStream(), local)
            up.join()
            failure.get()?.let { throw it }
        } finally {
            try { local.close() } finally { remote.close() }
            if (up.isAlive) up.join(1000)
        }
    }
    }


