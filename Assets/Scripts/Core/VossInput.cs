using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Voss.Core
{
    /// <summary>
    /// Single input abstraction so the project compiles with either the legacy Input Manager
    /// or the Input System package (Project Settings > Player > Active Input Handling).
    /// </summary>
    public static class VossInput
    {
        public static Vector2 Move()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return Vector2.zero;
            float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
            float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
#else
            return Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
#endif
        }

        /// <summary>Mouse delta normalised to roughly the legacy "Mouse X/Y" axis scale.</summary>
        public static Vector2 LookDelta()
        {
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current;
            if (m == null) return Vector2.zero;
            return m.delta.ReadValue() * 0.05f;
#else
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
        }

        public static bool SprintHeld()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
#else
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
        }

        public static bool InteractPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            var m = Mouse.current;
            return (kb != null && kb.eKey.wasPressedThisFrame) || (m != null && m.leftButton.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0);
#endif
        }

        public static bool BackPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            var m = Mouse.current;
            return (kb != null && kb.escapeKey.wasPressedThisFrame) || (m != null && m.rightButton.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1);
#endif
        }

        public static bool EvidencePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb.tabKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Tab);
#endif
        }

        public static bool AnyKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            var m = Mouse.current;
            return (kb != null && kb.anyKey.wasPressedThisFrame) || (m != null && m.leftButton.wasPressedThisFrame);
#else
            return Input.anyKeyDown;
#endif
        }

        public static bool LeftMouseHeld()
        {
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current;
            return m != null && m.leftButton.isPressed;
#else
            return Input.GetMouseButton(0);
#endif
        }
    }
}
