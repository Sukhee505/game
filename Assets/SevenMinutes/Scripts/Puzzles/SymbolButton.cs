using System.Collections;
using UnityEngine;
using SevenMinutes.Audio;
using SevenMinutes.Core;
using SevenMinutes.Player;

namespace SevenMinutes.Puzzles
{
    public enum DoorSymbol
    {
        Circle,
        Triangle,
        Square,
        Star
    }

    /// <summary>One of the four symbol buttons on the final door console.</summary>
    public class SymbolButton : MonoBehaviour, IInteractable
    {
        [SerializeField] private DoorSymbol symbol;
        [SerializeField] private FinalDoorPuzzle puzzle;
        [SerializeField] private Transform cap;

        private Vector3 rest;

        public DoorSymbol Symbol => symbol;
        public string Prompt => SymbolName(symbol) + " Düğmesine Bas";

        public static string SymbolName(DoorSymbol s)
        {
            switch (s)
            {
                case DoorSymbol.Circle: return "Daire";
                case DoorSymbol.Triangle: return "Üçgen";
                case DoorSymbol.Square: return "Kare";
                default: return "Yıldız";
            }
        }
        public bool CanInteract => puzzle != null && !puzzle.Solved;

        public void Configure(DoorSymbol s, FinalDoorPuzzle p, Transform capT)
        {
            symbol = s;
            puzzle = p;
            cap = capT;
        }

        private void Awake()
        {
            if (cap != null) rest = cap.localPosition;
        }

        public void Interact(PlayerController player)
        {
            AudioManager.Instance?.Play(SoundId.Click, transform.position, 0.9f);
            if (cap != null) StartCoroutine(Animate());
            puzzle.OnSymbolPressed(symbol);
        }

        private IEnumerator Animate()
        {
            Vector3 down = rest + cap.localRotation * Vector3.back * 0.03f;
            cap.localPosition = down;
            yield return new WaitForSeconds(0.18f);
            cap.localPosition = rest;
        }
    }
}
