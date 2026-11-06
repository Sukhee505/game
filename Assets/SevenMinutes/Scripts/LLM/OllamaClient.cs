using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace SevenMinutes.LLM
{
    /// <summary>
    /// Talks to a LOCAL Ollama server through its REST API (POST /api/chat, GET /api/tags).
    /// Fully asynchronous (UnityWebRequest + Task), so the frame rate is never blocked.
    /// Never throws: network errors, timeouts, missing models and bad payloads become LLMResult failures.
    /// </summary>
    public sealed class OllamaClient : ILLMClient
    {
        private readonly OllamaSettings settings;

        public OllamaClient(OllamaSettings settings)
        {
            this.settings = settings ?? new OllamaSettings();
        }

        public string ModelName => settings.model;
        private string BaseUrl => settings.baseUrl.TrimEnd('/');

        public async Task<LLMResult> ChatAsync(IReadOnlyList<ChatMessage> messages, string jsonSchema, CancellationToken ct = default)
        {
            float start = Time.realtimeSinceStartup;
            try
            {
                string body = BuildChatBody(messages, jsonSchema);
                using (var req = new UnityWebRequest(BaseUrl + "/api/chat", UnityWebRequest.kHttpVerbPOST))
                {
                    req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.SetRequestHeader("Content-Type", "application/json");
                    req.timeout = settings.timeoutSeconds;

                    bool completed = await SendAsync(req, ct);
                    float latency = Time.realtimeSinceStartup - start;
                    if (!completed || ct.IsCancellationRequested)
                        return LLMResult.Fail(LLMErrorType.Cancelled, "İstek iptal edildi.", latency);

                    var error = Classify(req, latency);
                    if (error != null) return error;

                    string text = req.downloadHandler.text;
                    OllamaChatResponse parsed = null;
                    try { parsed = JsonUtility.FromJson<OllamaChatResponse>(text); }
                    catch (Exception e) { Debug.LogWarning($"[Ollama] Could not parse envelope: {e.Message}"); }

                    if (parsed == null)
                        return LLMResult.Fail(LLMErrorType.InvalidResponse, "Sunucu okunamayan bir yanıt gönderdi.", latency);
                    if (!string.IsNullOrEmpty(parsed.error))
                        return LLMResult.Fail(LLMErrorType.HttpError, parsed.error, latency);
                    if (parsed.message == null || string.IsNullOrWhiteSpace(parsed.message.content))
                        return LLMResult.Fail(LLMErrorType.EmptyResponse, "Model boş bir yanıt döndürdü.", latency);

                    return LLMResult.Ok(parsed.message.content, latency);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Ollama] Unexpected error: {e}");
                return LLMResult.Fail(LLMErrorType.Unknown, e.Message, Time.realtimeSinceStartup - start);
            }
        }

        public async Task<LLMHealth> CheckHealthAsync(CancellationToken ct = default)
        {
            try
            {
                using (var req = UnityWebRequest.Get(BaseUrl + "/api/tags"))
                {
                    req.timeout = 3;
                    bool completed = await SendAsync(req, ct);
                    if (!completed || req.result != UnityWebRequest.Result.Success) return LLMHealth.Offline;

                    string tags = req.downloadHandler.text ?? "";
                    string model = settings.model;
                    bool hasModel = tags.Contains("\"" + model + "\"") ||
                                    (!model.Contains(":") && tags.Contains("\"" + model + ":latest\""));
                    return hasModel ? LLMHealth.Online : LLMHealth.ModelMissing;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Ollama] Health check failed: {e.Message}");
                return LLMHealth.Offline;
            }
        }

        private LLMResult Classify(UnityWebRequest req, float latency)
        {
            switch (req.result)
            {
                case UnityWebRequest.Result.Success:
                    return null;
                case UnityWebRequest.Result.ConnectionError:
                    string err = req.error ?? "";
                    if (err.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        err.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0)
                        return LLMResult.Fail(LLMErrorType.Timeout,
                            $"Model {settings.timeoutSeconds} saniye içinde yanıt vermedi.", latency);
                    return LLMResult.Fail(LLMErrorType.ServerUnreachable,
                        $"{BaseUrl} adresindeki Ollama'ya ulaşılamıyor. 'ollama serve' çalışıyor mu? ({err})", latency);
                case UnityWebRequest.Result.ProtocolError:
                    string serverMsg = ExtractError(req.downloadHandler?.text);
                    if (req.responseCode == 404)
                        return LLMResult.Fail(LLMErrorType.ModelNotFound,
                            $"'{settings.model}' modeli yüklü değil. Çalıştır: ollama pull {settings.model}", latency);
                    return LLMResult.Fail(LLMErrorType.HttpError, $"HTTP {req.responseCode}: {serverMsg}", latency);
                default:
                    return LLMResult.Fail(LLMErrorType.Unknown, req.error ?? "Bilinmeyen ağ hatası", latency);
            }
        }

        private static string ExtractError(string body)
        {
            if (string.IsNullOrEmpty(body)) return "(no body)";
            try
            {
                var e = JsonUtility.FromJson<OllamaError>(body);
                if (e != null && !string.IsNullOrEmpty(e.error)) return e.error;
            }
            catch { /* ignored: body was not JSON */ }
            return body.Length > 200 ? body.Substring(0, 200) : body;
        }

        /// <summary>Awaits a UnityWebRequest without blocking the main thread. Returns false when cancelled.</summary>
        private static Task<bool> SendAsync(UnityWebRequest req, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<bool>();
            var op = req.SendWebRequest();
            op.completed += _ => tcs.TrySetResult(true);
            if (op.isDone) tcs.TrySetResult(true);
            if (ct.CanBeCanceled)
            {
                ct.Register(() =>
                {
                    try { if (!op.isDone) req.Abort(); } catch { /* request already disposed */ }
                    tcs.TrySetResult(false);
                });
            }
            return tcs.Task;
        }

        private string BuildChatBody(IReadOnlyList<ChatMessage> messages, string jsonSchema)
        {
            var sb = new StringBuilder(1024);
            sb.Append("{\"model\":").Append(Quote(settings.model));
            sb.Append(",\"stream\":false");
            sb.Append(",\"keep_alive\":").Append(Quote(settings.keepAlive));
            sb.Append(",\"format\":").Append(settings.useJsonSchema && !string.IsNullOrEmpty(jsonSchema) ? jsonSchema : "\"json\"");
            sb.Append(",\"options\":{\"temperature\":")
              .Append(settings.temperature.ToString("0.###", CultureInfo.InvariantCulture)).Append('}');
            sb.Append(",\"messages\":[");
            for (int i = 0; i < messages.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append("{\"role\":").Append(Quote(messages[i].role))
                  .Append(",\"content\":").Append(Quote(messages[i].content)).Append('}');
            }
            sb.Append("]}");
            return sb.ToString();
        }

        internal static string Quote(string s)
        {
            if (s == null) return "\"\"";
            var sb = new StringBuilder(s.Length + 8);
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }
    }
}
