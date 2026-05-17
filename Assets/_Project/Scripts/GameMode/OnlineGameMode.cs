using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class OnlineGameMode : IGameMode
{
    public void Initialize()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        // Shutdown offline host session before switching to online transport
        if (nm.IsListening) nm.Shutdown();

        // Ensure we use real network transport for online
        var unityTransport = nm.GetComponent<UnityTransport>();
        if (unityTransport != null)
        {
            nm.NetworkConfig.NetworkTransport = unityTransport;
        }
    }

    public void Cleanup()
    {
        // Matchmaking handles shutdown via LeaveLobby()
    }

    public void OnPlayerDied(ulong playerId)
    {
        GameFlowManager.Instance?.NotifyPlayerDied(playerId);
    }

    public void OnPlayerWin(ulong playerId)
    {
        GameFlowManager.Instance?.NotifyPlayerFinished(playerId);
    }

    public void OnCoinChanged(ulong playerId, int coin)
    {
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.UpdateCoin(playerId, coin);
        }
    }

    public async UniTask StartGame()
    {
        if (MatchmakingManager.Instance == null)
        {
            Debug.LogError("MatchmakingManager.Instance is null");
            return;
        }

        await MatchmakingManager.Instance.StartQuickMatchmaking();
    }

    public void OnPaused(bool pause)
    {
        return;
    }
}
