using UnityEngine;

namespace MiniGames.Snake
{
    /// <summary>
    /// Sân chơi: nền cỏ lát gạch (Tiled) phủ đúng vùng lưới, kẻ ô mờ, bao quanh bởi tường đá xám.
    /// Tường không cần Collider: va chạm tường được kiểm tra bằng toạ độ ô trong SnakeBody.
    /// </summary>
    public static class Board
    {
        const float WallT = 0.4f;   // độ dày tường

        public static void Create(Transform parent, Vector2 center, Vector2 size)
        {
            var root = Shape.Root("Board", parent, center).transform;

            var grass = Art.Get("snake/grass");
            if (grass != null) Art.AddTiled(root, grass, Vector2.zero, size, 2f, -10, 0f, "Grass");
            else Shape.Add(root, SpriteFactory.Square, Shape.Hex("#3f9a45"), Vector2.zero, size, -10, 0f, "Grass");

            // Ô bàn cờ mờ giúp người chơi canh hướng rẽ
            var shade = new Color(0f, 0f, 0f, 0.07f);
            float c = SnakeGame.Cell;
            int cols = Mathf.RoundToInt(size.x / c), rows = Mathf.RoundToInt(size.y / c);
            var cells = new GameObject("Checker").transform;
            cells.SetParent(root, false);
            for (int y = 0; y < rows; y++)
                for (int x = (y % 2); x < cols; x += 2)
                    Shape.Add(cells, SpriteFactory.Square, shade,
                        new Vector2(-size.x / 2f + (x + 0.5f) * c, -size.y / 2f + (y + 0.5f) * c), Vector2.one * c, -9, 0f, "C");

            // Tường 4 phía
            float w = size.x + WallT * 2f, h = size.y + WallT * 2f;
            Wall(root, new Vector2(0f, size.y / 2f + WallT / 2f), new Vector2(w, WallT));
            Wall(root, new Vector2(0f, -size.y / 2f - WallT / 2f), new Vector2(w, WallT));
            Wall(root, new Vector2(-size.x / 2f - WallT / 2f, 0f), new Vector2(WallT, h));
            Wall(root, new Vector2(size.x / 2f + WallT / 2f, 0f), new Vector2(WallT, h));
        }

        static void Wall(Transform root, Vector2 pos, Vector2 size)
        {
            var art = Art.Get("snake/wall");
            if (art != null) Art.AddTiled(root, art, pos, size, WallT, -5, 0f, "Wall");
            else Shape.Add(root, SpriteFactory.Square, Shape.Hex("#9aa3ad"), pos, size, -5, 0f, "Wall");
        }
    }
}
