using UnityEngine;

public class PipeWorm : ABaseEnemy {
    [SerializeField] private GameObject idleStateSprite;
    [SerializeField] private GameObject attackStateSprite;
    [SerializeField] private float attackInterval = 2;

    private float time;

    void Start()
    {
        idleStateSprite.gameObject.SetActive(true);
        attackStateSprite.gameObject.SetActive(false);
    }

    void Update()
    {
        time += Time.deltaTime;
        if (time >= attackInterval)
        {
            time = 0;
            ChangeState();
        }
    }

    private void ChangeState()
    {
        idleStateSprite.gameObject.SetActive(!idleStateSprite.gameObject.activeSelf);
        attackStateSprite.gameObject.SetActive(!attackStateSprite.gameObject.activeSelf);
    }
}