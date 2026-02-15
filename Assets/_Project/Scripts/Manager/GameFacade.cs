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
    [SerializeField] private DuckController duckController;
    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private PipeSpawner pipeSpawner;
    [SerializeField] private Transform levelContainer;
    [SerializeField] private PreGameCountdown countDown;

    
    public LevelSO  CurrentPlayingLevel { get; private set; }
    
    public DuckBaseData CurrentSelectedDuck { get; set; } 
    
    public GameObject CurrentLevelPrefab { get; private set; }
    public Transform LevelContainer => levelContainer;
    
    public ABaseDuck ActiveDuck { get; private set; }
    
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
        await UIManager.Instance.CloseCurrentMenu();
        await sceneLoader.FadeIn();

        EDebug.Log("Setup bird");
        IsPlayingLevel = true;
        await SetupDuck();

        EDebug.Log("Setup level");
        GameFlowManager.Instance.SetupLevel(levelData.ID);
        SetupUIOnLevelStart();
        EDebug.Log("Setup complete");

        await UniTask.Delay(TimeSpan.FromSeconds(2f));
        await SceneLoader.Instance.FadeOut();
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        SoundManager.Instance.PlayBgMusic(levelData.BackgroundMusic);
        if (UIManager.Instance.IsPopupOpen(Popup.Pause))
            return;

        ActiveDuck.StartFly().Forget();
        ActiveDuck.CanAttack = true;
    }

    public async UniTask PlayLevel(LevelSO data)
    {
        if (GameManager.Instance.IsOnlineMode) return;
        GameManager.Instance.IsGameOver = false;
        GameManager.Instance.IsGameWin = false;
        GameManager.Instance.OnEnterLevel?.Invoke(data);
        SoundManager.Instance.StopBgMusic();
        await UIManager.Instance.CloseCurrentMenu();
        await sceneLoader.FadeIn();

        EDebug.Log("Setup bird");
        IsPlayingLevel = true;
        await SetupDuck();

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
        OnPlayerLeaveMatch?.Invoke();
        await UIManager.Instance.CloseCurrentMenu();
        await SceneLoader.Instance.FadeIn();
        EDebug.Log("Cleaning level ...");
        GameFlowManager.Instance.CleanupLevelServerRpc();
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

    private async UniTask SetupDuck()
    {
        if (!IsOwner) return;
        duckController.GetDuckServerRpc(CurrentSelectedDuck.SkinId);
        await UniTask.Delay(TimeSpan.FromSeconds(1));
        ActiveDuck = duckController.CurrentDuck;
        ActiveDuck.Setup();
        ActiveDuck.transform.position = new Vector3(0, -2, 0);
        ActiveDuck.CanAttack = false;
        SetupCamera();
    }

    private void SetupLevel(int id)
    {
        var data = DataManager.Instance.FindLevelDataById(id);
        var levelPrefab = Instantiate(data.LevelPrefab, levelContainer);
        levelPrefab.transform.localPosition = Vector3.zero;
        CurrentPlayingLevel = data;
        CurrentLevelPrefab = levelPrefab;
    }

    public void SetupLevelFromServer(GameObject levelInstance, LevelSO data)
    {
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