using System.Collections.Generic;
using UnityEngine;

namespace MiniGames
{
    /// <summary>
    /// Lớp cha của game. Lo các phần chung:
    ///  - Trạng thái game: Chờ bắt đầu → Đang chơi ⇄ Tạm dừng → Thua / Thắng
    ///  - Điểm, mạng, kỷ lục (lưu bằng PlayerPrefs)
    ///  - HUD và các bảng Tạm dừng / Game Over (vẽ bằng OnGUI)
    /// Mỗi game con chỉ cần viết BuildWorld() (dựng màn chơi) và Tick() (logic mỗi khung hình).
    /// </summary>
    public abstract class MiniGameBase : MonoBehaviour
    {
        public enum State { Ready, Playing, Paused, GameOver, Win }

        public State state { get; private set; }
        public int score { get; protected set; }
        public int lives { get; protected set; }
        public int highScore { get; private set; }

        public Launcher launcher;
        protected Camera cam;
        protected Transform world;        // mọi đối tượng sinh ra trong màn chơi nằm dưới đây
        protected float halfW, halfH;     // nửa chiều rộng / cao vùng camera nhìn thấy

        public float HalfW => halfW;
        public float HalfH => halfH;
        public Transform World => world;
        public Camera Cam => cam;

        readonly List<Popup> popups = new List<Popup>();
        float pendingEnd = -1f;
        bool pendingWin;
        bool newRecord;
        int beginFrame = -1;

        /// <summary>True đúng ở khung hình người chơi vừa bấm bắt đầu (để phím Space không bị tính 2 lần).</summary>
        public bool JustBegan => Time.frameCount == beginFrame;
        protected string endMessage = "";

        // ---- Các game con khai báo ----
        public abstract string Title { get; }
        protected abstract string SaveKey { get; }
        protected abstract string HelpText { get; }
        protected abstract Color Background { get; }
        protected virtual int StartLives => 3;
        protected virtual int MaxLives => 5;
        protected virtual bool ShowLives => true;
        protected abstract void BuildWorld();
        protected abstract void Tick();
        protected virtual void OnBegin() { }
        protected virtual void DrawHud() { }

        public static string HighScoreKey(string saveKey) => "MG_HS_" + saveKey;

        protected virtual void Start()
        {
            cam = Launcher.EnsureCamera();
            cam.backgroundColor = Background;
            highScore = PlayerPrefs.GetInt(HighScoreKey(SaveKey), 0);
            Restart();
        }

        public void Restart()
        {
            if (world != null) Destroy(world.gameObject);
            world = new GameObject("World").transform;
            world.SetParent(transform, false);
            popups.Clear();
            pendingEnd = -1f;
            endMessage = "";
            newRecord = false;
            score = 0;
            lives = StartLives;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            UpdateBounds();
            BuildWorld();
            state = State.Ready;
            Time.timeScale = 0f; // đứng hình cho tới khi người chơi bấm bắt đầu
        }

        void UpdateBounds()
        {
            halfH = cam.orthographicSize;
            halfW = halfH * cam.aspect;
        }

        protected virtual void Update()
        {
            UpdateBounds();

            for (int i = popups.Count - 1; i >= 0; i--)
            {
                popups[i].age += Time.unscaledDeltaTime;
                if (popups[i].age > Popup.Life) popups.RemoveAt(i);
            }

            switch (state)
            {
                case State.Ready:
                    if (GameInput.ConfirmPressed()) Begin();
                    break;
                case State.Playing:
                    if (GameInput.PausePressed()) { SetPaused(true); break; }
                    Tick();
                    if (pendingEnd >= 0f)
                    {
                        pendingEnd -= Time.deltaTime;
                        if (pendingEnd < 0f) FinishNow(pendingWin);
                    }
                    break;
                case State.Paused:
                    if (GameInput.PausePressed()) SetPaused(false);
                    break;
                case State.GameOver:
                case State.Win:
                    if (GameInput.Down(GKey.R) || GameInput.Down(GKey.Enter)) Restart();
                    else if (GameInput.Down(GKey.Escape)) Launcher.QuitGame();
                    break;
            }
        }

        void Begin()
        {
            state = State.Playing;
            Time.timeScale = 1f;
            beginFrame = Time.frameCount;
            OnBegin();
        }

        void SetPaused(bool p)
        {
            state = p ? State.Paused : State.Playing;
            Time.timeScale = p ? 0f : 1f;
        }

        // ---- Điểm, mạng ----
        public bool IsPlaying => state == State.Playing && pendingEnd < 0f;

        public void AddScore(int amount, Vector3 at, Color? color = null)
        {
            if (!IsPlaying) return;
            score = Mathf.Max(0, score + amount);
            ShowPopup(at, (amount >= 0 ? "+" : "") + amount, color ?? (amount >= 0 ? Color.yellow : new Color(1f, 0.4f, 0.4f)));
        }

