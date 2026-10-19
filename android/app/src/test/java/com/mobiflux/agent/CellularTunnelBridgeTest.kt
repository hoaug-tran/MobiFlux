package com.mobiflux.agent

import java.io.BufferedInputStream
import java.net.InetAddress
import java.net.ServerSocket
import java.net.Socket
import java.util.concurrent.Executors
import java.util.concurrent.TimeUnit
import org.junit.Test
import org.junit.Assert.assertArrayEquals

class CellularTunnelBridgeTest {
    @Test fun uploadEofPreservesDelayedResponse() {
        val executor = Executors.newFixedThreadPool(2)
        val request = ByteArray(131072) { (it % 251).toByte() }
        val response = ByteArray(262144) { (it % 239).toByte() }
        try {
            ServerSocket(0, 1, InetAddress.getLoopbackAddress()).use { ingress ->
                ServerSocket(0, 1, InetAddress.getLoopbackAddress()).use { upstream ->
                    val server = executor.submit {
                        upstream.accept().use { socket ->
                            socket.soTimeout = 5000
                            assertArrayEquals(request, socket.getInputStream().readBytes())
                            Thread.sleep(100)
                            socket.getOutputStream().write(response)
                            socket.shutdownOutput()
                        }
                    }
                    val bridge = executor.submit {
                        ingress.accept().use { local ->
                            Socket(InetAddress.getLoopbackAddress(), upstream.localPort).use { remote ->
                                CellularStreamBridge.bridge(local, remote, BufferedInputStream(local.getInputStream()))
                            }
                        }
                    }
                    Socket(InetAddress.getLoopbackAddress(), ingress.localPort).use { client ->
                        client.soTimeout = 5000
                        client.getOutputStream().write(request)
                        client.shutdownOutput()
                        assertArrayEquals(response, client.getInputStream().readBytes())
                    }
                    server.get(5, TimeUnit.SECONDS)
                    bridge.get(5, TimeUnit.SECONDS)
                }
            }
        } finally {
            executor.shutdownNow()
        }
    }
}

