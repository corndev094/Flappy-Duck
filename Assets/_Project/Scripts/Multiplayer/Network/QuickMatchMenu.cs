using Cysharp.Threading.Tasks;

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple UI for matchmaking - Main menu with Quick/Custom match
/// NO LOBBY UI - Auto-starts game after match found
/// </summary>
public class QuickMatchMenu : ABaseMenu
{
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quickMatchButton;
    [SerializeField] private Button cancelMatchButton;
    [SerializeField] private GameObject matchingPanel;
    [SerializeField] private TextMeshProUGUI infoText;

    private MatchmakingManager matchmaking;
    private bool canCancel;

    void Awake()
    {
        matchmaking = MatchmakingManager.Instance;
        if (matchmaking == null)
        {
            Debug.LogError("[MultiplayerUI] MatchmakingManager not found!");
            return;
        }

        // Setup buttons
        mainMenuButton?.onClick.AddListener(OnMainMenuClicked);
        quickMatchButton?.onClick.AddListener(OnQuickMatchClicked);
        cancelMatchButton?.onClick.AddListener(OnCancelMatchClicked);

        // Subscribe to events
        matchmaking.OnLeftLobby += HandleLeftLobby;
        matchmaking.OnMatchmakingFailed += HandleMatchmakingFailed;
        // matchmaking.OnMatchmakingCompleted += HandleMatchmakingCompleted;
        // Initial state
        ShowMainMenu();
    }

    void OnDestroy()
    {
        mainMenuButton?.onClick.RemoveListener(OnMainMenuClicked);
        quickMatchButton?.onClick.RemoveListener(OnQuickMatchClicked);
        cancelMatchButton?.onClick.RemoveListener(OnCancelMatchClicked);

        if (matchmaking != null)
        {
            matchmaking.OnLeftLobby -= HandleLeftLobby;
            // matchmaking.OnMatchmakingCompleted -= HandleMatchmakingCompleted;
            matchmaking.OnMatchmakingFailed -= HandleMatchmakingFailed;
        }
    }

    #region Button Handlers

    private async void OnMainMenuClicked()
    {
        await UIManager.Instance.SwitchToMenu(Menu.Main);
        GameManager.Instance.SetGameMode(global::GameMode.Offline);
    }

    private async void OnQuickMatchClicked()
    {
        SetInfoText("Finding match...");
        if (cancelMatchButton != null)
        {
            cancelMatchButton.interactable = false;
            canCancel = false;
            WaitForJoinedLobby();
        }
        matchingPanel?.SetActive(true);
        await GameManager.Instance.StartGame();

    }

    private async void OnCancelMatchClicked()
    {
        Debug.Log(canCancel);
        if (!canCancel) return; // Prevent multiple clicks
        canCancel = false;
        await matchmaking.CancelMatchmaking();
        matchingPanel?.SetActive(false);
        canCancel = true;
    }

    private async void WaitForJoinedLobby()
    {
        matchmaking.OnJoinedLobby += _ =>
        {
            cancelMatchButton.interactable = true;
            canCancel = true;
        };
    }

    #endregion

    #region Event Handlers

    private void HandleLeftLobby()
    {
        Debug.Log("Left lobby, resetting UI.");
        ResetUI();
    }

    // private async void HandleMatchmakingCompleted(MatchingResult result)
    // {
    //     switch (result)
    //     {
    //         case MatchingResult.Timeout:
    //             Debug.Log("Matchmaking timed out.");
    //             SetInfoText("Matchmaking timed out. Returning ...");
    //             canCancel = false;
    //             await UniTask.Delay(3000);
    //             ResetUI();
    //             break;
    //     }
    // }

    private async void HandleMatchmakingFailed(string error)
    {
        SetInfoText(error);
        await UniTask.Delay(3000);
        matchingPanel?.SetActive(false);
        infoText?.gameObject.SetActive(false);
    }

    #endregion

    #region UI Updates

    private void SetInfoText(string message)
    {
        if (infoText == null) return;
        infoText.SetText(message);
        if (!infoText.gameObject.activeSelf) infoText.gameObject.SetActive(true);
    }

    public void ResetUI()
    {
        ShowMainMenu();
        matchingPanel?.SetActive(false);
        infoText?.gameObject.SetActive(false);
        cancelMatchButton?.gameObject.SetActive(true);
    }

    private void ShowMainMenu()
    {
        mainPanel?.SetActive(true);
    }

    #endregion
}
