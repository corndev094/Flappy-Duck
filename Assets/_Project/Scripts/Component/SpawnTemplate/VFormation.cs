using System.Collections.Generic;
using UnityEngine;

public class VFormation : IFormationTemplate
{
    public List<Vector3> GetLocalOffsets(int count, float spacing)
    {
        var offsets = new List<Vector3> { Vector3.zero };
        int wings = (count - 1) / 2;
        for (int i = 1; i <= wings; i++)
        {
            float d = i * spacing;
            offsets.Add(new Vector3(-d, -d, 0));
            if (offsets.Count < count) offsets.Add(new Vector3(d, -d, 0));
        }
        return offsets;
    }
}