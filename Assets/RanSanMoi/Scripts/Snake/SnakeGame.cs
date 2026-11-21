using System.Collections.Generic;
using UnityEngine;

namespace MiniGames.Snake
{
    /// <summary>
    /// GAME 5 – RẮN SĂN MỒI
    ///
    /// Sân chơi là một LƯỚI ô vuông (Cols x Rows, mỗi ô cao Cell đơn vị). Rắn đi từng ô một theo nhịp StepTime.
    /// Đối tượng: SnakeBody (đầu + các đốt thân), Food (trái cây / táo vàng / ngôi sao), Board (nền cỏ + tường), đá.
    /// Tương tác:
    ///   Đầu rắn chạm mồi         → Trigger (OnTriggerEnter2D) → cộng điểm, rắn dài thêm
    ///   Đầu rắn đâm tường/đá/thân → kiểm tra theo LƯỚI ô     → Game Over
    ///   Rắn dài đủ WinLength đốt → Chiến thắng
    /// Vì rắn "nhảy" từng ô chứ không trượt liên tục, va chạm chết được kiểm tra bằng toạ độ ô (chính xác tuyệt đối),
    /// còn việc ăn mồi dùng Trigger của Unity để minh hoạ cơ chế va chạm vật lý.
    /// </summary>
    public class SnakeGame : MiniGameBase
    {
        public override string Title => "Rắn Săn Mồi";
        protected override string SaveKey => "snake";
        protected override Color Background => Shape.Hex("#173021");
        protected override int StartLives => 1;
        protected override bool ShowLives => false;
        protected override string HelpText =>
            "Phím mũi tên / W A S D: đổi hướng bò của rắn.\n\n" +
            "Ăn <color=#ff7a6b>trái cây</color>: +10 điểm, rắn dài thêm 1 đốt\n" +
            "<color=#ffd84a>Táo vàng</color> (có hạn giờ): +50 điểm, dài thêm 3 đốt\n" +
            "<color=#ffe680>Ngôi sao</color>: +20 điểm, rắn bò chậm lại 5 giây\n" +
            "Đâm vào tường, đá hoặc chính mình là thua. Rắn dài " + WinLength + " đốt là thắng!";

        public const float Cell = 0.5f;
        public const int Rows = 16;
        public const int WinLength = 40;
        const int MaxRocks = 10;

        public int Cols { get; private set; }
        Vector2 origin;                 // toạ độ thế giới của tâm ô (0,0)

        SnakeBody snake;
        Food fruit;                     // mồi thường, luôn có đúng 1
        Food bonus;                     // mồi đặc biệt, có hạn giờ
        readonly HashSet<Vector2Int> rocks = new HashSet<Vector2Int>();
        Transform rockRoot;
        int eaten;
        float slowTimer;
        bool over;

        /// <summary>Thời gian giữa 2 bước đi: rắn càng dài càng nhanh, ăn sao thì chậm lại.</summary>
        public float StepTime
        {
            get
            {
                float t = Mathf.Max(0.065f, 0.15f - (snake.Length - 3) * 0.0025f);
                return slowTimer > 0f ? t * 1.7f : t;
            }
        }

        public Vector2 CellToWorld(Vector2Int c) => origin + new Vector2(c.x, c.y) * Cell;
        public bool Inside(Vector2Int c) => c.x >= 0 && c.x < Cols && c.y >= 0 && c.y < Rows;
        /// <summary>Ô bị chặn: ngoài sân (tường) hoặc có đá.</summary>
        public bool IsBlocked(Vector2Int c) => !Inside(c) || rocks.Contains(c);

        protected override void BuildWorld()
        {
            rocks.Clear();
            eaten = 0;
            slowTimer = 0f;
            over = false;
            fruit = bonus = null;

            Cols = Mathf.Clamp(Mathf.FloorToInt((halfW * 2f - 0.9f) / Cell), 16, 32);
            var size = new Vector2(Cols * Cell, Rows * Cell);
            var center = new Vector2(0f, -0.55f);
            origin = center - size / 2f + Vector2.one * (Cell / 2f);

            Board.Create(world, center, size);
            rockRoot = new GameObject("Rocks").transform;
            rockRoot.SetParent(world, false);
            snake = SnakeBody.Create(world, this, new Vector2Int(Cols / 4, Rows / 2), 3);
            SpawnFruit();
        }

        protected override void Tick()
        {
            if (slowTimer > 0f) slowTimer -= Time.deltaTime;
            if (fruit == null && !over) SpawnFruit();   // lỡ chưa tìm được ô trống thì thử lại
            if (bonus != null && bonus.Expired)
            {
                Destroy(bonus.gameObject);
                bonus = null;
            }
        }

