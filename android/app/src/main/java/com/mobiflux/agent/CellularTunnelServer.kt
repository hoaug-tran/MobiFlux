package com.mobiflux.agent

import android.net.Network
import java.io.BufferedInputStream
import java.io.BufferedOutputStream
import java.io.EOFException
import java.net.InetSocketAddress
import java.net.ServerSocket
import java.net.Socket
import java.util.concurrent.ConcurrentHashMap
import java.util.concurrent.Semaphore
import kotlin.concurrent.thread

class AgentState {
    @Volatile private var cellular: Network? = null
    @Synchronized fun setCellular(network: Network) { cellular = network }
    @Synchronized fun removeCellular(network: Network) { if (cellular == network) cellular = null }
    @Synchronized fun clear() { cellular = null }
    fun current(): Network? = cellular
    fun statusJson() = "{\"cellularAvailable\":${cellular != null},\"canProxyTcp\":true,\"canResolveDnsOnCellular\":true,\"failClosed\":true}"
}

class CellularTunnelServer(private val state: AgentState) : AutoCloseable {
    private val server = ServerSocket(18080, 16, java.net.InetAddress.getLoopbackAddress())
    private val slots = Semaphore(128)
    private val sockets = ConcurrentHashMap.newKeySet<Socket>()
    @Volatile private var running = true

    fun start() = thread(name = "mobiflux-tunnel", isDaemon = true) {
        while (running) {
            val client = try { server.accept() } catch (_: Exception) { break }
            if (!slots.tryAcquire()) { client.close(); continue }
            sockets.add(client)
            if (!running) { client.close(); sockets.remove(client); slots.release(); break }
            thread(isDaemon = true) {
                try { accept(client) }
                finally { sockets.remove(client); slots.release() }
            }
        }
    }

    private fun accept(client: Socket) {
        client.use { socket ->
            socket.soTimeout = 30_000
            val input = BufferedInputStream(socket.getInputStream())
            val output = BufferedOutputStream(socket.getOutputStream())
            var connected = false
            try {
                val magic = ByteArray(4)
                input.readFully(magic)
                if (!magic.contentEquals("MFP1".toByteArray(Charsets.US_ASCII))) { output.write(1); output.flush(); return }
                val length = input.read()
                if (length !in 1..253) { output.write(1); output.flush(); return }
                val hostBytes = ByteArray(length)
                input.readFully(hostBytes)
                val high = input.read()
                val low = input.read()
                if (high < 0 || low < 0) throw EOFException()
                val port = (high shl 8) or low
                if (port == 0) { output.write(1); output.flush(); return }
                val host = String(hostBytes, Charsets.UTF_8)
                val network = state.current() ?: run { output.write(2); output.flush(); return }
                val addresses = network.getAllByName(host)
                var remote: Socket? = null
                for (address in addresses) {
                    val candidate = network.socketFactory.createSocket()
                    try {
                        candidate.connect(InetSocketAddress(address, port), 10_000)
                        remote = candidate
                        break
                    } catch (_: Exception) { candidate.close() }
                }
                val target = remote ?: run { output.write(3); output.flush(); return }
                target.use {
                    sockets.add(target)
                    try {
                        if (!running || state.current() != network) { output.write(2); output.flush(); return }
                        output.write(0); output.flush(); connected = true
                        socket.soTimeout = 0
                        CellularStreamBridge.bridge(socket, target, input)
                    } finally { sockets.remove(target) }
                }
            } catch (_: Exception) {
                if (!connected) try { output.write(4); output.flush() } catch (_: Exception) { }
            }
        }
    }

    override fun close() {
        running = false
        server.close()
        sockets.forEach { runCatching { it.close() } }
    }
}

private fun BufferedInputStream.readFully(buffer: ByteArray) {
    var offset = 0
    while (offset < buffer.size) {
        val count = read(buffer, offset, buffer.size - offset)
        if (count < 0) throw EOFException()
        offset += count
    }
}
