package com.mobiflux.agent

import java.net.ServerSocket
import kotlin.concurrent.thread

class AgentControlServer(private val state: AgentState) : AutoCloseable {
    private val server = ServerSocket(18081, 4, java.net.InetAddress.getLoopbackAddress()); private var running = true
    fun start() = thread(name = "mobiflux-control", isDaemon = true) { while (running) try { server.accept().use { socket ->
        val request = socket.getInputStream().bufferedReader().readLine().orEmpty()
        val body = if (request.startsWith("GET /agent/status")) state.statusJson() else "{\"error\":\"not-found\"}"
        val status = if (request.startsWith("GET /agent/status")) "200 OK" else "404 Not Found"
        socket.getOutputStream().bufferedWriter().use { it.write("HTTP/1.1 $status\r\nContent-Type: application/json\r\nContent-Length: ${body.toByteArray().size}\r\nConnection: close\r\n\r\n$body"); it.flush() }
    } } catch (_: Exception) { } }
    override fun close() { running = false; server.close() }
}