        // ---------- Sinh mồi, đá ----------

        /// <summary>Tìm ngẫu nhiên một ô trống (không có rắn, đá, mồi), cách đầu rắn ít nhất minDist ô.</summary>
        bool RandomFreeCell(int minDist, out Vector2Int cell)
        {
            for (int i = 0; i < 300; i++)
            {
                var c = new Vector2Int(Random.Range(0, Cols), Random.Range(0, Rows));
                if (IsBlocked(c) || snake.Occupies(c)) continue;
                if (fruit != null && fruit.cell == c) continue;
                if (bonus != null && bonus.cell == c) continue;
                var d = c - snake.HeadCell;
                if (Mathf.Abs(d.x) + Mathf.Abs(d.y) < minDist) continue;
                cell = c;
                return true;
            }
            cell = default;
            return false;
        }

        void SpawnFruit()
        {
            if (RandomFreeCell(2, out var c)) fruit = Food.Create(world, this, c, FoodKind.Fruit);
        }

        void SpawnBonus()
        {
            if (bonus != null) return;
            var kind = (eaten / 4) % 2 == 1 ? FoodKind.Golden : FoodKind.Star;
            if (RandomFreeCell(4, out var c)) bonus = Food.Create(world, this, c, kind);
        }

        void AddRock()
        {
            if (rocks.Count >= MaxRocks || !RandomFreeCell(5, out var c)) return;
            // Không đặt đá chắn ngay trước mặt rắn
            var ahead = snake.HeadCell + snake.Direction;
            if (c == ahead || c == ahead + snake.Direction) return;
            rocks.Add(c);
            var pos = CellToWorld(c);
            var rock = Art.Get("snake/rock");
            if (rock != null) Art.Add(rockRoot, rock, pos, Cell * 0.9f, 3, Random.Range(0f, 360f), "Rock");
            else Shape.Add(rockRoot, SpriteFactory.Circle, Shape.Hex("#8a9aa3"), pos, Vector2.one * Cell * 0.9f, 3, 0f, "Rock");
            Fx.Burst(pos, new Color(0.7f, 0.75f, 0.8f), 8, 2f, world, 0.4f, 0.1f, 0f, false);
        }

        // ---------- Gọi từ SnakeBody ----------

        /// <summary>Đầu rắn chạm mồi (Trigger).</summary>
        public void Eat(Food f)
        {
            if (!IsPlaying) return;
            Vector3 at = f.transform.position + Vector3.up * 0.4f;
            switch (f.kind)
            {
                case FoodKind.Fruit:
                    AddScore(10, at);
                    snake.Grow(1);
                    SoundFx.Play(Sfx.Coin, 0.8f);
                    Fx.Burst(f.transform.position, new Color(1f, 0.45f, 0.35f), 10, 2.5f, world, 0.4f, 0.12f);
                    fruit = null;
                    eaten++;
                    SpawnFruit();
                    if (eaten % 4 == 0) SpawnBonus();
                    if (eaten % 5 == 0) AddRock();
                    break;
                case FoodKind.Golden:
                    AddScore(50, at, Color.yellow);
                    snake.Grow(3);
                    SoundFx.Play(Sfx.PowerUp);
                    Fx.Burst(f.transform.position, Color.yellow, 16, 3.5f, world);
                    bonus = null;
                    break;
                case FoodKind.Star:
                    AddScore(20, at);
                    slowTimer = 5f;
                    ShowPopup(at + Vector3.up * 0.5f, "Chậm lại!", new Color(0.6f, 0.9f, 1f));
                    SoundFx.Play(Sfx.PowerUp);
                    Fx.Burst(f.transform.position, new Color(1f, 0.95f, 0.5f), 16, 3.5f, world);
                    bonus = null;
                    break;
            }
            Destroy(f.gameObject);
        }

        public void SnakeDied(string reason)
        {
            if (over) return;
            over = true;
            EndGame(false, 1.2f, reason);
        }

        public void SnakeWon()
        {
            if (over) return;
            over = true;
            AddScore(200, snake.transform.position + Vector3.up * 0.6f, Color.green);
            EndGame(true, 1f, "Rắn đã dài " + WinLength + " đốt!");
        }

        protected override void DrawHud()
        {
            if (snake == null) return;
            UI.Center(10, "Độ dài: " + snake.Length + " / " + WinLength, 28, Color.white, 40);
            if (slowTimer > 0f)
                UI.Center(44, "Chậm lại: " + Mathf.CeilToInt(slowTimer) + " giây", 20, new Color(0.6f, 0.9f, 1f), 30);
        }
    }
}
