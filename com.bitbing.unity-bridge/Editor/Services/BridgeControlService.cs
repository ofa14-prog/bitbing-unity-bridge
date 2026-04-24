using System;
using BitBing.UnityBridge.Editor.Helpers;

namespace BitBing.UnityBridge.Editor.Services
{
    /// <summary>
    /// Wraps McpListener with high-level state suitable for the Agent Panel.
    /// Adapted from COPLAY's BridgeControlService.
    /// Single owner for the listener — UI consults this instead of constructing its own.
    /// </summary>
    public class BridgeControlService : IDisposable
    {
        private McpListener _listener;
        private int _port;

        public bool IsRunning => _listener?.IsRunning == true;
        public int CurrentPort => _port;

        public event Action<bool, int> StateChanged;

        public bool Start()
        {
            if (IsRunning) return true;

            try
            {
                _port = PortManager.IsPortAvailable(PortManager.DefaultUnityPort)
                    ? PortManager.DefaultUnityPort
                    : PortManager.DiscoverUnityPort();

                _listener = new McpListener(_port);
                _listener.OnLog += (src, msg) => BitBingLog.Info(src, msg);
                _listener.Start();

                PortManager.SaveUnityPort(_port);
                BitBingLog.Info("BridgeControl", $"Bridge started on port {_port}");
                StateChanged?.Invoke(true, _port);
                return true;
            }
            catch (Exception ex)
            {
                BitBingLog.Error("BridgeControl", $"Failed to start: {ex.Message}");
                StateChanged?.Invoke(false, 0);
                return false;
            }
        }

        public void Stop()
        {
            if (_listener == null) return;
            try { _listener.Stop(); }
            catch (Exception ex) { BitBingLog.Warn("BridgeControl", $"Stop error: {ex.Message}"); }
            finally
            {
                _listener.Dispose();
                _listener = null;
                BitBingLog.Info("BridgeControl", "Bridge stopped");
                StateChanged?.Invoke(false, _port);
            }
        }

        public McpListener Listener => _listener;

        public void Dispose() => Stop();
    }
}
