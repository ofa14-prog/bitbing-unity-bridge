using UnityEngine;
using UnityEngine.UIElements;

namespace BitBing.UnityBridge.Editor.UI
{
    /// <summary>
    /// Pill-shaped agent indicator: colored dot + name + small status badge.
    /// Stays compact so 6 cards fit in a single row.
    /// </summary>
    public class AgentStatusCard : VisualElement
    {
        private readonly Color _color;
        private readonly Color _idleColor = new Color(0.36f, 0.38f, 0.45f);
        private readonly VisualElement _dot;
        private readonly Label _nameLabel;
        private readonly Label _badge;

        public AgentStatusCard(string agentId, Color color, string description)
        {
            _color = color;

            AddToClassList("agent-card-inner");
            tooltip = description;

            // Colored dot
            _dot = new VisualElement();
            _dot.style.width = 8;
            _dot.style.height = 8;
            _dot.style.borderTopLeftRadius = 4;
            _dot.style.borderTopRightRadius = 4;
            _dot.style.borderBottomLeftRadius = 4;
            _dot.style.borderBottomRightRadius = 4;
            _dot.style.backgroundColor = _idleColor;
            _dot.style.marginRight = 6;
            _dot.style.flexShrink = 0;
            Add(_dot);

            // Agent name
            _nameLabel = new Label(agentId);
            _nameLabel.style.fontSize = 10;
            _nameLabel.style.color = new Color(0.91f, 0.92f, 0.94f);
            _nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _nameLabel.style.flexShrink = 0;
            Add(_nameLabel);

            // Tiny status badge (●/✓/✗)
            _badge = new Label("");
            _badge.style.fontSize = 9;
            _badge.style.color = new Color(0.55f, 0.58f, 0.65f);
            _badge.style.marginLeft = 6;
            _badge.style.flexShrink = 0;
            Add(_badge);
        }

        public void SetRunning(string message = "Çalışıyor…")
        {
            _dot.style.backgroundColor = _color;
            _nameLabel.style.color = _color;
            _badge.text = "●";
            _badge.style.color = _color;
            tooltip = message;
        }

        public void SetSuccess(string message = "Tamamlandı")
        {
            var ok = new Color(0.32f, 0.78f, 0.51f);
            _dot.style.backgroundColor = ok;
            _nameLabel.style.color = new Color(0.91f, 0.92f, 0.94f);
            _badge.text = "✓";
            _badge.style.color = ok;
            tooltip = message;
        }

        public void SetError(string message = "Hata")
        {
            var err = new Color(0.96f, 0.39f, 0.36f);
            _dot.style.backgroundColor = err;
            _nameLabel.style.color = err;
            _badge.text = "✗";
            _badge.style.color = err;
            tooltip = message;
        }

        public void SetIdle(string description = "")
        {
            _dot.style.backgroundColor = _idleColor;
            _nameLabel.style.color = new Color(0.65f, 0.68f, 0.74f);
            _badge.text = "";
            tooltip = string.IsNullOrEmpty(description) ? _nameLabel.text : description;
        }

        public void SetWaiting() => SetIdle();
    }
}
