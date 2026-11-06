using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SevenMinutes.LLM
{
    /// <summary>
    /// Abstraction of a chat-completion backend. The NPC only depends on this interface,
    /// so the Ollama implementation can be swapped (e.g. LM Studio, llama.cpp) without touching NPC code.
    /// Implementations must never throw: every failure is returned as an <see cref="LLMResult"/>.
    /// </summary>
    public interface ILLMClient
    {
        string ModelName { get; }

        /// <param name="jsonSchema">Optional JSON schema (raw JSON) for structured output.</param>
        Task<LLMResult> ChatAsync(IReadOnlyList<ChatMessage> messages, string jsonSchema, CancellationToken ct = default);

        Task<LLMHealth> CheckHealthAsync(CancellationToken ct = default);
    }
}
