using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using BitBing.UnityBridge.Editor.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Debug = UnityEngine.Debug;

namespace BitBing.UnityBridge.Editor.UI
{
    /// <summary>
    /// Main EditorWindow — chat UI that drives the 6-agent pipeline.
    /// Prompt → Python chat server (/api/run NDJSON stream) → agent cards update live.
    /// Based on EKLENTİR.md §8.
    /// </summary>
    public class AgentPanelWindow : EditorWindow
    {
        // ── bridge
        private McpListener _mcpListener;
        private BridgeSettings _settings;

        // ── UI roots
        private VisualElement _root;
        private VisualElement _connectionDot;
        private Label _connectionLabel;
        private Button _connectButton;
        private VisualElement _agentCardsContainer;

        // ── Python server UI
        private VisualElement _serverDot;
        private Label _serverLabel;
        private Button _serverButton;

        // ── chat UI
        private ScrollView _chatScrollView;
        private VisualElement _chatContainer;
        private TextField _chatInput;
        private Button _sendButton;

        // ── agent cards (key = lowercase agentId)
        private readonly Dictionary<string, AgentStatusCard> _agentCards = new();

        // ── thread-safe main-thread action queue
        private readonly ConcurrentQueue<Action> _mainQueue = new();
        private CancellationTokenSource _pipelineCts;

        // ── Python server process
        private Process _serverProcess;
        private const int ChatServerPort = 8001;

        // ── colors
        private static readonly Color ColorVates     = new(1f,   59/255f, 59/255f);
        private static readonly Color ColorDiafor    = new(1f,  214/255f,   0);
        private static readonly Color ColorAhbab     = new(0,   230/255f, 118/255f);
        private static readonly Color ColorObsidere  = new(41/255f, 121/255f, 1f);
        private static readonly Color ColorPatientia = new(170/255f,  0,  1f);
        private static readonly Color ColorMagnumpus = new(1f,  109/255f,  0);

        private const string PythonChatUrl = "http://127.0.0.1:8001/api/run";

        [MenuItem("Window/BitBing/Agent Panel %#g")]
        public static void ShowWindow()
        {
            var w = GetWindow<AgentPanelWindow>("BitBing");
            w.minSize = new Vector2(320, 420);
            w.Show();
        }

        public void OnEnable()
        {
            _settings = BridgeSettings.GetOrCreate();
            _agentCards.Clear();

            BuildUI();
            InitializeMcpListener();
            EditorApplication.update += DrainMainQueue;

            // Auto-start Python server after first frame to avoid init-time issues
            EditorApplication.delayCall += CheckAndStartServer;
        }

        public void OnDisable()
        {
            EditorApplication.update -= DrainMainQueue;

            _pipelineCts?.Cancel();
            _pipelineCts = null;

            // Release the process handle (don't kill — let server keep running)
            _serverProcess?.Dispose();
            _serverProcess = null;

            if (_mcpListener != null)
            {
                _mcpListener.OnLog -= OnLogReceived;
                _mcpListener.OnMessageProcessed -= OnMessageProcessed;
                _mcpListener.Stop();
                _mcpListener.Dispose();
                _mcpListener = null;
            }
        }

        // ── MAIN-THREAD DRAIN ──────────────────────────────────────────────
        private void DrainMainQueue()
        {
            while (_mainQueue.TryDequeue(out var action))
                action?.Invoke();
        }

        // ── UI BUILD ──────────────────────────────────────────────────────
        private void BuildUI()
        {
            const string uxmlPath = "Packages/com.bitbing.unity-bridge/Editor/UI/AgentPanelWindow.uxml";
            const string ussPath  = "Packages/com.bitbing.unity-bridge/Editor/UI/AgentPanelWindow.uss";

            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (template == null) { BuildFallbackUI(); return; }

            _root = template.CloneTree();
            rootVisualElement.Add(_root);

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            if (styleSheet != null) rootVisualElement.styleSheets.Add(styleSheet);

            BindReferences();
            PopulateAgentCards();
            BindButtons();
        }

