using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SevenMinutes.Audio;
using SevenMinutes.Game;
using SevenMinutes.Robot;
using SevenMinutes.UI;

namespace SevenMinutes.Puzzles
{
    /// <summary>
    /// Final cooperative puzzle: the robot must stand on the pressure plate to power the console,
    /// then the player must win UNIT-6's trust (LLM NPC, trust ≥ 60). UNIT-6 reveals two numbers that open the door console.
    /// </summary>
    public class FinalDoorPuzzle : MonoBehaviour
    {
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private DoorSymbol[] sequence = { DoorSymbol.Star, DoorSymbol.Triangle, DoorSymbol.Circle, DoorSymbol.Square };
        [SerializeField] private PressurePlate plate;
        [SerializeField] private Transform plateStandPoint;
        [SerializeField] private Transform[] doorHalves;
        [SerializeField] private Vector3[] openOffsets;
        [SerializeField] private Transform[] bolts;
        [SerializeField] private Renderer[] slotLights;
        [SerializeField] private Renderer[] powerRenderers;
        [SerializeField] private Light panelLight;
        [SerializeField] private Light outsideLight;
        [SerializeField] private Light[] doorwayLights;
        [SerializeField] private ReadableObject workstationClue;
        [SerializeField] private KeypadController exitConsole;

        private readonly List<DoorSymbol> entered = new List<DoorSymbol>();
        private MaterialPropertyBlock block;
        private bool robotRequested;
        private bool hintedPower;
        private bool checking;
        private int hintAsks;

        public bool Powered { get; private set; }
        public bool Solved { get; private set; }
        public bool PanelInspected { get; private set; }
        public int FailedAttempts { get; private set; }
        public bool WaitingForRobot => !Powered && !Solved && !robotRequested;
        public DoorSymbol[] Sequence => sequence;

        /// <summary>The two numbers UNIT-6 gives when it trusts the player (random every run).</summary>
        public int CodeA { get; private set; }
        public int CodeB { get; private set; }
        public string Code => CodeA.ToString("00") + CodeB.ToString("00");
        public bool CodeRevealed { get; private set; }

        public void SetExitConsole(KeypadController console) => exitConsole = console;

        private void Awake()
        {
            CodeA = Random.Range(10, 100);
            CodeB = Random.Range(10, 100);
        }

        /// <summary>Called by UNIT-6 when the trust meter reaches the threshold.</summary>
        public void RevealCode()
        {
            if (CodeRevealed) return;
            CodeRevealed = true;
            AudioManager.Instance?.Play(SoundId.Success, transform.position, 0.8f);
            GameManager.Instance?.SetObjective($"Kodu kapının yanındaki konsola gir: {CodeA} {CodeB}");
            RobotDialogue.Say($"UNIT-6 sana güveniyor! Kod {CodeA} ve {CodeB}. Konsola gir!", 5f, true);
        }

        public void Configure(PressurePlate p, Transform stand, Transform[] halves, Vector3[] offsets, Transform[] lockBolts,
            Renderer[] slots, Renderer[] power, Light panel, Light outside, Light[] doorway, ReadableObject clue)
        {
            plate = p; plateStandPoint = stand; doorHalves = halves; openOffsets = offsets; bolts = lockBolts;
            slotLights = slots; powerRenderers = power; panelLight = panel; outsideLight = outside; doorwayLights = doorway;
            workstationClue = clue;
        }

        private void Start()
        {
            block = new MaterialPropertyBlock();
            plate.Activated += OnPowered;
            if (exitConsole != null)
            {
                exitConsole.SetCode(Code);
                exitConsole.Unlocked += OpenByGuard;
                exitConsole.WrongCode += () => GameHUD.Toast(CodeRevealed ? "Yanlış kod. UNIT-6'nın verdiği iki sayıyı yan yana gir." : "Kodu bilmiyorsun. Önce UNIT-6'nın güvenini kazan.", 2.5f);
            }
            plate.Released += OnUnpowered;
            SetPowerVisuals(false);
            ClearSlots();
            if (panelLight != null) panelLight.intensity = 0f;
            foreach (var l in doorwayLights) if (l != null) l.intensity = 0f;
        }

        // ---------------- robot cooperation ----------------

        public void RequestRobotPower()
        {
            if (!WaitingForRobot) return;
            robotRequested = true;
            RobotDialogue.Say("Anlaşıldı. UNIT-6'nın güç sistemini çalıştıracağım.", 3.5f, true);
            var robot = GameManager.Instance.Robot;
            robot.MoveTo(plateStandPoint.position, () => robot.TurnToward(transform));
        }

