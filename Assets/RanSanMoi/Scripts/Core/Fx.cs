using System.Collections.Generic;
using UnityEngine;

namespace MiniGames
{
    /// <summary>
    /// Hiệu ứng nổ / tóe hạt đơn giản: sinh ra nhiều hạt tròn nhỏ bay tỏa ra rồi mờ dần.
    /// Không dùng vật lý, chỉ cập nhật vị trí trong Update để nhẹ máy.
    /// </summary>
    public class Fx : MonoBehaviour
    {
        class Particle
        {
            public SpriteRenderer sr;
            public Vector2 vel;
            public float size;
        }

        readonly List<Particle> parts = new List<Particle>();
        float life, age;
        float gravity;
        SpriteRenderer flash;

        public static void Burst(Vector2 pos, Color color, int count = 14, float speed = 4f,
            Transform parent = null, float life = 0.55f, float size = 0.18f, float gravity = 0f, bool withFlash = true)
        {
            var go = new GameObject("Fx");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var fx = go.AddComponent<Fx>();
            fx.life = life;
            fx.gravity = gravity;

            if (withFlash)
                fx.flash = Shape.Add(go.transform, SpriteFactory.Circle, new Color(1f, 1f, 0.85f, 0.9f),
                    Vector2.zero, Vector2.one * size * 3f, 50, 0f, "Flash");

            for (int i = 0; i < count; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                float sp = speed * Random.Range(0.4f, 1f);
                float s = size * Random.Range(0.6f, 1.3f);
                var c = Color.Lerp(color, Color.white, Random.Range(0f, 0.35f));
                var sr = Shape.Add(go.transform, SpriteFactory.Circle, c, Vector2.zero, Vector2.one * s, 49, 0f, "P");
                fx.parts.Add(new Particle { sr = sr, vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * sp, size = s });
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            float t = Mathf.Clamp01(age / life);
            foreach (var p in parts)
            {
                p.vel.y -= gravity * dt;
                p.sr.transform.localPosition += (Vector3)(p.vel * dt);
                var c = p.sr.color;
                c.a = 1f - t;
                p.sr.color = c;
                p.sr.transform.localScale = Vector3.one * p.size * (1f - t * 0.5f);
            }
            if (flash != null)
            {
                float ft = Mathf.Clamp01(age / 0.15f);
                flash.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.4f, ft);
                var fc = flash.color;
                fc.a = 0.9f * (1f - ft);
                flash.color = fc;
            }
            if (age >= life) Destroy(gameObject);
        }
    }

    /// <summary>Chữ nổi "+10" bay lên rồi biến mất (vẽ bằng OnGUI trong MiniGameBase).</summary>
    public class Popup
    {
        public Vector3 worldPos;
        public string text;
        public Color color;
        public float age;
        public const float Life = 0.9f;
    }
}
