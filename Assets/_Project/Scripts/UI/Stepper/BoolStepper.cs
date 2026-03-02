using UnityEngine;

public class BoolStepper : Stepper<bool> {
    protected override void Start()
    {
        items = new() { true, false };
        base.Start();
    }
}
