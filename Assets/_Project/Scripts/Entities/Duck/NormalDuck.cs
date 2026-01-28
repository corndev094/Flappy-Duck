using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nguyen.Event;
using UnityEngine;

public class NormalDuck : ABaseDuck
{
    [SerializeField] private float detectRadius;
    [SerializeField] private ContactFilter2D detectFilter;

    private List<Collider2D> detectedEnemies = new();

    private void AutoAim(Transform enemy)
    {
        var dir = (enemy.transform.position - shootPosition.transform.position).normalized;
        var deg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        shootPosition.localEulerAngles = new Vector3(0, 0, deg);
    }

    private bool DetectNearestEnemy(out Transform nearestEnemy)
    {
        nearestEnemy = null;
        if (Physics2D.OverlapCircle(transform.position, detectRadius, detectFilter, detectedEnemies) > 0)
        {
            float nearestDist = float.MaxValue;
            var detectedRadiusSqrt = detectRadius * detectRadius;
            foreach(var enemy in detectedEnemies)
            {
                var dir = transform.position - enemy.transform.position;
                var sqrtMag = Vector2.SqrMagnitude(dir);
                if (sqrtMag < nearestDist && sqrtMag < Mathf.Abs(detectedRadiusSqrt))
                {
                    nearestDist = sqrtMag;
                    nearestEnemy = enemy.transform;
                }
            }
        }
        return nearestEnemy != null;
    }
}