using System;
using UnityEngine;

public class StickToPipeEnemy : ABaseEnemy
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform shootGameObject;
    [SerializeField] private float shootTimeElapse = 1;

    private float time = 0;
    
    private void Update()
    {
        time += Time.deltaTime;
        if (time >= shootTimeElapse)
        {
            time = 0;
            var bullet = Instantiate(bulletPrefab, shootGameObject.transform.position, shootGameObject.transform.rotation);
        }
    }
}