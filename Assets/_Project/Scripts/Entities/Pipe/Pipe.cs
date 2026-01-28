using UnityEngine;

public class Pipe : MonoBehaviour, IPipe {
    [SerializeField] private Transform abovePipe, underPipe;

    private Vector3 abovePipeInitialPos;
    private Vector3 underPipeInitialPos;
    
    private float pipesOffset;

    private void Awake() {
        abovePipeInitialPos = abovePipe.localPosition;
        underPipeInitialPos = underPipe.localPosition;
    }

    public virtual void Setup(float offset) {
        this.pipesOffset = offset;
        UpdatePipePositions(this.pipesOffset);
    }

    private void UpdatePipePositions(float offset) {
        abovePipe.localPosition = abovePipeInitialPos + new Vector3(0, offset, 0);
        underPipe.localPosition = underPipeInitialPos - new Vector3(0, offset, 0);
    }
}