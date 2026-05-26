using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LeaderboardSO", menuName = "LeaderboardSO", order = 0)]
public class LeaderboardSO : ScriptableObject {
    public List<LeaderBoardEntry> Entries = new();

    public void Clear() {
        Entries.Clear();
    }

    public void AddOrUpdateEntry(ulong playerId, string playerName, int score) {
        if (Entries == null) {
            Entries = new List<LeaderBoardEntry>();
        }
        int index = Entries.FindIndex(e => e.PlayerId == playerId);
        if (index >= 0) {
            var entry = Entries[index];
            entry.PlayerName = playerName;
            entry.Score = score;
            Entries[index] = entry;
        } else {
            Entries.Add(new LeaderBoardEntry {
                PlayerId = playerId,
                PlayerName = playerName,
                Score = score
            });
        }
    }

    [Serializable]
    public struct LeaderBoardEntry {
        public ulong PlayerId;
        public string PlayerName;
        public int Score;
    }
}