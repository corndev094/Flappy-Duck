using Cysharp.Threading.Tasks;
using UnityEngine;

public class HomingMovement : IMovementBehavior
{
    [Tooltip("Mục tiêu để bám theo (thường là player)")]
    public Transform target;

    [Tooltip("Độ trễ khi bám – càng cao càng 'lì'")]
    public float homingStrength = 5f;

    public async UniTask Move(GameObject entity, Vector3 startPos, Vector3 endPos, float speed, Vector3? customDirection = null)
    {
        if (target == null)
        {
            Debug.LogWarning("HomingMovement: target is null! Falling back to linear.");
            await new LinearMovement().Move(entity, startPos, endPos, speed);
            return;
        }

        entity.transform.position = startPos;
        float maxDistance = Vector3.Distance(startPos, endPos);
        float distanceTraveled = 0f;

        while (distanceTraveled < maxDistance)
        {
            if (entity == null || target == null) return;
            // Hướng từ enemy → mục tiêu
            Vector3 toTarget = (target.position - entity.transform.position).normalized;
            
            // Cập nhật vị trí: di chuyển về phía mục tiêu, nhưng giới hạn tốc độ
            Vector3 moveDir = Vector3.Lerp(entity.transform.forward, toTarget, Time.deltaTime * homingStrength);
            EDebug.Log(() => entity);
            entity.transform.position += moveDir * speed * Time.deltaTime;
            entity.transform.forward = moveDir; // Tự động xoay theo hướng bay

            distanceTraveled += speed * Time.deltaTime;
            await UniTask.Yield();
        }

        // Đảm bảo đến đúng endPos nếu cần (tuỳ design)
        if (entity != null)
        {
            entity.transform.position = endPos;
        }
    }
}