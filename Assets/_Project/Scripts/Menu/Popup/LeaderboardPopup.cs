using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.Netcode;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardPopup : ABasePopup {
    [SerializeField] private LeaderboardEntry[] leaderboardEntries = new LeaderboardEntry[4];
    [SerializeField] private LeaderboardSO leaderboardData;
    [SerializeField] private Button nextBtn;
    [SerializeField] private DOTweenAnimation dotweenAnimation;

    void OnEnable()
    {
        onOpen += UpdateLeaderboard;
        nextBtn.onClick.AddListener(NextButtonClicked);
    }

    void OnDisable()
    {
        onOpen -= UpdateLeaderboard;
        nextBtn.onClick.RemoveListener(NextButtonClicked);
    }

    protected override async UniTask PlayOpenTransition()
    {
        if (dotweenAnimation == null || dotweenAnimation.tween == null)
        {
            transform.localScale = Vector3.one;
            return;
        }
        dotweenAnimation.DORestart();
        await  dotweenAnimation.tween.AsyncWaitForCompletion();
    }

    protected override async UniTask PlayCloseTransition()
    {
        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);
    }

    private void UpdateLeaderboard()
    {
        LeaderboardManager.Instance.CopyDataToScriptableObjects();
        
        foreach (var entry in leaderboardEntries)
        {
            if (entry.EntryContainer != null)
            {
                entry.EntryContainer.SetActive(false);
            }
        }

        if (leaderboardData == null || leaderboardData.Entries == null) return;

        var sortedPlayers = leaderboardData.Entries.OrderByDescending(entry => entry.Score).ToArray();
        ulong localClientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;
        
        int displayIndex = 0;
        for (int i = 0; i < sortedPlayers.Length; i++)
        {
            var entryData = sortedPlayers[i];
            if (entryData.PlayerId == 0) continue; // Default value/empty player id
            if (displayIndex >= leaderboardEntries.Length) break;

            var uiEntry = leaderboardEntries[displayIndex];
            if (uiEntry.EntryContainer == null || uiEntry.PlayerName == null || uiEntry.Score == null)
            {
                displayIndex++;
                continue;
            }

            int rank = displayIndex + 1;
            bool isLocalPlayer = entryData.PlayerId == localClientId;
            string displayName = isLocalPlayer ? $"<b>{entryData.PlayerName} (Bạn)</b>" : entryData.PlayerName;
            
            string rankColorHex;
            switch (rank)
            {
                case 1:
                    rankColorHex = "#FFD700"; // Gold
                    break;
                case 2:
                    rankColorHex = "#C0C0C0"; // Silver
                    break;
                case 3:
                    rankColorHex = "#CD7F32"; // Bronze
                    break;
                default:
                    rankColorHex = isLocalPlayer ? "#00FF00" : "#FFFFFF"; // Green for local player, White for others
                    break;
            }

            uiEntry.PlayerName.text = $"<color={rankColorHex}>{rank}. {displayName}</color>";
            uiEntry.Score.text = $"<color={rankColorHex}>{entryData.Score}</color>";
            uiEntry.EntryContainer.SetActive(true);
            
            displayIndex++;
        }
    }

    private async void NextButtonClicked()
    {
        await Close();
        await UIManager.Instance.OpenPopup(Popup.LevelResult);
    }

    [System.Serializable]
    public struct LeaderboardEntry {
        public GameObject EntryContainer;
        public TMP_Text PlayerName;
        public TMP_Text Score;
    }
}