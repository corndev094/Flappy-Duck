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
        mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        quickMatchButton.onClick.AddListener(OnQuickMatchClicked);
        cancelMatchButton.onClick.AddListener(OnCancelMatchClicked);

        // Subscribe to events
        matchmaking.OnLeftLobby += HandleLeftLobby;
        matchmaking.OnMatchmakingCompleted += HandleMatchmakingCompleted;
        matchmaking.OnMatchmakingFailed += HandleMatchmakingFailed;

        // Initial state
        ShowMainMenu();
    }

    void OnDestroy()
    {
        mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
        quickMatchButton.onClick.RemoveListener(OnQuickMatchClicked);
        cancelMatchButton.onClick.RemoveListener(OnCancelMatchClicked);

        if (matchmaking != null)
        {
            matchmaking.OnLeftLobby -= HandleLeftLobby;
            matchmaking.OnMatchmakingCompleted -= HandleMatchmakingCompleted;
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
        cancelMatchButton.gameObject.SetActive(true);
        matchingPanel.SetActive(true);
        canCancel = false;

        await GameManager.Instance.StartGame();
        canCancel = true;
    }

    private async void OnCancelMatchClicked()
    {
        if (!canCancel) return; // Prevent multiple clicks
        SetInfoText("Cancelling matchmaking...");
        canCancel = false;
        await matchmaking.CancelMatchmaking();
        matchingPanel?.SetActive(false);
        canCancel = true;
    }

    #endregion

    #region Event Handlers

    private void HandleLeftLobby()
    {
        ResetUI();
    }

    private async void HandleMatchmakingCompleted(MatchingResult result)
    {
        switch (result)
        {
            case MatchingResult.Cancelled:
                SetInfoText("Cancelling ...");
                canCancel = false;
                await UniTask.Delay(3000);
                ResetUI();
                break;
            case MatchingResult.Failed:
                SetInfoText("Failed to find match. Returning ...");
                canCancel = false;
                await UniTask.Delay(3000);
                ResetUI();
                break;
            case MatchingResult.Timeout:
                SetInfoText("Matchmaking timed out. Returning ...");
                canCancel = false;
                await UniTask.Delay(3000);
                ResetUI();
                break;
        }
    }

    private async void HandleMatchmakingFailed(string error)
    {
        SetInfoText(error);
        await UniTask.Delay(3000);
        matchingPanel.SetActive(false);
        infoText.gameObject.SetActive(false);
    }

    #endregion

    #region UI Updates

    private void SetInfoText(string message)
    {
        infoText.SetText(message);
        if (!infoText.gameObject.activeSelf) infoText.gameObject.SetActive(true);
    }

    public void ResetUI()
    {
        ShowMainMenu();
        matchingPanel.SetActive(false);
        infoText.gameObject.SetActive(false);
        cancelMatchButton.gameObject.SetActive(true);
    }

    private void ShowMainMenu()
    {
        mainPanel.SetActive(true);
    }

    #endregion
}