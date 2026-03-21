# SƠ ĐỒ USE CASE CHI TIẾT DỰ ÁN FLAPPY DUCK

Dưới đây là các sơ đồ PlantUML chi tiết bóc tách cho 10 chức năng (Use Case) cụ thể dựa trên việc phân tích mã nguồn (`GameFlowManager`, `MatchmakingManager`, `ClientNetworkTransform`,...).

1. **Lựa chọn Class Vịt**
2. **Tấn công bằng Đạn/Phi tiêu**
3. **Điều khiển nhân vật**
4. **Kết thúc trận đấu**
5. **Tạo phòng chờ**
6. **Tham gia phòng có sẵn**
7. **Rời phòng**
8. **Tự động Đồng bộ vị trí & Trạng thái**
9. **Xử lý va chạm & Tính toán sát thương**
10. **Cập nhật Điểm số & Giao diện UI**

---

### 1. Lựa chọn Class Vịt (Offline Mode)
Trong chế độ Offline/Local, người chơi truy cập danh sách Vịt từ ScriptableObject trước khi bắt đầu trận đấu.

```plantuml
@startuml
left to right direction
actor "Người chơi" as Player

rectangle "Lựa chọn Class Vịt (Offline)" {
    usecase "Mở giao diện Chọn Vịt" as UC1
    usecase "Duyệt danh sách Vịt (ScriptableObject)" as UC2
    usecase "Xem thông số (Máu, Kỹ năng)" as UC3
    usecase "Khóa Vịt chưa mở" as UC4
    usecase "Lưu Class Vịt vào PlayerPrefs" as UC5
}

Player --> UC1
UC1 .> UC2 : <<include>>
Player --> UC3
UC3 .> UC2 : <<include>>
UC2 .> UC4 : <<extend>> (Nếu chưa đủ cấp)
Player --> UC5
UC5 .> UC2 : <<include>>
@enduml
```

---

### 2. Tấn công bằng Đạn/Phi tiêu
Áp dụng cơ chế tấn công vật lý, trong Multiplayer có sử dụng thao tác cấp phép từ hệ thống (ServerRpc).

```plantuml
@startuml
left to right direction
actor "Người chơi" as Player
actor "Server" as Server

rectangle "Tấn công bằng Vũ khí" {
    usecase "Nhấn phím/Nút Tấn công" as UC1
    usecase "Kiểm tra Hồi chiêu (Cooldown)" as UC2
    usecase "Gửi lệnh Tấn công (ServerRpc)" as UC3
    usecase "Tạo vệt đạn (Spawn Projectile)" as UC4
    usecase "Phát hiệu ứng SFX/VFX" as UC5
}

Player --> UC1
UC1 .> UC2 : <<include>>
UC1 .> UC3 : <<include>> \n(Nếu Online Mode)
Server --> UC4
UC4 .> UC5 : <<extend>>
UC3 .> UC4 : <<include>>
@enduml
```

---

### 3. Điều khiển nhân vật
Điều khiển thông qua Component Input kết hợp vật lý Rigidbody2D và ClientNetworkTransform.

```plantuml
@startuml
left to right direction
actor "Người chơi" as Player

rectangle "Điều khiển Nhân vật" {
    usecase "Nhấn phím Nhảy (Space/Flap)" as UC1
    usecase "Thêm lực Vector hướng lên (AddForce)" as UC2
    usecase "Phát hoạt ảnh bay cánh" as UC3
    usecase "Gửi vị trí mới (ClientNetworkTransform)" as UC4
}

Player --> UC1
UC1 .> UC2 : <<include>>
UC1 .> UC3 : <<include>>
UC2 .> UC4 : <<include>>
@enduml
```

---

### 4. Kết thúc trận đấu
Xử lý bởi `GameFlowManager` khi HP của toàn bộ Vịt về 0 hoặc bị loại.

```plantuml
@startuml
left to right direction
actor "Server / Host" as Server
actor "Client UI" as Client

rectangle "Kết thúc Trận đấu (Game Over)" {
    usecase "Kiểm tra số người chơi còn sống" as UC1
    usecase "Ghi nhận người vượt kỷ lục" as UC2
    usecase "Dừng vật lý & Tạm ngưng Spawn" as UC3
    usecase "Gửi thông báo Game Over (ClientRpc)" as UC4
    usecase "Hiển thị Bảng xếp hạng (Result Panel)" as UC5
}

Server --> UC1
UC1 .> UC2 : <<include>>
UC1 .> UC3 : <<extend>> \n(Nếu chết hết)
UC1 .> UC4 : <<include>> \n(Nếu chết hết)

Client --> UC5
UC4 .down.> UC5 : <<include>>
@enduml
```

---

### 5. Tạo phòng chờ (Create Lobby)
Xử lý bởi `MatchmakingManager`.

