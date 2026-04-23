using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using BitBing.UnityBridge.Editor.Settings;

namespace BitBing.UnityBridge.Editor.UI
{
    /// <summary>
    /// Main EditorWindow for the BitBing Platform panel.
    /// Provides UI for monitoring agent status, viewing logs, and sending commands.
    /// Based on EKLENTİR.md §8.
    /// </summary>
    public class AgentPanelWindow : EditorWindow
    {
        private McpListener _mcpListener;
        private BridgeSettings _settings;
        private VisualElement _root;
        private VisualElement _connectionDot;
        private Label _connectionLabel;
        private Button _connectButton;
        private VisualElement _agentCardsContainer;
        private VisualElement _logContainer;
        private ScrollView _logScrollView;
        private Dictionary<string, AgentStatusCard> _agentCards;

        private static readonly Color ColorVates = new Color(1f, 59f / 255f, 59f / 255f);
        private static readonly Color ColorDiafor = new Color(1f, 214f / 255f, 0);
        private static readonly Color ColorAhbab = new Color(0, 230f / 255f, 118f / 255f);
        private static readonly Color ColorObsidere = new Color(41f / 255f, 121f / 255f, 1f);
        private static readonly Color ColorPatientia = new Color(170f / 255f, 0, 1f);
        private static readonly Color ColorMagnumpus = new Color(1f, 109f / 255f, 0);

        [MenuItem("Window/BitBing/Agent Panel %#g")]
        public static void ShowWindow()
        {
            var window = GetWindow<AgentPanelWindow>("BitBing");
            window.minSize = new Vector2(300, 400);
            window.Show();
        }

        public void OnEnable()
        {
            _settings = BridgeSettings.GetOrCreate();
            _agentCards = new Dictionary<string, AgentStatusCard>();

            CreateUI();
            InitializeMcpListener();
        }

        public void OnDisable()
        {
            _mcpListener?.Stop();
            _mcpListener?.Dispose();
        }

        private void CreateUI()
        {
            const string uxmlPath = "Packages/com.bitbing.unity-bridge/Editor/UI/AgentPanelWindow.uxml";
            const string ussPath = "Packages/com.bitbing.unity-bridge/Editor/UI/AgentPanelWindow.uss";

            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);

            if (template == null)
            {
                CreateDefaultUI();
                return;
            }

            _root = template.CloneTree();
            rootVisualElement.Add(_root);
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            if (styleSheet != null) rootVisualElement.styleSheets.Add(styleSheet);

            SetupReferences();
            SetupAgentCards();
            SetupButtons();
        }

        private void CreateDefaultUI()
        {
            _root = new VisualElement();
            _root.style.flexGrow = 1;
            _root.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
            _root.style.paddingLeft = 8;
            _root.style.paddingRight = 8;
            _root.style.paddingTop = 8;
            _root.style.paddingBottom = 8;

            rootVisualElement.Add(_root);

            CreateConnectionSection();
            CreateAgentSection();
            CreateLogSection();
            CreateCommandsSection();
        }

        private void SetupReferences()
        {
            _connectionDot = _root.Query<VisualElement>("connectionDot");
            _connectionLabel = _root.Query<Label>("connectionLabel");
            _connectButton = _root.Query<Button>("connectButton");
            _agentCardsContainer = _root.Query<VisualElement>("agentCardsContainer");
            _logContainer = _root.Query<VisualElement>("logContainer");
            _logScrollView = _root.Query<ScrollView>("logScrollView");

            if (_connectButton != null)
            {
                _connectButton.clicked += OnConnectButtonClicked;
            }
        }

        private void SetupAgentCards()
        {
            CreateAgentCard("vates", ColorVates, "Kullanıcı inputunu analiz eder");
            CreateAgentCard("Diafor", ColorDiafor, "Bağımlılıkları kontrol eder");
            CreateAgentCard("ahbab", ColorAhbab, "Unity'ye komut gönderir");
            CreateAgentCard("obsidere", ColorObsidere, "Çıktıları denetler");
            CreateAgentCard("patientia", ColorPatientia, "Testleri yapıyor");
            CreateAgentCard("magnumpus", ColorMagnumpus, "Çıktıları paketliyor");
        }

        private void CreateAgentCard(string agentId, Color color, string description)
        {
            var card = new AgentStatusCard(agentId, color, description);
            _agentCards[agentId] = card;
            _agentCardsContainer?.Add(card);
        }

