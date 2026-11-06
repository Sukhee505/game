using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using SevenMinutes.LLM;

namespace SevenMinutes.NPC
{
    public sealed class DialogueTurnResult
    {
        public bool Success;
        public NpcDecision Decision;
        public LLMErrorType Error;
        public string ErrorMessage;
        public string RawOutput;
        public float LatencySeconds;
        public int Attempts;
    }

    /// <summary>
    /// Pure C# conversation logic (no MonoBehaviour): history window, structured output,
    /// one automatic repair attempt when the model returns invalid JSON.
    /// </summary>
    public sealed class DialogueSession
    {
        private readonly ILLMClient client;
        private readonly INpcResponseParser parser;
        private readonly NpcPersona persona;
        private readonly int maxHistoryMessages;
        private readonly List<ChatMessage> history = new List<ChatMessage>();


        public DialogueSession(ILLMClient client, INpcResponseParser parser, NpcPersona persona, int maxHistoryMessages = 12)
        {
            this.client = client;
            this.parser = parser;
            this.persona = persona;
            this.maxHistoryMessages = Mathf.Max(2, maxHistoryMessages);
        }

        public void Reset()
        {
            history.Clear();
        }

        public async Task<DialogueTurnResult> SendAsync(string playerText, CancellationToken ct)
        {
            var result = new DialogueTurnResult();
            var userMsg = ChatMessage.ForUser(playerText);

            var messages = BuildMessages(userMsg);
            for (int attempt = 1; attempt <= 2; attempt++)
            {
                result.Attempts = attempt;
                var llm = await client.ChatAsync(messages, NpcPersona.JsonSchema, ct);
                result.LatencySeconds += llm.LatencySeconds;

                if (!llm.Success)
                {
                    // Network / server problems: do not retry here, report to the UI.
                    result.Error = llm.Error;
                    result.ErrorMessage = llm.ErrorMessage;
                    return result;
                }

                result.RawOutput = llm.Content;
                if (parser.TryParse(llm.Content, out var decision, out var parseError))
                {
                    history.Add(userMsg);
                    // Store a normalised JSON answer so the model keeps seeing the expected format.
                    history.Add(ChatMessage.ForAssistant(ToJson(decision)));
                    TrimHistory();
                    result.Success = true;
                    result.Decision = decision;
                    return result;
                }

                Debug.LogWarning($"[Dialogue] Invalid model output (attempt {attempt}): {parseError}\n{llm.Content}");
                result.Error = LLMErrorType.InvalidResponse;
                result.ErrorMessage = parseError;

                // Repair attempt: show the bad answer and ask again for the exact format.
                messages = BuildMessages(userMsg);
                messages.Add(ChatMessage.ForAssistant(llm.Content));
                messages.Add(ChatMessage.ForUser(NpcPersona.RepairPrompt));
            }
            return result;
        }

        private List<ChatMessage> BuildMessages(ChatMessage newUserMessage)
        {
            var list = new List<ChatMessage>(history.Count + 2) { ChatMessage.ForSystem(persona.SystemPrompt) };
            list.AddRange(history);
            list.Add(newUserMessage);
            return list;
        }

        private void TrimHistory()
        {
            while (history.Count > maxHistoryMessages) history.RemoveRange(0, 2); // drop oldest user+assistant pair
        }

        private static string ToJson(NpcDecision d) =>
            "{\"reply\":" + OllamaClient.Quote(d.Reply) +
            ",\"action\":\"" + NpcDecision.ActionToWire(d.Action) +
            "\",\"trust_delta\":" + d.TrustDelta + "}";
    }
}