```plantuml
@startuml
left to right direction
actor "Người chơi (Host)" as Player
actor "Unity Game Services" as UGS

rectangle "Tạo phòng chờ (Lobby)" {
    usecase "Nhập Tên Phòng & Số người tối đa" as UC1
    usecase "Gọi API CreateLobbyAsync" as UC2
    usecase "Gọi API AllocateRelayAsync" as UC3
    usecase "Lấy Join Code hiển thị" as UC4
    usecase "Chuyển sang Lobby Scene" as UC5
}

Player --> UC1
Player --> UC2
UC2 .> UC3 : <<include>>
UC3 .> UC4 : <<include>>
UC4 .> UC5 : <<include>>

UGS --> UC2
UGS --> UC3
@enduml
```

---

### 6. Tham gia phòng có sẵn (Join Lobby)

```plantuml
@startuml
left to right direction
actor "Người chơi (Client)" as Player
actor "Unity Game Services" as UGS

rectangle "Tham gia phòng Lobby" {
    usecase "Nhập Join Code" as UC1
    usecase "Tìm nhanh (Quick Match)" as UC_Quick
    usecase "Truy vấn danh sách phòng khả dụng" as UC2
    usecase "Gọi API JoinLobbyById/Code" as UC3
    usecase "Kết nối Relay bằng Allocation" as UC4
    usecase "Cập nhật Player Data" as UC5
}

Player --> UC1
Player --> UC_Quick
UC_Quick .> UC2 : <<include>>

UC1 .> UC3 : <<include>>
UC2 .> UC3 : <<include>>
UC3 .> UC4 : <<include>>
UC4 .> UC5 : <<include>>

UGS --> UC2
UGS --> UC3
UGS --> UC4
@enduml
```

---

### 7. Rời phòng (Leave Lobby)

```plantuml
@startuml
left to right direction
actor "Người chơi" as Player
actor "Server/UGS" as Server

rectangle "Rời phòng chờ / Thoát Game" {
    usecase "Nhấn nút Rời phòng" as UC1
    usecase "Gửi API RemovePlayerFromLobby" as UC2
    usecase "Chuyển Host nếu Host thoát" as UC3
    usecase "Đóng kết nối Netcode" as UC4
    usecase "Trở về Main Menu" as UC5
}

Player --> UC1
UC1 .> UC2 : <<include>>
UC2 .> UC3 : <<extend>> \n(Nếu là Host)
UC1 .> UC4 : <<include>>
UC4 .> UC5 : <<include>>

Server --> UC2
Server --> UC3
@enduml
```

---

### 8. Tự động Đồng bộ vị trí & Trạng thái
Dựa trên `NetworkVariable` và thư viện có sẵn của Ngo.

```plantuml
@startuml
left to right direction
actor "Game Engine (Physics)" as Engine
actor "Netcode System" as Netcode

rectangle "Đồng bộ Vị trí & Trạng thái" {
    usecase "Thay đổi Tọa độ/Rotation" as UC1
    usecase "Đồng bộ ClientNetworkTransform" as UC2
    usecase "Cập nhật NetworkVariable (HP, Trạng thái)" as UC3
    usecase "Lắng nghe OnValueChanged" as UC4
}

Engine --> UC1
UC1 .> UC2 : <<include>>
Netcode --> UC2
Netcode --> UC3
UC3 .> UC4 : <<include>>
@enduml
```

---

### 9. Xử lý va chạm & Tính toán sát thương
Quá trình Server Authoritative khi phát hiện va chạm đạn hoặc tường.

```plantuml
@startuml
left to right direction
actor "Physics System (Rigidbody)" as Phys
actor "Server" as Server

rectangle "Tính toán Sát thương & Va chạm" {
    usecase "Phát tín hiệu OnTriggerEnter2D" as UC1
    usecase "Lọc/Phân loại đối tượng (Chướng ngại vật/Kẻ địch)" as UC2
    usecase "Trừ HP (Take Damage)" as UC3
    usecase "Kiểm tra giới hạn máu (HP <= 0)" as UC4
    usecase "Triệu hồi Event Tử vong (OnDeath)" as UC5
}

Phys --> UC1
UC1 .> UC2 : <<include>>
Server --> UC3
UC2 .> UC3 : <<extend>> \n(Nếu là vật nguy hiểm)
UC3 .> UC4 : <<include>>
UC4 .> UC5 : <<extend>> \n(Nếu HP hết)
@enduml
```

---

### 10. Cập nhật Điểm số & Giao diện UI
Logic phản ứng (Reactive) thông qua UIManager và Observer Pattern.

```plantuml
@startuml
left to right direction
actor "Game Logic (Sự kiện)" as Events
actor "UI Manager" as UIMgr

rectangle "Cập nhật Giao diện (UI & Score)" {
    usecase "Phát sự kiện Sinh tồn được +1 Điểm" as UC1
    usecase "Phát sự kiện HP thay đổi" as UC2
    usecase "Khởi chạy hàm Cập nhật Text/Thanh Máu" as UC3
    usecase "Bật hiệu ứng chớp tắt UI (Feedback)" as UC4
}

Events --> UC1
Events --> UC2
UC1 .> UC3 : <<include>>
UC2 .> UC3 : <<include>>
UIMgr --> UC3
UC3 .> UC4 : <<extend>>
@enduml
```