        private void BindReferences()
        {
            _serverDot          = _root.Q<VisualElement>("serverDot");
            _serverLabel        = _root.Q<Label>("serverLabel");
            _serverButton       = _root.Q<Button>("serverButton");
            _connectionDot      = _root.Q<VisualElement>("connectionDot");
            _connectionLabel    = _root.Q<Label>("connectionLabel");
            _connectButton      = _root.Q<Button>("connectButton");
            _agentCardsContainer = _root.Q<VisualElement>("agentCardsContainer");

            if (_serverButton != null)
                _serverButton.clicked += OnServerButtonClicked;
            _chatScrollView     = _root.Q<ScrollView>("chatScrollView");
            _chatContainer      = _root.Q<VisualElement>("chatContainer");
            _chatInput          = _root.Q<TextField>("chatInput");
            _sendButton         = _root.Q<Button>("sendButton");

            if (_connectButton != null)
                _connectButton.clicked += OnConnectClicked;

            if (_sendButton != null)
                _sendButton.clicked += OnSend;

            if (_chatInput != null)
            {
                _chatInput.RegisterCallback<KeyDownEvent>(e =>
                {
                    if (e.keyCode == KeyCode.Return)
                    {
                        OnSend();
                        e.StopPropagation();
                    }
                });
            }
        }

        private void PopulateAgentCards()
        {
            AddAgentCard("vates",     ColorVates,     "Girdi analizi → Görev DAG");
            AddAgentCard("diafor",    ColorDiafor,    "Bağımlılık & bağlantı");
            AddAgentCard("ahbab",     ColorAhbab,     "Unity komut uygulayıcı");
            AddAgentCard("obsidere",  ColorObsidere,  "Hata denetimi");
            AddAgentCard("patientia", ColorPatientia, "Build puanlama");
            AddAgentCard("magnumpus", ColorMagnumpus, "Paketleme & teslimat");
        }

        private void AddAgentCard(string agentId, Color color, string description)
        {
            var card = new AgentStatusCard(agentId, color, description);
            // 2-column grid: each card ~50% width
            card.style.width = new StyleLength(Length.Percent(49));
            _agentCards[agentId] = card;
            _agentCardsContainer?.Add(card);
        }

        private void BindButtons()
        {
            var play           = _root.Q<Button>("playButton");
            var stop           = _root.Q<Button>("stopButton");
            var screenshot     = _root.Q<Button>("screenshotButton");
            var refresh        = _root.Q<Button>("refreshButton");
            var clearLogs      = _root.Q<Button>("clearLogsButton");
            var clearConsole   = _root.Q<Button>("clearConsoleButton");

            if (play != null)         play.clicked         += OnEnterPlayMode;
            if (stop != null)         stop.clicked         += OnExitPlayMode;
            if (screenshot != null)   screenshot.clicked   += OnTakeScreenshot;
            if (refresh != null)      refresh.clicked      += OnRefreshAssets;
            if (clearLogs != null)    clearLogs.clicked    += OnClearChat;
            if (clearConsole != null) clearConsole.clicked += OnClearConsole;
        }

        // ── CHAT: SEND ────────────────────────────────────────────────────
        private void OnSend()
        {
            if (_chatInput == null) return;
            var prompt = _chatInput.value?.Trim();
            if (string.IsNullOrEmpty(prompt)) return;

            _chatInput.value = string.Empty;
            AddBubble(prompt, isUser: true);
            ResetAgentCards();
            SetSendEnabled(false);

            _pipelineCts?.Cancel();
            _pipelineCts = new CancellationTokenSource();
            _ = RunPipelineAsync(prompt, _pipelineCts.Token);
        }

        private async Task RunPipelineAsync(string prompt, CancellationToken ct)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
                var payload = JsonConvert.SerializeObject(new { prompt, gameType = "2D Platformer" });
                var content = new StringContent(payload, Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                try
                {
                    response = await client.PostAsync(PythonChatUrl, content, ct);
                }
                catch (Exception ex)
                {
                    Enqueue(() => AddBubble($"❌ Python sunucusuna bağlanılamadı: {ex.Message}\n\nÖnce 'unity-mcp-chat' komutunu çalıştır.", isUser: false));
                    Enqueue(() => SetSendEnabled(true));
                    return;
                }

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream, Encoding.UTF8);

