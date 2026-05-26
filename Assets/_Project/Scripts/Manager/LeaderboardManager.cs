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
            LeaderBoardData.AddOrUpdateEntry(player.ClientId, player.PlayerName.ToString(), 0);
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
            LeaderBoardData.AddOrUpdateEntry(player.ClientId, player.PlayerName.ToString(), player.Coin);
        }
    }
}