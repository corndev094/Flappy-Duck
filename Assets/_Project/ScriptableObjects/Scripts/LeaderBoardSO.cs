using System;
using UnityEngine;

[CreateAssetMenu(fileName = "LeaderboardSO", menuName = "LeaderboardSO", order = 0)]
public class LeaderboardSO : ScriptableObject {
    public LeaderBoardEntry[] Entries = new LeaderBoardEntry[4];

    public void Clear() {
        for (int i = 0; i < Entries.Length; i++) {
            Entries[i].PlayerId = 0;
            Entries[i].PlayerName = "";
            Entries[i].Score = 0;
        }
    }

    [Serializable]
    public struct LeaderBoardEntry {
        public ulong PlayerId;
        public string PlayerName;
        public int Score;
    }
}