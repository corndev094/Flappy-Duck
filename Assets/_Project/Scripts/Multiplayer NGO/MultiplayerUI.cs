namespace Multiplayer
{
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Simple UI for matchmaking - Main menu with Quick/Custom match
    /// NO LOBBY UI - Auto-starts game after match found
    /// </summary>
    public class MultiplayerUI : MonoBehaviour
    {
        [SerializeField] private MatchmakingFilterUI filterUI;
        [Header("Main Menu")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private Button quickMatchButton;
        [SerializeField] private Button customMatchButton;
        [SerializeField] private Button cancelMatchButton;
        [SerializeField] private TMP_Text statusText;

        private MatchmakingManager matchmaking;

        void Awake()
        {
            matchmaking = MatchmakingManager.Instance;
            if (matchmaking == null)
            {
                Debug.LogError("[MultiplayerUI] MatchmakingManager not found!");
                return;
            }

            // Setup buttons
            quickMatchButton?.onClick.AddListener(OnQuickMatchClicked);
            customMatchButton?.onClick.AddListener(OnCustomMatchClicked);
            cancelMatchButton?.onClick.AddListener(OnCancelMatchClicked);

            // Subscribe to events
            matchmaking.OnLeftLobby += HandleLeftLobby;
            matchmaking.OnMatchmakingCompleted += HandleMatchmakingCompleted;
            matchmaking.OnMatchmakingFailed += HandleMatchmakingFailed;

            // Initial state
            ShowMainMenu();
        }

        void OnDestroy()
        {
            if (matchmaking != null)
            {
                matchmaking.OnLeftLobby -= HandleLeftLobby;
                matchmaking.OnMatchmakingCompleted -= HandleMatchmakingCompleted;
                matchmaking.OnMatchmakingFailed -= HandleMatchmakingFailed;
            }
        }

        #region Button Handlers

        private async void OnQuickMatchClicked()
        {
            Debug.Log("[MultiplayerUI] Quick match clicked");
            SetStatusText("Finding match...");
            SetButtonsInteractable(false);
            SetCancelButtonVisible(true);

            await matchmaking.StartQuickMatchmaking();
        }

        private void OnCustomMatchClicked()
        {
            // Open custom matchmaking panel (if you have MatchmakingFilterUI)
            if (filterUI != null)
            {
                filterUI.Show();
            }
            else
            {
                Debug.LogWarning("[MultiplayerUI] MatchmakingFilterUI not found. Using quick match instead.");
                OnQuickMatchClicked();
            }
        }

        private async void OnCancelMatchClicked()
        {
            SetStatusText("Cancelling...");
            SetCancelButtonVisible(false);
            SetButtonsInteractable(false);

            await matchmaking.CancelMatchmaking();
        }

        #endregion

        #region Event Handlers

        private void HandleLeftLobby()
        {
            ShowMainMenu();
            SetStatusText("Left");
        }

        private void HandleMatchmakingCompleted(MatchingResult result)
        {
            SetCancelButtonVisible(false);

            switch (result)
            {
                case MatchingResult.Success:
                    SetStatusText("Match found! Starting game...");
                    // UI will auto-hide when scene loads
                    break;
                case MatchingResult.Failed:
                    SetStatusText("Failed to find match");
                    SetButtonsInteractable(true);
                    break;
                case MatchingResult.Cancelled:
                    SetStatusText("Matchmaking cancelled");
                    SetButtonsInteractable(true);
                    break;
                case MatchingResult.Timeout:
                    SetStatusText("No players found");
                    SetButtonsInteractable(true);
                    break;
            }
        }

        private void HandleMatchmakingFailed(string error)
        {
            SetStatusText($"Error: {error}");
            SetButtonsInteractable(true);
            SetCancelButtonVisible(false);
        }

        #endregion

        #region UI Updates

        private void ShowMainMenu()
        {
            mainPanel?.SetActive(true);
            SetButtonsInteractable(true);
            SetCancelButtonVisible(false);
        }

        private void SetStatusText(string text)
        {
            if (statusText != null)
                statusText.text = text;

            Debug.Log($"[MultiplayerUI] {text}");
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (quickMatchButton != null)
                quickMatchButton.interactable = interactable;
            if (customMatchButton != null)
                customMatchButton.interactable = interactable;
        }

        private void SetCancelButtonVisible(bool visible)
        {
            if (cancelMatchButton != null)
            {
                cancelMatchButton.gameObject.SetActive(visible);
            }
        }

        #endregion
    }
}
