using UnityEngine;

public abstract class ADescription : ScriptableObject {
    [TextArea(5, 20)]
    public string Description;
}