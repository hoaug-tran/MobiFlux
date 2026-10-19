package com.mobiflux.agent

import java.io.BufferedReader
import java.io.InputStreamReader
import java.net.ServerSocket
import java.net.Socket
import kotlin.concurrent.thread

class AgentControlServer(private val state: AgentState) : AutoCloseable {
    private val server = ServerSocket(18081, 8, java.net.InetAddress.getLoopbackAddress())
    @Volatile private var running = true
    @Volatile private var active: Socket? = null
    fun start() = thread(name = "mobiflux-control", isDaemon = true) {
        while (running) try {
            val socket = server.accept()
            active = socket
            if (!running) { socket.close(); break }
            thread(isDaemon = true) {
                socket.use { sock ->
                    try {
                        sock.soTimeout = 5_000
                        val reader = BufferedReader(InputStreamReader(sock.getInputStream(), Charsets.US_ASCII))
                        val requestLine = reader.readLine() ?: return@use
                        while (true) {
                            val header = reader.readLine() ?: break
                            if (header.isEmpty()) break
                        }
                        val parts = requestLine.split(' ')
                        val found = parts.size == 3 && parts[0] == "GET" && parts[1] == "/agent/status"
                        val body = if (found) state.statusJson() else "{\"error\":\"not-found\"}"
                        val status = if (found) "200 OK" else "404 Not Found"
                        val bytes = body.toByteArray(Charsets.UTF_8)
                        val out = sock.getOutputStream()
                        out.write("HTTP/1.1 $status\r\nContent-Type: application/json; charset=utf-8\r\nContent-Length: ${bytes.size}\r\nConnection: close\r\n\r\n$body".toByteArray(Charsets.UTF_8))
                        out.flush()
                    } catch (_: Exception) { }
                }
            }
        } catch (_: Exception) { } finally { active = null }
    }
    override fun close() { running = false; server.close(); active?.close() }
}
