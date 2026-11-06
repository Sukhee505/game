using System;
using System.Collections;
using UnityEngine;
using SevenMinutes.Audio;
using SevenMinutes.Core;
using SevenMinutes.Player;
using SevenMinutes.UI;

namespace SevenMinutes.Puzzles
{
    public enum ContainerKind
    {
        Drawer,
        HingedDoor
    }

    /// <summary>Drawer, cabinet or wardrobe that can be searched. It may hold a note (real or fake) or nothing.</summary>
    public class SearchableContainer : MonoBehaviour, IInteractable
    {
        [SerializeField] private string displayName = "Çekmece";
        [SerializeField] private string locationName = "çekmece";
        [SerializeField] private ContainerKind kind = ContainerKind.Drawer;
        [SerializeField] private Transform movingPart;
        [SerializeField] private Vector3 slide = new Vector3(0f, 0f, 0.35f);
        [SerializeField] private float swingAngle = -105f;
        [SerializeField] private GameObject noteVisual;
        [TextArea] [SerializeField] private string emptyMessage = "Burada hiçbir şey yok.";

        private bool open;
        private bool animating;
        private Vector3 closedPos;
        private Quaternion closedRot;

        public NoteData Content { get; private set; }
        public string LocationName => locationName;
        public bool IsOpen => open;

        public static event Action<SearchableContainer> Searched;

        public string Prompt => !open ? "Ara: " + displayName : "Notu Oku";
        public bool CanInteract => !animating && (!open || Content != null);

        public void Configure(string display, string location, ContainerKind k, Transform part, Vector3 slideOffset, float angle, GameObject note)
        {
            displayName = display; locationName = location; kind = k; movingPart = part;
            slide = slideOffset; swingAngle = angle; noteVisual = note;
        }

        private void Awake()
        {
            if (movingPart == null) movingPart = transform;
            closedPos = movingPart.localPosition;
            closedRot = movingPart.localRotation;
            if (noteVisual != null && Content == null) noteVisual.SetActive(false);
        }

        public void SetContent(NoteData note, string emptyText = null)
        {
            Content = note;
            if (emptyText != null) emptyMessage = emptyText;
            if (noteVisual != null) noteVisual.SetActive(note != null);
        }

        public void Interact(PlayerController player)
        {
            if (!open)
            {
                StartCoroutine(OpenRoutine());
                return;
            }
            if (Content != null) NoteUI.Instance?.Show(Content);
        }

        private IEnumerator OpenRoutine()
        {
            animating = true;
            open = true;
            AudioManager.Instance?.Play(kind == ContainerKind.Drawer ? SoundId.Drawer : SoundId.Creak, transform.position, 0.8f,
                kind == ContainerKind.Drawer ? 1f : 1.3f);
            Vector3 toPos = kind == ContainerKind.Drawer ? closedPos + slide : closedPos;
            Quaternion toRot = kind == ContainerKind.HingedDoor ? closedRot * Quaternion.Euler(0f, swingAngle, 0f) : closedRot;
            float t = 0f;
            while (t < 0.45f)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / 0.45f);
                movingPart.localPosition = Vector3.Lerp(closedPos, toPos, k);
                movingPart.localRotation = Quaternion.Slerp(closedRot, toRot, k);
                yield return null;
            }
            animating = false;
            Searched?.Invoke(this);
            if (Content == null) GameHUD.Toast(emptyMessage, 1.6f);
            else GameHUD.Toast("İçeride bir şey var...", 1.6f);
        }

        private void OnDestroy()
        {
            Searched = null;
        }
    }
}
