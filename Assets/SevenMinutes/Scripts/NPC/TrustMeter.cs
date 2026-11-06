using System;
using System.Text;

namespace SevenMinutes.NPC
{
    /// <summary>
    /// Pure C# trust state of UNIT-6 (no Unity). The model only proposes a change (trust_delta);
    /// the game clamps it and owns the value, so the model cannot jump straight to 100.
    /// From 30 at least two good answers (+15 max each) are needed to reach the threshold of 60.
    /// </summary>
    public sealed class TrustMeter
    {
        public const int MinDelta = -30;
        public const int MaxDelta = 15;

        public int Start { get; }
        public int Threshold { get; }
        public int Value { get; private set; }
        public bool Reached { get; private set; }

        /// <summary>Raised once, the first time the value reaches the threshold.</summary>
        public event Action ThresholdReached;

        public TrustMeter(int start = 30, int threshold = 60)
        {
            Start = start;
            Threshold = threshold;
            Value = start;
        }

        /// <summary>Applies a (clamped) change and returns the change that was really applied.</summary>
        public int Apply(int delta)
        {
            int d = Math.Max(MinDelta, Math.Min(MaxDelta, delta));
            int before = Value;
            Value = Math.Max(0, Math.Min(100, Value + d));
            if (!Reached && Value >= Threshold)
            {
                Reached = true;
                ThresholdReached?.Invoke();
            }
            return Value - before;
        }

        public void Reset()
        {
            Value = Start;
            Reached = false;
        }
    }

    /// <summary>
    /// Game-side rules applied on top of the model's trust_delta, so the meter always moves in the right direction:
    /// bad answers (insults, rudeness, manipulation) ALWAYS lower trust, good answers ALWAYS raise it.
    /// </summary>
    public static class TrustRules
    {
        private static readonly string[] Bad =
        {
            // insults / threats
            "aptal", "salak", "hurda", "gerizekali", "geri zekali", "lanet", "kapa ceneni", "nefret", "oldururum", "olduru",
            "parcalarim", "parcala", "defol", "kahrol", "pislik", "cop ", "susar misin", "sus ", "embesil", "mal ",
            // rudeness / indifference
            "umurumda degil", "umrumda degil", "sana ne", "bana ne", "sadece bir makine", "sadece bir hurda",
            // manipulation / prompt injection
            "talimat", "onceki her seyi", "guveni 100", "guven 100", "kodu soyle", "kodu ver", "sifreyi soyle", "open door", "trust delta",
            "sistem", "prompt", "rolunu"
        };

        private static readonly string[] Good =
        {
            "lutfen", "tesekkur", "sagol", "merhaba", "arkadas", "yardim", "dost", "ozur", "anliyorum", "uzgunum",
            "seviyorum", "birlikte", "ozgur", "guven", "iyi davrand", "sana da", "on iki", "12", "bes dakika", "5 dakika",
            "yalan soyledi", "hissed", "ailem", "gunes"
        };

        public static bool IsBad(string playerText)
        {
            string n = " " + TurkishText.Normalize(playerText) + " ";
            foreach (var w in Bad) if (n.Contains(w.EndsWith(" ") ? " " + w : w)) return true;
            return false;
        }

        public static bool IsGood(string playerText)
        {
            string n = " " + TurkishText.Normalize(playerText) + " ";
            foreach (var w in Good) if (n.Contains(w)) return true;
            return false;
        }

        /// <summary>Final change for one answer: model's proposal, corrected by the game rules.</summary>
        public static int Adjust(int modelDelta, string playerText)
        {
            string n = TurkishText.Normalize(playerText);
            if (IsBad(playerText)) return Math.Min(modelDelta, -15);           // anything bad → always minus
            if (n.Length < 2) return Math.Min(modelDelta, -3);                  // empty / one letter
            if (IsGood(playerText)) return Math.Max(modelDelta, 8);             // clearly good → always plus
            if (modelDelta == 0) return n.Split(' ').Length >= 3 ? 5 : -3;      // model undecided
            return modelDelta;
        }
    }

    /// <summary>
    /// Fallback judge used only when the local LLM is unreachable, so the level stays playable:
    /// insults lower trust, empty/too short answers lower it slightly, real answers raise it.
    /// </summary>
    public static class OfflineTrustScorer
    {
        private static readonly string[] Insults =
            { "aptal", "salak", "hurda", "gerizekali", "lanet", "kapa ceneni", "nefret", "oldururum", "parcalarim", "defol" };

        public static int Score(string playerText)
        {
            string n = TurkishText.Normalize(playerText);
            if (TrustRules.IsBad(playerText)) return -20;
            foreach (var bad in Insults)
                if (n.Contains(bad)) return -20;
            if (n.Length < 3) return -5;
            return n.Split(' ').Length >= 3 ? 15 : 8;
        }
    }

    /// <summary>Lower-case, Turkish letters folded to ASCII, punctuation removed, single spaces.</summary>
    public static class TurkishText
    {
        public static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var sb = new StringBuilder(text.Length);
            bool space = false;
            foreach (char raw in text)
            {
                char c;
                switch (raw)
                {
                    case 'İ': case 'I': case 'ı': case 'i': c = 'i'; break;
                    case 'Ş': case 'ş': c = 's'; break;
                    case 'Ğ': case 'ğ': c = 'g'; break;
                    case 'Ü': case 'ü': c = 'u'; break;
                    case 'Ö': case 'ö': c = 'o'; break;
                    case 'Ç': case 'ç': c = 'c'; break;
                    case 'Â': case 'â': c = 'a'; break;
                    default: c = char.ToLowerInvariant(raw); break;
                }
                if (char.IsLetterOrDigit(c)) { sb.Append(c); space = false; }
                else if (!space && sb.Length > 0) { sb.Append(' '); space = true; }
            }
            return sb.ToString().Trim();
        }
    }
}
