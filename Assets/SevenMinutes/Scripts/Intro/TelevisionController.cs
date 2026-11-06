using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Video;
using SevenMinutes.Audio;
using SevenMinutes.Core;
using SevenMinutes.Environment;
using SevenMinutes.Game;

namespace SevenMinutes.Intro
{
    /// <summary>
    /// The hacked TV: lights flicker, static, digital glitches, then the provided video (Assets/Media/IntroductionVideo.mp4)
    /// plays once on the screen with its own audio. When it ends the TV dies and the real countdown starts.
    /// If the video is missing, a placeholder screen explains where to put it.
    /// </summary>
    public class TelevisionController : MonoBehaviour
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Header("Video")]
        [SerializeField] private VideoClip introClip;
        [Tooltip("Fallback: a file with this name inside StreamingAssets is used when no clip is assigned.")]
        [SerializeField] private string streamingFileName = "IntroductionVideo.mp4";
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private AudioSource videoAudio;

        [Header("Screen")]
        [SerializeField] private Renderer screen;
        [SerializeField] private Light screenLight;
        [SerializeField] private GameObject placeholder;
        [SerializeField] private FlickerLight[] roomLights;

        private Material screenMat;
        private RenderTexture renderTexture;
        private Texture2D staticTex;
        private bool prepared, finished, failed, hasVideo;
        private AudioSource staticLoop;

        public bool IsPlaying { get; private set; }

        public void Configure(VideoClip clip, VideoPlayer vp, AudioSource audio, Renderer screenRenderer, Light light,
            GameObject placeholderObject, FlickerLight[] lights)
        {
            introClip = clip; videoPlayer = vp; videoAudio = audio; screen = screenRenderer; screenLight = light;
            placeholder = placeholderObject; roomLights = lights;
        }

        private void Awake()
        {
            screenMat = screen.material;
            staticTex = BuildStatic();
            if (placeholder != null) placeholder.SetActive(false);
            ScreenOff();
            SetupVideo();
        }

        private void Start()
        {
            if (GameManager.Instance != null) GameManager.Instance.StateChanged += OnStateChanged;
        }

        private void OnDestroy()
        {
            if (renderTexture != null) renderTexture.Release();
        }

        private void SetupVideo()
        {
            if (videoPlayer == null) return;
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.skipOnDrop = true;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            renderTexture = new RenderTexture(1280, 720, 0) { name = "TV_RT" };
            renderTexture.Create();
            videoPlayer.targetTexture = renderTexture;
            videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
            videoPlayer.timeUpdateMode = VideoTimeUpdateMode.GameTime;

            if (introClip != null)
            {
                videoPlayer.source = VideoSource.VideoClip;
                videoPlayer.clip = introClip;
                hasVideo = true;
            }
            else
            {
                string path = Path.Combine(Application.streamingAssetsPath, streamingFileName);
                if (File.Exists(path))
                {
                    videoPlayer.source = VideoSource.Url;
                    videoPlayer.url = path;
                    hasVideo = true;
                }
            }

            if (videoAudio != null)
            {
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                videoPlayer.controlledAudioTrackCount = 1;
                videoPlayer.EnableAudioTrack(0, true);
                videoPlayer.SetTargetAudioSource(0, videoAudio);
            }

            videoPlayer.prepareCompleted += _ => prepared = true;
            videoPlayer.loopPointReached += _ => finished = true;
            videoPlayer.errorReceived += (_, msg) =>
            {
                failed = true;
                Debug.LogWarning("[TV] Video error: " + msg);
            };
            if (!hasVideo)
                Debug.LogWarning("[TV] No intro video found. Put your video at Assets/Media/IntroductionVideo.mp4 " +
                                 "(then run 5 MINUTES ▸ Build Game Scene again) or drag it into TelevisionController ▸ Intro Clip.");
        }

        private void OnStateChanged(GameState state)
        {
            if (videoPlayer == null || !IsPlaying) return;
            if (state == GameState.Paused) videoPlayer.Pause();
            else if (state == GameState.Playing || state == GameState.Introduction)
            {
                if (!finished && prepared) videoPlayer.Play();
            }
        }

        public void PlaySequence(Action onFinished) => StartCoroutine(Run(onFinished));

        private IEnumerator Run(Action onFinished)
        {
            IsPlaying = true;
            var audio = AudioManager.Instance;
            if (hasVideo) videoPlayer.Prepare();

            // 1. lights flicker
            foreach (var l in roomLights) if (l != null) l.Burst(1.6f);
            audio?.Play(SoundId.Glitch, transform.position, 0.5f, 0.7f);
            yield return new WaitForSeconds(1.7f);

            // 2. TV switches itself on: static
            if (audio != null) staticLoop = audio.CreateLoop(SoundId.Static, transform, 0.55f, 15f);
            if (screenLight != null) screenLight.enabled = true;
            float t = 0f;
            while (t < 2.2f)
            {
                t += Time.deltaTime;
                ShowStatic(Color.white);
                yield return null;
            }

            // 3. hacked: digital interference
            audio?.Play(SoundId.Glitch, transform.position, 1f);
            t = 0f;
            while (t < 1.2f)
            {
                t += Time.deltaTime;
                float r = UnityEngine.Random.value;
                if (r < 0.3f) ShowStatic(new Color(1f, 0.2f, 0.2f));
                else if (r < 0.5f) ShowStatic(new Color(0.2f, 1f, 0.4f));
                else if (r < 0.65f) ShowSolid(Color.black);
                else ShowStatic(Color.white);
                if (screenLight != null) screenLight.intensity = UnityEngine.Random.Range(0.3f, 1.6f);
                yield return new WaitForSeconds(0.04f);
            }
            if (staticLoop != null) Destroy(staticLoop.gameObject);

            // 4. the video
            float wait = 0f;
            while (hasVideo && !prepared && !failed && wait < 8f)
            {
                wait += Time.deltaTime;
                ShowStatic(new Color(0.6f, 0.6f, 0.6f));
                yield return null;
            }

            if (hasVideo && prepared && !failed)
            {
                screenMat.SetTexture(BaseMapId, renderTexture);
                screenMat.SetTextureScale(BaseMapId, Vector2.one);
                screenMat.SetTextureOffset(BaseMapId, Vector2.zero);
                screenMat.SetColor(BaseColorId, Color.white);
                if (screenLight != null) screenLight.intensity = 1.1f;
                videoPlayer.Play();
                double length = videoPlayer.length > 0 ? videoPlayer.length : 60.0;
                float elapsed = 0f;
                while (!finished && !failed && elapsed < length + 4f)
                {
                    elapsed += Time.deltaTime;
                    if (screenLight != null) screenLight.intensity = 0.9f + 0.2f * Mathf.PerlinNoise(Time.time * 3f, 0f);
                    yield return null;
                }
                videoPlayer.Stop();
            }
            else
            {
                // placeholder: explains where the video must be placed
                if (placeholder != null) placeholder.SetActive(true);
                ShowSolid(new Color(0.05f, 0.0f, 0.0f));
                audio?.Play(SoundId.Stinger, transform.position, 0.6f, 0.8f);
                yield return new WaitForSeconds(6f);
                audio?.Play(SoundId.Laugh, transform.position, 1f);
                yield return new WaitForSeconds(2.5f);
                if (placeholder != null) placeholder.SetActive(false);
            }

            // 5. TV dies
            audio?.Play(SoundId.Glitch, transform.position, 0.8f, 1.2f);
            t = 0f;
            while (t < 0.6f)
            {
                t += Time.deltaTime;
                if (UnityEngine.Random.value < 0.5f) ShowStatic(Color.white); else ShowSolid(Color.black);
                yield return new WaitForSeconds(0.04f);
            }
            ScreenOff();
            yield return new WaitForSeconds(0.9f);
            audio?.Play(SoundId.Stinger, null, 0.8f);
            IsPlaying = false;
            onFinished?.Invoke();
        }

        private void ShowStatic(Color tint)
        {
            screenMat.SetTexture(BaseMapId, staticTex);
            screenMat.SetTextureScale(BaseMapId, new Vector2(1.5f, 1.5f));
            screenMat.SetTextureOffset(BaseMapId, new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));
            screenMat.SetColor(BaseColorId, tint);
            if (screenLight != null) screenLight.color = new Color(0.7f, 0.75f, 0.9f);
        }

        private void ShowSolid(Color c)
        {
            screenMat.SetTexture(BaseMapId, Texture2D.whiteTexture);
            screenMat.SetColor(BaseColorId, c);
        }

        private void ScreenOff()
        {
            ShowSolid(new Color(0.01f, 0.01f, 0.012f));
            if (screenLight != null) screenLight.enabled = false;
        }

        private static Texture2D BuildStatic()
        {
            var tex = new Texture2D(256, 160, TextureFormat.RGB24, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            var rnd = new System.Random(5);
            var px = new Color32[256 * 160];
            for (int i = 0; i < px.Length; i++)
            {
                byte v = (byte)rnd.Next(0, 255);
                if (rnd.Next(100) < 4) v = 255;
                px[i] = new Color32(v, v, v, 255);
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }
    }
}
