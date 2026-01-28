using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TwoStateToggle : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button offBtn;
    [SerializeField] private Button onBtn;

    [Header("Backgrounds")]
    [SerializeField] private Image offBg;
    [SerializeField] private Image onBg;

    [Header("Colors")]
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(0.7f, 0.7f, 0.7f);

    public Action<bool> OnValueChanged;

    bool isOn = true;

    void Start()
    {
        offBtn.onClick.AddListener(() => SetState(false));
        onBtn.onClick.AddListener(() => SetState(true));
    }

    public void SetStateImmediately(bool on)
    {
        OnValueChanged?.Invoke(on);
        isOn = on;

        onBg.color  = on ? activeColor : inactiveColor;
        offBg.color = on ? inactiveColor : activeColor;
    }

    public void SetState(bool on)
    {
        SetStateImmediately(on);
        if (on)
        {
            onBg.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);
        }
        else
        {
            offBg.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);
        }
    }
}
