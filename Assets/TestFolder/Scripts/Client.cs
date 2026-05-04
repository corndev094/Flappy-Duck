using UnityEngine;
using Unity.Netcode;
using NaughtyAttributes;

public class Client : NetworkBehaviour {
    public int data;

    private void Start()
    {
        if (string.IsNullOrEmpty(NetworkManager.Singleton.ConnectedHostname))
        {
            NetworkManager.Singleton.StartServer();
            Debug.Log("Start as Server");
        }
        else
        {
            NetworkManager.Singleton.StartClient();
            Debug.Log("Start as Client");
        }
    }


    // 1. Client gọi hàm này để gửi yêu cầu lên Server
    [Rpc(SendTo.Server)]
    [Button]
    public void RequestDataRpc(RpcParams rpcParams = default)
    {
        // Lấy ID của Client vừa gửi RPC
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        Debug.Log($"Server nhận được yêu cầu từ Client ID: {senderClientId}");

        // 2. Tạo mục tiêu chỉ định hướng tới chính Client đó (Dùng Temp để tránh rác bộ nhớ)
        BaseRpcTarget targetClient = RpcTarget.Single(senderClientId, RpcTargetUse.Temp);

        // 3. Gửi phản hồi lại cho Client thông qua RPC khác
        ResponseDataRpc("Dữ liệu đã được Server xử lý!", targetClient);
    }

    // Hàm này chạy trên Client, chỉ gửi đến Client được chỉ định ở tham số
    [Rpc(SendTo.SpecifiedInParams)]
    public void ResponseDataRpc(string responseData, RpcParams rpcParams = default)
    {
        Debug.Log($"Client nhận được phản hồi từ Server: {responseData}");
    }
}