using System.Collections.Generic;
using UnityEngine;
using SevenMinutes.Game;
using SevenMinutes.Robot;
using SevenMinutes.UI;

namespace SevenMinutes.Puzzles
{
    /// <summary>
    /// Level 1. The real note (four equations → 6674) is hidden in one of several possible furniture pieces,
    /// chosen at random each game. The other containers hold misleading notes, objects or nothing.
    /// </summary>
    public class MathPuzzle : MonoBehaviour
    {
        [SerializeField] private SearchableContainer[] secretCandidates;
        [SerializeField] private SearchableContainer[] otherContainers;
        [SerializeField] private KeypadController keypad;
        [SerializeField] private string code = "6674";

        public bool SecretFound { get; private set; }
        public bool Solved { get; private set; }
        public SearchableContainer SecretLocation { get; private set; }

        private static readonly NoteData Secret = new NoteData("Katlanmış, kanlı bir not",
            "Bunu okuyorsan, ev seni seçti demektir.\n" +
            "Kapı dört sayıyı hatırlıyor. Sırayla çöz:\n\n" +
            "      I.     3 × 2  = ?\n" +
            "      II.    18 ÷ 3 = ?\n" +
            "      III.   5 + 2  = ?\n" +
            "      IV.    12 − 8 = ?\n\n" +
            "Her cevap şifrenin bir rakamıdır.\n" +
            "Acele et. Seni dinliyor.", true);

        private static readonly NoteData[] FalseNotes =
        {
            new NoteData("Buruşuk not", "Cevap pencerenin yakınında bir yerde."),
            new NoteData("Yırtık sayfa", "İlk sayıya güvenme."),
            new NoteData("Karalanmış not", "Sanırım şifre 1234.\n\n...hayır. Yanlıştı. İki parmağımı aldı."),
            new NoteData("Eski mektup", "Ev her şeyi hatırlar.\nSeni de hatırlıyor."),
            new NoteData("Yapışkan not", "Lambaları say. Tekrar say.\nHiçbir zaman aynı değiller."),
        };

        private static readonly NoteData[] Objects =
        {
            new NoteData("Bir çocuk çizimi", "Beş pencereli bir evin pastel boya çizimi.\nBiri bütün pencereleri siyaha boyamış."),
            new NoteData("Paslı anahtar", "Eski, paslı bir anahtar. İkiye bükülmüş — işe yaramaz."),
        };

        public void Configure(SearchableContainer[] candidates, SearchableContainer[] others, KeypadController pad)
        {
            secretCandidates = candidates;
            otherContainers = others;
            keypad = pad;
        }

        private void Start()
        {
            var rnd = new System.Random();
            SecretLocation = secretCandidates[rnd.Next(secretCandidates.Length)];

            var rest = new List<SearchableContainer>();
            foreach (var c in secretCandidates) if (c != SecretLocation) rest.Add(c);
            rest.AddRange(otherContainers);
            for (int i = rest.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                (rest[i], rest[j]) = (rest[j], rest[i]);
            }

            SecretLocation.SetContent(Secret);
            int f = 0, o = 0;
            for (int i = 0; i < rest.Count; i++)
            {
                if (f < FalseNotes.Length && i % 3 != 2) rest[i].SetContent(FalseNotes[f++]);
                else if (o < Objects.Length && i % 3 == 2) rest[i].SetContent(Objects[o++]);
                else rest[i].SetContent(null);
            }

            keypad.SetCode(code);
            keypad.Unlocked += OnUnlocked;
            keypad.WrongCode += OnWrongCode;
            NoteUI.NoteRead += OnNoteRead;
        }

        private void OnNoteRead(NoteData note)
        {
            if (note == null || !note.isSecret || SecretFound) return;
            SecretFound = true;
            RobotDialogue.Say("Bu not önemli görünüyor. Dört işlem... dört rakam.", 4f, true);
            var gm = GameManager.Instance;
            if (gm != null && gm.Timer.Started) gm.SetObjective("Dört işlemi çöz ve şifreyi kapının tuş takımına gir.");
        }

        private int wrong;

        private void OnWrongCode()
        {
            wrong++;
            if (wrong == 2) RobotDialogue.Say("Notun her satırını çöz. Bir cevap, bir rakam.", 3.5f);
        }

        private void OnUnlocked()
        {
            Solved = true;
            var gm = GameManager.Instance;
            gm?.CompleteChallenge();
            gm?.SetObjective("Evi keşfet ve aşağı inmenin bir yolunu bul.");
            RobotDialogue.Say("Harika iş. Bu odadan çıkalım.", 3f, true);
        }
    }
}
