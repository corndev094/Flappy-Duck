using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Localization;

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
        GameFlowManager.Instance.ResetAllPlayerStats();
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
        Debug.Log("Game started");
    }

    /// <summary>
    /// Offline level loading path — called by OfflineGameMode.StartGame().
    /// Works via local host (SinglePlayerTransport), so all Netcode APIs function normally.
    /// </summary>
    public async UniTask PlayLevel(LevelSO data)
    {
        GameFlowManager.Instance.ResetAllPlayerStats();
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
        OnPlayerLeaveMatch?.Invoke();
        await UIManager.Instance.CloseCurrentMenu();
        await SceneLoader.Instance.FadeIn();

        EDebug.Log("Cleaning level ...");
        CleanupLevel();

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
        if (GameManager.Instance.IsOnlineMode)
        {
            await UIManager.Instance.OpenMenu(Menu.QuickMatch);
        }
        else
        {
            await UIManager.Instance.OpenMenu(Menu.LevelMap);
        }
        SoundManager.Instance.PlayBgMusic();
    }

    public async UniTask WinLevel(int coin)
    {
        if (UIManager.Instance.TryGetPopup(Popup.LevelResult, out var menu) && menu != null && menu is LevelResultPopup levelResultMenu)
        {
            levelResultMenu.Setup(true, coin);
            await UIManager.Instance.OpenPopup(Popup.LevelResult);
        }
    }

    public async UniTask LoseLevel(int coin)
    {
        if (UIManager.Instance.TryGetPopup(Popup.LevelResult, out var menu) && menu != null && menu is LevelResultPopup levelResultMenu)
        {
            levelResultMenu.Setup(false, coin);
            await UIManager.Instance.OpenPopup(Popup.LevelResult);
        }
    }

    private async UniTask SetupDuck()
    {
        // Online mode must play Normal Duck
        duckController.GetDuckServerRpc(GameManager.Instance.IsOnlineMode ? DuckSkinID.Normal : CurrentSelectedDuck.SkinId);
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

        EDebug.LogError($"Timeout waiting for duck spawn after {timeout}s (IsListening: {NetworkManager.Singleton.IsListening}, IsClient: {NetworkManager.Singleton.IsClient}, IsConnectedClient: {NetworkManager.Singleton.IsConnectedClient})");
        return null;
    }

    private void SetupLevel(LevelSO data)
    {
        var level = Instantiate(data.LevelPrefab, levelContainer);
        level.transform.localPosition = Vector3.zero;
        CurrentLevelData = data;
        CurrentLevelPrefab = level;
        // foreach (var go in level.GetComponentsInChildren<GameObject>(true))
        // {
        //     Debug.Log($"Name: {go.name}   -   Hash: {go.GetComponent<NetworkObject>()?.NetworkObjectId}");
        // }
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
