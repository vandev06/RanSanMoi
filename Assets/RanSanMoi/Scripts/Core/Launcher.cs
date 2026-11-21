using UnityEngine;

namespace MiniGames
{
    /// <summary>
    /// Khởi động game "Rắn Săn Mồi" ngay khi bấm Play, ở BẤT KỲ scene nào (kể cả scene trống),
    /// nhờ [RuntimeInitializeOnLoadMethod]. Project này chỉ có một game nên không có menu chọn.
    /// </summary>
    public class Launcher : MonoBehaviour
    {
        /// <summary>Đặt false nếu không muốn game tự khởi động trong mọi scene.</summary>
        public static bool AutoBoot = true;

        static Launcher instance;
        MiniGameBase current;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (!AutoBoot || instance != null) return;
            new GameObject("Rắn Săn Mồi Launcher").AddComponent<Launcher>();
        }

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            Application.targetFrameRate = 60;
        }

        void Start()
        {
            EnsureCamera();
            var go = new GameObject("Game");
            current = go.AddComponent<Snake.SnakeGame>();
            go.name = current.Title;
            current.launcher = this;
        }

        /// <summary>Đảm bảo có Camera chính, dạng Orthographic (2D), thấy 10 đơn vị theo chiều cao.</summary>
        public static Camera EnsureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            if (cam.GetComponent<AudioListener>() == null && FindAnyListener() == null)
                cam.gameObject.AddComponent<AudioListener>();
            return cam;
        }

        static AudioListener FindAnyListener()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<AudioListener>();
#else
            return Object.FindObjectOfType<AudioListener>();
#endif
        }

        /// <summary>Thoát game (trong Unity Editor thì dừng chế độ Play).</summary>
        public static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
