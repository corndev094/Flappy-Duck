using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager> {
    [Header("Menus")] 
    [SerializeField] private MainMenu mainMenu;
    [SerializeField] private SettingsMenu settingsMenu;
    [SerializeField] private LevelMapMenu levelMapMenu;
    [SerializeField] private LevelResultMenu levelResultMenu;
    [SerializeField] private DuckSelectionMenu duckSelectionMenu;
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private HUD hud;
    [SerializeField] private TMP_Text versionText;
    [SerializeField] private Image bgImage;
    private Dictionary<Menu, ABaseMenu> menus;
    private Dictionary<Popup, ABasePopup> popups;

    public HUD HUD => hud;
    public ABaseMenu CurrentMenu { get; set; }
    private Canvas mainCanvas;
    public Canvas MainCanvas => mainCanvas;

    protected override void Awake()
    {
        menus = new()
        {
            {Menu.Main, mainMenu},
            {Menu.Settings, settingsMenu},
            {Menu.LevelMap, levelMapMenu},
            {Menu.DuckSelection, duckSelectionMenu},
        };
        popups = new()
        {
            {Popup.Pause, pauseMenu},
            {Popup.LevelResult, levelResultMenu},
        };
    }

    private void Start() {
        versionText.SetText(Application.version);
    }

    void OnValidate()
    {
        if (mainCanvas == null)
        {
            mainCanvas = GetComponent<Canvas>();
            if (mainCanvas != null && mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay && mainCanvas.worldCamera == null)
            {
                mainCanvas.worldCamera = Camera.main;
            }
        }
    }

    #region Menu
    private async UniTask GenericSwitchMenu(ABaseMenu targetMenu)
    {
        if (CurrentMenu != null)
        {
            await CurrentMenu.Close();
        }
        await targetMenu.Open();
        CurrentMenu = targetMenu;
    }

    public async UniTask SwitchToMenu(Menu menuType)
    {
        if (menus.TryGetValue(menuType, out ABaseMenu targetMenu))
        {
            await GenericSwitchMenu(targetMenu);
        }
        else
        {
            EDebug.LogError($"Menu of type {menuType} not found!");
        }
    }

    public async UniTask CloseCurrentMenu()
    {
        if (CurrentMenu != null) await CurrentMenu.Close();
        else
        {
            EDebug.LogError("Current Menu is null.");
        }
    }

    public async UniTask<ABaseMenu> OpenMenu(Menu menuType)
    {
        if (menus.TryGetValue(menuType, out var menu))
        {
            if (menu == null)
            {
                EDebug.LogError($"Does not contains menu of type {menuType}");
                return null;
            }
            CurrentMenu = menu;
            await CurrentMenu.Open();
            return menu;
        }
        else
        {
            EDebug.LogError($"Does not contains menu of type {menuType}");
        }
        return null;
    }

    public bool TryGetMenu(Menu menuType,out ABaseMenu menu)
    {
        return menus.TryGetValue(menuType, out menu);
    }

    public bool IsMenuOpen(Menu menuType)
    {
        return menus[menuType].gameObject.activeSelf;
    }
    #endregion


    #region Popup
    public bool TryGetPopup(Popup popupType, out ABasePopup popup)
    {
        return popups.TryGetValue(popupType, out popup);
    }
    
    #endregion
}

public enum Menu {Main, Settings, LevelMap, LevelResult, DuckSelection, Pause};
public enum Popup {Pause,
    LevelResult
}