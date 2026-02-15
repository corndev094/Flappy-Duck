using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Netcode;

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

    public ABaseDuck CurrentBird { get; set; }
    public LevelSO CurrentPlayingLevel { get; set;}
    public bool IsGameOver { get; set; } = false;
    public bool IsGameWin { get; set; } = false;

    public bool IsOnlineMode => CurrentGameMode == GameMode.Online;

    private void Start() {
        SetGameMode(GameMode.Offline);
    }
    public void SetGameMode(GameMode mode)
    {
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

    public void AddScore(ulong playerId, int score)
    {
        currentGameMode?.OnScoreChanged(playerId, score);
    }

    public void Pause(bool pause)
    {
        currentGameMode.OnPaused(pause);
    }

    [ClientRpc]
    public void ShowGameOverClientRpc()
    {
        UIManager.Instance.OpenPopup(Popup.LevelResult).Forget();
    }

    public void HandleWinCondition()
    {
        IsGameOver = true;
        IsGameWin = true;
        
        if (CurrentPlayingLevel != null)
        {
            // Unlock next level
            DataManager.Instance.SaveHighestLevel(CurrentPlayingLevel.ID + 1);
        }

        GameFacade.Instance.WinLevel().Forget();
        OnWin?.Invoke();
    }

    public void HandleLoseCondition()
    {
        IsGameOver = true;
        IsGameWin = false;
        GameFacade.Instance.LoseLevel().Forget();
        OnLose?.Invoke();
    }
}

public enum GameMode {Offline, Online}