using System;

namespace BitBing.UnityBridge.Editor.Services
{
    /// <summary>
    /// Minimal singleton service locator, adapted from COPLAY's MCPServiceLocator.
    /// Holds the long-lived BridgeControlService so the Agent Panel and other
    /// Editor windows talk to the same listener instance.
    /// </summary>
    public static class BitBingServiceLocator
    {
        private static BridgeControlService _bridge;
        private static readonly object _lock = new object();

        public static BridgeControlService Bridge
        {
            get
            {
                if (_bridge == null)
                {
                    lock (_lock)
                    {
                        if (_bridge == null) _bridge = new BridgeControlService();
                    }
                }
                return _bridge;
            }
        }

        public static void Reset()
        {
            lock (_lock)
            {
                _bridge?.Dispose();
                _bridge = null;
            }
        }
    }
}
