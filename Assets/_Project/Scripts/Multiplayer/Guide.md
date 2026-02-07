## Hướng dẫn Setup Multiplayer trong Unity Editor

### 1. Prerequisites (Cài đặt Packages)

Trước khi bắt đầu, cài đặt các package sau qua **Package Manager** (`Window -> Package Manager`):

1. **Netcode for GameObjects** (`com.unity.netcode.gameobjects`)
2. **Relay** (`com.unity.services.relay`)
3. **Authentication** (`com.unity.services.authentication`)
4. **Unity Transport** (thường tự động cài khi cài NGO)
5. **ParrelSync** (tùy chọn nhưng khuyến khích) - Cài từ GitHub: `https://github.com/VeriorPies/ParrelSync.git`

**Lưu ý:** Game hiện tại đã dùng **Input System**, đảm bảo nó được cài đặt và enabled.

### 2. Unity Project Settings

**A. Enable Relay Service:**
1. Vào **Edit -> Project Settings -> Services**
2. Link project với Unity Cloud (có thể dùng Unity Personal)
3. Bật **Relay** và **Authentication**

**B. Input System Settings:**
1. Vào **Edit -> Project Settings -> Player**
2. Tìm **Configuration -> Active Input Handling** và để là **Input System Package** (hoặc Both)

### 3. Scene Setup

**A. Bootstrap Scene (Scene chính luôn load đầu tiên):**

Tạo scene mới tên `Bootstrap` và đảm bảo nó là scene đầu tiên trong Build Settings.

Trong scene này, tạo 3 GameObject:

**GameObject 1: NetworkManager**
- Thêm component **NetworkManager**
- Thêm component **UnityTransport** (nếu chưa có)
- Trong NetworkManager:
  - **Network Transport**: Kéo UnityTransport vào
  - **Player Prefab**: Để trống (sẽ setup ở bước 4)
  - **Auto Spawn Player Prefab**: Tắt (false) - chúng ta sẽ spawn thủ công

**GameObject 2: RelayManager**
- Thêm script `RelayManager.cs`
- Script này kế thừa từ `Singleton<T>` nên sẽ tự động singleton

**GameObject 3: MatchFlowManager**
- Thêm script `MatchFlowManager.cs`
- Script này cần **NetworkObject** component (vì kế thừa NetworkBehaviour)
- Thêm component **NetworkObject**
- **Lưu ý quan trọng:** Đặt MatchFlowManager trong scene Bootstrap và dùng `DontDestroyOnLoad`:

```csharp
// Thêm vào MatchFlowManager.cs trong Awake() sau khi set Instance:
DontDestroyOnLoad(gameObject);
```

### 4. Player Prefab Setup

Player (Duck) cần được setup để hoạt động với NGO:

**A. Trên Duck Prefab (ABaseDuck):**
1. Thêm component **NetworkObject**
2. Thêm component **PlayerNetworkController** (script đã có sẵn)
3. Trong **PlayerNetworkController**:
   - Kéo chính Duck prefab vào field `Duck Controller`
   - Đảm bảo Duck có `Rigidbody2D` và `PlayerInput`

**B. Đăng ký vào NetworkManager:**
1. Chọn **NetworkManager** trong Bootstrap scene
2. Trong **Player Prefab List**: 
   - Thêm Duck prefab vào
   - Hoặc nếu có nhiều loại duck (NormalDuck, RamboDuck), thêm tất cả

**C. Input Setup cho Multiplayer:**

Trong `PlayerNetworkController.cs`, đã có method `OnJump(InputValue value)` được gọi bởi Input System. Để input hoạt động:

1. Trong **PlayerInput** component trên Duck prefab:
   - **Behavior**: Chọn `Invoke Unity Events`
   - Trong **Events -> Action Maps -> Player**:
     - Tìm **Jump** action và kéo Duck prefab vào
     - Chọn method: `PlayerNetworkController.OnJump`

**D. Lưu ý về Camera:**

Trong `PlayerNetworkController.OnNetworkSpawn()`, thêm code để chỉ camera theo owner:

```csharp
public override void OnNetworkSpawn()
{
    rb = GetComponent<Rigidbody2D>();
    if (duckController == null)
    {
        duckController = GetComponent<ABaseDuck>();
    }

    if (IsOwner)
    {
        // Chỉ owner mới có camera theo dõi
        CameraController.Instance.Target = transform;
        duckController.Setup(); // Setup HP, Stamina
        duckController.StartFly().Forget(); // Bắt đầu bay
    }
    else
    {
        // Disable PlayerInput cho non-owner
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null) playerInput.enabled = false;
    }
}
```

