using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BitBing.UnityBridge.Editor
{
    /// <summary>
    /// MCP (Model Context Protocol) listener for Unity Editor.
    /// Listens for incoming MCP requests via HTTP and executes them via command system.
    /// Based on COPLAY unity-mcp architecture.
    /// </summary>
    public class McpListener : IDisposable
    {
        private HttpListener _httpListener;
        private CancellationTokenSource _cts;
        private readonly int _port;
        private bool _isRunning;

        public event Action<string, string> OnLog;
        public event Action<McpMessage, McpResponse> OnMessageProcessed;

        public bool IsRunning => _isRunning;

        public McpListener(int port = 8080)
        {
            _port = port;
        }

        public void Start()
        {
            if (_isRunning) return;

            _cts = new CancellationTokenSource();
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add($"http://localhost:{_port}/");
            _httpListener.Prefixes.Add($"http://127.0.0.1:{_port}/");

            try
            {
                _httpListener.Start();
                _isRunning = true;
                Log($"MCP Listener started on port {_port}");

                Task.Run(() => ListenAsync(_cts.Token));
            }
            catch (Exception ex)
            {
                Log($"Failed to start MCP Listener: {ex.Message}");
                _isRunning = false;
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;

            _cts?.Cancel();
            _httpListener?.Stop();
            _httpListener?.Close();
            _isRunning = false;
            Log("MCP Listener stopped");
        }

        private async Task ListenAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _httpListener.IsListening)
            {
                try
                {
                    var context = await _httpListener.GetContextAsync();
                    _ = Task.Run(() => HandleRequestAsync(context), ct);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log($"Listener error: {ex.Message}");
                }
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            var response = context.Response;
            var request = context.Request;

            try
            {
                string body;
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    body = await reader.ReadToEndAsync();
                }

                Log($"Received MCP request: {body.Substring(0, Mathf.Min(200, body.Length))}...");

                var mcpMessage = ParseMcpMessage(body);
                var mcpResponse = ProcessMessage(mcpMessage);

                var responseJson = JsonConvert.SerializeObject(mcpResponse);
                var buffer = Encoding.UTF8.GetBytes(responseJson);

                response.ContentType = "application/json";
                response.ContentEncoding = Encoding.UTF8;
                response.StatusCode = 200;
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);

                OnMessageProcessed?.Invoke(mcpMessage, mcpResponse);
            }
            catch (Exception ex)
            {
                Log($"Request handling error: {ex.Message}");

                var errorResponse = new McpResponse
                {
                    JsonRpc = "2.0",
                    Error = new McpError
                    {
                        Code = -32603,
                        Message = ex.Message
                    }
                };

                var buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(errorResponse));
                response.StatusCode = 500;
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            finally
            {
                response.Close();
            }
        }

        private McpMessage ParseMcpMessage(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<McpMessage>(json);
            }
            catch
            {
                return new McpMessage
                {
                    JsonRpc = "2.0",
                    Id = "parse_error"
                };
            }
        }

        private McpResponse ProcessMessage(McpMessage message)
        {
            var response = new McpResponse
            {
                JsonRpc = "2.0",
                Id = message.Id
            };

            try
            {
                if (message.Method == "tools/list")
                {
                    response.Result = McpToolRegistry.GetToolList();
                    return response;
                }

                if (message.Method == "tools/call")
                {
                    var result = McpToolRegistry.CallTool(message.Params);
                    response.Result = result;
                    return response;
                }

                if (message.Method == "initialize")
                {
                    response.Result = new Dictionary<string, object>
                    {
                        ["protocolVersion"] = "2024-11-05",
                        ["capabilities"] = new Dictionary<string, object>
                        {
                            ["tools"] = new Dictionary<string, object>()
                        },
                        ["serverInfo"] = new Dictionary<string, object>
                        {
                            ["name"] = "unity-bridge",
                            ["version"] = "0.1.0"
                        }
                    };
                    return response;
                }

                response.Error = new McpError
                {
                    Code = -32601,
                    Message = $"Method not found: {message.Method}"
                };
            }
            catch (Exception ex)
            {
                response.Error = new McpError
                {
                    Code = -32603,
                    Message = ex.Message
                };
            }

            return response;
        }

        private void Log(string message)
        {
            OnLog?.Invoke("McpListener", message);
            Debug.Log($"[McpListener] {message}");
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
        }
    }

    #region MCP Message Classes

    public class McpMessage
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params")]
        public object Params { get; set; }
    }

    public class McpResponse
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("result")]
        public object Result { get; set; }

        [JsonProperty("error")]
        public McpError Error { get; set; }
    }

    public class McpError
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    #endregion
}
