using System;
using Cysharp.Threading.Tasks;
using Multiplayer;
using NaughtyAttributes;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Contains abstract functions
/// </summary>
public class GameFacade : NetworkSingleton<GameFacade> {
    [SerializeField] private DuckController duckControllerPefab;
    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private PipeSpawner pipeSpawner;
    [SerializeField] private Transform levelContainer;
    [SerializeField] private PreGameCountdown countDown;

    
    public LevelSO  CurrentPlayingLevel { get; private set; }
    
    public DuckBaseData CurrentSelectedDuck { get; set; } 
    
    public GameObject CurrentLevelPrefab { get; private set; }
    
    public ABaseDuck ActiveDuck { get; private set; }
    
    public bool IsPlayingLevel { get; set; }

    public Action OnPlayerInMatch;
    public Action OnPlayerLeaveMatch;
    private DuckController duckController;

    void Start()
    {
        var dataManager = DataManager.Instance;
        CurrentSelectedDuck = dataManager.DuckList.List[dataManager.GetLastSelectedDuck()];
    }

    public async void LoadMultiplayerLevel()
    {
        if (!GameManager.Instance.IsOnlineMode) return;
        // Init data setup
        OnPlayerInMatch.Invoke();
        Debug.Log("Setup for online level ...");
        var levelData = DataManager.Instance.OnlineLevel;
        GameManager.Instance.IsGameOver.Value = false;
        GameManager.Instance.IsGameWin.Value = false;
        GameManager.Instance.OnEnterLevel?.Invoke(levelData);
        SoundManager.Instance.StopBgMusic();

        // Fade in to setup
        await UIManager.Instance.CloseCurrentMenu();
        await sceneLoader.FadeIn();

        EDebug.Log("Setup bird");
        IsPlayingLevel = true;
        SetupBird();

        EDebug.Log("Setup level");
        SetupLevelServerRpc(levelData.ID);
        SetupUIOnLevelStart();
        EDebug.Log("Setup complete");

        await UniTask.Delay(TimeSpan.FromSeconds(2f));
        await SceneLoader.Instance.FadeOut();
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        SoundManager.Instance.PlayBgMusic(levelData.BackgroundMusic);
        await WaitForPlayers();
        if (UIManager.Instance.IsPopupOpen(Popup.Pause))
            return;

        ActiveDuck.StartFly().Forget();
        ActiveDuck.CanAttack = true;
    }

    public async UniTask PlayLevel(LevelSO data)
    {
        if (GameManager.Instance.IsOnlineMode) return;
        GameManager.Instance.IsGameOver.Value = false;
        GameManager.Instance.IsGameWin.Value = false;
        GameManager.Instance.OnEnterLevel?.Invoke(data);
        SoundManager.Instance.StopBgMusic();
        await UIManager.Instance.CloseCurrentMenu();
        await sceneLoader.FadeIn();

        EDebug.Log("Setup bird");
        IsPlayingLevel = true;
        SetupBird();

        EDebug.Log("Setup level");
        SetupLevel(data.ID);
        SetupUIOnLevelStart();
        EDebug.Log("Setup complete");

        await UniTask.Delay(TimeSpan.FromSeconds(2f));
        await SceneLoader.Instance.FadeOut();
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        SoundManager.Instance.PlayBgMusic(data.BackgroundMusic);
        await countDown.StartCountdown(3);
        if (UIManager.Instance.IsPopupOpen(Popup.Pause))
            return;

        ActiveDuck.StartFly().Forget();
        ActiveDuck.CanAttack = true;
    }

    public async UniTask ReturnToMenu()
    {
        OnPlayerLeaveMatch.Invoke();
        await UIManager.Instance.CloseCurrentMenu();
        await SceneLoader.Instance.FadeIn();
        EDebug.Log("Cleaning level ...");
        CleanupLevel();
        ResetUIOnReturnToMenu();
        await UniTask.Delay(TimeSpan.FromSeconds(1));
        SceneLoader.Instance.FadeOut().Forget();
        await UIManager.Instance.OpenMenu(Menu.LevelMap);
        SoundManager.Instance.PlayBgMusic();
    }

