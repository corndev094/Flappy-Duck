using System;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Contains abstract functions
/// </summary>
public class GameFacade : NetworkSingleton<GameFacade> {
    [SerializeField] private DuckController duckController;
    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private PipeSpawner pipeSpawner;
    [SerializeField] private Transform levelContainer;
    [SerializeField] private PreGameCountdown countDown;

    [field: SerializeField] public LevelSO  CurrentPlayingLevel { get; private set; }
    [field: SerializeField] public DuckBaseData CurrentSelectedDuck { get; set; }
    [field: SerializeField] public GameObject CurrentLevelPrefab { get; private set; }
    [field: SerializeField] public Transform LevelContainer => levelContainer;
    [field: SerializeField] public ABaseDuck ActiveDuck { get; set; }
    
    public bool IsPlayingLevel { get; set; }

    public Action OnPlayerInMatch;
    public Action OnPlayerLeaveMatch;

    void Start()
    {
        var dataManager = DataManager.Instance;
        CurrentSelectedDuck = dataManager.DuckList.List[dataManager.GetLastSelectedDuck()];
    }

    public async UniTask LoadMultiplayerLevel()
    {
        if (!GameManager.Instance.IsOnlineMode) return;
        // Init data setup
        OnPlayerInMatch?.Invoke();
        Debug.Log("Setup for online level ...");
        var levelData = DataManager.Instance.OnlineLevel;
        GameManager.Instance.IsGameOver = false;
        GameManager.Instance.IsGameWin = false;
        GameManager.Instance.OnEnterLevel?.Invoke(levelData);
        SoundManager.Instance.StopBgMusic();

        // Fade in to setup
        await UIManager.Instance.CloseTopPopup();
        await UIManager.Instance.CloseCurrentMenu();
        await sceneLoader.FadeIn();

        EDebug.Log("Setup bird");
        IsPlayingLevel = true;
        await SetupDuck();

        EDebug.Log("Setup level");
        SetupLevel(levelData.ID);
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

    public async UniTask PlayLevel(LevelSO data)
    {
        if (GameManager.Instance.IsOnlineMode) return;
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
        SetupLevel(data.ID);
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
        GameFlowManager.Instance.CleanupLevelServerRpc();
        ResetUIOnReturnToMenu();
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

    public async UniTask WinLevel()
    {
        if (UIManager.Instance.TryGetPopup(Popup.LevelResult, out var menu) && menu != null && menu is LevelResultPopup levelResultMenu)
        {
            levelResultMenu.Setup(true);
            await UIManager.Instance.OpenPopup(Popup.LevelResult);
        }
    }

    public async UniTask LoseLevel()
    {
        if (UIManager.Instance.TryGetPopup(Popup.LevelResult, out var menu) && menu != null && menu is LevelResultPopup levelResultMenu)
        {
            levelResultMenu.Setup(false);
            await UIManager.Instance.OpenPopup(Popup.LevelResult);
        }
    }

    private async UniTask SetupDuck()
    {
        duckController.GetDuckServerRpc(CurrentSelectedDuck.SkinId);
        ActiveDuck = await WaitForDuckSpawn();
        Debug.Log(ActiveDuck.OwnerClientId);
        if (ActiveDuck == null)
        {
            EDebug.LogError("Failed to spawn duck!");
            return;
        }
        ActiveDuck.transform.position = new Vector3(0, -2, 0);
        ActiveDuck.CanAttack = false;
        SetupCamera();
    }

    private async UniTask<ABaseDuck> WaitForDuckSpawn()
    {
        // Đợi tối đa 5 giây
        float timeout = 5f;
        float elapsed = 0f;
        
        while (elapsed < timeout)
        {
            // Tìm trong spawned objects
            foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
            {
                if (netObj.TryGetComponent<ABaseDuck>(out var duck))
                {
                    if (netObj.IsOwner) // Là duck của client này
                    {
                        return duck;
                    }
                }
            }
            
            await UniTask.Yield();
            elapsed += Time.deltaTime;
        }
        
        EDebug.LogError("Timeout waiting for duck spawn");
        return null;
    }

    private void SetupLevel(int id)
    {
        var data = DataManager.Instance.FindLevelDataById(id);
        var level = Instantiate(data.LevelPrefab, levelContainer);
        level.transform.localPosition = Vector3.zero;
        CurrentPlayingLevel = data;
        CurrentLevelPrefab = level;
    }

    public void SetupLevelFromServer(int id)
    {
        var data = DataManager.Instance.FindLevelDataById(id);
        var levelInstance = Instantiate(data.LevelPrefab, GameFacade.Instance.LevelContainer);
        levelInstance.GetComponent<NetworkObject>().Spawn();
        levelInstance.transform.localPosition = Vector3.zero;
        CurrentLevelPrefab = levelInstance;
        CurrentPlayingLevel = data;
    }

    private void SetupCamera()
    {
        CameraController.Instance.enabled = true;
        CameraController.Instance.Target = ActiveDuck.transform;
    }

    // private async UniTask WaitForPlayers()
    // {
    //     while (GameManager.Instance.IsOnlineMode)
    //     {
    //         await UniTask.Yield();
    //     }
    // }

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


    public void CleanupLevelLocal()
    {
        duckController.DisableAll();
        CameraController.Instance.Target = null;
        CameraController.Instance.transform.position = Vector3.zero;
        CurrentPlayingLevel = DataManager.Instance.EmptyLevel;
        IsPlayingLevel = false;
        CurrentLevelPrefab = null;
    }
}