### 5. UI Setup (Tạo/Room)

Tạo UI để host/join game. Script mẫu:

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MultiplayerUI : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField codeInput;
    [SerializeField] private TextMeshProUGUI codeDisplay;
    [SerializeField] private TextMeshProUGUI statusText;

    void Start()
    {
        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
    }

    async void OnHostClicked()
    {
        statusText.text = "Creating relay...";
        string joinCode = await RelayManager.Instance.CreateHost();
        
        if (!string.IsNullOrEmpty(joinCode))
        {
            codeDisplay.text = $"Join Code: {joinCode}";
            statusText.text = "Host started! Waiting for players...";
            
            // Chuyển sang lobby panel (scene load tự động qua NetworkManager)
        }
        else
        {
            statusText.text = "Failed to create relay!";
        }
    }

    async void OnJoinClicked()
    {
        string code = codeInput.text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            statusText.text = "Please enter join code!";
            return;
        }

        statusText.text = "Joining...";
        bool success = await RelayManager.Instance.JoinGame(code);
        
        if (success)
        {
            statusText.text = "Joined!";
        }
        else
        {
            statusText.text = "Failed to join! Check code and try again.";
        }
    }
}
```

### 6. Build Settings

Vào **File -> Build Settings** và thêm các scene theo thứ tự:
1. **Bootstrap** (scene index 0 - luôn load đầu tiên)
2. **MainMenu** (hoặc scene có UI tạo/join phòng)
3. **LobbyScene** (scene chờ đủ player)
4. **GameScene** (scene chơi chính)

### 7. Test trong Editor (Dùng ParrelSync)

**Bước 1: Cài ParrelSync**
```
Package Manager -> Add package from git URL:
https://github.com/VeriorPies/ParrelSync.git?path=/ParrelSync
```

**Bước 2: Tạo Clone**
1. Vào menu **ParrelSync -> Clones Manager**
2. Click **Create new clone** → Đặt tên "Client"
3. ParrelSync sẽ tạo bản copy project

**Bước 3: Mở 2 Editor**
1. Mở project gốc bằng Unity Hub (Host)
2. Mở project clone bằng Unity Hub (Client)
   - Path: `D:\Unity Project\Flappy Duck_clone_0`

**Bước 4: Test**
1. Ở Editor Host: Play Mode → Click "Host" → Copy Join Code
2. Ở Editor Client: Play Mode → Paste Join Code → Click "Join"
3. Cả 2 editor sẽ kết nối với nhau qua Unity Relay

**Lưu ý khi test:**
- Code thay đổi chỉ áp dụng cho project gốc
- Clone tự động sync code từ gốc (không cần copy)
- Debug Log sẽ hiển thị ở cả 2 editor

### 8. Debugging Tips

**Console Filters:**
```csharp
// Chỉ log ở server
if (NetworkManager.Singleton.IsServer) Debug.Log("Server: ...");

// Chỉ log ở client
if (!NetworkManager.Singleton.IsServer) Debug.Log("Client: ...");