    public async UniTask WinLevel()
    {
        if (UIManager.Instance.TryGetPopup(Popup.LevelResult, out var menu) && menu != null && menu is LevelResultMenu levelResultMenu)
        {
            levelResultMenu.Setup(true);
            await UIManager.Instance.OpenMenu(Menu.LevelResult);
        }
    }

    public async UniTask LoseLevel()
    {
        if (UIManager.Instance.TryGetPopup(Popup.LevelResult, out var menu) && menu != null && menu is LevelResultMenu levelResultMenu)
        {
            levelResultMenu.Setup(false);
            await UIManager.Instance.OpenMenu(Menu.LevelResult);
        }
    }

    private void SetupBird()
    {
        duckController = Instantiate(duckControllerPefab, Vector3.zero, Quaternion.identity, null);
        duckController.GetComponent<NetworkObject>().Spawn();
        ActiveDuck = duckController.ActiveDuck(CurrentSelectedDuck.SkinId);
        ActiveDuck.Setup();
        ActiveDuck.transform.position = new Vector3(0, -2, 0);
        ActiveDuck.gameObject.SetActive(true);
        ActiveDuck.CanAttack = false;
        SetupCameraClientRpc(ActiveDuck.NetworkObjectId);
    }

    private void SetupLevel(int id)
    {
        var data = DataManager.Instance.FindLevelDataById(id);
        var levelPrefab = Instantiate(data.LevelPrefab, levelContainer);
        levelPrefab.transform.localPosition = Vector3.zero;
        CurrentPlayingLevel = data;
        CurrentLevelPrefab = levelPrefab;
    }

    [ServerRpc]
    private void SetupLevelServerRpc(int id)
    {
        var data = DataManager.Instance.FindLevelDataById(id);
        var levelPrefab = Instantiate(data.LevelPrefab, levelContainer);
        levelPrefab.GetComponent<NetworkObject>().Spawn();
        levelPrefab.transform.localPosition = Vector3.zero;
        CurrentPlayingLevel = data;
        CurrentLevelPrefab = levelPrefab;
    }

    [ClientRpc]
    private void SetupCameraClientRpc(ulong targetNetId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(targetNetId, out var target)) return;

        CameraController.Instance.Target = target.transform;
    }

    private async UniTask WaitForPlayers()
    {
        while (GameManager.Instance.IsOnlineMode && !GameFlowManager.Instance.IsAllPlayersInMatch())
        {
            await UniTask.Yield();
        }
    }

    private void SetupUIOnLevelStart()
    {
        UIManager.Instance.HUD.gameObject.SetActive(true);
        UIManager.Instance.VersionText.gameObject.SetActive(false);
        UIManager.Instance.BgImage.gameObject.SetActive(false);
    }

    private void ResetUIOnReturnToMenu()
    {
        UIManager.Instance.HUD.gameObject.SetActive(false);
        UIManager.Instance.VersionText.gameObject.SetActive(true);
        UIManager.Instance.BgImage.gameObject.SetActive(true);
    }


    private void CleanupLevel()
    {
        if (CurrentLevelPrefab != null)
        {
            if (CurrentLevelPrefab.TryGetComponent<NetworkObject>(out var netObj) && IsServer)
                netObj.Despawn(true);
            else
                Destroy(CurrentLevelPrefab);
        }
        if (duckController != null)
        {
            if (duckController.TryGetComponent<NetworkObject>(out var netObj) && IsServer)
                netObj.Despawn(true);
            else
                Destroy(duckController);
        }

        CameraController.Instance.Target = null;
        CameraController.Instance.transform.position = Vector3.zero;
        CurrentPlayingLevel = DataManager.Instance.EmptyLevel;
        IsPlayingLevel = false;
    }
}