using System;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

public class CoinCollectionUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private float popScaleDuration = 0.25f;

    private int currentAmount;
    public event Action<int> OnCoinCollected;

    void Awake()
    {
        gameObject.SetActive(false);
    }

    // Hiển thị coin nhận được (không có button)
    public void Show(int amount)
    {
        currentAmount = amount;
        if (amountText != null)
            amountText.text = $"You got {amount}";

        gameObject.SetActive(true);

        // Optional: nhỏ gọn animation mở lên
        if (transform.localScale.x > 0)
            transform.localScale = Vector3.one * 0.8f;
        transform.DOScale(1f, popScaleDuration).SetEase(DG.Tweening.Ease.OutBack);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // Click để nhận coin (không cần button, chỉ click vào UI coin)
    public void OnPointerClick(PointerEventData eventData)
    {
        OnCoinCollected?.Invoke(currentAmount);
        // Ẩn UI sau khi nhận
        Hide();
    }

    // Optional: helper cho chơi animation khi nhận coin, nếu muốn gọi từ LevelMapMenu
    public void PlayReceiveAnimation(int amount)
    {
        // Implement thêm animation nếu cần, hiện tại Show() đã có hiệu ứng mở/nhỏ
        Show(amount);
    }
}