// Log với ClientID
Debug.Log($"[Client {NetworkManager.Singleton.LocalClientId}] ...");
```

**Network Visualization:**
- Vào **Netcode -> Network Visualization** để xem lưu lượng network

**Common Issues:**

| Issue | Solution |
|-------|----------|
| "Not listening" error | Gọi `NetworkManager.Singleton.Shutdown()` trước khi StartHost/Client mới |
| Client không thấy host | Kiểm tra Join Code có đúng không (phân biệt hoa thường) |
| Player không spawn | Kiểm tra prefab đã thêm vào PlayerPrefabs list chưa |
| Input không hoạt động | Đảm bảo chỉ Owner mới có PlayerInput enabled |
| Scene không load đồng bộ | Dùng `NetworkManager.Singleton.SceneManager.LoadScene()` |

---

### 4. Best Practices và Lỗi thường gặp với NGO

**Best Practices:**

*   **Host-Authoritative:** Luôn tuân thủ mô hình này. Client chỉ gửi *input*, không bao giờ tự thay đổi trạng thái của chính nó. Server (Host) nhận input, thực thi game logic, và đồng bộ kết quả cuối cùng qua `NetworkVariable` hoặc `ClientRpc`. Điều này chống gian lận cơ bản và giữ cho game state nhất quán.
*   **`NetworkVariable` vs. RPC:**
    *   Dùng `NetworkVariable` cho **trạng thái (state)**: HP, score, ammo, vị trí. Nó sẽ tự động đồng bộ khi giá trị thay đổi trên server và đảm bảo client mới vào cũng nhận được giá trị đúng.
    *   Dùng `ClientRpc` cho **sự kiện (event)** chỉ xảy ra một lần và không cần lưu lại: hiệu ứng cháy nổ, âm thanh va chạm.
    *   Dùng `ServerRpc` cho Client gửi **input** hoặc **yêu cầu** lên Server.
*   **Đừng lạm dụng `NetworkTransform`:** Mặc dù tiện lợi, `NetworkTransform` đồng bộ mọi frame và có thể tốn băng thông. Với các game cần độ chính xác cao hoặc tối ưu, hãy tự viết logic đồng bộ vị trí như trong ví dụ `PlayerNetworkController` (Server cập nhật `NetworkVariable<Vector3>`, Client thực hiện `Lerp` để di chuyển mượt mà).
*   **`NetworkObject` Spawning:** Mọi object cần được đồng bộ phải có component `NetworkObject`. Đăng ký prefab của chúng vào danh sách `PlayerPrefabs` trên `NetworkManager`. Để spawn object trong game (đạn, item), hãy dùng `NetworkObject.Spawn()`.
*   **Scene Management:** Luôn dùng `NetworkManager.Singleton.SceneManager` để load scene. Đừng dùng `UnityEngine.SceneManagement.SceneManager` trong game multiplayer, vì nó sẽ không đồng bộ cho các client.
*   **Tổ chức Manager:** Đặt các manager singleton (Relay, MatchFlow) và `NetworkManager` trong một scene khởi động (bootstrap) và dùng `DontDestroyOnLoad` để chúng tồn tại qua các scene.

**Lỗi thường gặp và cách tránh:**

*   **"Not listening" error:** Lỗi này xảy ra khi bạn cố `StartHost()` hoặc `StartClient()` khi một kết nối đã tồn tại. Luôn gọi `NetworkManager.Singleton.Shutdown()` trước khi bắt đầu một host/client mới (ví dụ: khi rời lobby, quay về menu chính).
*   **`NetworkObject` không được spawn:**
    *   Kiểm tra xem prefab đã được thêm vào danh sách `PlayerPrefabs` của `NetworkManager` chưa.
    *   Đảm bảo prefab có component `NetworkObject` ở root.
*   **Client di chuyển giật cục:** Xảy ra khi bạn set vị trí của client trực tiếp bằng giá trị từ `NetworkVariable`. Luôn dùng `Vector3.Lerp` hoặc các kỹ thuật smoothing khác trong `Update()` để di chuyển mượt mà đến vị trí target do server gửi.
*   **RPC không được gọi:**
    *   `ServerRpc` phải có hậu tố `ServerRpc`. `ClientRpc` phải có hậu tố `ClientRpc`.
    *   `ServerRpc` chỉ có thể được gọi từ client là owner của `NetworkObject` đó (trừ khi set `RequireOwnership = false`).
    *   `ClientRpc` phải được gọi từ server.

### 5. Hướng dẫn Test trong Editor và Build

**Cách 1: Dùng ParrelSync (Khuyến khích)**

1.  Cài đặt ParrelSync từ Unity Asset Store hoặc GitHub.
2.  Trong Unity Editor, vào menu `ParrelSync -> Clones Manager`.
3.  Click `Create new clone`. ParrelSync sẽ tạo một bản copy của project của bạn.
4.  Mở project clone đó bằng Unity Hub. Giờ bạn có 2 Editor đang mở cùng một project.
5.  Chạy Play Mode trên cả 2 Editor. Một cái dùng để `StartHost`, cái còn lại dùng Join Code để `StartClient`.
6.  Đây là cách hiệu quả nhất để debug cả code phía client và server.

**Cách 2: Dùng Build và Editor**

1.  Build game ra một bản thực thi (.exe). Mở menu `File -> Build Settings`, thêm các scene cần thiết và click `Build`.
2.  Chạy bản build này.
3.  Trong Unity Editor, nhấn Play.
4.  Dùng một trong hai (Editor hoặc build) để `StartHost`, và cái còn lại để `StartClient`.

**Tip Pro:** Để tự động hóa, bạn có thể viết một script nhỏ để đọc command line arguments. Ví dụ, nếu game khởi động với argument `-mode host`, nó sẽ tự `StartHost()`. Nếu `-mode client -code 123456`, nó sẽ tự `StartClient()` với code đó. Điều này rất hữu ích khi cần test nhanh với nhiều client.

---

### 6. Room-Based vs Matchmaking - Hệ thống hiện tại

**Hiện tại game đang dùng: Room-Based (Join Code)**

Hệ thống hiện tại sử dụng Unity Relay với Join Code, nghĩa là:
- Host tạo phòng → nhận Join Code (ví dụ: `ABC123`)
- Chia sẻ code cho bạn bè
- Bạn bè nhập code để vào phòng

**Ưu điểm:**
- Đơn giản, dễ implement
- Người chơi kiểm soát ai vào phòng
- Phù hợp chơi với bạn bè

**Nhược điểm:**
- Không có tự động ghép trận (matchmaking)
- Cần chia sẻ code ngoài game

---

### 7. Thêm Matchmaking tự động (Tuỳ chọn nâng cao)

Nếu muốn thêm matchmaking tự động (tìm trận ngẫu nhiên), cần thêm **Unity Lobby Service**:

**A. Cài đặt thêm package:**
```
com.unity.services.lobby
```

**B. Flow Matchmaking:**
```
1. Player bấm "Find Match"
2. → Tìm Lobby có sẵn (LobbyService.Instance.QueryLobbiesAsync)
3. → Nếu có: Join lobby đó
4. → Nếu không: Tạo lobby mới và đợi
5. → Khi đủ người: Host gọi StartGame
```

**C. Code mẫu MatchmakingManager (nếu cần):**

```csharp
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class MatchmakingManager : MonoBehaviour
{
    private Lobby currentLobby;
    private float heartbeatTimer;
    private const float HEARTBEAT_INTERVAL = 15f;

    public async Task<bool> FindOrCreateMatch(int maxPlayers = 4)
    {
        try
        {
            // Bước 1: Tìm lobby có sẵn
            var queryResponse = await LobbyService.Instance.QueryLobbiesAsync(new QueryLobbiesOptions
            {
                Count = 1,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                }
            });

            if (queryResponse.Results.Count > 0)
            {
                // Có lobby sẵn → Join
                currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(queryResponse.Results[0].Id);
                string relayCode = currentLobby.Data["RelayCode"].Value;
                return await RelayManager.Instance.JoinGame(relayCode);
            }
            else
            {
                // Không có → Tạo mới và host
                string relayCode = await RelayManager.Instance.CreateHost();
                
                var options = new CreateLobbyOptions
                {
                    IsPrivate = false,
                    Data = new Dictionary<string, DataObject>
                    {
                        { "RelayCode", new DataObject(DataObject.VisibilityOptions.Public, relayCode) }
                    }
                };
                
                currentLobby = await LobbyService.Instance.CreateLobbyAsync("FlappyDuck", maxPlayers, options);
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Matchmaking failed: {e.Message}");
            return false;
        }
    }

    private void Update()
    {
        // Host cần gửi heartbeat để lobby không bị xóa
        if (currentLobby != null && RelayManager.Instance.IsHost)
        {
            heartbeatTimer += Time.deltaTime;
            if (heartbeatTimer >= HEARTBEAT_INTERVAL)
            {
                heartbeatTimer = 0;
                LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            }
        }
    }

    public async void LeaveLobby()
    {
        if (currentLobby != null)
        {
            await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
            currentLobby = null;
        }
    }
}
```

**Lưu ý:** Lobby Service yêu cầu Unity Gaming Services account và có rate limits.

---

### 8. Checklist trước khi Test

- [ ] NetworkManager có UnityTransport component
- [ ] RelayManager và MatchFlowManager trong scene Bootstrap
- [ ] MatchFlowManager có NetworkObject component
- [ ] Duck prefab có NetworkObject + PlayerNetworkController
- [ ] Duck prefab đã thêm vào NetworkManager > Network Prefabs
- [ ] Unity Services đã link project (Project Settings > Services)
- [ ] Relay service đã enable trong Unity Dashboard

### 9. Kết luận

Hệ thống multiplayer hiện tại đã hoàn chỉnh cho **Room-Based** gameplay:

| Component | Trạng thái | Chức năng |
|-----------|------------|-----------|
| RelayManager | ✅ Done | Kết nối P2P qua Unity Relay |
| MatchFlowManager | ✅ Done | Quản lý lobby, game state, player list |
| PlayerNetworkController | ✅ Done | Đồng bộ input, vị trí, HP/Stamina |
| NetworkBullet | ✅ Done | Đạn server-authoritative |
| NetworkPipeSpawner | ✅ Done | Spawn pipe đồng bộ |
| MultiplayerUI | ✅ Done | UI Host/Join/Lobby |

**Để chơi được:**
1. Setup scenes theo hướng dẫn trên
2. Test với ParrelSync (2 Editor)
3. Host tạo phòng → copy Join Code
4. Client nhập code → vào phòng
5. Host bấm Start Game
