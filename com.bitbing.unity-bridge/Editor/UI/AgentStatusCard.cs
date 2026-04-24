using UnityEngine;
using UnityEngine.UIElements;

namespace BitBing.UnityBridge.Editor.UI
{
    /// <summary>
    /// Compact agent status row: colored dot + name + live status/message.
    /// </summary>
    public class AgentStatusCard : VisualElement
    {
        private readonly Color _color;
        private readonly VisualElement _dot;
        private readonly Label _statusLabel;
        private readonly Label _messageLabel;

        public AgentStatusCard(string agentId, Color color, string description)
        {
            _color = color;

            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.paddingTop = 3;
            style.paddingBottom = 3;
            style.paddingLeft = 4;
            style.paddingRight = 4;

            _dot = new VisualElement();
            _dot.style.width = 7;
            _dot.style.height = 7;
            _dot.style.borderTopLeftRadius = 4;
            _dot.style.borderTopRightRadius = 4;
            _dot.style.borderBottomLeftRadius = 4;
            _dot.style.borderBottomRightRadius = 4;
            _dot.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
            _dot.style.marginRight = 5;
            _dot.style.flexShrink = 0;
            Add(_dot);

            var nameLabel = new Label(agentId);
            nameLabel.style.fontSize = 10;
            nameLabel.style.color = color;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.minWidth = 60;
            nameLabel.style.flexShrink = 0;
            Add(nameLabel);

            _statusLabel = new Label("idle");
            _statusLabel.style.fontSize = 9;
            _statusLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            _statusLabel.style.flexShrink = 0;
            _statusLabel.style.marginRight = 4;
            Add(_statusLabel);

            _messageLabel = new Label(description);
            _messageLabel.style.fontSize = 9;
            _messageLabel.style.color = new Color(0.45f, 0.45f, 0.45f);
            _messageLabel.style.flexGrow = 1;
            _messageLabel.style.overflow = Overflow.Hidden;
            _messageLabel.style.textOverflow = TextOverflow.Ellipsis;
            _messageLabel.style.whiteSpace = WhiteSpace.NoWrap;
            Add(_messageLabel);
        }

        public void SetRunning(string message = "Çalışıyor...")
        {
            _dot.style.backgroundColor = _color;
            _statusLabel.text = "▶";
            _statusLabel.style.color = _color;
            _messageLabel.text = message;
        }

        public void SetSuccess(string message = "Tamamlandı")
        {
            _dot.style.backgroundColor = new Color(0.25f, 0.73f, 0.31f);
            _statusLabel.text = "✓";
            _statusLabel.style.color = new Color(0.25f, 0.73f, 0.31f);
            _messageLabel.text = message;
        }

        public void SetError(string message = "Hata")
        {
            _dot.style.backgroundColor = new Color(0.97f, 0.32f, 0.29f);
            _statusLabel.text = "✗";
            _statusLabel.style.color = new Color(0.97f, 0.32f, 0.29f);
            _messageLabel.text = message;
        }

        public void SetIdle(string description = "")
        {
            _dot.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
            _statusLabel.text = "idle";
            _statusLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            if (!string.IsNullOrEmpty(description))
                _messageLabel.text = description;
        }

        // Legacy compat
        public void SetWaiting() => SetIdle();
    }
}
