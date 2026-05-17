using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;

public class SimpleTabNavigation : MonoBehaviour
{
    [SerializeField] private List<TabInfo> tabs;
    private CanvasGroup currentPanelGroup;
    public float fadeDuration = 0.3f;
    private int currentTab = 0;
    private bool isTransitioning = false;
    public Action<TabName> OnTabChanged;

    private void Awake()
    {
        if (tabs == null || tabs.Count == 0)
        {
            Debug.LogError("No tabs assigned!", this);
            return;
        }

        // Kiểm tra chỉ số currentTab hợp lệ
        if (currentTab < 0 || currentTab >= tabs.Count)
        {
            currentTab = 0; // Reset về 0 nếu sai
            Debug.LogWarning("Invalid initial tab index. Defaulting to 0.", this);
        }

        for (int i = 0; i < tabs.Count; i++)
        {
            var tab = tabs[i];

            // Null check bắt buộc
            if (tab.tabButton == null)
                Debug.LogError($"Tab {i}: Missing Button!", this);
            if (tab.tabPanel == null)
                Debug.LogError($"Tab {i}: Missing Panel!", this);

            // Gắn sự kiện click
            int idx = i;
            tab.tabButton?.onClick.AddListener(() => ShowTabAsync(idx).Forget());

            // Chuẩn bị CanvasGroup
            if (tab.tabPanel == null) continue;
            var group = GetOrCreateCanvasGroup(tab.tabPanel);
            group.alpha = i == currentTab ? 1 : 0;
            tab.tabPanel.SetActive(i == currentTab);

            // Cập nhật giao diện nút tab
            UpdateTabVisual(i, i == currentTab);
        }

        // Gán panel đầu tiên làm currentPanelGroup
        if (tabs[currentTab].tabPanel != null)
        {
            currentPanelGroup = GetOrCreateCanvasGroup(tabs[currentTab].tabPanel);
        }
    }

    public async UniTask ShowTabAsync(int tabIndex)
    {
        if (isTransitioning || tabs == null || tabIndex == currentTab || tabIndex < 0 || tabIndex >= tabs.Count)
            return;

        isTransitioning = true;
        currentTab = tabIndex;

        var newPanel = tabs[tabIndex].tabPanel;
        if (newPanel == null)
        {
            isTransitioning = false;
            return;
        }
        var newGroup = GetOrCreateCanvasGroup(newPanel);

        // Fade out panel cũ
        if (currentPanelGroup != null)
        {
            var tween = currentPanelGroup.DOFade(0, fadeDuration).SetLink(gameObject);
            await UniTask.Delay(TimeSpan.FromSeconds(fadeDuration), cancellationToken: this.GetCancellationTokenOnDestroy());
            currentPanelGroup.gameObject.SetActive(false); // Sau khi fade out xong
        }

        // Show & fade in panel mới
        newPanel.SetActive(true);
        newGroup.alpha = 0;
        var fadeInTween = newGroup.DOFade(1, fadeDuration).SetLink(gameObject);
        await UniTask.Delay(TimeSpan.FromSeconds(fadeDuration), cancellationToken: this.GetCancellationTokenOnDestroy());

        currentPanelGroup = newGroup;

        // Cập nhật giao diện
        for (int i = 0; i < tabs.Count; i++)
        {
            UpdateTabVisual(i, i == tabIndex);
        }

        OnTabChanged?.Invoke(tabs[tabIndex].tabName);
        isTransitioning = false;
    }

    private CanvasGroup GetOrCreateCanvasGroup(GameObject panel)
    {
        if (panel.TryGetComponent<CanvasGroup>(out var group)) return group;
        return panel.AddComponent<CanvasGroup>();
    }

    private void UpdateTabVisual(int index, bool isSelected)
    {
        var tab = tabs[index];
        if (tab.tabBackground != null)
        {
            tab.tabBackground.color = isSelected ? tab.selectedColor : tab.normalColor;
        }
        if (tab.tabButton != null)
        {
            tab.tabButton.interactable = !isSelected;
        }
    }
}

[System.Serializable]
public struct TabInfo
{
    public Button tabButton;
    public GameObject tabPanel;
    public TabName tabName;
    public Image tabBackground;
    public Color normalColor;
    public Color selectedColor;
}

public enum TabName
{
    Join,
    Create
}
