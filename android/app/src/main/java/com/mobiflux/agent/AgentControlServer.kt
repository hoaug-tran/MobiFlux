package com.mobiflux.agent

import java.net.ServerSocket
import java.net.Socket
import kotlin.concurrent.thread

class AgentControlServer(private val state: AgentState) : AutoCloseable {
    private val server = ServerSocket(18081, 4, java.net.InetAddress.getLoopbackAddress())
    @Volatile private var running = true
    @Volatile private var active: Socket? = null
    fun start() = thread(name = "mobiflux-control", isDaemon = true) {
        while (running) try {
            server.accept().use { socket ->
                active = socket
                if (!running) return@use
                socket.soTimeout = 5_000
                val input = socket.getInputStream()
                val line = StringBuilder()
                while (line.length < 2048) {
                    val next = input.read()
                    if (next < 0 || next == 10) break
                    if (next != 13) line.append(next.toChar())
                }
                val parts = line.toString().split(' ')
                val found = parts.size == 3 && parts[0] == "GET" && parts[1] == "/agent/status"
                val body = if (found) state.statusJson() else "{\"error\":\"not-found\"}"
                val status = if (found) "200 OK" else "404 Not Found"
                socket.getOutputStream().bufferedWriter().use {
                    it.write("HTTP/1.1 $status\r\nContent-Type: application/json\r\nContent-Length: ${body.toByteArray().size}\r\nConnection: close\r\n\r\n$body")
                    it.flush()
                }
            }
        } catch (_: Exception) { } finally { active = null }
    }
    override fun close() { running = false; server.close(); active?.close() }
}
