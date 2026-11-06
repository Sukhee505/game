using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SevenMinutes.Core
{
    public enum GameKey
    {
        Forward,
        Back,
        Left,
        Right,
        Run,
        Jump,
        Crouch,
        Interact,
        Pause
    }

    /// <summary>
    /// Thin input layer that works with the new Input System (Unity 6 default) and with the legacy Input Manager.
    /// </summary>
    public static class InputReader
    {
        public static Vector2 Move
        {
            get
            {
                float x = (Held(GameKey.Right) ? 1f : 0f) - (Held(GameKey.Left) ? 1f : 0f);
                float y = (Held(GameKey.Forward) ? 1f : 0f) - (Held(GameKey.Back) ? 1f : 0f);
                var v = new Vector2(x, y);
                return v.sqrMagnitude > 1f ? v.normalized : v;
            }
        }

        /// <summary>Mouse movement in "legacy axis" units (≈ pixels * 0.1).</summary>
        public static Vector2 Look
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var m = Mouse.current;
                return m != null ? m.delta.ReadValue() * 0.1f : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
#else
                return Vector2.zero;
#endif
            }
        }

        public static bool Held(GameKey key)
        {
#if ENABLE_INPUT_SYSTEM
            var c = Control(key);
            return c != null && c.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(Legacy(key));
#else
            return false;
#endif
        }

        public static bool Down(GameKey key)
        {
#if ENABLE_INPUT_SYSTEM
            var c = Control(key);
            return c != null && c.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(Legacy(key));
#else
            return false;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static UnityEngine.InputSystem.Controls.KeyControl Control(GameKey key)
        {
            var k = Keyboard.current;
            if (k == null) return null;
            switch (key)
            {
                case GameKey.Forward: return k.wKey;
                case GameKey.Back: return k.sKey;
                case GameKey.Left: return k.aKey;
                case GameKey.Right: return k.dKey;
                case GameKey.Run: return k.leftShiftKey;
                case GameKey.Jump: return k.spaceKey;
                case GameKey.Crouch: return k.leftCtrlKey;
                case GameKey.Interact: return k.eKey;
                case GameKey.Pause: return k.escapeKey;
                default: return null;
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        private static KeyCode Legacy(GameKey key)
        {
            switch (key)
            {
                case GameKey.Forward: return KeyCode.W;
                case GameKey.Back: return KeyCode.S;
                case GameKey.Left: return KeyCode.A;
                case GameKey.Right: return KeyCode.D;
                case GameKey.Run: return KeyCode.LeftShift;
                case GameKey.Jump: return KeyCode.Space;
                case GameKey.Crouch: return KeyCode.LeftControl;
                case GameKey.Interact: return KeyCode.E;
                case GameKey.Pause: return KeyCode.Escape;
                default: return KeyCode.None;
            }
        }
#endif
    }
}
