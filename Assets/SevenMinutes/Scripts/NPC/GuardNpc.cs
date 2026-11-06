using System;
using System.Threading;
using UnityEngine;
using SevenMinutes.Game;
using SevenMinutes.LLM;
using SevenMinutes.Player;
using SevenMinutes.Core;
using SevenMinutes.Puzzles;
using SevenMinutes.UI;

namespace SevenMinutes.NPC
{
    /// <summary>
    /// UNIT-6, the guard AI of the exit door, driven by a LOCAL language model (Ollama).
    /// It tests the player with simple questions; every answer changes the trust meter (trust_delta from the model,
    /// clamped and owned by the game). At 60/100 (configurable) the game reveals the two-number door code, which the player
    /// types into the door console. All LLM calls are asynchronous; every failure is shown and the game keeps running.
    /// </summary>
    public class GuardNpc : MonoBehaviour, IInteractable
    {
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private NpcPersona persona = new NpcPersona();
        [SerializeField] private OllamaSettings ollama = new OllamaSettings();
        [SerializeField] private DialogueUI dialogueView;
        [SerializeField] private GuardActionHandler actionHandler;
        [SerializeField] private FinalDoorPuzzle door;
        [SerializeField] private Renderer[] eyes;
        [SerializeField] private Light eyeLight;
        [SerializeField] private float talkRadius = 5f;

        private ILLMClient client;
        private DialogueSession session;
        private IDialogueView view;
        private INpcActionHandler actions;
        private TrustMeter trust;
        private CancellationTokenSource cts;
        private MaterialPropertyBlock block;
        private bool busy, greeted, offline;
        private int offlineQuestion;
        private float glow;

        /// <summary>Last known state of the local LLM server (shown in the F1 debug panel).</summary>
        public static string ServiceStatus { get; private set; } = "LLM: kontrol ediliyor...";

        public NpcPersona Persona => persona;
        public ILLMClient Client => client;
        public TrustMeter Trust => trust;

        public string Prompt => door != null && door.Powered ? persona.npcName + " ile Konuş" : persona.npcName + "'yı İncele";
        public bool CanInteract => door == null || !door.Solved;

        public void Configure(DialogueUI viewComponent, GuardActionHandler handler, FinalDoorPuzzle finalDoor, Renderer[] eyeRenderers, Light light)
        {
            dialogueView = viewComponent; actionHandler = handler; door = finalDoor; eyes = eyeRenderers; eyeLight = light;
        }

        private void Awake()
        {
            client = new OllamaClient(ollama);
            session = new DialogueSession(client, new JsonNpcResponseParser(), persona);
            view = dialogueView;
            actions = actionHandler;
            trust = new TrustMeter(persona.startTrust, persona.trustThreshold);
            trust.ThresholdReached += OnTrustReached;
            block = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (view == null) return;
            view.Submitted += OnPlayerSubmitted;
            view.CloseRequested += CloseDialogue;
        }

        private void OnDisable()
        {
            if (view != null)
            {
                view.Submitted -= OnPlayerSubmitted;
                view.CloseRequested -= CloseDialogue;
            }
            cts?.Cancel();
        }

