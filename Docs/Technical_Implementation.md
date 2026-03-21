# PHÂN TÍCH CÁC KỸ THUẬT LẬP TRÌNH (CODING TECHNIQUES) TRONG DỰ ÁN FLAPPY DUCK

Dựa trên cấu trúc mã nguồn trong thư mục `Assets/_Project/Scripts` và kiến trúc của dự án Flappy Duck, dưới đây là danh sách các mô hình thiết kế (Design Patterns), kiến trúc và kỹ thuật lập trình chính đã được áp dụng:

## 1. Kiến trúc Game & Design Patterns

### 1.1. Hierarchical State Machine (HSM - Máy trạng thái phân cấp)
- **Vị trí áp dụng**: Thư mục `Scripts/HSM`
- **Mục đích**: Quản lý các trạng thái phức tạp của nhân vật (Ducks) và AI (nếu có).
- **Mô tả kỹ thuật**: Thay vì sử dụng FSM (Ngữ cảnh máy trạng thái hữu hạn) cơ bản dễ gây ra sự bùng nổ số lượng chuyển đổi trạng thái (State Explosion), HSM cho phép lồng các trạng thái vào nhau theo cấp bậc (Ví dụ: Trạng thái `Action` là trạng thái cha, chứa các trạng thái con `Move` và `Attack`). Nhờ đó, code linh hoạt, rất dễ mở rộng thêm các tính năng/trạng thái mới mà không phá vỡ logic cũ.

### 1.2. Singleton Pattern
- **Vị trí áp dụng**: Cấu trúc các classes thuộc `Scripts/Manager` (như `GameFlowManager`, `MatchmakingManager`, `UIManager`).
- **Mục đích**: Đảm bảo chỉ tồn tại duy nhất một instance của các lớp hệ thống quản lý xuyên suốt vòng đời của game.
- **Mô tả kỹ thuật**: Sử dụng Singleton pattern kết hợp với `DontDestroyOnLoad` để duy trì các Manager logic hoặc kết nối mạng khi chuyển cảnh (Scene Transitions).

### 1.3. OOP - Kế thừa và Đa hình (Inheritance & Polymorphism)
- **Vị trí áp dụng**: Thư mục `Scripts/Entities` và `Scripts/Base`
- **Mục đích**: Quản lý nhiều lớp nhân vật (Classes) có hành vi độc lập nhưng chung nền tảng như 5 loại Vịt: Normal, Tiny, Ninja, Soldier, Ghost.
- **Mô tả kỹ thuật**: Xây dựng lớp trừu tượng `BaseEntity` (hoặc `BaseDuck`) xử lý các vấn đề chung (máu - HP, nhảy, di chuyển, Network). Các class con (như `NinjaDuck`, `GhostDuck`) sẽ kế thừa và ghi đè (`override`) thuộc tính hoặc phương thức kỹ năng kích hoạt (Ví dụ: `UseSkill()`).

### 1.4. Data-Driven Design (Kiến trúc hướng dữ liệu) với ScriptableObjects
- **Vị trí áp dụng**: Quản lý dữ liệu thông số trong thư mục `Scripts/Entities` hoặc `Scripts/GameMode`.
- **Mục đích**: Quản lý cấu hình thông số của từng loại vật phẩm, kỹ năng hoặc thông số class nhân vật riêng biệt.
- **Mô tả kỹ thuật**: Tách rời Dữ liệu (Dữ liệu con Vịt: máu cơ bản, tốc độ bay...) và Logic xử lý ra khỏi nhau. Các file ScriptableObject có thể chỉnh sửa trực tiếp trên Unity Editor mà không cần phải compile lại code, rất lợi thế cho Game Designer thực hiện cân bằng game (Balance).

---

## 2. Kỹ Thuật Lập Trình Multiplayer (Network Engineering)

