# MobiFlux Android Agent

The agent exposes only Android loopback ports: `18080` is the cellular stream tunnel and `18081` provides a minimal status endpoint. Windows must reach both through per-device USB ADB forwards; the app never listens on Wi-Fi/LAN.

The `MFP1` protocol resolves the requested host through the selected `Network` and creates the target socket from that same network's socket factory. If Android has no cellular network, the agent returns an error and opens no default-network socket. This is the fail-closed route invariant.
