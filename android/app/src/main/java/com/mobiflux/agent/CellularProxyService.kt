package com.mobiflux.agent

import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.Service
import android.content.Context
import android.content.Intent
import android.net.ConnectivityManager
import android.net.Network
import android.net.NetworkCapabilities
import android.net.NetworkRequest
import android.os.IBinder
import androidx.annotation.RequiresApi

class CellularProxyService : Service() {
    private lateinit var connectivity: ConnectivityManager
    private val state = AgentState()
    private var callback: ConnectivityManager.NetworkCallback? = null
    private var tunnel: CellularTunnelServer? = null
    private var control: AgentControlServer? = null

    override fun onCreate() {
        super.onCreate(); startForeground(1, notification())
        connectivity = getSystemService(Context.CONNECTIVITY_SERVICE) as ConnectivityManager
        tunnel = CellularTunnelServer(state).also { it.start() }
        control = AgentControlServer(state).also { it.start() }
        callback = object : ConnectivityManager.NetworkCallback() {
            override fun onAvailable(network: Network) { state.setCellular(network) }
            override fun onLost(network: Network) { state.removeCellular(network) }
            override fun onUnavailable() { state.clear() }
        }
        val request = NetworkRequest.Builder().addTransportType(NetworkCapabilities.TRANSPORT_CELLULAR).addCapability(NetworkCapabilities.NET_CAPABILITY_INTERNET).build()
        connectivity.requestNetwork(request, callback!!)
    }
    override fun onDestroy() { callback?.let { connectivity.unregisterNetworkCallback(it) }; tunnel?.close(); control?.close(); super.onDestroy() }
    override fun onBind(intent: Intent?): IBinder? = null
    @RequiresApi(26)
    private fun notification(): android.app.Notification {
        val manager = getSystemService(NotificationManager::class.java); val channel = NotificationChannel("mobiflux", "MobiFlux cellular proxy", NotificationManager.IMPORTANCE_LOW); manager.createNotificationChannel(channel)
        return android.app.Notification.Builder(this, "mobiflux").setContentTitle("MobiFlux Agent").setContentText("Cellular-only proxy node active").setSmallIcon(android.R.drawable.stat_sys_upload).build()
    }
}
