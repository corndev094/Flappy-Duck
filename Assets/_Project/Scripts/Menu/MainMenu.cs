using Multiplayer;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : ABaseMenu {
    [Header("Menu")] 
    [SerializeField] private ABaseMenu settingsMenu;
    [SerializeField] private ABaseMenu levelMapMenu;
    [Header("Main Buttons")] 
    [SerializeField] private Button openMapBtn;
    [SerializeField] private Button ducksBtn;
    [SerializeField] private Button settingsBtn;
    [SerializeField] private Button playOnlineBtn;
    [SerializeField] private Button exitBtn;

    private void OnEnable() {
        openMapBtn.onClick.AddListener(OpenLevelMap);
        settingsBtn.onClick.AddListener(OpenSettings);
        ducksBtn.onClick.AddListener(OpenDuckSelection);
        exitBtn.onClick.AddListener(Exit);
        playOnlineBtn.onClick.AddListener(PlayOnline);
    }

    private void OnDisable() {
        openMapBtn.onClick.RemoveListener(OpenLevelMap);
        settingsBtn.onClick.RemoveListener(OpenSettings);
        ducksBtn.onClick.RemoveListener(OpenDuckSelection);
        exitBtn.onClick.RemoveListener(Exit);
        playOnlineBtn.onClick.RemoveListener(PlayOnline);
    }

    private async void OpenSettings()
    {
        await UIManager.Instance.SwitchToMenu(Menu.Settings);
    }

    private async void OpenLevelMap()
    {
        await UIManager.Instance.SwitchToMenu(Menu.LevelMap);
    }

    private async void OpenDuckSelection()
    {
        await UIManager.Instance.SwitchToMenu(Menu.DuckSelection);
    }

    private async void PlayOnline()
    {
        await UIManager.Instance.SwitchToMenu(Menu.QuickMatch);
        GameManager.Instance.SetGameMode(GameMode.Online);
    }

    private void Exit()
    {
        Application.Quit();
    }
}
