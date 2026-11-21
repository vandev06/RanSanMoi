// Đọc phím bấm, chạy được với CẢ HAI hệ thống input của Unity:
//  - Input Manager cũ (UnityEngine.Input)
//  - Input System mới (gói com.unity.inputsystem, mặc định ở Unity 6)
// Nhờ vậy project không bị lỗi "You are trying to read Input using the UnityEngine.Input class..."
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
#define MG_NEW_INPUT
#endif

using UnityEngine;
#if MG_NEW_INPUT
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace MiniGames
{
    public enum GKey
    {
        Left, Right, Up, Down, A, D, W, S,
        Space, Enter, Escape, P, R, M, J,
        Alpha1, Alpha2, Alpha3, Alpha4
    }

    public static class GameInput
    {
#if MG_NEW_INPUT
        static KeyControl Ctl(GKey k)
        {
            var kb = Keyboard.current;
            if (kb == null) return null;
            switch (k)
            {
                case GKey.Left: return kb.leftArrowKey;
                case GKey.Right: return kb.rightArrowKey;
                case GKey.Up: return kb.upArrowKey;
                case GKey.Down: return kb.downArrowKey;
                case GKey.A: return kb.aKey;
                case GKey.D: return kb.dKey;
                case GKey.W: return kb.wKey;
                case GKey.S: return kb.sKey;
                case GKey.Space: return kb.spaceKey;
                case GKey.Enter: return kb.enterKey;
                case GKey.Escape: return kb.escapeKey;
                case GKey.P: return kb.pKey;
                case GKey.R: return kb.rKey;
                case GKey.M: return kb.mKey;
                case GKey.J: return kb.jKey;
                case GKey.Alpha1: return kb.digit1Key;
                case GKey.Alpha2: return kb.digit2Key;
                case GKey.Alpha3: return kb.digit3Key;
                case GKey.Alpha4: return kb.digit4Key;
            }
            return null;
        }

        public static bool Held(GKey k) { var c = Ctl(k); return c != null && c.isPressed; }
        public static bool Down(GKey k) { var c = Ctl(k); return c != null && c.wasPressedThisFrame; }
        public static bool Up(GKey k) { var c = Ctl(k); return c != null && c.wasReleasedThisFrame; }
        public static bool MouseHeld => Mouse.current != null && Mouse.current.leftButton.isPressed;
        public static bool MouseDown => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        public static Vector2 MousePos => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        static KeyCode Code(GKey k)
        {
            switch (k)
            {
                case GKey.Left: return KeyCode.LeftArrow;
                case GKey.Right: return KeyCode.RightArrow;
                case GKey.Up: return KeyCode.UpArrow;
                case GKey.Down: return KeyCode.DownArrow;
                case GKey.A: return KeyCode.A;
                case GKey.D: return KeyCode.D;
                case GKey.W: return KeyCode.W;
                case GKey.S: return KeyCode.S;
                case GKey.Space: return KeyCode.Space;
                case GKey.Enter: return KeyCode.Return;
                case GKey.Escape: return KeyCode.Escape;
                case GKey.P: return KeyCode.P;
                case GKey.R: return KeyCode.R;
                case GKey.M: return KeyCode.M;
                case GKey.J: return KeyCode.J;
                case GKey.Alpha1: return KeyCode.Alpha1;
                case GKey.Alpha2: return KeyCode.Alpha2;
                case GKey.Alpha3: return KeyCode.Alpha3;
                case GKey.Alpha4: return KeyCode.Alpha4;
            }
            return KeyCode.None;
        }

        public static bool Held(GKey k) => Input.GetKey(Code(k));
        public static bool Down(GKey k) => Input.GetKeyDown(Code(k));
        public static bool Up(GKey k) => Input.GetKeyUp(Code(k));
        public static bool MouseHeld => Input.GetMouseButton(0);
        public static bool MouseDown => Input.GetMouseButtonDown(0);
        public static Vector2 MousePos => Input.mousePosition;
#endif

        // ---- Các "hành động" dùng trong game ----

        /// <summary>-1 (trái) .. 1 (phải): phím mũi tên hoặc A/D.</summary>
        public static float Horizontal()
        {
            float h = 0f;
            if (Held(GKey.Left) || Held(GKey.A)) h -= 1f;
            if (Held(GKey.Right) || Held(GKey.D)) h += 1f;
            return h;
        }

        /// <summary>-1 (xuống) .. 1 (lên): phím mũi tên hoặc W/S.</summary>
        public static float Vertical()
        {
            float v = 0f;
            if (Held(GKey.Down) || Held(GKey.S)) v -= 1f;
            if (Held(GKey.Up) || Held(GKey.W)) v += 1f;
            return v;
        }

        public static bool JumpPressed() => Down(GKey.Space) || Down(GKey.Up) || Down(GKey.W);
        public static bool JumpHeld() => Held(GKey.Space) || Held(GKey.Up) || Held(GKey.W);
        public static bool FireHeld() => Held(GKey.Space) || Held(GKey.J) || MouseHeld;
        public static bool FlapPressed() => Down(GKey.Space) || Down(GKey.Up) || Down(GKey.W) || MouseDown;
        public static bool PausePressed() => Down(GKey.Escape) || Down(GKey.P);
        public static bool ConfirmPressed() => Down(GKey.Space) || Down(GKey.Enter);
    }
}
