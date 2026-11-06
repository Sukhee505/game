using System;

namespace SevenMinutes.LLM
{
    [Serializable]
    public class ChatMessage
    {
        public string role;
        public string content;

        public ChatMessage() { }

        public ChatMessage(string role, string content)
        {
            this.role = role;
            this.content = content;
        }

        public static ChatMessage ForSystem(string text) => new ChatMessage("system", text);
        public static ChatMessage ForUser(string text) => new ChatMessage("user", text);
        public static ChatMessage ForAssistant(string text) => new ChatMessage("assistant", text);
    }

    public enum LLMErrorType
    {
        None,
        ServerUnreachable,
        Timeout,
        ModelNotFound,
        HttpError,
        EmptyResponse,
        InvalidResponse,
        Cancelled,
        Unknown
    }

    public sealed class LLMResult
    {
        public bool Success { get; private set; }
        public string Content { get; private set; }
        public LLMErrorType Error { get; private set; }
        public string ErrorMessage { get; private set; }
        public float LatencySeconds { get; private set; }

        public static LLMResult Ok(string content, float latency) =>
            new LLMResult { Success = true, Content = content, Error = LLMErrorType.None, LatencySeconds = latency };

        public static LLMResult Fail(LLMErrorType error, string message, float latency = 0f) =>
            new LLMResult { Success = false, Error = error, ErrorMessage = message, LatencySeconds = latency };
    }

    public enum LLMHealth
    {
        Online,
        ModelMissing,
        Offline
    }

    /// <summary>Settings for the local Ollama server (editable in the Inspector).</summary>
    [Serializable]
    public class OllamaSettings
    {
        public string baseUrl = "http://localhost:11434";
        public string model = "llama3.2:3b";
        [UnityEngine.Range(5, 180)] public int timeoutSeconds = 60;
        [UnityEngine.Range(0f, 1.5f)] public float temperature = 0.7f;
        public bool useJsonSchema = true;
        public string keepAlive = "15m";
    }

    // ---- DTOs that mirror Ollama's /api/chat response ----
    [Serializable]
    internal class OllamaChatResponse
    {
        public ChatMessage message;
        public bool done;
        public string error;
    }

    [Serializable]
    internal class OllamaError
    {
        public string error;
    }
}
