using Cysharp.Threading.Tasks;

public interface IGameMode
{
    void Initialize();
    void Cleanup();
    UniTask StartGame();
    void OnPlayerDied(ulong playerId);
    void OnPlayerWin(ulong playerId);
    void OnScoreChanged(ulong playerId, int score);
    void OnPaused(bool pause);
}