using UnityEngine;
using SevenMinutes.Core;
using SevenMinutes.Player;
using SevenMinutes.UI;

namespace SevenMinutes.Puzzles
{
    /// <summary>Photographs, newspapers, diaries, screens: shows a text when used.</summary>
    public class ReadableObject : MonoBehaviour, IInteractable
    {
        [SerializeField] private string prompt = "Oku";
        [SerializeField] private string title = "";
        [TextArea(4, 12)] [SerializeField] private string body = "";
        [SerializeField] private bool isClue;

        public bool HasBeenRead { get; private set; }
        public bool IsClue => isClue;

        public string Prompt => prompt;
        public bool CanInteract => true;

        public void Configure(string promptText, string noteTitle, string noteBody, bool clue)
        {
            prompt = promptText;
            title = noteTitle;
            body = noteBody;
            isClue = clue;
        }

        public void Interact(PlayerController player)
        {
            HasBeenRead = true;
            NoteUI.Instance?.Show(new NoteData(title, body));
        }
    }
}
