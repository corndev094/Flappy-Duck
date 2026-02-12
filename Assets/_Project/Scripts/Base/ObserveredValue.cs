using System;

public class ObserveredValue<T> {
    private T value;
    public event Action<T, T> OnValueChanged;
    public T Value { 
        get { return value; } 
        set
        {
            if (this.value.Equals(value)) return;
            var oldValue = this.value;
            this.value = value;
            OnValueChanged?.Invoke(oldValue, value);
        }
    }

    public ObserveredValue(T initValue)
    {
        this.value = initValue;
    }
}