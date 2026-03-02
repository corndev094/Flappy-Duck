using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI panel for custom matchmaking with filters
/// </summary>
public class MatchmakingFilterUI : ABaseMenu
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [Header("Buttons")]
    [SerializeField] private Button returnButton;
    [SerializeField] private Button findMatchButton;
    [SerializeField] private Button cancelButton;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;

    private MatchmakingManager matchmaking;

    void Awake()
    {
        // Setup buttons
        returnButton?.onClick.AddListener(OnReturnClicked);
        findMatchButton?.onClick.AddListener(OnFindMatchClicked);
        cancelButton?.onClick.AddListener(OnCancelClicked);
    }

    void OnDestroy()
    {
        returnButton?.onClick.RemoveListener(OnReturnClicked);
        findMatchButton?.onClick.RemoveListener(OnFindMatchClicked);
        cancelButton?.onClick.RemoveListener(OnCancelClicked);

    }

    #region Public Methods

    #endregion

    #region Button Handlers

    private async void OnReturnClicked()
    {
        UIManager.Instance.SwitchToMenu(Menu.QuickMatch).Forget();
    }

    private async void OnFindMatchClicked()
    {
        if (matchmaking == null)
        {
            SetStatus("Matchmaking manager not found!");
            return;
        }

        // Start matchmaking
        SetStatus("Finding match...");
        SetButtonsInteractable(false);
        SetCancelButtonVisible(true);

        var result = await matchmaking.StartMatchmaking();

        if (result != MatchingResult.Success)
        {
            SetButtonsInteractable(true);
            SetCancelButtonVisible(false);
        }
    }

    private async void OnCancelClicked()
    {
        SetStatus("Cancelling...");
        SetButtonsInteractable(false);

        if (matchmaking != null)
        {
            await matchmaking.CancelMatchmaking();
        }

        SetButtonsInteractable(true);
    }

    #endregion

    #region Helper Methods

    private void SetStatus(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
        }
        Debug.Log($"[MatchmakingFilterUI] {text}");
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (findMatchButton != null)
            findMatchButton.interactable = interactable;
        if (cancelButton != null)
            cancelButton.interactable = interactable;
    }

    private void SetCancelButtonVisible(bool visible)
    {
        if (cancelButton != null)
        {
            cancelButton.gameObject.SetActive(visible);
        }
    }


    #endregion
}