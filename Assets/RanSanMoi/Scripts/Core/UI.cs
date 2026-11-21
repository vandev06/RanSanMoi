using UnityEngine;

namespace MiniGames
{
    /// <summary>
    /// Giao diện vẽ bằng IMGUI (OnGUI): không cần Canvas, font hay prefab.
    /// Mọi kích thước tính theo màn hình cao 720px rồi tự co giãn.
    /// </summary>
    public static class UI
    {
        public static float S => Mathf.Max(0.5f, Screen.height / 720f);
        public static float W => Screen.width / S;   // chiều rộng "ảo"
        public const float H = 720f;                 // chiều cao "ảo"

        static GUIStyle label, button;
        static Texture2D white;

        static void Ensure()
        {
            if (white == null)
            {
                white = new Texture2D(1, 1);
                white.SetPixel(0, 0, Color.white);
                white.Apply();
            }
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontStyle = FontStyle.Bold };
                button = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
            }
        }

        /// <summary>Gọi ở đầu OnGUI: chuyển sang hệ toạ độ ảo cao 720.</summary>
        public static void Begin()
        {
            Ensure();
            GUI.matrix = Matrix4x4.Scale(new Vector3(S, S, 1f));
        }

        public static void Text(Rect r, string text, int size, Color color, TextAnchor align = TextAnchor.UpperLeft, bool shadow = true)
        {
            label.fontSize = size;
            label.alignment = align;
            if (shadow)
            {
                label.normal.textColor = new Color(0f, 0f, 0f, 0.6f * color.a);
                GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, label);
            }
            label.normal.textColor = color;
            GUI.Label(r, text, label);
        }

        /// <summary>Chữ căn giữa theo chiều ngang, tại toạ độ y.</summary>
        public static void Center(float y, string text, int size, Color color, float height = 60f)
        {
            Text(new Rect(0, y, W, height), text, size, color, TextAnchor.MiddleCenter);
        }

        public static void Panel(Rect r, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(r, white);
            GUI.color = old;
        }

        /// <summary>Phủ mờ toàn màn hình.</summary>
        public static void Dim(float alpha = 0.55f) => Panel(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, alpha));

        public static bool Button(Rect r, string text, int size = 26)
        {
            button.fontSize = size;
            bool clicked = GUI.Button(r, text, button);
            if (clicked) SoundFx.Play(Sfx.Click);
            return clicked;
        }

        /// <summary>Hình chữ nhật căn giữa màn hình theo chiều ngang.</summary>
        public static Rect CenterRect(float y, float w, float h) => new Rect((W - w) / 2f, y, w, h);

        /// <summary>Đổi toạ độ thế giới sang toạ độ GUI ảo (để vẽ chữ nổi "+10").</summary>
        public static bool WorldToGui(Camera cam, Vector3 world, out Vector2 gui)
        {
            gui = Vector2.zero;
            if (cam == null) return false;
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z < 0) return false;
            gui = new Vector2(sp.x / S, (Screen.height - sp.y) / S);
            return true;
        }
    }
}
