using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SocialPlatforms.Impl;

/// <summary>
/// Central facade for game operations.
/// Handles level loading, duck setup, and UI transitions for both offline and online modes.
/// </summary>
public class GameFacade : NetworkSingleton<GameFacade> {
    [SerializeField] private DuckController duckController;
    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private PipeSpawner pipeSpawner;
    [SerializeField] private Transform levelContainer;
    [SerializeField] private PreGameCountdown countDown;

    [field: SerializeField, ReadOnly] public LevelSO  CurrentLevelData { get; private set; }
    [field: SerializeField, ReadOnly] public DuckBaseData CurrentSelectedDuck { get; set; }
    [field: SerializeField, ReadOnly] public GameObject CurrentLevelPrefab { get; private set; }
    [field: SerializeField, ReadOnly] public ABaseDuck ActiveDuck { get; set; }
    [field: SerializeField, ReadOnly] public bool IsPlayingLevel { get; set; }
    
    public Transform LevelContainer => levelContainer;

    public Action OnPlayerInMatch;
    public Action OnPlayerLeaveMatch;

    void Start()
    {
        var dataManager = DataManager.Instance;
        CurrentSelectedDuck = dataManager.DuckList.List[dataManager.GetLastSelectedDuck()];
    }

    /// <summary>
    /// Online level loading path — called via GameFlowManager [Rpc(SendTo.ClientAndHost)] on all clients.
    /// </summary>
    public async UniTask LoadMultiplayerLevel()
    {
        if (IsServer && GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.ResetAllPlayerStats();
        }
        OnPlayerInMatch?.Invoke();
        Debug.Log("Setup for online level ...");
        var levelData = DataManager.Instance.OnlineLevel;
        GameManager.Instance.IsGameOver = false;
        GameManager.Instance.IsGameWin = false;
        GameManager.Instance.OnEnterLevel?.Invoke(levelData);
        SoundManager.Instance.StopBgMusic();

        await UIManager.Instance.CloseTopPopup();
        await UIManager.Instance.CloseCurrentMenu();
        await sceneLoader.FadeIn();

        EDebug.Log("Setup bird");
        IsPlayingLevel = true;
        await SetupDuck();
        if (ActiveDuck == null)
        {
            IsPlayingLevel = false;
            EDebug.LogError("Failed to load multiplayer level because duck spawn failed.");
            await SceneLoader.Instance.FadeOut();
            return;
        }

        EDebug.Log("Setup level");
        SetupLevel(levelData);
        SetupUIOnLevelStart();
        EDebug.Log("Setup completed");

        await UniTask.Delay(TimeSpan.FromSeconds(2f));
        await SceneLoader.Instance.FadeOut();
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        SoundManager.Instance.PlayBgMusic(levelData.BackgroundMusic);

        ActiveDuck.StartFly().Forget();
        ActiveDuck.CanAttack = true;
        if (IsServer) GameFlowManager.Instance.OnLevelLoaded();
    }

