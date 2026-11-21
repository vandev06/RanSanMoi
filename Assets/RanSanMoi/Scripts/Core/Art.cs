using System.Collections.Generic;
using UnityEngine;

namespace MiniGames
{
    /// <summary>
    /// Nạp hình ảnh thật (sprite PNG) từ thư mục Resources/MiniGamesArt.
    /// Ảnh lấy từ các bộ CC0 của Kenney.nl và OpenGameArt (xem ART_CREDITS.md).
    ///
    /// Mọi sprite được chuẩn hoá: CAO đúng 1 đơn vị Unity, tâm ở giữa,
    /// nên Art.Add(..., height) sẽ cho hình cao đúng "height" và rộng theo tỉ lệ ảnh gốc.
    ///
    /// Nếu thiếu ảnh (ví dụ chưa chép thư mục Resources), Art.Get trả về null và
    /// mỗi đối tượng tự quay về cách vẽ bằng hình khối như phiên bản cũ, game vẫn chạy.
    /// </summary>
    public static class Art
    {
        const string Folder = "MiniGamesArt/";
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        /// <summary>Lấy sprite theo đường dẫn trong Resources/MiniGamesArt, ví dụ "space/player_ship". Không có thì trả null.</summary>
        public static Sprite Get(string path)
        {
            if (cache.TryGetValue(path, out var s)) return s;

            var tex = Resources.Load<Texture2D>(Folder + path);
            if (tex != null)
            {
                tex.wrapMode = TextureWrapMode.Clamp;
                // FullRect: cần cho chế độ vẽ lặp (Tiled) và không cần bật Read/Write cho ảnh
                s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f),
                    tex.height, 0, SpriteMeshType.FullRect);
                s.name = path;
            }
            cache[path] = s;
            return s;
        }

        /// <summary>true nếu có đủ tất cả các ảnh được liệt kê.</summary>
        public static bool Has(params string[] paths)
        {
            foreach (var p in paths)
                if (Get(p) == null) return false;
            return true;
        }

        /// <summary>Tỉ lệ rộng / cao của sprite.</summary>
        public static float Aspect(Sprite s) => s == null ? 1f : s.rect.width / s.rect.height;

        /// <summary>Thêm một hình có chiều cao "height", rộng theo tỉ lệ ảnh. flipX = lật ngang.</summary>
        public static SpriteRenderer Add(Transform parent, Sprite sprite, Vector2 localPos, float height,
            int order = 0, float rotation = 0f, string name = "Sprite", bool flipX = false)
        {
            var sr = Shape.Add(parent, sprite, Color.white, localPos, new Vector2(height, height), order, rotation, name);
            sr.flipX = flipX;
            return sr;
        }

        /// <summary>Thêm hình được kéo giãn vừa đúng khung size (rộng x cao), không giữ tỉ lệ.</summary>
        public static SpriteRenderer AddStretched(Transform parent, Sprite sprite, Vector2 localPos, Vector2 size,
            int order = 0, float rotation = 0f, string name = "Sprite")
        {
            float aspect = Aspect(sprite);
            return Shape.Add(parent, sprite, Color.white, localPos, new Vector2(size.x / aspect, size.y), order, rotation, name);
        }

        /// <summary>
        /// Thêm hình được LẶP LẠI (lát gạch) phủ kín khung size, mỗi viên cao tileHeight.
        /// Dùng cho nền đất, cột đá, ảnh nền trời dài.
        /// </summary>
        public static SpriteRenderer AddTiled(Transform parent, Sprite sprite, Vector2 localPos, Vector2 size,
            float tileHeight, int order = 0, float rotation = 0f, string name = "Tiled")
        {
            var sr = Shape.Add(parent, sprite, Color.white, localPos, new Vector2(tileHeight, tileHeight), order, rotation, name);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = size / tileHeight;
            return sr;
        }
    }
}
