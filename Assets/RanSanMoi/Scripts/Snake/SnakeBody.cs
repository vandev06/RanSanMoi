using System.Collections.Generic;
using UnityEngine;

namespace MiniGames.Snake
{
    /// <summary>
    /// Con rắn. Danh sách "cells" lưu toạ độ ô của từng đốt, cells[0] là đầu.
    /// Mỗi bước: thêm ô mới phía trước đầu, bỏ ô cuối (đuôi) — trừ khi đang cần dài thêm (grow > 0).
    ///
    /// Đầu rắn mang Rigidbody2D (Dynamic, không trọng lực) + CircleCollider2D để nhận sự kiện
    /// OnTriggerEnter2D khi chạm vào mồi (mồi là Trigger). Các đốt thân chỉ là hình vẽ.
    /// </summary>
    public class SnakeBody : MonoBehaviour
    {
        SnakeGame game;
        readonly List<Vector2Int> cells = new List<Vector2Int>();
        readonly List<SpriteRenderer> segments = new List<SpriteRenderer>();   // segments[i] vẽ ô cells[i + 1]
        readonly Queue<Vector2Int> turns = new Queue<Vector2Int>();             // hàng đợi đổi hướng (tối đa 2)
        Transform segRoot, face;
        Sprite bodySprite;
        Vector2Int dir = Vector2Int.right;
        Vector2Int lastQueued = Vector2Int.right;
        int grow;
        float stepTimer;
        bool dead;

        public int Length => cells.Count;
        public Vector2Int HeadCell => cells[0];
        public Vector2Int Direction => dir;
        public bool Occupies(Vector2Int c) => cells.Contains(c);

        public static SnakeBody Create(Transform parent, SnakeGame game, Vector2Int start, int length)
        {
            var go = Shape.Root("SnakeHead", parent, game.CellToWorld(start));
            var s = go.AddComponent<SnakeBody>();
            s.game = game;
            s.bodySprite = Art.Get("snake/body");

            // Hình đầu: hình tròn xanh + đôi mắt, cả cụm "Face" xoay theo hướng bò
            s.face = new GameObject("Face").transform;
            s.face.SetParent(go.transform, false);
            float c = SnakeGame.Cell;
            var head = Art.Get("snake/head");
            if (head != null) Art.Add(s.face, head, Vector2.zero, c * 1.08f, 12, 0f, "Head");
            else Shape.Add(s.face, SpriteFactory.Circle, Shape.Hex("#3f8f2a"), Vector2.zero, Vector2.one * c * 1.08f, 12, 0f, "Head");
            foreach (float side in new[] { 1f, -1f })
            {
                Shape.Add(s.face, SpriteFactory.Circle, Color.white, new Vector2(c * 0.16f, side * c * 0.22f), Vector2.one * c * 0.32f, 13, 0f, "Eye");
                Shape.Add(s.face, SpriteFactory.Circle, Color.black, new Vector2(c * 0.24f, side * c * 0.22f), Vector2.one * c * 0.16f, 14, 0f, "Pupil");
            }

            // Vật lý: Dynamic (không trọng lực) để chắc chắn nhận được Trigger từ mồi.
            // Collider THƯỜNG, bán kính nhỏ hơn nửa ô để không "chạm" nhầm mồi ở ô bên cạnh.
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = c * 0.3f;
            var rb = Phys.Body(go, 0f);
            rb.interpolation = RigidbodyInterpolation2D.None; // rắn nhảy từng ô: đặt vị trí trực tiếp
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;    // vận tốc luôn = 0: không cho "ngủ" để Trigger luôn được kiểm tra

            s.segRoot = new GameObject("SnakeSegments").transform;
            s.segRoot.SetParent(parent, false);
            for (int i = 0; i < length; i++)
            {
                s.cells.Add(start - new Vector2Int(i, 0));
                if (i > 0) s.AddSegment();
            }
            s.Refresh();
            return s;
        }

        public void Grow(int n) => grow += n;

