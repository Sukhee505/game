using UnityEngine;

namespace SevenMinutes.UI
{
    /// <summary>Shared horror-themed IMGUI styles (built once per resolution).</summary>
    public static class UIStyles
    {
        private static float builtFor = -1f;

        public static float Scale { get; private set; } = 1f;
        public static GUIStyle Label, LabelSmall, LabelCenter, Title, Subtitle, Timer, Button, Panel, Paper, PaperText,
            Terminal, TerminalInput, Prompt, RobotLine, Keypad, KeypadDisplay, Slider, Thumb;

        public static Texture2D White { get; private set; }
        private static Texture2D panelTex, buttonTex, buttonHover, paperTex, terminalTex, promptTex;

        public static readonly Color Blood = new Color(0.75f, 0.06f, 0.05f);
        public static readonly Color Bone = new Color(0.86f, 0.84f, 0.78f);
        public static readonly Color RobotBlue = new Color(0.45f, 0.78f, 1f);

        public static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        public static void Ensure()
        {
            float s = Mathf.Max(0.7f, Screen.height / 1080f);
            if (Mathf.Approximately(s, builtFor) && Label != null && White != null) return;
            builtFor = s;
            Scale = s;

            White = Solid(Color.white);
            panelTex = Solid(new Color(0.03f, 0.03f, 0.04f, 0.88f));
            buttonTex = Solid(new Color(0.09f, 0.08f, 0.08f, 0.95f));
            buttonHover = Solid(new Color(0.35f, 0.04f, 0.03f, 0.95f));
            paperTex = Solid(new Color(0.88f, 0.85f, 0.74f, 1f));
            terminalTex = Solid(new Color(0.01f, 0.04f, 0.02f, 0.97f));
            promptTex = Solid(new Color(0f, 0f, 0f, 0.6f));

            Label = new GUIStyle(GUI.skin.label) { fontSize = Px(22), richText = true, wordWrap = true };
            Label.normal.textColor = Bone;
            LabelSmall = new GUIStyle(Label) { fontSize = Px(17) };
            LabelCenter = new GUIStyle(Label) { alignment = TextAnchor.MiddleCenter };
            Title = new GUIStyle(LabelCenter) { fontSize = Px(78), fontStyle = FontStyle.Bold, wordWrap = false };
            Title.normal.textColor = Blood;
            Subtitle = new GUIStyle(LabelCenter) { fontSize = Px(30), fontStyle = FontStyle.Bold };
            Timer = new GUIStyle(LabelCenter) { fontSize = Px(46), fontStyle = FontStyle.Bold, wordWrap = false };

            Button = new GUIStyle(GUI.skin.button) { fontSize = Px(26), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            Button.normal.background = buttonTex;
            Button.hover.background = buttonHover;
            Button.active.background = buttonHover;
            Button.focused.background = buttonTex;
            Button.normal.textColor = Bone;
            Button.hover.textColor = Color.white;
            Button.active.textColor = Color.white;
            Button.border = new RectOffset(0, 0, 0, 0);

            Panel = new GUIStyle(GUI.skin.box) { padding = new RectOffset(Px(24), Px(24), Px(18), Px(18)) };
            Panel.normal.background = panelTex;

            Paper = new GUIStyle(GUI.skin.box) { padding = new RectOffset(Px(40), Px(40), Px(34), Px(30)) };
            Paper.normal.background = paperTex;
            PaperText = new GUIStyle(Label) { fontSize = Px(25), fontStyle = FontStyle.Italic };
            PaperText.normal.textColor = new Color(0.12f, 0.1f, 0.09f);

            Terminal = new GUIStyle(GUI.skin.box) { padding = new RectOffset(Px(22), Px(22), Px(18), Px(18)), alignment = TextAnchor.UpperLeft };
            Terminal.normal.background = terminalTex;
            TerminalInput = new GUIStyle(GUI.skin.textField) { fontSize = Px(24), padding = new RectOffset(Px(10), Px(10), Px(8), Px(8)) };
            TerminalInput.normal.textColor = new Color(0.4f, 1f, 0.5f);
            TerminalInput.focused.textColor = new Color(0.5f, 1f, 0.6f);
            TerminalInput.normal.background = Solid(new Color(0f, 0.12f, 0.04f));
            TerminalInput.focused.background = TerminalInput.normal.background;

            Prompt = new GUIStyle(LabelCenter) { fontSize = Px(22), fontStyle = FontStyle.Bold, wordWrap = false, padding = new RectOffset(Px(14), Px(14), Px(6), Px(6)) };
            Prompt.normal.background = promptTex;
            Prompt.normal.textColor = Color.white;

            RobotLine = new GUIStyle(Label) { fontSize = Px(24), alignment = TextAnchor.MiddleLeft };
            RobotLine.normal.textColor = RobotBlue;

            Keypad = new GUIStyle(Button) { fontSize = Px(34) };
            KeypadDisplay = new GUIStyle(LabelCenter) { fontSize = Px(48), fontStyle = FontStyle.Bold, wordWrap = false };
            KeypadDisplay.normal.background = Solid(new Color(0.02f, 0.06f, 0.03f));
            KeypadDisplay.normal.textColor = new Color(0.4f, 1f, 0.5f);

            foreach (var st in new[] { Label, LabelSmall, LabelCenter, Title, Subtitle, Timer, Panel, Paper, PaperText, Terminal, Prompt, RobotLine, KeypadDisplay })
                Lock(st);

            Slider = new GUIStyle(GUI.skin.horizontalSlider);
            Thumb = new GUIStyle(GUI.skin.horizontalSliderThumb);
        }

        /// <summary>Keeps text colour and background identical in every state (no white text on mouse hover).</summary>
        public static GUIStyle Lock(GUIStyle s)
        {
            var c = s.normal.textColor;
            var bg = s.normal.background;
            foreach (var st in new[] { s.hover, s.active, s.focused, s.onNormal, s.onHover, s.onActive, s.onFocused })
            {
                st.textColor = c;
                st.background = bg;
            }
            return s;
        }

        public static int Px(float v) => Mathf.RoundToInt(v * Scale);

        public static Rect Centered(float w, float h) =>
            new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);

        public static void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, White);
            GUI.color = old;
        }
    }
}
