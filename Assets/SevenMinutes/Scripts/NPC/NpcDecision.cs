using System;

namespace SevenMinutes.NPC
{
    public enum NpcAction
    {
        None,
        OpenDoor,
        WrongAnswer,
        Attack
    }

    /// <summary>Validated, strongly typed result of one NPC turn (parsed from the model's JSON).</summary>
    public sealed class NpcDecision
    {
        public string Reply { get; }
        public NpcAction Action { get; }
        /// <summary>How much this answer changed UNIT-6's trust (already clamped to the allowed range).</summary>
        public int TrustDelta { get; }

        public NpcDecision(string reply, NpcAction action, int trustDelta)
        {
            Reply = reply;
            Action = action;
            TrustDelta = trustDelta;
        }

        public static string ActionToWire(NpcAction a)
        {
            switch (a)
            {
                case NpcAction.OpenDoor: return "open_door";
                case NpcAction.WrongAnswer: return "wrong_answer";
                case NpcAction.Attack: return "attack";
                default: return "none";
            }
        }
    }

    /// <summary>Raw shape of the JSON the model must produce (parsed with JsonUtility).</summary>
    [Serializable]
    internal class NpcDecisionDto
    {
        public const int Missing = -999;
        public string reply;
        public string action;
        public int trust_delta = Missing;
    }
}
