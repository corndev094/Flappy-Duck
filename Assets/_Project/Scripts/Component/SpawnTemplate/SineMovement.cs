using Cysharp.Threading.Tasks;
using UnityEngine;

public class SineMovement : IMovementBehavior
{
    public async UniTask Move(GameObject entity, Vector3 startPos, Vector3 endPos, float speed, Vector3? customDirection = null)
    {
        Vector3 dir = (endPos - startPos).normalized;
        Vector3 perp = new Vector3(-dir.y, dir.x, 0);
        float distance = Vector3.Distance(startPos, endPos);
        if (distance < 0.01f) return;

        float duration = distance / speed;
        float elapsed = 0f;
        float frequency = 4f;
        float amplitude = 1f;

        while (elapsed < duration)
        {
            if (entity == null) return;
            float t = elapsed / duration;
            Vector3 straight = startPos + dir * (distance * t);
            float wave = Mathf.Sin(t * frequency * Mathf.PI * 2f) * amplitude;
            entity.transform.position = straight + perp * wave;
            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }
        if (entity != null)
        {
            entity.transform.position = endPos;
        }
    }
}