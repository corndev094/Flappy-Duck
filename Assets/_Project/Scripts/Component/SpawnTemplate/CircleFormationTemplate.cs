using System.Collections.Generic;
using UnityEngine;

public class CircleFormation : IFormationTemplate
{
    public List<Vector3> GetLocalOffsets(int count, float radius)
    {
        var offsets = new List<Vector3>();
        for (int i = 0; i < count; i++)
        {
            float angle = i * (360f / count) * Mathf.Deg2Rad;
            offsets.Add(new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius);
        }
        return offsets;
    }
}