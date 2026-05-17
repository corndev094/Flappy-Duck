using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

public class LeaderboardManager : NetworkSingleton<LeaderboardManager>
{
    [field: SerializeField, ReadOnly] public LeaderboardSO LeaderBoardData { get; set; }
    public void Setup()
    {
        LeaderBoardData.Clear();
        for (int i = 0; i < GameFlowManager.Instance.PlayerList.Count; i++)
        {
            var player = GameFlowManager.Instance.PlayerList[i];
            LeaderBoardData.Entries[i] = new LeaderboardSO.LeaderBoardEntry
            {
                PlayerId = player.ClientId,
                PlayerName = player.PlayerName.ToString(),
                Score = 0
            };
        }
    }

    [ClientRpc]
    public void CopyDataToScriptableObjectsClientRpc()
    {
        CopyDataToScriptableObjects();
    }

    public void CopyDataToScriptableObjects()
    {
        for (int i = 0; i < GameFlowManager.Instance.PlayerList.Count; i++)
        {
            var player = GameFlowManager.Instance.PlayerList[i];
            var entry = LeaderBoardData.Entries[i];

            entry.PlayerId = player.ClientId;
            entry.PlayerName = player.PlayerName.ToString();
            entry.Score = player.Coin;
        }
    }
}