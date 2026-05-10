using UnityEngine;
using System;
using Cysharp.Threading.Tasks;

/// <summary>
/// Control Win Lose
/// </summary>
public class GameManager : NetworkSingleton<GameManager> {
    public GameMode CurrentGameMode = GameMode.Offline;
    private IGameMode currentGameMode;

    public Action<LevelSO> OnEnterLevel;
    public Action OnWin;
    public Action OnLose;
    public Material GrayScaleMat;
    public LevelSO CurrentPlayingLevel { get; set;}
    public bool IsGameOver { get; set; } = false;
    public bool IsGameWin { get; set; } = false;

    public bool IsOnlineMode => CurrentGameMode == GameMode.Online;

    private void Start() {
        SetGameMode(GameMode.Offline);
    }
    public void SetGameMode(GameMode mode)
    {
        // Cleanup previous mode before switching
        currentGameMode?.Cleanup();

        CurrentGameMode = mode;
        currentGameMode = mode switch
        {
            GameMode.Offline => new OfflineGameMode(),
            GameMode.Online => new OnlineGameMode(),
            _ => null
        };
        currentGameMode.Initialize();
    }   

    public async UniTask StartGame()    
    {
        await currentGameMode.StartGame();
    }

    public void NotifyPlayerDied(ulong playerId)
    {
        currentGameMode?.OnPlayerDied(playerId);
    }

    public void NotifyPlayerWin(ulong playerId)
    {
        currentGameMode?.OnPlayerWin(playerId);
    }

    public void Pause(bool pause)
    {
        currentGameMode.OnPaused(pause);
    }

    public void HandleWinCondition()
    {
        IsGameOver = true;
        IsGameWin = true;
        int coin = 0;

        if (CurrentPlayingLevel != null)
        {
            // Unlock next level
            DataManager.Instance.SaveHighestLevel(CurrentPlayingLevel.ID + 1);
            PlayerNetworkData? playerData = GameFlowManager.Instance.GetOfflinePlayerData();
            coin = playerData.Value.Coin;
            if (playerData != null)
                DataManager.Instance.SaveCurrency(ConstantString.COIN, DataManager.Instance.GetCurrency(ConstantString.COIN) + coin);
        }

        GameFacade.Instance.WinLevel(coin).Forget();
        OnWin?.Invoke();
    }

    public void HandleLoseCondition()
    {
        int coin = 0;
        IsGameOver = true;
        IsGameWin = false;
        OnLose?.Invoke();
        PlayerNetworkData? playerData = GameFlowManager.Instance.GetOfflinePlayerData();
        coin = playerData.Value.Coin;
        if (playerData != null)
            DataManager.Instance.SaveCurrency(ConstantString.COIN, DataManager.Instance.GetCurrency(ConstantString.COIN) + coin);
        GameFacade.Instance.LoseLevel(coin).Forget();
    }
}

public enum GameMode {Offline, Online}