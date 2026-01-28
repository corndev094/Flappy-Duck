using System.Collections.Generic;
using UnityEngine;

public class SingleFormation : IFormationTemplate
{
    public List<Vector3> GetLocalOffsets(int count, float scale) => new() { Vector3.zero };
}