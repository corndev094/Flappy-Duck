using System;

public enum SplinePointType
{
    Level,
    Coin,
    Special
}

[Serializable]
public class SplinePointData
{
    public SplinePointType pointType;
    public int pointId;
    public bool isCollected;
}
