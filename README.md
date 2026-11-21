# Rắn Săn Mồi

Game 2D viết hoàn toàn bằng C# cho Unity. Bấm **Play** là vào thẳng game, không cần kéo thả gì.
Nhân vật và cảnh nền dùng ảnh thật CC0 (xem `ART_CREDITS.md`), nằm trong `Resources/MiniGamesArt`.

## Mở project

1. Unity Hub → **Projects** → **Add** → **Add project from disk** → chọn thư mục `RanSanMoi`.
2. Mở project (lần đầu Unity tạo thư mục `Library`, mất vài phút).
3. Mở `Assets/Scenes/SampleScene` rồi bấm **Play** ▶.

## Điều khiển

| Game | Di chuyển | Hành động |
|---|---|---|
| Rắn Săn Mồi | ← → ↑ ↓ / W A S D: đổi hướng | (rắn tự bò) |

**Esc / P**: tạm dừng. Khi thua hoặc thắng: **R / Enter** chơi lại, **Esc** thoát game.

## Đối tượng và tương tác

| Đối tượng | Lớp | Thành phần Unity | Vai trò |
|---|---|---|---|
| Đầu rắn | `SnakeBody` | Rigidbody2D (Dynamic, không trọng lực), CircleCollider2D (**Collider thường**) | Bò từng ô theo nhịp; giữ danh sách ô của cả thân |
| Đốt thân | (SpriteRenderer) | không có Collider | Vẽ theo danh sách ô, nhỏ dần về phía đuôi |
| Trái cây / táo vàng / ngôi sao | `Food` | CircleCollider2D (**Trigger**) | Mồi; táo vàng và ngôi sao có hạn 7 giây |
| Sân, tường, đá | `Board`, đá | không có Collider | Va chạm kiểm tra theo toạ độ ô |

| Tương tác | Kết quả | Cách phát hiện |
|---|---|---|
| Đầu rắn chạm trái cây | +10 điểm, dài thêm 1 đốt | Trigger → `OnTriggerEnter2D` |
| Đầu rắn chạm táo vàng | +50 điểm, dài thêm 3 đốt | Trigger → `OnTriggerEnter2D` |
| Đầu rắn chạm ngôi sao | +20 điểm, bò chậm 5 giây | Trigger → `OnTriggerEnter2D` |
| Đâm tường / đá / thân | Game Over | So toạ độ ô (lưới) |
| Rắn dài 40 đốt | Chiến thắng | Đếm số ô |

## Hình ảnh

| Game | Nhân vật / đối tượng | Môi trường |
|---|---|---|
| Rắn Săn Mồi | Rắn xanh (khối tròn) có mắt, trái cây, táo vàng, ngôi sao | Nền cỏ kẻ ô, tường đá xám, đá chướng ngại |

## Cấu trúc code

```
Scripts/
  Core/            Phần khung: MiniGameBase (điểm, mạng, HUD, tạm dừng), Launcher (tự khởi động game),
                   Art (nạp ảnh), SpriteFactory, SoundFx, Fx, UI, GameInput
  Snake/   Code riêng của game
```
