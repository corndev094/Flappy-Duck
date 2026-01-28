using Cysharp.Threading.Tasks;
using UnityEngine;

public class LinearMovement : IMovementBehavior
{
    public async UniTask Move(GameObject entity, Vector3 startPos, Vector3 endPos, float speed, Vector3? customDirection = null)
    {
        float dist = Vector3.Distance(startPos, endPos);
        if (dist < 0.01f) return;
        
        float duration = dist / speed;
        float elapsed = 0f;
        entity.transform.position = startPos;

        while (elapsed < duration)
        {
            if (entity == null) return;
            entity.transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }
        if (entity != null)
        {
            entity.transform.position = endPos;
        }
    }
}