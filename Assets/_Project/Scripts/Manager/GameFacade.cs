using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Contains abstract functions
/// </summary>
public class GameFacade : Singleton<GameFacade> {
    [SerializeField] private DuckList duckList;
    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private PipeSpawner pipeSpawner;
    [SerializeField] private Transform levelContainer;
    [SerializeField] private PreGameCountdown countDown;
    [SerializeField] private TMP_Text versionText;
    [SerializeField] private Image bgImage; 

    [Header("DEBUG")]
    public LevelSO  CurrentPlayingLevel { get; private set; }
    public DuckBaseData CurrentSelectedDuck { get; set; } 
    public GameObject CurrentLevelPrefab { get; private set; }
    public ABaseDuck ActiveDuck { get; private set; }
    public bool IsPlayingLevel { get; set; }

    public async UniTask StartGame()
    {
        CurrentSelectedDuck = duckList.List[DataManager.Instance.GetLastSelectedDuck()];
    }

    /// <summary>
    /// Close currentMenu, setup Bird, and fade in transition
    /// </summary>
    /// <returns></returns>
    public async UniTask PlayLevel(LevelSO data)
    {
        GameManager.Instance.IsGameOver = false;
        GameManager.Instance.IsGameWin = false;
        GameManager.Instance.OnEnterLevel?.Invoke(data);
        SoundManager.Instance.StopBgMusic();
        await UIManager.Instance.CloseCurrentMenu();
        await sceneLoader.FadeIn();

        EDebug.Log("Setup bird");
        IsPlayingLevel = true;
        SetupBird();

        EDebug.Log("Setup level");
        SetupLevel(data);
        SetupUIOnLevelStart();
        EDebug.Log("Setup complete");

        await UniTask.Delay(TimeSpan.FromSeconds(2f));
        await SceneLoader.Instance.FadeOut();
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        SoundManager.Instance.PlayBgMusic(data.BackgroundMusic);
        await countDown.StartCountdown(3);
        if (UIManager.Instance.IsMenuOpen(Menu.Pause))
            return;

        ActiveDuck.StartFly().Forget();
        ActiveDuck.CanAttack = true;
    }

    public async UniTask ReturnToMenu()
    {
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

    public void Pause()
    {
        if (ActiveDuck == null) return;
        ActiveDuck.CanAttack = false;
        ActiveDuck.StopFlying();
    }

    public void Resume()
    {
        if (ActiveDuck == null) return;
        ActiveDuck.CanAttack = true;
        ActiveDuck.StartFly().Forget();
    }

    private void SetupBird()
    {
        ActiveDuck = DuckController.Instance.ActiveDuck(CurrentSelectedDuck.SkinId);
        ActiveDuck.Setup();
        ActiveDuck.transform.position = new Vector3(0, -2, 0);
        ActiveDuck.gameObject.SetActive(true);
        ActiveDuck.CanAttack = false;
    }

    private void SetupLevel(LevelSO data)
    {
        var levelPrefab = Instantiate(data.LevelPrefab, levelContainer);
        levelPrefab.transform.localPosition = Vector3.zero;
        CurrentPlayingLevel = data;
        CurrentLevelPrefab = levelPrefab;
        CameraController.Instance.Target = ActiveDuck.transform;
    }

    private void SetupUIOnLevelStart()
    {
        UIManager.Instance.HUD.gameObject.SetActive(true);
        versionText.gameObject.SetActive(false);
        bgImage.gameObject.SetActive(false);
    }

    private void ResetUIOnReturnToMenu()
    {
        UIManager.Instance.HUD.gameObject.SetActive(false);
        versionText.gameObject.SetActive(true);
        bgImage.gameObject.SetActive(true);
    }

    private void CleanupLevel()
    {
        if (CurrentLevelPrefab != null)
            Destroy(CurrentLevelPrefab);
        if (ActiveDuck != null)
            ActiveDuck.gameObject.SetActive(false);
        CameraController.Instance.Target = null;
        CameraController.Instance.transform.position = Vector3.zero;
        CurrentPlayingLevel = GameManager.Instance.EmptyLevel;
        IsPlayingLevel = false;
    }
}