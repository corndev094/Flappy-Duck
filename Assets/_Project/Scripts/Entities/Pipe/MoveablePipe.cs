using UnityEngine;

public class MoveablePipe : PipeDecorator {
    [SerializeField] private float moveSpeed = 3;
    [SerializeField] private float moveDistance = 1f;

    private float initialY;

    private void Start()
    {
        initialY = transform.position.y;
    }

    private void Update()
    {
        float yOffset = Mathf.Sin(Time.time * moveSpeed) * moveDistance;
        transform.position = new Vector3(transform.position.x, initialY + yOffset, transform.position.z);
    }

    protected override void BeforeSetup()
    {
        // No action needed before setup
    }

    protected override void AfterSetup()
    {
        // No action needed after setup
    }

    private void Reset() {
        if (!gameObject.name.Contains("[MoveablePipe]"))
        {
            gameObject.name = "[MoveablePipe]" + gameObject.name;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position + new Vector3(0, moveDistance, 0), 0.1f);
        Gizmos.DrawSphere(transform.position - new Vector3(0, moveDistance, 0), 0.1f);
    }
}