using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using SevenMinutes.Audio;
using SevenMinutes.Core;
using SevenMinutes.Player;
using SevenMinutes.UI;

namespace SevenMinutes.Environment
{
    public enum DoorMotion
    {
        Swing,
        Slide
    }

    /// <summary>
    /// Generic door: swinging or sliding, optionally locked. A carving NavMeshObstacle keeps the robot out while closed.
    /// </summary>
    public class DoorController : MonoBehaviour, IInteractable
    {
        [SerializeField] private string doorName = "Kapı";
        [SerializeField] private bool locked = true;
        [SerializeField] private bool playerCanOpen = true;
        [TextArea] [SerializeField] private string lockedMessage = "Kilitli.";
        [SerializeField] private Transform movingPart;
        [SerializeField] private DoorMotion motion = DoorMotion.Swing;
        [SerializeField] private float swingAngle = 100f;
        [SerializeField] private Vector3 slideOffset = new Vector3(0f, 2.6f, 0f);
        [SerializeField] private float duration = 1.4f;
        [SerializeField] private NavMeshObstacle obstacle;
        [SerializeField] private SoundId openSound = SoundId.Creak;

        private Quaternion closedRot;
        private Vector3 closedPos;
        private bool moving;

        public bool IsOpen { get; private set; }
        public bool IsLocked => locked;
        public event Action<DoorController> Opened;

        public string Prompt => doorName == "Kapı" ? "Kapıyı Aç" : doorName + " Aç";
        public bool CanInteract => playerCanOpen && !IsOpen && !moving;

        public void Configure(string displayName, bool isLocked, bool playerOpens, string lockedText, Transform part,
            DoorMotion doorMotion, float angle, Vector3 slide, float time, NavMeshObstacle navObstacle, SoundId sound)
        {
            doorName = displayName; locked = isLocked; playerCanOpen = playerOpens; lockedMessage = lockedText;
            movingPart = part; motion = doorMotion; swingAngle = angle; slideOffset = slide; duration = time;
            obstacle = navObstacle; openSound = sound;
        }

        private void Awake()
        {
            if (movingPart == null) movingPart = transform;
            closedRot = movingPart.localRotation;
            closedPos = movingPart.localPosition;
        }

        public void Interact(PlayerController player)
        {
            if (locked)
            {
                AudioManager.Instance?.Play(SoundId.Rattle, transform.position, 0.8f);
                GameHUD.Toast(lockedMessage, 2.2f);
                return;
            }
            Open();
        }

        public void Unlock(bool openNow)
        {
            locked = false;
            if (openNow) Open();
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            if (obstacle != null) obstacle.enabled = false;
            AudioManager.Instance?.Play(openSound, transform.position, 0.9f);
            StartCoroutine(Animate(true));
            Opened?.Invoke(this);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (obstacle != null) obstacle.enabled = true;
            StartCoroutine(Animate(false));
        }

        private IEnumerator Animate(bool open)
        {
            moving = true;
            Quaternion fromR = movingPart.localRotation;
            Vector3 fromP = movingPart.localPosition;
            Quaternion toR = open && motion == DoorMotion.Swing ? closedRot * Quaternion.Euler(0f, swingAngle, 0f) : closedRot;
            Vector3 toP = open && motion == DoorMotion.Slide ? closedPos + slideOffset : closedPos;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                movingPart.localRotation = Quaternion.Slerp(fromR, toR, k);
                movingPart.localPosition = Vector3.Lerp(fromP, toP, k);
                yield return null;
            }
            movingPart.localRotation = toR;
            movingPart.localPosition = toP;
            moving = false;
        }
    }
}