        private void OnPowered()
        {
            if (Solved || Powered) return;
            Powered = true;
            AudioManager.Instance?.Play(SoundId.PowerUp, transform.position, 1f);
            SetPowerVisuals(true);
            RobotDialogue.Say("Güç geldi! UNIT-6 uyanıyor. Seni test edecek — dürüst ve kibar ol.", 4f, true);
            GameManager.Instance?.SetObjective("UNIT-6 ile konuş (E). Güvenini 60'a çıkar, sana kapı kodunu versin.");
        }

        private void OnUnpowered()
        {
            if (Solved) return;
            Powered = false;
            robotRequested = false;
            SetPowerVisuals(false);
            ClearSlots();
            entered.Clear();
        }

        /// <summary>Opens the great door (called by the exit console when the right code is entered).</summary>
        public void OpenByGuard()
        {
            if (Solved || checking) return;
            checking = true;
            StartCoroutine(OpenSequence());
        }

        // ---------------- console ----------------

        public void OnSymbolPressed(DoorSymbol symbol)
        {
            if (Solved || checking) return;
            PanelInspected = true;
            if (!Powered)
            {
                AudioManager.Instance?.Play(SoundId.Error, transform.position, 0.6f, 0.8f);
                GameHUD.Toast("Hiçbir şey olmuyor. Konsolda güç yok.", 2.5f);
                if (!hintedPower)
                {
                    hintedPower = true;
                    RobotDialogue.Say("Konsolun güce ihtiyacı var. Benimle konuş — o plakanın üstünde durabilirim.", 4.5f, true);
                }
                return;
            }

            entered.Add(symbol);
            SetSlot(entered.Count - 1, new Color(1f, 0.9f, 0.6f));
            AudioManager.Instance?.Play(SoundId.KeyPress, transform.position, 0.6f, 0.8f + entered.Count * 0.1f);
            if (entered.Count >= sequence.Length) StartCoroutine(Check());
        }

        private IEnumerator Check()
        {
            checking = true;
            yield return new WaitForSeconds(0.35f);
            bool ok = true;
            for (int i = 0; i < sequence.Length; i++) if (entered[i] != sequence[i]) ok = false;

            if (ok)
            {
                for (int i = 0; i < slotLights.Length; i++) SetSlot(i, new Color(0.2f, 1f, 0.3f));
                AudioManager.Instance?.Play(SoundId.Success, transform.position, 1f);
                yield return OpenSequence();
            }
            else
            {
                FailedAttempts++;
                for (int i = 0; i < slotLights.Length; i++) SetSlot(i, new Color(1f, 0.1f, 0.05f));
                AudioManager.Instance?.Play(SoundId.Error, transform.position, 1f);
                GameHUD.Toast("YANLIŞ SIRA", 1.5f);
                if (FailedAttempts == 2) RobotDialogue.Say("Sembollerde yardım lazımsa benimle konuş.", 3.5f);
                yield return new WaitForSeconds(0.9f);
                ClearSlots();
                entered.Clear();
            }
            checking = false;
        }

