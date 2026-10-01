package com.mobiflux.agent

import android.net.Network
import java.io.BufferedInputStream
import java.io.BufferedOutputStream
import java.net.ServerSocket
import java.net.Socket
import kotlin.concurrent.thread

class AgentState {
    @Volatile private var cellular: Network? = null
    fun setCellular(network: Network) { cellular = network }
    fun removeCellular(network: Network) { if (cellular == network) cellular = null }
    fun clear() { cellular = null }
    fun current(): Network? = cellular
    fun statusJson() = "{\"cellularAvailable\":${cellular != null},\"canProxyTcp\":true,\"canResolveDnsOnCellular\":true,\"failClosed\":true}"
}

class CellularTunnelServer(private val state: AgentState) : AutoCloseable {
    private val server = ServerSocket(18080, 16, java.net.InetAddress.getLoopbackAddress())
    private var running = true
    fun start() = thread(name = "mobiflux-tunnel", isDaemon = true) { while (running) try { accept(server.accept()) } catch (_: Exception) { } }
    private fun accept(client: Socket) = thread(isDaemon = true) {
        client.use { socket ->
            val input = BufferedInputStream(socket.getInputStream()); val output = BufferedOutputStream(socket.getOutputStream())
            try {
                val magic = ByteArray(4); input.readFully(magic); if (!magic.contentEquals("MFP1".toByteArray())) { output.write(1); return@thread }
                val length = input.read(); if (length !in 1..253) { output.write(1); return@thread }
                val host = String(input.readNBytes(length), Charsets.UTF_8); val port = (input.read() shl 8) or input.read()
                val network = state.current() ?: run { output.write(2); output.flush(); return@thread }
                // Both DNS resolution and socket creation are bound to the cellular Network. There is deliberately no default-network fallback.
                val address = network.getAllByName(host).firstOrNull() ?: run { output.write(3); output.flush(); return@thread }
                val remote = network.socketFactory.createSocket(address, port)
                output.write(0); output.flush(); bridge(socket, remote)
            } catch (_: Exception) { try { output.write(4); output.flush() } catch (_: Exception) { } }
        }
    }
    private fun bridge(local: Socket, remote: Socket) {
        remote.use { target ->
            val up = thread(isDaemon = true) { local.getInputStream().copyTo(target.getOutputStream()); target.shutdownOutput() }
            target.getInputStream().copyTo(local.getOutputStream()); local.shutdownOutput(); up.join()
        }
    }
    override fun close() { running = false; server.close() }
}

private fun BufferedInputStream.readFully(buffer: ByteArray) { var offset = 0; while (offset < buffer.size) { val count = read(buffer, offset, buffer.size - offset); if (count < 0) throw java.io.EOFException(); offset += count } }
