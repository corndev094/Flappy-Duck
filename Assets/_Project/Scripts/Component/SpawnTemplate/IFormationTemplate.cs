using System.Collections.Generic;
using UnityEngine;

public interface IFormationTemplate
{
    List<Vector3> GetLocalOffsets(int count, float scale);
}