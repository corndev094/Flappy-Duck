using UnityEngine;

public static class Extension {
    public static float FromSecondsToMinus(this float seconds)
    {
        return seconds / 60;
    }
}