using UnityEngine;

namespace MiniGames.Snake
{
    public enum FoodKind { Fruit, Golden, Star }

    /// <summary>
    /// Mồi nằm trong một ô của lưới. Có CircleCollider2D dạng TRIGGER: đầu rắn đi vào là "ăn".
    /// Mồi đặc biệt (táo vàng, ngôi sao) chỉ tồn tại Life giây, sắp hết thì nhấp nháy.
    /// </summary>
    public class Food : MonoBehaviour
    {
        static readonly string[] Fruits = { "snake/apple", "snake/orange", "snake/cherries" };
        static readonly Color[] FruitColors = { Shape.Hex("#ff5a4a"), Shape.Hex("#ffa630"), Shape.Hex("#d6243a") };

        public FoodKind kind;
        public Vector2Int cell;
        public bool eaten;
        float life = -1f;     // < 0: không hết hạn
        float age;
        SpriteRenderer sr;
        float baseScale;

        public bool Expired => life > 0f && age >= life;

        public static Food Create(Transform parent, SnakeGame game, Vector2Int cell, FoodKind kind)
        {
            var go = Shape.Root("Food_" + kind, parent, game.CellToWorld(cell));
            var f = go.AddComponent<Food>();
            f.kind = kind;
            f.cell = cell;
            float c = SnakeGame.Cell;

            Sprite art = null;
            Color fallback = Color.white;
            Sprite shape = SpriteFactory.Circle;
            switch (kind)
            {
                case FoodKind.Fruit:
                    int i = Random.Range(0, Fruits.Length);
                    art = Art.Get(Fruits[i]);
                    fallback = FruitColors[i];
                    break;
                case FoodKind.Golden:
                    art = Art.Get("snake/golden_apple");
                    fallback = Shape.Hex("#ffd84a");
                    f.life = 7f;
                    break;
                case FoodKind.Star:
                    art = Art.Get("snake/star");
                    fallback = Shape.Hex("#ffe680");
                    shape = SpriteFactory.Star;
                    f.life = 7f;
                    break;
            }
            float h = kind == FoodKind.Fruit ? c * 0.85f : c * 1.0f;
            f.sr = art != null
                ? Art.Add(go.transform, art, Vector2.zero, h, 8, 0f, "Sprite")
                : Shape.Add(go.transform, shape, fallback, Vector2.zero, Vector2.one * h, 8, 0f, "Sprite");
            f.baseScale = f.sr.transform.localScale.y;

            // Vùng ăn: TRIGGER (không cản đường), bán kính nhỏ để chỉ đúng ô này mới chạm
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = c * 0.3f;
            return f;
        }

        void Update()
        {
            age += Time.deltaTime;
            // Nhịp "thở" nhẹ cho dễ thấy
            float s = baseScale * (1f + Mathf.Sin(age * 6f) * 0.07f);
            var ls = sr.transform.localScale;
            sr.transform.localScale = new Vector3(ls.x / ls.y * s, s, 1f);
            if (kind == FoodKind.Star) sr.transform.localRotation = Quaternion.Euler(0f, 0f, age * 90f);

            // Sắp hết hạn: nhấp nháy
            if (life > 0f && life - age < 2f) sr.enabled = Mathf.Repeat(age * 8f, 1f) < 0.6f;
        }
    }
}
