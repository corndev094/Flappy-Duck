using UnityEngine;

public abstract class PipeDecorator : MonoBehaviour, IPipe
{
    protected IPipe decoratedPipe;

    protected virtual void Awake()
    {
        var pipes = GetComponents<IPipe>();
        int myIndex = System.Array.IndexOf(pipes, this);
        if (myIndex > 0)
        {
            decoratedPipe = pipes[myIndex - 1];
        }
        else
        {
            Debug.LogError("PipeDecorator must be placed after another IPipe component to decorate.", this);
        }
    }

    public void Setup(float offset)
    {
        BeforeSetup();
        decoratedPipe?.Setup(offset);
        AfterSetup();
    }

    protected abstract void BeforeSetup();
    protected abstract void AfterSetup();
}