using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniGames
{
    /// <summary>
    /// Tạo các hình cơ bản (vuông, tròn, tam giác, tim, ngôi sao) bằng code,
    /// nên project không cần file ảnh nào. Mỗi sprite có kích thước 1x1 đơn vị Unity.
    /// Muốn dùng ảnh thật: thay Sprite ở hàm Shape.Add bằng sprite của bạn.
    /// </summary>
    public static class SpriteFactory
    {
        const int Size = 64;
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Square => Get("square", (u, v) => true);
        public static Sprite Circle => Get("circle", (u, v) => (u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f) <= 0.25f);

        /// <summary>Tam giác cân, đỉnh hướng lên trên.</summary>
        public static Sprite Triangle => Get("triangle", (u, v) => Mathf.Abs(u - 0.5f) <= (1f - v) * 0.5f);

        public static Sprite Heart => Get("heart", (u, v) =>
        {
            float x = (u - 0.5f) * 2.4f;
            float y = (v - 0.45f) * 2.4f;
            float a = x * x + y * y - 1f;
            return a * a * a - x * x * y * y * y <= 0f;
        });

        public static Sprite Star => Get("star", (u, v) =>
        {
            float x = u - 0.5f, y = v - 0.5f;
            float r = Mathf.Sqrt(x * x + y * y);
            float ang = Mathf.Atan2(y, x) - Mathf.PI / 2f;
            float k = Mathf.Cos(5f * ang); // 5 cánh
            float edge = Mathf.Lerp(0.22f, 0.5f, (k + 1f) * 0.5f);
            return r <= edge;
        });

        static Sprite Get(string key, Func<float, float, bool> inside)
        {
            if (cache.TryGetValue(key, out var s) && s != null) return s;

            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[Size * Size];
            const int ss = 4; // khử răng cưa: lấy 4x4 mẫu mỗi pixel
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int hit = 0;
                    for (int sy = 0; sy < ss; sy++)
                        for (int sx = 0; sx < ss; sx++)
                        {
                            float u = (x + (sx + 0.5f) / ss) / Size;
                            float v = (y + (sy + 0.5f) / ss) / Size;
                            if (inside(u, v)) hit++;
                        }
                    byte a = (byte)(255 * hit / (ss * ss));
                    pixels[y * Size + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            s = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
            s.name = key;
            cache[key] = s;
            return s;
        }

        // ---- Vật liệu (material) cho SpriteRenderer ----
        // Với URP 2D, sprite mặc định là "Lit" và sẽ tối đen nếu scene không có đèn 2D.
        // Ta dùng shader "Unlit" để hình luôn hiện đúng màu ở mọi template.
        static Material spriteMat;
        static bool matChecked;

        public static Material SpriteMaterial
        {
            get
            {
                if (!matChecked)
                {
                    matChecked = true;
                    if (GraphicsSettings.currentRenderPipeline != null)
                    {
                        Shader sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                        if (sh == null) sh = Shader.Find("Sprites/Default");
                        if (sh != null) spriteMat = new Material(sh);
                    }
                }
                return spriteMat;
            }
        }
    }

    /// <summary>Hàm tiện ích để ghép hình: tạo một GameObject có SpriteRenderer.</summary>
    public static class Shape
    {
        public static SpriteRenderer Add(Transform parent, Sprite sprite, Color color,
            Vector2 localPos, Vector2 size, int order = 0, float rotation = 0f, string name = "Shape")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            var mat = SpriteFactory.SpriteMaterial;
            if (mat != null) sr.sharedMaterial = mat;
            return sr;
        }

        /// <summary>Tạo một GameObject rỗng làm "gốc" cho đối tượng, đặt tại vị trí pos.</summary>
        public static GameObject Root(string name, Transform parent, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            return go;
        }

        public static Color Hex(string hex)
        {
            Color c;
            return ColorUtility.TryParseHtmlString(hex, out c) ? c : Color.magenta;
        }
    }

    /// <summary>Tiện ích vật lý, tương thích Unity 2021 → Unity 6.</summary>
    public static class Phys
    {
        public static Rigidbody2D Body(GameObject go, float gravityScale = 0f,
            RigidbodyType2D type = RigidbodyType2D.Dynamic, bool freezeRotation = true)
        {
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = type;
            rb.gravityScale = gravityScale;
            if (freezeRotation) rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            return rb;
        }

        public static Vector2 GetVel(this Rigidbody2D rb)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }

        public static void SetVel(this Rigidbody2D rb, Vector2 v)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
        }
    }
}
