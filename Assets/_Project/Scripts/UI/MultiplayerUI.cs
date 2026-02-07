using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Text;

public class MultiplayerUI : MonoBehaviour
{
    [Header("Main Panel")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Lobby Panel")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private TMP_Text joinCodeText;
    [SerializeField] private TMP_Text playerListText;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveButton;

    private void Start()
    {
        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
        startGameButton.onClick.AddListener(OnStartGameClicked);
        leaveButton.onClick.AddListener(OnLeaveClicked);
        
        mainPanel.SetActive(true);
        lobbyPanel.SetActive(false);
    }
    
    private void OnDestroy()
    {
        if (MatchFlowManager.Instance != null)
        {
            MatchFlowManager.Instance.PlayerList.OnListChanged -= OnPlayerListChanged;
        }
    }

    private async void OnHostClicked()
    {
        feedbackText.text = "Creating lobby...";
        string joinCode = await RelayManager.Instance.CreateHost();

        if (!string.IsNullOrEmpty(joinCode))
        {
            mainPanel.SetActive(false);
            lobbyPanel.SetActive(true);
            joinCodeText.text = $"Join Code: {joinCode}";
            startGameButton.gameObject.SetActive(true);
            MatchFlowManager.Instance.PlayerList.OnListChanged += OnPlayerListChanged;
            UpdatePlayerListUI();
        }
        else
        {
            feedbackText.text = "Failed to create lobby.";
        }
    }

    private async void OnJoinClicked()
    {
        string code = joinCodeInput.text;
        if (string.IsNullOrWhiteSpace(code))
        {
            feedbackText.text = "Please enter a Join Code.";
            return;
        }

        feedbackText.text = $"Joining lobby with code {code}...";
        bool success = await RelayManager.Instance.JoinGame(code);
        
        if (success)
        {
            mainPanel.SetActive(false);
            lobbyPanel.SetActive(true);
            joinCodeText.text = $"Joined with code: {code}";
            startGameButton.gameObject.SetActive(false); // Only host can start
            MatchFlowManager.Instance.PlayerList.OnListChanged += OnPlayerListChanged;
        }
        else
        {
            feedbackText.text = "Failed to join lobby. Check the code and try again.";
        }
    }
    
    private void OnStartGameClicked()
    {
        // Example: load level 1. A real implementation would have level selection.
        MatchFlowManager.Instance.StartGameServerRpc();
    }
    
    private void OnLeaveClicked()
    {
        NetworkManager.Singleton.Shutdown();
        mainPanel.SetActive(true);
        lobbyPanel.SetActive(false);
        if (MatchFlowManager.Instance != null)
        {
            MatchFlowManager.Instance.PlayerList.OnListChanged -= OnPlayerListChanged;
        }
    }

    private void OnPlayerListChanged(NetworkListEvent<PlayerData> changeEvent)
    {
        UpdatePlayerListUI();
    }

    private void UpdatePlayerListUI()
    {
        if (playerListText == null) return;

        StringBuilder sb = new StringBuilder("Players:\n");
        foreach (var player in MatchFlowManager.Instance.PlayerList)
        {
            sb.AppendLine(player.PlayerName.ToString());
        }
        playerListText.text = sb.ToString();
    }
}