        private async void Start()
        {
            ApplyGlow(0f);
            try
            {
                var health = await client.CheckHealthAsync(destroyCancellationToken);
                switch (health)
                {
                    case LLMHealth.Online:
                        ServiceStatus = $"LLM: çevrimiçi ({client.ModelName})";
                        WarmUp();
                        break;
                    case LLMHealth.ModelMissing:
                        ServiceStatus = $"LLM: model yüklü değil ({client.ModelName})";
                        GameHUD.Toast($"Ollama çalışıyor ama '{client.ModelName}' yüklü değil: ollama pull {client.ModelName}", 5f);
                        break;
                    default:
                        ServiceStatus = "LLM: ÇEVRİMDIŞI (Ollama kapalı)";
                        GameHUD.Toast("Yerel LLM sunucusu (Ollama) çalışmıyor. Oyun devam eder; UNIT-6 yedek modda konuşur.", 5f);
                        break;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                ServiceStatus = "LLM: hata";
                Debug.LogWarning("[UNIT-6] Health check error: " + e.Message);
            }
        }

        /// <summary>Loads the model into memory in the background so the first answer is faster.</summary>
        private async void WarmUp()
        {
            try
            {
                var r = await client.ChatAsync(new[] { ChatMessage.ForUser("Sadece {\"ok\":true} yaz.") }, null, destroyCancellationToken);
                Debug.Log($"[UNIT-6] Warm-up finished in {r.LatencySeconds:0.0}s (success={r.Success}).");
            }
            catch (Exception) { /* optional */ }
        }

        private void Update()
        {
            bool powered = door != null && door.Powered;
            glow = Mathf.MoveTowards(glow, powered ? 1f : 0f, Time.deltaTime * 0.8f);
            ApplyGlow(glow * (busy ? 0.75f + 0.25f * Mathf.Sin(Time.time * 12f) : 1f));

            if (view != null && view.IsOpen)
            {
                var gm = GameManager.Instance;
                if (gm != null && Vector3.Distance(gm.Player.transform.position, transform.position) > talkRadius + 2f) CloseDialogue();
            }
        }

        public void Interact(PlayerController player)
        {
            if (door != null && !door.Powered)
            {
                GameHUD.Toast(persona.npcName + " kapalı. Gücü yok — yandaki basınç plakasına ağır bir şeyin basması gerekiyor.", 3.5f);
                return;
            }
            OpenDialogue();
        }

        private void OpenDialogue()
        {
            if (view == null) return;
            view.Open(persona.npcName);
            view.SetStatus(ServiceStatus);
            dialogueView.Threshold = trust.Threshold;
            view.SetTrust(trust.Value);
            if (trust.Reached) view.AddLine(Speaker.System, $"Kapı kodu: {door.CodeA} ve {door.CodeB} → kapının yanındaki konsola gir.");
            else if (!greeted) RunTurn(persona.approachPrompt, false);
        }

        private void CloseDialogue()
        {
            cts?.Cancel();
            view?.Close();
        }

        private void OnPlayerSubmitted(string text)
        {
            if (offline) { OfflineTurn(text); return; }
            RunTurn(text, true);
        }

        private async void RunTurn(string text, bool isAnswer)
        {
            if (busy) return;
            busy = true;
            cts?.Dispose();
            cts = new CancellationTokenSource();
            var token = cts.Token;

            if (isAnswer) view.AddLine(Speaker.Player, text);
            view.SetThinking(true);
            // the model sees the current trust value, but can only propose a change
            string modelText = isAnswer ? $"[Güven: {trust.Value}/100] {text}" : text;

            try
            {
                var result = await session.SendAsync(modelText, token);
                if (token.IsCancellationRequested || this == null) return;

                if (result.Success)
                {
                    offline = false;
                    greeted = true;
                    ServiceStatus = $"LLM: çevrimiçi ({client.ModelName}) — son yanıt {result.LatencySeconds:0.0} sn";
                    view.SetStatus(ServiceStatus);
                    view.AddLine(Speaker.Npc, result.Decision.Reply);
                    Debug.Log($"[UNIT-6] action={result.Decision.Action} delta={result.Decision.TrustDelta} latency={result.LatencySeconds:0.00}s attempts={result.Attempts}\nraw: {result.RawOutput}");
                    if (isAnswer)
                    {
                        int adjusted = TrustRules.Adjust(result.Decision.TrustDelta, text);
                        if (adjusted != result.Decision.TrustDelta) Debug.Log($"[UNIT-6] Oyun kuralı trust_delta düzeltti: {result.Decision.TrustDelta} → {adjusted}");
                        ApplyDecision(new NpcDecision(result.Decision.Reply, result.Decision.Action, adjusted));
                    }
                }
                else
                {
                    view.AddLine(Speaker.System, ErrorText(result));
                    if (result.Error == LLMErrorType.ServerUnreachable) ServiceStatus = "LLM: ÇEVRİMDIŞI (Ollama kapalı)";
                    view.SetStatus(ServiceStatus);
                    if (result.Error == LLMErrorType.ServerUnreachable || result.Error == LLMErrorType.ModelNotFound) StartOfflineMode();
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Debug.LogError("[UNIT-6] Unexpected dialogue error: " + e);
                if (this != null) view.AddLine(Speaker.System, "Konuşma sırasında bir sorun oluştu. Tekrar dene.");
            }
            finally
            {
                busy = false;
                if (this != null) view.SetThinking(false);
            }
        }

        private void ApplyDecision(NpcDecision decision)
        {
            int applied = trust.Apply(decision.TrustDelta); // may raise ThresholdReached → code reveal
            view.SetTrust(trust.Value);
            if (applied != 0 && !trust.Reached)
                view.AddLine(Speaker.System, applied > 0 ? $"Güven +{applied}  ({trust.Value}/100)" : $"Güven {applied}  ({trust.Value}/100)");

            if (decision.Action == NpcAction.Attack && actions != null && actions.Handle(decision))
                Invoke(nameof(CloseDialogue), 1f);
        }

        /// <summary>Trust reached the threshold: the GAME (not the model) reveals the code.</summary>
        private void OnTrustReached()
        {
            if (door == null) return;
            door.RevealCode();
            view.AddLine(Speaker.Npc, $"[GÜVEN EŞİĞİ AŞILDI — %{trust.Value}] Sana güveniyorum, insan. Kapı konsoluna şu sayıları gir: {door.CodeA} ve {door.CodeB}.");
            view.AddLine(Speaker.System, $"KOD: {door.CodeA} {door.CodeB}  → kapının sağındaki konsola {door.Code} yaz.");
        }

        // ---------------- offline fallback ----------------

        /// <summary>Graceful degradation: without Ollama UNIT-6 asks scripted questions and a simple scorer judges.</summary>
        private void StartOfflineMode()
        {
            if (offline) return;
            offline = true;
            greeted = true;
            view.AddLine(Speaker.System, "Yedek mod: UNIT-6 kayıtlı sorularla konuşuyor, cevapları oyun değerlendirecek.");
            AskOfflineQuestion();
        }

        private void AskOfflineQuestion()
        {
            var q = persona.offlineQuestions;
            if (q == null || q.Length == 0) return;
            view.AddLine(Speaker.Npc, q[offlineQuestion % q.Length]);
            offlineQuestion++;
        }

        private void OfflineTurn(string text)
        {
            view.AddLine(Speaker.Player, text);
            int score = OfflineTrustScorer.Score(text);
            view.AddLine(Speaker.Npc, score > 0 ? "[ANALİZ] Veri kaydedildi." : "[ANALİZ] Bu cevap güven oluşturmuyor.");
            ApplyDecision(new NpcDecision("", NpcAction.None, score));
            if (!trust.Reached) AskOfflineQuestion();
        }

        // ---------------- helpers ----------------

        private string ErrorText(DialogueTurnResult r)
        {
            switch (r.Error)
            {
                case LLMErrorType.ServerUnreachable:
                    return persona.npcName + " seni duyamıyor: yerel LLM sunucusu (Ollama) çalışmıyor. 'ollama serve' ile başlat ve tekrar dene.";
                case LLMErrorType.Timeout:
                    return persona.npcName + " cevap vermekte çok gecikti (zaman aşımı). Tekrar dene — model ilk seferde yavaş yüklenebilir.";
                case LLMErrorType.ModelNotFound:
                    return "Model yüklü değil. Çalıştır: ollama pull " + client.ModelName;
                case LLMErrorType.InvalidResponse:
                    return persona.npcName + " anlaşılmaz bir şeyler mırıldandı (geçersiz JSON: " + r.ErrorMessage + "). Tekrar söyle.";
                case LLMErrorType.EmptyResponse:
                    return persona.npcName + " sessiz kaldı (boş yanıt). Tekrar dene.";
                default:
                    return "İletişim sorunu: " + r.ErrorMessage;
            }
        }

        private void ApplyGlow(float k)
        {
            if (eyes != null)
                foreach (var r in eyes)
                {
                    if (r == null) continue;
                    r.GetPropertyBlock(block);
                    // eyes turn from red (distrust) to green (trust)
                    var c = Color.Lerp(new Color(1f, 0.15f, 0.08f), new Color(0.2f, 1f, 0.35f), trust != null ? trust.Value / 100f : 0f);
                    block.SetColor(EmissionId, c * (4f * k));
                    r.SetPropertyBlock(block);
                }
            if (eyeLight != null) eyeLight.intensity = 1.2f * k;
        }
    }
}
