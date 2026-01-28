using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using TMPro;

/// <summary>
/// Control Win Lose
/// </summary>
public class GameManager : Singleton<GameManager> {
    public LevelSO EmptyLevel;
    public Action<LevelSO> OnEnterLevel;
    public Action OnLose;
    public Action OnWin;
    public Material GrayScaleMat;

    public ABaseDuck CurrentBird { get; set; }
    public LevelSO CurrentPlayingLevel { get; set;}
    public bool IsGameOver { get; set; }
    public bool IsGameWin { get; set; }

    void Start()
    {
        GameFacade.Instance.StartGame().Forget();
        foreach (var duck in DuckController.Instance.Ducks.Values)
        {
            duck.OnBirdReachedFinish += HandleWinCondition;
            duck.OnBirdDie += HandleLoseCondition;
        }
    }

    void HandleWinCondition()
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

    void HandleLoseCondition()
    {
        IsGameOver = true;
        IsGameWin = false;
        GameFacade.Instance.LoseLevel().Forget();
        OnLose?.Invoke();
    }
}