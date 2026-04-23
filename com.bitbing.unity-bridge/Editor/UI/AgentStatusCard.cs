using UnityEngine;
using UnityEngine.UIElements;

namespace BitBing.UnityBridge.Editor.UI
{
    /// <summary>
    /// Visual card showing an agent's status with color indicator.
    /// </summary>
    public class AgentStatusCard : VisualElement
    {
        private readonly string _agentId;
        private readonly Color _color;
        private readonly VisualElement _statusDot;
        private readonly Label _statusLabel;
        private readonly Label _descriptionLabel;

        public string Status
        {
            set
            {
                if (_statusLabel != null)
                {
                    _statusLabel.text = value;
                }
            }
        }

        public AgentStatusCard(string agentId, Color color, string description)
        {
            _agentId = agentId;
            _color = color;

            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.paddingTop = 4;
            style.paddingBottom = 4;
            style.paddingLeft = 4;
            style.paddingRight = 4;

            _statusDot = new VisualElement();
            _statusDot.style.width = 8;
            _statusDot.style.height = 8;
            _statusDot.style.borderTopLeftRadius = 4;
            _statusDot.style.borderTopRightRadius = 4;
            _statusDot.style.borderBottomLeftRadius = 4;
            _statusDot.style.borderBottomRightRadius = 4;
            _statusDot.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
            Add(_statusDot);

            var nameLabel = new Label(agentId);
            nameLabel.style.fontSize = 11;
            nameLabel.style.color = color;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.minWidth = 70;
            Add(nameLabel);

            _statusLabel = new Label("Bekliyor");
            _statusLabel.style.fontSize = 10;
            _statusLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            _statusLabel.style.flexGrow = 1;
            Add(_statusLabel);

            _descriptionLabel = new Label(description);
            _descriptionLabel.style.fontSize = 9;
            _descriptionLabel.style.color = new Color(0.4f, 0.4f, 0.4f);
            _descriptionLabel.style.maxWidth = 120;
            _descriptionLabel.style.whiteSpace = WhiteSpace.Normal;
            _descriptionLabel.style.textOverflow = TextOverflow.Ellipsis;
            Add(_descriptionLabel);
        }

        public void SetStatus(string status, Color? dotColor = null)
        {
            Status = status;

            if (dotColor.HasValue)
            {
                _statusDot.style.backgroundColor = dotColor.Value;
            }
        }

        public void SetRunning()
        {
            _statusDot.style.backgroundColor = _color;
            Status = "Çalışıyor...";
        }

        public void SetWaiting()
        {
            _statusDot.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
            Status = "Bekliyor";
        }

        public void SetSuccess()
        {
            _statusDot.style.backgroundColor = Color.green;
            Status = "Tamamlandı ✓";
        }

        public void SetError()
        {
            _statusDot.style.backgroundColor = Color.red;
            Status = "Hata ✗";
        }
    }
}
