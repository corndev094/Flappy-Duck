using System;
using Unity.Collections;
using Unity.Netcode;

/// <summary>
/// Network-serializable data structure for player information.
/// Used in NetworkList<PlayerData> for lobby and match management.
/// </summary>
[Serializable]
public struct PlayerData : INetworkSerializable, IEquatable<PlayerData>
{
    public ulong ClientId;
    public FixedString64Bytes PlayerName;
    public SkinID SelectedSkin;
    public int Score;
    public bool IsReady;
    public bool IsAlive;

    public PlayerData(ulong clientId, string playerName = "", SkinID skin = SkinID.Normal)
    {
        ClientId = clientId;
        PlayerName = string.IsNullOrEmpty(playerName) ? $"Player {clientId}" : playerName;
        SelectedSkin = skin;
        Score = 0;
        IsReady = false;
        IsAlive = true;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref SelectedSkin);
        serializer.SerializeValue(ref Score);
        serializer.SerializeValue(ref IsReady);
        serializer.SerializeValue(ref IsAlive);
    }

    public bool Equals(PlayerData other)
    {
        return ClientId == other.ClientId;
    }

    public override int GetHashCode()
    {
        return ClientId.GetHashCode();
    }

    public override string ToString()
    {
        return $"[Player {ClientId}] {PlayerName} | Skin: {SelectedSkin} | Score: {Score} | Ready: {IsReady} | Alive: {IsAlive}";
    }
}
