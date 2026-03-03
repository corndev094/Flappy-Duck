using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.SinglePlayer;
using UnityEngine;

public class OfflineGameMode : IGameMode
{
    public void Initialize()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError("NetworkManager not found in scene!");
            return;
        }

        // Shutdown any existing session before starting a new one
        if (nm.IsListening) nm.Shutdown();

        // Switch to zero-overhead single-player transport
        var singlePlayerTransport = nm.GetComponent<SinglePlayerTransport>();
        if (singlePlayerTransport == null)
        {
            singlePlayerTransport = nm.gameObject.AddComponent<SinglePlayerTransport>();
        }
        nm.NetworkConfig.NetworkTransport = singlePlayerTransport;
        nm.StartHost();
    }

    public void Cleanup()
    {
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening)
        {
            nm.Shutdown();
        }
    }

    public void OnPlayerDied(ulong playerId)
    {
        GameManager.Instance.HandleLoseCondition();
    }

    public void OnPlayerWin(ulong playerId)
    {
        GameManager.Instance.HandleWinCondition();
    }

    public void OnScoreChanged(ulong playerId, int score)
    {
        
    }

    public async UniTask StartGame()
    {
        var levelData = GameManager.Instance.CurrentPlayingLevel;
        await GameFacade.Instance.PlayLevel(levelData);
    }

    public void OnPaused(bool pause)
    {
        if (GameFacade.Instance.ActiveDuck == null) return;
        if (pause)
        {
            GameFacade.Instance.ActiveDuck.CanAttack = false;
            GameFacade.Instance.ActiveDuck.StopFlying();
        }
        else
        {
            GameFacade.Instance.ActiveDuck.CanAttack = true;
            GameFacade.Instance.ActiveDuck.StartFly().Forget();
        }
    }
}
