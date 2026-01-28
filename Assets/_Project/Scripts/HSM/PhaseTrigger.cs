using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PhaseTrigger : MonoBehaviour {
    [SerializeField] private APhase phase;
    [SerializeField] private Collider2D col;

    void Awake()
    {
        col ??= GetComponent<Collider2D>(); 
    }

    void Start()
    {
        if (!col.isTrigger) col.isTrigger = true;
    }

    void Reset()
    {
        gameObject.tag = "PhaseTrigger";
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            phase.StartPhase();
        }
    }
}