                while (!reader.EndOfStream && !ct.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line))
                        HandleStreamLine(line);
                }
            }
            catch (OperationCanceledException) { /* window closed or new send */ }
            catch (Exception ex)
            {
                Enqueue(() => AddBubble($"❌ Hata: {ex.Message}", isUser: false));
            }
            finally
            {
                Enqueue(() => SetSendEnabled(true));
            }
        }

        private void HandleStreamLine(string json)
        {
            JObject obj;
            try { obj = JObject.Parse(json); }
            catch { return; }

            var type = obj["type"]?.ToString();
            switch (type)
            {
                case "agent_event":
                    var id      = obj["agentId"]?.ToString() ?? "";
                    var status  = obj["status"]?.ToString() ?? "";
                    var message = obj["message"]?.ToString() ?? "";
                    Enqueue(() => UpdateAgentCard(id, status, message));
                    break;

                case "pipeline_complete":
                    var score   = (int)(obj["score"] ?? 0);
                    var grade   = obj["grade"]?.ToString() ?? "?";
                    var summary = obj["summary"]?.ToString() ?? "";
                    Enqueue(() => AddBubble($"✅ Puan: {score}/100 ({grade})\n\n{summary}", isUser: false));
                    break;

                case "pipeline_failed":
                    var reason = obj["reason"]?.ToString() ?? "Bilinmeyen hata";
                    Enqueue(() => AddBubble($"❌ Pipeline başarısız: {reason}", isUser: false));
                    break;
            }
        }

        // ── AGENT CARD UPDATES ────────────────────────────────────────────
        private void UpdateAgentCard(string agentId, string status, string message)
        {
            if (!_agentCards.TryGetValue(agentId, out var card))
            {
                // Case-insensitive fallback (e.g. "Diafor" vs "diafor")
                foreach (var kvp in _agentCards)
                {
                    if (string.Equals(kvp.Key, agentId, StringComparison.OrdinalIgnoreCase))
                    { card = kvp.Value; break; }
                }
            }
            if (card == null) return;

            switch (status)
            {
                case "running": card.SetRunning(message); break;
                case "done":    card.SetSuccess(message); break;
                case "error":   card.SetError(message);   break;
                default:        card.SetIdle(message);    break;
            }
        }

        private void ResetAgentCards()
        {
            foreach (var card in _agentCards.Values)
                card.SetIdle();
        }

        // ── CHAT BUBBLE ───────────────────────────────────────────────────
        private void AddBubble(string text, bool isUser)
        {
            if (_chatContainer == null) return;

            var bubble = new Label(text);
            bubble.style.whiteSpace = WhiteSpace.Normal;
            bubble.style.fontSize = 11;
            bubble.style.paddingTop = 5;
            bubble.style.paddingBottom = 5;
            bubble.style.paddingLeft = 9;
            bubble.style.paddingRight = 9;
            bubble.style.marginBottom = 6;
            bubble.style.borderTopLeftRadius     = 8;
            bubble.style.borderTopRightRadius    = 8;
            bubble.style.borderBottomLeftRadius  = isUser ? 8 : 2;
            bubble.style.borderBottomRightRadius = isUser ? 2 : 8;

            if (isUser)
            {
                bubble.style.alignSelf = Align.FlexEnd;
                bubble.style.backgroundColor = new Color(0.12f, 0.44f, 0.92f);
                bubble.style.color = Color.white;
                bubble.style.maxWidth = new StyleLength(Length.Percent(85));
            }
            else
            {
                bubble.style.alignSelf = Align.FlexStart;
                bubble.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
                bubble.style.color = new Color(0.86f, 0.86f, 0.86f);
                bubble.style.borderTopWidth    = 1;
                bubble.style.borderBottomWidth = 1;
                bubble.style.borderLeftWidth   = 1;
                bubble.style.borderRightWidth  = 1;
                bubble.style.borderTopColor    = new Color(1f, 1f, 1f, 0.08f);
                bubble.style.borderBottomColor = new Color(1f, 1f, 1f, 0.08f);
                bubble.style.borderLeftColor   = new Color(1f, 1f, 1f, 0.08f);
                bubble.style.borderRightColor  = new Color(1f, 1f, 1f, 0.08f);
                bubble.style.maxWidth = new StyleLength(Length.Percent(92));
            }

            _chatContainer.Add(bubble);

            if (_chatScrollView?.panel != null)
            {
                try { _chatScrollView.ScrollTo(bubble); }
                catch (NullReferenceException) { }
            }
        }

        private void SetSendEnabled(bool enabled)
        {
            if (_sendButton != null) _sendButton.SetEnabled(enabled);
        }

        // ── MAIN-QUEUE HELPER ────────────────────────────────────────────
        private void Enqueue(Action action) => _mainQueue.Enqueue(action);

        // ── PYTHON SERVER MANAGEMENT ─────────────────────────────────────

        private void CheckAndStartServer()
        {
            if (IsServerRunning())
            {
                SetServerUI("running", "Çalışıyor — localhost:" + ChatServerPort, "Durdur");
                return;
            }
            StartPythonServer();
        }

        private void OnServerButtonClicked()
        {
            if (_serverButton == null) return;
            if (_serverButton.text == "Durdur")
                StopPythonServer();
            else
                StartPythonServer();
        }

        private void StartPythonServer()
        {
            SetServerUI("starting", "Başlatılıyor…", "—");

            var (exe, args, workDir) = FindServerCommand();

            if (exe == null)
            {
                var hint = FindPythonServerDir() is { } dir
                    ? $"Kur: cd \"{dir}\" && pip install -e ."
                    : "unity-mcp-chat bulunamadı — pip install -e . çalıştır";
                SetServerUI("error", hint, "Tekrar Dene");
                return;
            }

            try
            {
                var info = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    WorkingDirectory = workDir ?? "",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                _serverProcess?.Dispose();
                _serverProcess = new Process { StartInfo = info, EnableRaisingEvents = true };

                _serverProcess.Exited += (_, __) =>
                {
                    Enqueue(() => SetServerUI("error", "Sunucu durdu", "Başlat"));
                    _serverProcess?.Dispose();
                    _serverProcess = null;
                };

                _serverProcess.Start();

                // Check after 2.5 s whether it actually came up
                Task.Delay(2500).ContinueWith(_ =>
                {
                    bool up = IsServerRunning();
                    Enqueue(() => SetServerUI(
                        up ? "running" : "error",
                        up ? "Çalışıyor — localhost:" + ChatServerPort : "Başlatılamadı",
                        up ? "Durdur" : "Tekrar Dene"));
                });
            }
            catch (Exception ex)
            {
                SetServerUI("error", $"Hata: {ex.Message}", "Tekrar Dene");
            }
        }

        private void StopPythonServer()
        {
            try
            {
                if (_serverProcess != null && !_serverProcess.HasExited)
                {
                    _serverProcess.Kill();
                    _serverProcess.Dispose();
                    _serverProcess = null;
                }
            }
            catch { /* already gone */ }
            SetServerUI("", "Durduruldu", "Başlat");
        }

        private void SetServerUI(string dotState, string labelText, string buttonText)
        {
            if (_serverDot != null)
            {
                _serverDot.RemoveFromClassList("running");
                _serverDot.RemoveFromClassList("starting");
                _serverDot.RemoveFromClassList("error");
                if (!string.IsNullOrEmpty(dotState))
                    _serverDot.AddToClassList(dotState);

                // Fallback: also set inline color for non-USS layouts
                _serverDot.style.backgroundColor = dotState switch
                {
                    "running"  => new Color(0.25f, 0.73f, 0.31f),
                    "starting" => new Color(0.82f, 0.60f, 0.13f),
                    "error"    => new Color(0.97f, 0.32f, 0.29f),
                    _          => new Color(0.4f, 0.4f, 0.4f),
                };
            }
            if (_serverLabel != null) _serverLabel.text = labelText;
            if (_serverButton != null) _serverButton.text = buttonText;
        }

        private bool IsServerRunning()
        {
            try
            {
                using var tcp = new TcpClient();
                var ar = tcp.BeginConnect("127.0.0.1", ChatServerPort, null, null);
                bool ok = ar.AsyncWaitHandle.WaitOne(400);
                if (ok) tcp.EndConnect(ar);
                return ok;
            }
            catch { return false; }
        }

        /// <summary>Returns (exe, args, workingDir) or (null,_,_) if not found.</summary>
        private (string exe, string args, string workDir) FindServerCommand()
        {
            // 1) unity-mcp-chat installed by pip (preferred)
            if (ExistsInPath("unity-mcp-chat"))
                return ("unity-mcp-chat", "", null);

            // 2) python -m uvicorn from detected server directory
            var serverDir = FindPythonServerDir();
            if (serverDir != null)
            {
                var python = ExistsInPath("python3") ? "python3"
                           : ExistsInPath("python")  ? "python"
                           : null;
                if (python != null)
                    return (python,
                            "-m uvicorn unity_mcp_server.chat_server:app --host 127.0.0.1 --port 8001",
                            Path.Combine(serverDir, "src"));
            }

            return (null, null, null);
        }

        private static string FindPythonServerDir()
        {
            // Walk up from Assets/ looking for the sibling unity-mcp-server directory
            var dir = new DirectoryInfo(Application.dataPath);
            for (int i = 0; i < 7; i++)
            {
                dir = dir.Parent;
                if (dir == null) break;
                var candidate = Path.Combine(dir.FullName, "unity-mcp-server");
                if (Directory.Exists(candidate) &&
                    File.Exists(Path.Combine(candidate, "pyproject.toml")))
                    return candidate;
            }
            return null;
        }

        private static bool ExistsInPath(string command)
        {
            var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
            var exts = new[] { "", ".exe", ".cmd", ".bat" };
            foreach (var dir in pathEnv.Split(Path.PathSeparator))
            {
                foreach (var ext in exts)
                {
                    if (File.Exists(Path.Combine(dir, command + ext)))
                        return true;
                }
            }
            return false;
        }

        // ── MCP LISTENER ─────────────────────────────────────────────────
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

        private void OnConnectClicked()
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
                _connectionDot.style.backgroundColor = connected ? ColorAhbab : new Color(0.4f, 0.4f, 0.4f);
                if (connected)
                    _connectionDot.AddToClassList("connected");
                else
                    _connectionDot.RemoveFromClassList("connected");
            }
            if (_connectionLabel != null)
                _connectionLabel.text = connected ? $"Bağlı — localhost:{_settings.mcpPort}" : "Bağlı değil";
            if (_connectButton != null)
                _connectButton.text = connected ? "Kes" : "Bağlan";
        }

        private void OnLogReceived(string source, string message)
        {
            if (rootVisualElement == null || rootVisualElement.panel == null) return;
            // Logs go to Unity console only — no separate log panel in new layout
        }

        private void OnMessageProcessed(McpMessage message, McpResponse response)
        {
            var entry = $"MCP: {message.Method} → {(response.Error != null ? "HATA" : "OK")}";
            Debug.Log($"[BitBing] {entry}");
        }

        // ── QUICK COMMANDS ────────────────────────────────────────────────
        private void OnEnterPlayMode()
        {
            var result = new Commands.EnterPlayModeCommand().Execute();
            Debug.Log($"[BitBing] Play Mode: {(result.Success ? "başlatıldı" : result.Error)}");
        }

        private void OnExitPlayMode()
        {
            var result = new Commands.ExitPlayModeCommand().Execute();
            Debug.Log($"[BitBing] Stop: {(result.Success ? "durduruldu" : result.Error)}");
        }

        private void OnTakeScreenshot()
        {
            var path = $"{_settings.screenshotDir}/screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var result = new Commands.TakeScreenshotCommand(path).Execute();
            Debug.Log($"[BitBing] Screenshot: {(result.Success ? path : result.Error)}");
        }

        private void OnRefreshAssets()
        {
            AssetDatabase.Refresh();
            Debug.Log("[BitBing] Asset database yenilendi");
        }

        private void OnClearConsole()
        {
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(SceneView));
            var type = assembly?.GetType("UnityEditor.LogEntries");
            type?.GetMethod("Clear")?.Invoke(null, null);
        }

        private void OnClearChat()
        {
            _chatContainer?.Clear();
        }

        // ── FALLBACK UI (no UXML) ─────────────────────────────────────────
        private void BuildFallbackUI()
        {
            _root = new VisualElement();
            _root.style.flexGrow = 1;
            _root.style.flexDirection = FlexDirection.Column;
            _root.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
            _root.style.paddingLeft = 6;
            _root.style.paddingRight = 6;
            _root.style.paddingTop = 6;
            _root.style.paddingBottom = 6;
            rootVisualElement.Add(_root);

            // Server row
            var serverRow = new VisualElement();
            serverRow.style.flexDirection = FlexDirection.Row;
            serverRow.style.alignItems = Align.Center;
            serverRow.style.marginBottom = 4;

            _serverDot = new VisualElement();
            _serverDot.style.width = 7; _serverDot.style.height = 7;
            _serverDot.style.borderTopLeftRadius = 4; _serverDot.style.borderTopRightRadius = 4;
            _serverDot.style.borderBottomLeftRadius = 4; _serverDot.style.borderBottomRightRadius = 4;
            _serverDot.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
            _serverDot.style.marginRight = 6;
            serverRow.Add(_serverDot);

            _serverLabel = new Label("Python sunucu kontrol ediliyor…");
            _serverLabel.style.flexGrow = 1;
            _serverLabel.style.fontSize = 10;
            _serverLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
            serverRow.Add(_serverLabel);

            _serverButton = new Button(OnServerButtonClicked) { text = "Başlat" };
            _serverButton.style.fontSize = 10;
            serverRow.Add(_serverButton);
            _root.Add(serverRow);

            // Connection row
            var connRow = new VisualElement();
            connRow.style.flexDirection = FlexDirection.Row;
            connRow.style.alignItems = Align.Center;
            connRow.style.marginBottom = 6;

            _connectionDot = new VisualElement();
            _connectionDot.style.width = 8;
            _connectionDot.style.height = 8;
            _connectionDot.style.borderTopLeftRadius = 4;
            _connectionDot.style.borderTopRightRadius = 4;
            _connectionDot.style.borderBottomLeftRadius = 4;
            _connectionDot.style.borderBottomRightRadius = 4;
            _connectionDot.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
            _connectionDot.style.marginRight = 6;
            connRow.Add(_connectionDot);

            _connectionLabel = new Label("Bağlı değil");
            _connectionLabel.style.flexGrow = 1;
            _connectionLabel.style.fontSize = 11;
            _connectionLabel.style.color = Color.white;
            connRow.Add(_connectionLabel);

            _connectButton = new Button(OnConnectClicked) { text = "Bağlan" };
            _connectButton.style.fontSize = 10;
            connRow.Add(_connectButton);
            _root.Add(connRow);

            // Agent cards container
            _agentCardsContainer = new VisualElement();
            _agentCardsContainer.style.flexDirection = FlexDirection.Row;
            _agentCardsContainer.style.flexWrap = Wrap.Wrap;
            _agentCardsContainer.style.marginBottom = 6;
            _root.Add(_agentCardsContainer);
            PopulateAgentCards();

            // Chat title
            var chatTitle = new Label("CHAT");
            chatTitle.style.fontSize = 10;
            chatTitle.style.color = new Color(0.5f, 0.5f, 0.5f);
            chatTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            chatTitle.style.marginBottom = 3;
            _root.Add(chatTitle);

            // Chat scroll
            _chatScrollView = new ScrollView();
            _chatScrollView.style.flexGrow = 1;
            _chatScrollView.style.backgroundColor = new Color(0.14f, 0.14f, 0.14f);
            _chatScrollView.style.borderTopLeftRadius = 4;
            _chatScrollView.style.borderTopRightRadius = 4;
            _chatScrollView.style.borderBottomLeftRadius = 4;
            _chatScrollView.style.borderBottomRightRadius = 4;
            _chatScrollView.style.marginBottom = 5;

            _chatContainer = new VisualElement();
            _chatContainer.style.flexDirection = FlexDirection.Column;
            _chatContainer.style.paddingLeft = 5;
            _chatContainer.style.paddingRight = 5;
            _chatContainer.style.paddingTop = 5;
            _chatContainer.style.paddingBottom = 5;
            _chatScrollView.Add(_chatContainer);
            _root.Add(_chatScrollView);

            // Input row
            var inputRow = new VisualElement();
            inputRow.style.flexDirection = FlexDirection.Row;
            inputRow.style.marginBottom = 5;

            _chatInput = new TextField();
            _chatInput.style.flexGrow = 1;
            _chatInput.style.fontSize = 11;
            _chatInput.style.marginRight = 5;
            _chatInput.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Return) { OnSend(); e.StopPropagation(); }
            });
            inputRow.Add(_chatInput);

            _sendButton = new Button(OnSend) { text = "Gönder" };
            _sendButton.style.fontSize = 11;
            inputRow.Add(_sendButton);
            _root.Add(inputRow);

            // Quick commands
            var cmdRow = new VisualElement();
            cmdRow.style.flexDirection = FlexDirection.Row;
            cmdRow.style.flexWrap = Wrap.Wrap;
            AddCmdButton(cmdRow, "▶", OnEnterPlayMode);
            AddCmdButton(cmdRow, "⏹", OnExitPlayMode);
            AddCmdButton(cmdRow, "📸", OnTakeScreenshot);
            AddCmdButton(cmdRow, "🔄", OnRefreshAssets);
            AddCmdButton(cmdRow, "🧹", OnClearChat);
            AddCmdButton(cmdRow, "C", OnClearConsole);
            _root.Add(cmdRow);
        }

        private static void AddCmdButton(VisualElement parent, string text, Action callback)
        {
            var btn = new Button(callback) { text = text };
            btn.style.fontSize = 10;
            btn.style.paddingLeft = 6;
            btn.style.paddingRight = 6;
            btn.style.marginRight = 3;
            btn.style.marginBottom = 2;
            parent.Add(btn);
        }
    }
}
