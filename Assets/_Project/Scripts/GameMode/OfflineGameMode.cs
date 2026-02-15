using Cysharp.Threading.Tasks;

public class OfflineGameMode : IGameMode
{
    public void Initialize()
    {

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