        void AddSegment()
        {
            float c = SnakeGame.Cell;
            var sr = bodySprite != null
                ? Art.Add(segRoot, bodySprite, Vector2.zero, c * 0.92f, 10, 0f, "Segment")
                : Shape.Add(segRoot, SpriteFactory.Circle, Shape.Hex("#7fc241"), Vector2.zero, Vector2.one * c * 0.92f, 10, 0f, "Segment");
            segments.Add(sr);
        }

        void Update()
        {
            if (dead || !game.IsPlaying) return;
            ReadInput();

            stepTimer += Time.deltaTime;
            float step = game.StepTime;
            if (stepTimer >= step)
            {
                stepTimer = Mathf.Min(stepTimer - step, step);
                Step();
            }
        }

        void ReadInput()
        {
            Vector2Int want = Vector2Int.zero;
            if (GameInput.Down(GKey.Left) || GameInput.Down(GKey.A)) want = Vector2Int.left;
            else if (GameInput.Down(GKey.Right) || GameInput.Down(GKey.D)) want = Vector2Int.right;
            else if (GameInput.Down(GKey.Up) || GameInput.Down(GKey.W)) want = Vector2Int.up;
            else if (GameInput.Down(GKey.Down) || GameInput.Down(GKey.S)) want = Vector2Int.down;
            if (want == Vector2Int.zero || turns.Count >= 2) return;

            // Không cho quay đầu 180° (đâm vào cổ mình) và bỏ qua phím trùng hướng
            Vector2Int last = turns.Count > 0 ? lastQueued : dir;
            if (want == last || want == -last) return;
            turns.Enqueue(want);
            lastQueued = want;
        }

        void Step()
        {
            if (turns.Count > 0) dir = turns.Dequeue();
            Vector2Int next = cells[0] + dir;

            // Đuôi sẽ rời ô của nó trong bước này (nếu không dài thêm) nên đầu được phép đi vào ô đuôi
            bool tailLeaves = grow == 0;
            int checkCount = tailLeaves ? cells.Count - 1 : cells.Count;
            for (int i = 0; i < checkCount; i++)
            {
                if (cells[i] == next) { Die(next, "Rắn tự cắn vào thân!"); return; }
            }
            if (!game.Inside(next)) { Die(next, "Rắn đâm vào tường!"); return; }
            if (game.IsBlocked(next)) { Die(next, "Rắn đâm vào đá!"); return; }

            cells.Insert(0, next);
            if (grow > 0)
            {
                grow--;
                AddSegment();
            }
            else cells.RemoveAt(cells.Count - 1);

            Refresh();
            if (cells.Count >= SnakeGame.WinLength) game.SnakeWon();
        }

        /// <summary>Đặt đầu và các đốt vào đúng ô; đốt về phía đuôi nhỏ dần.</summary>
        void Refresh()
        {
            transform.position = game.CellToWorld(cells[0]);
            face.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            float c = SnakeGame.Cell;
            for (int i = 0; i < segments.Count; i++)
            {
                var t = segments[i].transform;
                t.position = game.CellToWorld(cells[i + 1]);
                float k = segments.Count <= 1 ? 1f : (float)i / (segments.Count - 1);
                t.localScale = Vector3.one * c * Mathf.Lerp(0.92f, 0.6f, k);
                segments[i].sortingOrder = 10 - Mathf.Min(i, 9) / 3;   // đốt gần đầu vẽ đè đốt phía sau
            }
        }

        void Die(Vector2Int at, string reason)
        {
            dead = true;
            SoundFx.Play(Sfx.Hurt);
            Fx.Burst(Vector2.Lerp(transform.position, game.CellToWorld(at), 0.5f), new Color(0.6f, 1f, 0.4f), 18, 4f, game.World);
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>()) sr.color = Color.Lerp(sr.color, Color.gray, 0.6f);
            foreach (var sr in segments) sr.color = Color.Lerp(sr.color, Color.gray, 0.6f);
            game.SnakeDied(reason);
        }

        // ===== TƯƠNG TÁC: đầu rắn đi vào vùng Trigger của mồi =====
        void OnTriggerEnter2D(Collider2D other)
        {
            if (dead) return;
            var food = other.GetComponent<Food>();
            if (food == null || food.eaten) return;
            food.eaten = true;
            game.Eat(food);
        }
    }
}
