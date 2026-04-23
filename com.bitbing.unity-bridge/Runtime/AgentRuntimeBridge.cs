using UnityEngine;

namespace BitBing.UnityBridge.Runtime
{
    /// <summary>
    /// Runtime bridge for communication between the running game and the editor.
    /// This component can be attached to GameObjects in the runtime.
    /// </summary>
    public class AgentRuntimeBridge : MonoBehaviour
    {
        [Header("Bridge Configuration")]
        [SerializeField]
        private bool _enabled = true;

        public bool IsEnabled => _enabled;

        public void SendEvent(string eventType, object payload)
        {
            if (!_enabled) return;

            // Runtime'dan editor'e event gönderme implementasyonu
            // Bu şu an için placeholder olarak kalıyor
            Debug.Log($"[RuntimeBridge] Event: {eventType}");
        }
    }
}