        public void ShowPopup(Vector3 at, string text, Color color)
        {
            popups.Add(new Popup { worldPos = at, text = text, color = color });
        }

        public void GainLife(Vector3 at)
        {
            if (lives < MaxLives) lives++;
            ShowPopup(at, "+1 ♥", new Color(1f, 0.5f, 0.7f));
        }

        /// <summary>Trừ 1 mạng. Trả về true nếu vẫn còn mạng.</summary>
        public bool LoseLife()
        {
            if (!IsPlaying) return lives > 0;
            lives--;
            if (lives <= 0)
            {
                lives = 0;
                EndGame(false, 1.0f);
                return false;
            }
            return true;
        }

        /// <summary>Kết thúc game (thắng hoặc thua) sau một khoảng trễ nhỏ cho hiệu ứng kịp chạy.</summary>
        public void EndGame(bool win, float delay = 0f, string message = "")
        {
            if (state != State.Playing || pendingEnd >= 0f) return;
            endMessage = message;
            pendingWin = win;
            if (delay <= 0f) FinishNow(win);
            else pendingEnd = delay;
        }

        void FinishNow(bool win)
        {
            pendingEnd = -1f;
            state = win ? State.Win : State.GameOver;
            Time.timeScale = 0f;
            SoundFx.Play(win ? Sfx.Win : Sfx.GameOver);
            newRecord = score > highScore;
            if (newRecord)
            {
                highScore = score;
                PlayerPrefs.SetInt(HighScoreKey(SaveKey), highScore);
                PlayerPrefs.Save();
            }
        }

        protected virtual void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        // ---- Giao diện ----
        void OnGUI()
        {
            UI.Begin();

            // Chữ nổi "+10"
            foreach (var p in popups)
            {
                float t = p.age / Popup.Life;
                if (UI.WorldToGui(cam, p.worldPos, out var g))
                {
                    var c = p.color; c.a = 1f - t;
                    UI.Text(new Rect(g.x - 100, g.y - 40 - t * 50, 200, 40), p.text, 26, c, TextAnchor.MiddleCenter);
                }
            }

            // HUD
            UI.Text(new Rect(20, 12, 500, 40), "Điểm: " + score, 30, Color.white);
            UI.Text(new Rect(20, 48, 500, 30), "Kỷ lục: " + Mathf.Max(highScore, score), 20, new Color(1f, 1f, 1f, 0.75f));
            if (ShowLives)
            {
                string hearts = lives > 0 ? new string('♥', lives) : "-";
                UI.Text(new Rect(UI.W - 420, 12, 400, 40), "Mạng: <color=#ff5577>" + hearts + "</color>", 30, Color.white, TextAnchor.UpperRight);
            }
            DrawHud();

            if (state == State.Playing)
                UI.Text(new Rect(UI.W - 420, UI.H - 40, 400, 30), "Esc / P: tạm dừng", 16, new Color(1, 1, 1, 0.5f), TextAnchor.UpperRight);

            switch (state)
            {
                case State.Ready:
                    UI.Dim(0.45f);
                    UI.Center(150, Title, 56, Color.white, 80);
                    UI.Text(UI.CenterRect(240, 800, 250), HelpText, 22, new Color(0.9f, 0.95f, 1f), TextAnchor.UpperCenter);
                    UI.Center(500, "Nhấn SPACE / ENTER để bắt đầu", 28, Color.yellow);
                    if (UI.Button(UI.CenterRect(570, 260, 60), "Bắt đầu")) Begin();
                    break;

                case State.Paused:
                    UI.Dim(0.6f);
                    UI.Center(190, "TẠM DỪNG", 56, Color.white, 80);
                    if (UI.Button(UI.CenterRect(300, 320, 60), "Tiếp tục")) SetPaused(false);
                    if (UI.Button(UI.CenterRect(375, 320, 60), "Chơi lại")) Restart();
                    if (UI.Button(UI.CenterRect(450, 320, 60), "Thoát game")) Launcher.QuitGame();
                    break;

                case State.GameOver:
                case State.Win:
                    UI.Dim(0.65f);
                    bool win = state == State.Win;
                    UI.Center(150, win ? "CHIẾN THẮNG!" : "GAME OVER", 64, win ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.4f, 0.4f), 90);
                    if (!string.IsNullOrEmpty(endMessage)) UI.Center(235, endMessage, 24, Color.white);
                    UI.Center(280, "Điểm: " + score, 40, Color.white);
                    UI.Center(330, (newRecord ? "KỶ LỤC MỚI! " : "Kỷ lục: ") + highScore, 26, Color.yellow);
                    if (UI.Button(UI.CenterRect(410, 320, 60), "Chơi lại (R)")) Restart();
                    if (UI.Button(UI.CenterRect(485, 320, 60), "Thoát (Esc)")) Launcher.QuitGame();
                    break;
            }
        }
    }
}
