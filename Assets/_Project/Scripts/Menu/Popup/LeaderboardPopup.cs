using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Mono.CSharp;
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
            entry.EntryContainer.SetActive(false);
        }
        var sortedPlayers = leaderboardData.Entries.OrderByDescending(entry => entry.Score).ToArray();
        for (int i = 0; i < sortedPlayers.Length; i++)
        {
            var entryData = sortedPlayers[i];
            int playerIndex = GameFlowManager.Instance.GetPlayerIndex(entryData.PlayerId);
            if (playerIndex == -1 || string.IsNullOrEmpty(entryData.PlayerName)) continue;

            leaderboardEntries[i].PlayerName.text = $"{playerIndex + 1}. {entryData.PlayerName}";
            leaderboardEntries[i].Score.text = entryData.Score.ToString();
            leaderboardEntries[i].EntryContainer.SetActive(true);
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