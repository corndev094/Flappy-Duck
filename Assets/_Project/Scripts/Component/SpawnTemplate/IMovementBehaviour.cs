using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IMovementBehavior
{
    UniTask Move(GameObject entity, Vector3 startPos, Vector3 endPos, float speed, Vector3? customDirection = null);
}