        private void SetupButtons()
        {
            var playButton = _root.Q<Button>("playButton");
            var stopButton = _root.Q<Button>("stopButton");
            var screenshotButton = _root.Q<Button>("screenshotButton");
            var refreshButton = _root.Q<Button>("refreshButton");
            var clearConsoleButton = _root.Q<Button>("clearConsoleButton");
            var clearLogsButton = _root.Q<Button>("clearLogsButton");

            if (playButton != null) playButton.clicked += () => ExecuteCommand("enter_play_mode");
            if (stopButton != null) stopButton.clicked += () => ExecuteCommand("exit_play_mode");
            if (screenshotButton != null) screenshotButton.clicked += OnTakeScreenshot;
            if (refreshButton != null) refreshButton.clicked += OnRefreshAssets;
            if (clearConsoleButton != null) clearConsoleButton.clicked += OnClearConsole;
            if (clearLogsButton != null) clearLogsButton.clicked += OnClearLogs;
        }

        private void CreateConnectionSection()
        {
            var section = new VisualElement();
            section.style.marginBottom = 8;
            section.style.paddingLeft = 8;
            section.style.paddingRight = 8;
            section.style.paddingTop = 4;
            section.style.paddingBottom = 4;
            section.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            section.style.borderTopLeftRadius = 4;
            section.style.borderTopRightRadius = 4;
            section.style.borderBottomLeftRadius = 4;
            section.style.borderBottomRightRadius = 4;

            var title = new Label("BAĞLANTI");
            title.style.fontSize = 10;
            title.style.color = new Color(0.5f, 0.5f, 0.5f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            section.Add(title);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            _connectionDot = new VisualElement();
            _connectionDot.style.width = 8;
            _connectionDot.style.height = 8;
            _connectionDot.style.borderTopLeftRadius = 4;
            _connectionDot.style.borderTopRightRadius = 4;
            _connectionDot.style.borderBottomLeftRadius = 4;
            _connectionDot.style.borderBottomRightRadius = 4;
            _connectionDot.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
            row.Add(_connectionDot);

            _connectionLabel = new Label("Bağlı değil");
            _connectionLabel.style.flexGrow = 1;
            _connectionLabel.style.marginLeft = 8;
            _connectionLabel.style.fontSize = 11;
            _connectionLabel.style.color = Color.white;
            row.Add(_connectionLabel);

            _connectButton = new Button(OnConnectButtonClicked);
            _connectButton.text = "Bağlan";
            _connectButton.style.fontSize = 10;
            row.Add(_connectButton);

            section.Add(row);
            _root.Add(section);
        }

        private void CreateAgentSection()
        {
            var section = new VisualElement();
            section.style.marginBottom = 8;

            var title = new Label("AGENT DURUMLARI");
            title.style.fontSize = 10;
            title.style.color = new Color(0.5f, 0.5f, 0.5f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            section.Add(title);

            _agentCardsContainer = new VisualElement();
            _agentCardsContainer.style.flexDirection = FlexDirection.Column;
            section.Add(_agentCardsContainer);

            _root.Add(section);
        }

        private void CreateLogSection()
        {
            var section = new VisualElement();
            section.style.marginBottom = 8;

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;

            var title = new Label("LOG AKIŞI");
            title.style.fontSize = 10;
            title.style.color = new Color(0.5f, 0.5f, 0.5f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(title);

            var clearButton = new Button(OnClearLogs);
            clearButton.text = "Temizle";
            clearButton.style.fontSize = 10;
            clearButton.style.marginLeft = 4;
            header.Add(clearButton);

            section.Add(header);

            _logScrollView = new ScrollView();
            _logScrollView.style.flexGrow = 1;
            _logScrollView.style.maxHeight = 200;
            _logScrollView.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
            _logScrollView.style.borderTopLeftRadius = 3;
            _logScrollView.style.borderTopRightRadius = 3;
            _logScrollView.style.borderBottomLeftRadius = 3;
            _logScrollView.style.borderBottomRightRadius = 3;

            _logContainer = new VisualElement();
            _logScrollView.Add(_logContainer);
            section.Add(_logScrollView);

            _root.Add(section);
        }

        private void CreateCommandsSection()
        {
            var section = new VisualElement();
            section.style.marginBottom = 8;

            var title = new Label("HIZLI KOMUTLAR");
            title.style.fontSize = 10;
            title.style.color = new Color(0.5f, 0.5f, 0.5f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            section.Add(title);

            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.flexWrap = Wrap.Wrap;

            AddCommandButton(buttons, "▶ Play Mode", OnEnterPlayMode);
            AddCommandButton(buttons, "⏹ Stop", OnExitPlayMode);
            AddCommandButton(buttons, "📸 Ekran Al", OnTakeScreenshot);
            AddCommandButton(buttons, "🔄 Refresh", OnRefreshAssets);
            AddCommandButton(buttons, "🧹 Clear Console", OnClearConsole);

            section.Add(buttons);
            _root.Add(section);
        }

        private void AddCommandButton(VisualElement container, string text, Action callback)
        {
            var button = new Button(callback);
            button.text = text;
            button.style.fontSize = 10;
            button.style.marginRight = 4;
            button.style.marginBottom = 4;
            container.Add(button);
        }

        private void InitializeMcpListener()
        {
            _mcpListener = new McpListener(_settings.mcpPort);
            _mcpListener.OnLog += OnLogReceived;
            _mcpListener.OnMessageProcessed += OnMessageProcessed;

            if (_settings.autoConnect)
            {
                _mcpListener.Start();
                UpdateConnectionUI(true);
            }
        }

        private void OnConnectButtonClicked()
        {
            if (_mcpListener.IsRunning)
            {
                _mcpListener.Stop();
                UpdateConnectionUI(false);
            }
            else
            {
                _mcpListener.Start();
                UpdateConnectionUI(true);
            }
        }

        private void UpdateConnectionUI(bool connected)
        {
            if (_connectionDot != null)
            {
                _connectionDot.style.backgroundColor = connected ? ColorAhbab : new Color(0.5f, 0.5f, 0.5f);
            }

            if (_connectionLabel != null)
            {
                _connectionLabel.text = connected ? $"Bağlı — localhost:{_settings.mcpPort}" : "Bağlı değil";
            }

            if (_connectButton != null)
            {
                _connectButton.text = connected ? "Kes" : "Bağlan";
            }
        }

        private void OnLogReceived(string source, string message)
        {
            if (!_settings.logVerbose && message.Contains("Heartbeat")) return;

            var logLine = new VisualElement();
            logLine.style.flexDirection = FlexDirection.Row;
            logLine.style.paddingTop = 2;
            logLine.style.paddingBottom = 2;
            logLine.style.paddingLeft = 4;
            logLine.style.paddingRight = 4;

            var timestamp = new Label(DateTime.Now.ToString("HH:mm:ss"));
            timestamp.style.fontSize = 9;
            timestamp.style.color = new Color(0.4f, 0.4f, 0.4f);
            timestamp.style.marginRight = 4;
            logLine.Add(timestamp);

            var msg = new Label($"[{source}] {message}");
            msg.style.fontSize = 10;
            msg.style.color = Color.white;
            msg.style.whiteSpace = WhiteSpace.Normal;
            logLine.Add(msg);

            _logContainer?.Add(logLine);

            while (_logContainer?.childCount > 100)
            {
                _logContainer.RemoveAt(0);
            }

            _logScrollView?.ScrollTo(logLine);
        }

        private void OnMessageProcessed(McpMessage message, McpResponse response)
        {
            var logEntry = $"MCP: {message.Method} → {(response.Error != null ? "ERROR" : "OK")}";
            OnLogReceived("MCP", logEntry);
        }

        private void ExecuteCommand(string command)
        {
            OnLogReceived("Command", $"Executed: {command}");

            switch (command)
            {
                case "enter_play_mode":
                    OnEnterPlayMode();
                    break;
                case "exit_play_mode":
                    OnExitPlayMode();
                    break;
            }
        }

        private void OnEnterPlayMode()
        {
            var cmd = new Commands.EnterPlayModeCommand();
            var result = cmd.Execute();
            OnLogReceived("PlayMode", result.Success ? "Play Mode başlatıldı" : $"Hata: {result.Error}");
        }

        private void OnExitPlayMode()
        {
            var cmd = new Commands.ExitPlayModeCommand();
            var result = cmd.Execute();
            OnLogReceived("PlayMode", result.Success ? "Play Mode durduruldu" : $"Hata: {result.Error}");
        }

        private void OnTakeScreenshot()
        {
            var path = $"{_settings.screenshotDir}/screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var cmd = new Commands.TakeScreenshotCommand(path);
            var result = cmd.Execute();
            OnLogReceived("Screenshot", result.Success ? $"Kaydedildi: {path}" : $"Hata: {result.Error}");
        }

        private void OnRefreshAssets()
        {
            AssetDatabase.Refresh();
            OnLogReceived("Assets", "Asset database yenilendi");
        }

        private void OnClearConsole()
        {
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(SceneView));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method?.Invoke(null, null);
            OnLogReceived("Console", "Console temizlendi");
        }

        private void OnClearLogs()
        {
            _logContainer?.Clear();
            OnLogReceived("Panel", "Loglar temizlendi");
        }
    }
}
