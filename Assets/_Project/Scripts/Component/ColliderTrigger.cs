using UnityEngine;
using UnityEngine.Events;

public class ColliderTrigger : MonoBehaviour {
    public UnityEvent onTriggerEnter;
    public UnityEvent onTriggerStay;
    public UnityEvent onTriggerExit;

    private Collider2D col;

#if UNITY_EDITOR

    void OnValidate()
    {
        if (col == null)
        {
            col = GetComponent<Collider2D>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider2D>();
            }
        }
    }

#endif

    private void OnTriggerEnter2D(Collider2D other) {
        onTriggerEnter.Invoke();
    }

    private void OnTriggerStay2D(Collider2D other) {
        onTriggerStay.Invoke();
    }

    private void OnTriggerExit2D(Collider2D other) {
        onTriggerExit.Invoke();
    }
}