### 2.1. Server-Authoritative Logic (Ủy quyền máy chủ)
- **Công nghệ**: Unity Netcode for GameObjects (NGO) - Thư mục `Scripts/Multiplayer`.
- **Mục đích**: Tránh việc gian lận (Hack/Cheat) từ phía Client.
- **Mô tả kỹ thuật**: Quy định Host/Server là nơi tính toán logic cốt lõi. Client bị hạn chế, chỉ có quyền gửi "bàn phím/chuột input" (Ví dụ: bấm nút nhảy, bắn phi tiêu). Mọi yếu tố liên quan tới trừ Máu (HP), Điểm số (Score), Kết quả trúng đòn đều do Server xử lý và cập nhật kết quả xuống Client để đồng bộ.

### 2.2. Remote Procedure Calls (RPCs) và NetworkVariables
- **Công nghệ**: Thư viện `Unity.Netcode`.
- **Mô tả kỹ thuật**: 
  - Sử dụng **`ServerRpc`**: Chức năng để Client gửi yêu cầu thao tác lên Server (Ví dụ: Xin phép nhảy lên, Xin phép sử dụng chiêu thức tàng hình).
  - Sử dụng **`ClientRpc`**: Chức năng để Server ép các Client hiển thị hiệu ứng (Ví dụ: Báo hiệu vịt Ninja đang lướt nhanh cho mọi Client khác cùng nhìn thấy).
  - Sử dụng **`NetworkVariable<T>`**: Để tự động đồng bộ hóa các trạng thái liên tục hoặc thông số như Máu (HP hiện tại) từ Server xuống mọi Clients theo biến thời gian thực, với tính năng `OnValueChanged` để kích hoạt update giao diện sức khỏe.

### 2.3. Tích hợp Dịch vụ Unity Gaming Services (UGS) - Relay & Lobby
- **Vị trí áp dụng**: Quản lý ghép trận trong thư mục `Scripts/Multiplayer/Matchmaking`.
- **Mục đích**: Giải quyết vấn đề kết nối P2P khi Port-forwarding.
- **Mô tả kỹ thuật**: 
  - **Lobby API**: Quản lý phòng chờ, host tạo ra phòng có danh sách người tham gia ảo, hiển thị Join Code cho bạn bè.
  - **Relay API**: Cho phép các người chơi phía sau mạng NAT hoặc Tường lửa được kết nối mượt mà tới Host thông qua một server relay của chính Unity mà không gặp lỗi từ chối kết nối đường truyền mạng.

---

## 3. Các thành phần Kỹ thuật nâng cao khác

### 3.1. Entity-Component System (Component-based architecture)
- **Vị trí áp dụng**: `Scripts/Component`.
- **Mô tả kỹ thuật**: Không gom toàn bộ mã nguồn vào một "Script Quái Vật" (God-class). Một con Vịt có thể được lắp ghép từ nhiều module nhỏ độc lập như `MovementComponent`, `CombatComponent`, `HealthComponent`. Việc này hạn chế tối đa sự dính dấp giữa các phần với nhau (Decoupling) tuân thủ quy tắc SOLID.

### 3.2. Unity Editor Scripting / Custom Attributes
- **Vị trí áp dụng**: Thư mục `Scripts/Editor`
- **Mô tả kỹ thuật**: Xây dựng hoặc mở rộng giao diện người dùng Inspector trên Unity bằng `EditorGUILayout` để công cụ hóa quá trình phát triển, tạo các box nhập liệu tùy biến giúp cho Devs cấu hình game dễ thở hơn.

### 3.3. Các phương thức quản lý Cảnh (Scene Management & UI)
- **Vị trí áp dụng**: Thư mục `Scripts/UI`, `Scripts/Utilities`.
- **Mô tả kỹ thuật**: Xử lý logic tải cảnh Game độc lập không bất đồng bộ với Backend mạng để tránh giật lag. Xử lý thiết kế UI theo mô hình tựa MVC/MVP để quản lý dữ liệu tách biệt với việc chèn text lên Canvas đồ họa.
