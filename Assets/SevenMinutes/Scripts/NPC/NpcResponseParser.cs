using System;
using UnityEngine;

namespace SevenMinutes.NPC
{
    public interface INpcResponseParser
    {
        bool TryParse(string raw, out NpcDecision decision, out string error);
    }

    /// <summary>
    /// Turns the model's text into an <see cref="NpcDecision"/>. Robust against the usual small-model mistakes:
    /// markdown code fences, text around the JSON object, wrong casing of the action, out-of-range trust.
    /// Anything that cannot be validated is rejected (never guessed from free text).
    /// </summary>
    public sealed class JsonNpcResponseParser : INpcResponseParser
    {
        private const int MaxReplyLength = 600;

        public bool TryParse(string raw, out NpcDecision decision, out string error)
        {
            decision = null;
            error = null;

            if (string.IsNullOrWhiteSpace(raw)) { error = "boş çıktı"; return false; }

            string json = ExtractJsonObject(raw);
            if (json == null) { error = "JSON nesnesi bulunamadı"; return false; }

            NpcDecisionDto dto;
            try { dto = JsonUtility.FromJson<NpcDecisionDto>(json); }
            catch (Exception e) { error = "geçersiz JSON: " + e.Message; return false; }

            if (dto == null) { error = "JSON eşlenemedi"; return false; }
            if (string.IsNullOrWhiteSpace(dto.reply)) { error = "'reply' alanı yok"; return false; }
            if (!TryMapAction(dto.action, out var action)) { error = $"bilinmeyen eylem '{dto.action}'"; return false; }
            if (dto.trust_delta == NpcDecisionDto.Missing) { error = "'trust_delta' alanı yok"; return false; }

            string reply = dto.reply.Trim();
            if (reply.Length > MaxReplyLength) reply = reply.Substring(0, MaxReplyLength) + "...";

            // The model is never allowed to open the door itself: the game decides from the trust meter.
            if (action == NpcAction.OpenDoor || action == NpcAction.WrongAnswer) action = NpcAction.None;
            decision = new NpcDecision(reply, action, Mathf.Clamp(dto.trust_delta, TrustMeter.MinDelta, TrustMeter.MaxDelta));
            return true;
        }

        /// <summary>Takes the substring from the first '{' to its matching '}' (ignores code fences / chatter).</summary>
        internal static string ExtractJsonObject(string text)
        {
            int start = text.IndexOf('{');
            if (start < 0) return null;

            int depth = 0;
            bool inString = false, escaped = false;
            for (int i = start; i < text.Length; i++)
            {
                char c = text[i];
                if (inString)
                {
                    if (escaped) escaped = false;
                    else if (c == '\\') escaped = true;
                    else if (c == '"') inString = false;
                    continue;
                }
                if (c == '"') inString = true;
                else if (c == '{') depth++;
                else if (c == '}' && --depth == 0) return text.Substring(start, i - start + 1);
            }
            return null; // unbalanced braces
        }

        private static bool TryMapAction(string value, out NpcAction action)
        {
            action = NpcAction.None;
            if (value == null) return false;
            string v = value.Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
            switch (v)
            {
                case "none":
                case "":
                    action = NpcAction.None; return true;
                case "open_door":
                case "opendoor":
                case "open_gate":
                    action = NpcAction.OpenDoor; return true;
                case "wrong_answer":
                case "wronganswer":
                case "wrong":
                    action = NpcAction.WrongAnswer; return true;
                case "attack":
                    action = NpcAction.Attack; return true;
                default:
                    return false;
            }
        }
    }
}
