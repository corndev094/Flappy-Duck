using Cysharp.Threading.Tasks;
using UnityEngine;

public class OrbitMovement : IMovementBehavior
{
    public Transform center;      // Tâm quay (player, boss, v.v.)
    public float radius = 3f;     // Bán kính quỹ đạo
    public float angularSpeed = 60f; // Độ/giây
    public bool clockwise = true;

    public async UniTask Move(GameObject entity, Vector3 startPos, Vector3 endPos, float speed, Vector3? customDirection = null)
    {
        if (center == null)
        {
            Debug.LogWarning("OrbitMovement: center is null! Falling back to linear.");
            await new LinearMovement().Move(entity, startPos, endPos, speed);
            return;
        }

        // Tính góc ban đầu từ center → startPos
        Vector3 toStart = startPos - center.position;
        float currentAngle = Mathf.Atan2(toStart.y, toStart.x) * Mathf.Rad2Deg;

        float duration = 5f; // Hoặc dùng distance / speed nếu cần
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (entity == null || center == null) return;
            currentAngle += (clockwise ? -1 : 1) * angularSpeed * Time.deltaTime;
            float rad = currentAngle * Mathf.Deg2Rad;
            Vector3 pos = center.position + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * radius;
            entity.transform.position = pos;

            // Xoay enemy hướng ra ngoài (hoặc vào trong)
            Vector3 lookDir = (pos - center.position).normalized;
            entity.transform.right = lookDir; // hoặc .forward tuỳ pivot

            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }
    }
}