        private IEnumerator OpenSequence()
        {
            Solved = true;
            var audio = AudioManager.Instance;
            var gm = GameManager.Instance;
            gm?.CompleteChallenge();

            for (int i = 0; i < bolts.Length; i++)
            {
                var b = bolts[i];
                Vector3 from = b.localPosition;
                Vector3 to = from + new Vector3(0f, 0f, 0.45f);
                audio?.Play(SoundId.Unlock, b.position, 0.9f, 0.8f + i * 0.08f);
                float t = 0f;
                while (t < 0.35f)
                {
                    t += Time.deltaTime;
                    b.localPosition = Vector3.Lerp(from, to, t / 0.35f);
                    yield return null;
                }
                yield return new WaitForSeconds(0.2f);
            }

            var robot = gm != null ? gm.Robot : null;
            if (robot != null) robot.Follow();
            RobotDialogue.Say("Başardık. Hadi buradan çıkalım.", 4.5f, true);
            gm?.SetObjective("KAÇ! Kapıdan geç.");

            var grind = audio != null ? audio.CreateLoop(SoundId.Grind, transform, 0.9f, 40f, 0.6f) : null;
            audio?.Play(SoundId.Slam, transform.position, 0.8f, 0.6f);
            Vector3[] start = new Vector3[doorHalves.Length];
            for (int i = 0; i < doorHalves.Length; i++) start[i] = doorHalves[i].localPosition;

            Color sky0 = RenderSettings.ambientSkyColor, eq0 = RenderSettings.ambientEquatorColor, gr0 = RenderSettings.ambientGroundColor;
            float fog0 = RenderSettings.fogDensity;
            float sun0 = outsideLight != null ? outsideLight.intensity : 0f;
            Quaternion sunRot0 = outsideLight != null ? outsideLight.transform.rotation : Quaternion.identity;
            Quaternion sunRot1 = Quaternion.Euler(52f, -25f, 0f);
            Color sunCol0 = outsideLight != null ? outsideLight.color : Color.white;
            Material sky = RenderSettings.skybox != null ? new Material(RenderSettings.skybox) : null;
            if (sky != null) RenderSettings.skybox = sky;
            float exp0 = sky != null && sky.HasProperty("_Exposure") ? sky.GetFloat("_Exposure") : 1f;
            Color fogCol0 = RenderSettings.fogColor;
            float time = 0f, dur = 3.5f;
            while (time < dur)
            {
                time += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, time / dur);
                for (int i = 0; i < doorHalves.Length; i++) doorHalves[i].localPosition = start[i] + openOffsets[i] * k;
                foreach (var l in doorwayLights) if (l != null) l.intensity = 3f * k;
                if (outsideLight != null)
                {
                    outsideLight.intensity = Mathf.Lerp(sun0, 1.7f, k);
                    outsideLight.color = Color.Lerp(sunCol0, new Color(1f, 0.95f, 0.85f), k);
                    outsideLight.transform.rotation = Quaternion.Slerp(sunRot0, sunRot1, k);
                }
                if (sky != null)
                {
                    if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", Mathf.Lerp(exp0, 1.3f, k));
                    if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", new Color(0.5f, 0.6f, 0.85f));
                    
                    if (sky.HasProperty("_SunSize")) sky.SetFloat("_SunSize", Mathf.Lerp(0.02f, 0.05f, k));
                }
                RenderSettings.fogColor = Color.Lerp(fogCol0, new Color(0.7f, 0.8f, 0.95f), k);
                RenderSettings.ambientSkyColor = Color.Lerp(sky0, new Color(0.75f, 0.82f, 0.95f), k);
                RenderSettings.ambientEquatorColor = Color.Lerp(eq0, new Color(0.55f, 0.55f, 0.5f), k);
                RenderSettings.ambientGroundColor = Color.Lerp(gr0, new Color(0.3f, 0.28f, 0.22f), k);
                RenderSettings.fogDensity = Mathf.Lerp(fog0, 0.0015f, k);
                yield return null;
            }
            if (grind != null) Destroy(grind.gameObject);
            audio?.Play(SoundId.Slam, transform.position, 0.9f, 0.5f);
        }

        // ---------------- hints ----------------

        public string Hint()
        {
            if (!Powered && !robotRequested) return null; // handled by RequestRobotPower
            if (!Powered) return "Plakaya yürüyorum...";
            if (CodeRevealed) return $"Kod {CodeA} ve {CodeB}. Kapının sağındaki konsola {Code} yaz!";
            hintAsks++;
            switch (hintAsks)
            {
                case 1: return "UNIT-6 seni test ediyor. Dürüst, kibar ve sakin cevap ver. Güven çubuğu 60'a ulaşmalı.";
                case 2: return "Maskeli adam 10 dakika dedi ama aslında 5 dakika vardı. Ve sen bana iyi davrandın — bunu ona söyleyebilirsin.";
                default: return "Ona hakaret etme ve onu kandırmaya çalışma: yapay zekâ bunu fark eder, güveni düşer.";
            }
        }

        // ---------------- visuals ----------------

        private void SetPowerVisuals(bool on)
        {
            if (panelLight != null) panelLight.intensity = on ? 1.5f : 0f;
            if (powerRenderers == null) return;
            foreach (var r in powerRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor(EmissionId, on ? new Color(0.2f, 0.7f, 1f) * 3f : new Color(0.05f, 0.02f, 0.02f));
                r.SetPropertyBlock(block);
            }
        }

        private void ClearSlots()
        {
            for (int i = 0; i < slotLights.Length; i++) SetSlot(i, new Color(0.04f, 0.04f, 0.05f));
        }

        private void SetSlot(int i, Color c)
        {
            if (i < 0 || i >= slotLights.Length || slotLights[i] == null) return;
            var r = slotLights[i];
            r.GetPropertyBlock(block);
            block.SetColor(EmissionId, c * 2.5f);
            block.SetColor(BaseColorId, c);
            r.SetPropertyBlock(block);
        }
    }
}
