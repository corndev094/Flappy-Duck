using System.Collections.Generic;
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
    [SerializeField] private DuckSelectionMenu duckSelectionMenu;
    [Header("Popups")]
    [SerializeField] private LevelResultPopup levelResultPopup;
    [SerializeField] private PausePopup pausePopup;

    [Header("Multiplayer")]
    [SerializeField] private QuickMatchMenu quickMatchMenu;
    [SerializeField] private MatchmakingFilterUI filterMatchMenu;

    [Header("HUD")]
    [SerializeField] private HUD hud;

    [Header("Other Elements")]
    [SerializeField] private TMP_Text versionText;
    [SerializeField] private Image bgImage;

    private Dictionary<Menu, ABaseMenu> menus;
    private Dictionary<Popup, ABasePopup> popups;
    private Canvas mainCanvas;

    public HUD HUD => hud;
    public ABaseMenu CurrentMenu { get; set; }
    public ABasePopup TopPopup => popupStack.Count > 0 ? popupStack.Peek() : null;
    public Canvas MainCanvas => mainCanvas;
    public Image BgImage => bgImage;
    public TMP_Text VersionText => versionText;

    private Stack<ABasePopup> popupStack;

    protected override void Awake()
    {
        popupStack = new();
        menus = new()
        {
            {Menu.Main, mainMenu},
            {Menu.Settings, settingsMenu},
            {Menu.LevelMap, levelMapMenu},
            {Menu.DuckSelection, duckSelectionMenu},
            {Menu.QuickMatch, quickMatchMenu},
            {Menu.FilterMatch, filterMatchMenu},
        };
        popups = new()
        {
            {Popup.Pause, pausePopup},
            {Popup.LevelResult, levelResultPopup},
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
        return menus[menuType].gameObject.activeInHierarchy;
    }
    #endregion


    #region Popup

    public async UniTask<ABasePopup> OpenPopup(Popup popupType)
    {
        if (popups.TryGetValue(popupType, out var popup))
        {
            if (popup == null)
            {
                EDebug.LogError($"Does not contains menu of type {popupType}");
                return null;
            }
            await CloseTopPopup();
            popupStack.Push(popup);
            await popup.Open();
            return popup;
        }
        else
        {
            EDebug.LogError($"Does not contains Popup of type {popupType}");
        }
        return null;
    }

    public async UniTask CloseTopPopup()
    {
        if (TopPopup != null) await TopPopup.Close();
        if (popupStack.Count > 0) popupStack.Pop();
    }

    public bool TryGetPopup(Popup popupType, out ABasePopup popup)
    {
        return popups.TryGetValue(popupType, out popup);
    }

    public bool IsPopupOpen(Popup popupType)
    {
        return popups[popupType].gameObject.activeInHierarchy;
    }

    #endregion
}

public enum Menu { Main, Settings, LevelMap, DuckSelection, QuickMatch, FilterMatch };
public enum Popup { Pause, LevelResult }