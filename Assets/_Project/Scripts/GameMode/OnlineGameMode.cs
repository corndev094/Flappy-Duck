using Cysharp.Threading.Tasks;
using Multiplayer;
using UnityEngine;

public class OnlineGameMode : IGameMode
{
    public void Initialize()
    {

    }

    public void OnPlayerDied(ulong playerId)
    {
        GameFlowManager.Instance.NotifyPlayerDied(playerId);
    }

    public void OnPlayerWin(ulong playerId)
    {
        if (GameFlowManager.Instance != null && !MatchmakingManager.Instance.IsHost)
        {
            GameFlowManager.Instance.CheckGameOver();
        }
    }

    public void OnScoreChanged(ulong playerId, int score)
    {
        if (GameFlowManager.Instance != null && !MatchmakingManager.Instance.IsHost)
        {
            GameFlowManager.Instance.AddScore(playerId, score);
        }
    }

    public async UniTask StartGame()
    {
        var result = await MatchmakingManager.Instance.StartQuickMatchmaking();
        if (result == MatchingResult.Success)
        {
            Debug.Log("Wait for match ...");
        }
    }

    public void OnPaused(bool pause)
    {
        return;
    }
}