    /// <summary>
    /// Offline level loading path — called by OfflineGameMode.StartGame().
    /// Works via local host (SinglePlayerTransport), so all Netcode APIs function normally.
    /// </summary>
    public async UniTask PlayLevel(LevelSO data)
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening){
            NetworkManager.Singleton.StartHost();
        }

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.ResetAllPlayerStats();
        }
        GameManager.Instance.IsGameOver = false;
        GameManager.Instance.IsGameWin = false;
        GameManager.Instance.OnEnterLevel?.Invoke(data);
        SoundManager.Instance.StopBgMusic();
        await UIManager.Instance.CloseTopPopup();
        await UIManager.Instance.CloseCurrentMenu();
        await sceneLoader.FadeIn();

        EDebug.Log("Setup bird");
        IsPlayingLevel = true;
        await SetupDuck();
        if (ActiveDuck == null)
        {
            IsPlayingLevel = false;
            EDebug.LogError("Failed to play level because duck spawn failed.");
            await SceneLoader.Instance.FadeOut();
            return;
        }

        EDebug.Log("Setup level");
        SetupLevel(data);
        SetupUIOnLevelStart();
        EDebug.Log("Setup completed");

        await UniTask.Delay(TimeSpan.FromSeconds(2f));
        await SceneLoader.Instance.FadeOut();
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        SoundManager.Instance.PlayBgMusic(data.BackgroundMusic);
        await countDown.StartCountdown(3);

        ActiveDuck.StartFly().Forget();
        ActiveDuck.CanAttack = true;
    }

    public async UniTask ReturnToMenu()
    {
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.IsManualDisconnect = true;
        }

        OnPlayerLeaveMatch?.Invoke();
        await UIManager.Instance.CloseCurrentMenu();
        await SceneLoader.Instance.FadeIn();

        EDebug.Log("Cleaning level ...");
        CleanupLevel();
        UIManager.Instance.CloseAllPopupImmediately();

        if (GameManager.Instance.IsOnlineMode)
        {
            UIManager.Instance.TryGetMenu(Menu.QuickMatch, out var menu);
            if (menu != null && menu is QuickMatchMenu quickMatchMenu)
            {
                quickMatchMenu.ResetUI();
            }

            if (MatchmakingManager.Instance != null)
            {
                await MatchmakingManager.Instance.LeaveLobby();
            }
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SetupUIOnReturnToMainMenu();
        await UniTask.Delay(TimeSpan.FromSeconds(1));
        SceneLoader.Instance.FadeOut().Forget();
        await UIManager.Instance.OpenMenu(Menu.Main);
        SoundManager.Instance.PlayBgMusic();
    }

    public async UniTask WinLevel(int coin)
    {
        if (GameManager.Instance.IsOnlineMode)
        {
            UIManager.Instance.OpenPopup(Popup.Leaderboard).Forget();
        }
        if (UIManager.Instance.TryGetPopup(Popup.LevelResult, out var menu) && menu != null && menu is LevelResultPopup levelResultMenu)
        {
            levelResultMenu.Setup(true, coin);
            UIManager.Instance.OpenPopup(Popup.LevelResult).Forget();
        }
    }

    public async UniTask LoseLevel(int coin)
    {
        if (GameManager.Instance.IsOnlineMode)
        {
            UIManager.Instance.OpenPopup(Popup.Leaderboard).Forget();
        }
        if (UIManager.Instance.TryGetPopup(Popup.LevelResult, out var menu) && menu != null && menu is LevelResultPopup levelResultMenu)
        {
            levelResultMenu.Setup(false, coin);
            UIManager.Instance.OpenPopup(Popup.LevelResult).Forget();
        }
    }

    private async UniTask SetupDuck()
    {
        if (!await WaitForDuckControllerReady())
        {
            return;
        }

        // Online mode must play Normal Duck
        DuckSkinID skinId = GameManager.Instance.IsOnlineMode || CurrentSelectedDuck == null
            ? DuckSkinID.Normal
            : CurrentSelectedDuck.SkinId;
        duckController.GetDuckServerRpc(skinId);
        ActiveDuck = await WaitForDuckSpawn();
        duckController.CurrentDuck = ActiveDuck;
        if (ActiveDuck == null)
        {
            EDebug.LogError("Failed to spawn duck!");
            return;
        }
        ActiveDuck.CanAttack = false;
        SetupCamera();
    }

    private async UniTask<bool> WaitForDuckControllerReady()
    {
        const float timeout = 5f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (duckController == null)
            {
                EDebug.LogError("DuckController reference is missing on GameFacade.");
                return false;
            }

            if (!duckController.TryGetComponent<NetworkObject>(out _))
            {
                EDebug.LogError("DuckController must have a NetworkObject component before calling RPCs.");
                return false;
            }

            if (duckController.IsSpawned)
            {
                return true;
            }

            await UniTask.Yield();
            elapsed += Time.deltaTime;
        }

        EDebug.LogError("Timeout waiting for DuckController NetworkObject to spawn before requesting duck.");
        return false;
    }

    private async UniTask<ABaseDuck> WaitForDuckSpawn()
    {
        float timeout = 10f;
        float elapsed = 0f;

        if (NetworkManager.Singleton == null)
        {
            EDebug.LogError("NetworkManager.Singleton is null while waiting for duck spawn");
            return null;
        }

        while (elapsed < timeout)
        {
            var nm = NetworkManager.Singleton;
            if (!nm.IsListening)
            {
                await UniTask.Yield();
                elapsed += Time.deltaTime;
                continue;
            }

            // Primary path: the local owned player object is the authoritative duck reference.
            var localPlayerObject = nm.LocalClient?.PlayerObject;
            if (localPlayerObject != null && localPlayerObject.TryGetComponent<ABaseDuck>(out var localDuck))
            {
                return localDuck;
            }

            // Fallback path: scan spawned objects in case player object binding is late.
            foreach (var netObj in nm.SpawnManager.SpawnedObjects.Values)
            {
                if (netObj != null && netObj.IsOwner && netObj.TryGetComponent<ABaseDuck>(out var duck))
                {
                    return duck;
                }
            }

            await UniTask.Yield();
            elapsed += Time.deltaTime;
        }

        var networkManager = NetworkManager.Singleton;
        EDebug.LogError(networkManager != null
            ? $"Timeout waiting for duck spawn after {timeout}s (IsListening: {networkManager.IsListening}, IsClient: {networkManager.IsClient}, IsConnectedClient: {networkManager.IsConnectedClient})"
            : $"Timeout waiting for duck spawn after {timeout}s because NetworkManager.Singleton was destroyed");
        return null;
    }

    private void SetupLevel(LevelSO data)
    {
        var level = Instantiate(data.LevelPrefab, levelContainer);
        level.transform.localPosition = Vector3.zero;
        CurrentLevelData = data;
        CurrentLevelPrefab = level;
        LeaderboardManager.Instance.Setup();
    }

    /// <summary>
    /// Cleanup the current level, despawn the duck, and reset state.
    /// Works for both online and offline modes since Netcode is always active.
    /// </summary>
    private void CleanupLevel()
    {
        // Despawn duck via Netcode (works on local host too)
        if (ActiveDuck != null)
        {
            duckController.ReleaseDuckServerRpc();
            ActiveDuck = null;
            duckController.CurrentDuck = null;
        }

        // Destroy level prefab (locally instantiated, not network-spawned)
        if (CurrentLevelPrefab != null)
        {
            Destroy(CurrentLevelPrefab);
        }

        // Reset state
        CameraController.Instance.Target = null;
        CameraController.Instance.transform.position = Vector3.zero;
        CurrentLevelData = DataManager.Instance.EmptyLevel;
        IsPlayingLevel = false;
        CurrentLevelPrefab = null;
    }

    /// <summary>
    /// Legacy cleanup method — kept for external callers (GameFlowManager).
    /// Delegates to the unified CleanupLevel().
    /// </summary>
    public void CleanupLevelLocal()
    {
        CleanupLevel();
    }

    private void SetupCamera()
    {
        CameraController.Instance.enabled = true;
        CameraController.Instance.Target = ActiveDuck.transform;
    }

    private void SetupUIOnLevelStart()
    {
        UIManager.Instance.HUD.gameObject.SetActive(true);
        UIManager.Instance.VersionText.gameObject.SetActive(false);
        UIManager.Instance.BgImage.gameObject.SetActive(false);
    }

    private void SetupUIOnReturnToMainMenu()
    {
        UIManager.Instance.HUD.gameObject.SetActive(false);
        UIManager.Instance.VersionText.gameObject.SetActive(true);
        UIManager.Instance.BgImage.gameObject.SetActive(true);
    }
}
