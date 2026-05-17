using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel to display game results/rankings after match ends
/// </summary>
public class UIResultPanel : MonoBehaviour
{
    public static UIResultPanel Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text rankingText;
    [SerializeField] private Button closeButton;

    [Header("Rank Colors")]
    [SerializeField] private Color firstPlaceColor = new Color(1f, 0.84f, 0f); // Gold
    [SerializeField] private Color secondPlaceColor = new Color(0.75f, 0.75f, 0.75f); // Silver
    [SerializeField] private Color thirdPlaceColor = new Color(0.8f, 0.5f, 0.2f); // Bronze
    [SerializeField] private Color defaultColor = Color.white;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Setup buttons
        closeButton?.onClick.AddListener(OnCloseClicked);

        Hide();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Show results with player rankings
    /// </summary>
    public void ShowResult(ulong[] ranking)
    {
        if (ranking == null || ranking.Length == 0) return;

        panel?.SetActive(true);

        // Set title
        if (titleText != null)
        {
            ulong localClientId = Unity.Netcode.NetworkManager.Singleton?.LocalClientId ?? ulong.MaxValue;
            bool isWinner = ranking.Length > 0 && ranking[0] == localClientId;
            titleText.text = isWinner ? "🏆 VICTORY!" : "GAME OVER";
            titleText.color = isWinner ? firstPlaceColor : defaultColor;
        }

        // Build ranking list
        if (rankingText != null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("RANKING\n");

            for (int i = 0; i < ranking.Length; i++)
            {
                ulong clientId = ranking[i];
                string rank = GetRankSymbol(i);
                
                // Get player name from MatchFlowManager
                string playerName = GetPlayerName(clientId);
                
                // Check if this is local player
                ulong localClientId = Unity.Netcode.NetworkManager.Singleton?.LocalClientId ?? ulong.MaxValue;
                bool isLocalPlayer = clientId == localClientId;
                string indicator = isLocalPlayer ? " (You)" : "";

                sb.AppendLine($"{rank} {playerName}{indicator}");
            }

            rankingText.text = sb.ToString();
        }
    }

    /// <summary>
    /// Hide the panel
    /// </summary>
    public void Hide()
    {
        panel?.SetActive(false);
    }

    private void OnCloseClicked()
    {
        Hide();

        // Return to main menu via GameFlowManager
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.Disconnect();
        }
    }

    #region Helper Methods

    private string GetRankSymbol(int rank)
    {
        return rank switch
        {
            0 => "🥇",
            1 => "🥈",
            2 => "🥉",
            _ => $"#{rank + 1}"
        };
    }

    private Color GetRankColor(int rank)
    {
        return rank switch
        {
            0 => firstPlaceColor,
            1 => secondPlaceColor,
            2 => thirdPlaceColor,
            _ => defaultColor
        };
    }

    private string GetPlayerName(ulong clientId)
    {
        // Get name from GameFlowManager
        if (GameFlowManager.Instance != null)
        {
            var playerData = GameFlowManager.Instance.GetPlayerData(clientId);
            if (playerData.HasValue)
            {
                return playerData.Value.PlayerName.ToString();
            }
        }

        // Fallback
        return $"Player {clientId}";
    }

    #endregion
}
