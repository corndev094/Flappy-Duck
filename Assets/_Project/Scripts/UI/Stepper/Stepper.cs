using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public abstract class Stepper<T> : MonoBehaviour
{
    [Header("Configs")]
    [SerializeField, Min(0)] protected float spacing;
    [SerializeField] protected Direction direction = Direction.Horizontal;
    [SerializeField] protected Ease easeType = Ease.Linear;
    [SerializeField] protected float duration;

    [Header("References")]
    [SerializeField] protected RectTransform viewPort;
    [SerializeField] protected RectTransform content;
    [SerializeField] protected RectTransform itemPrefab;
    [SerializeField] protected Button previousButton;
    [SerializeField] protected Button nextButton;
    public UnityEvent<T> OnItemSelected;
    [SerializeField] protected List<T> items = new();

    [Header("SFX")]
    [SerializeField] private AudioClip navigateSfx;

    protected int TotalChildren => items.Count;
    protected int _currentSelectedItem;
    protected Tweener _tweener;

    void OnEnable()
    {
        Debug.Log($"{viewPort.sizeDelta}");
    }

    protected virtual void Awake()
    {
        if (direction == Direction.Horizontal)
        {
            content.pivot = new Vector2(0, 0.5f);
            content.anchorMin = new Vector2(0, 0);
            content.anchorMax = new Vector2(0, 1);
            content.offsetMin = new Vector2(content.offsetMin.x, 0);
            content.offsetMax = new Vector2(content.offsetMax.x, 0);
            var contentWidth = (viewPort.sizeDelta.x * TotalChildren) + (spacing * (TotalChildren - 1));
            content.sizeDelta = new Vector2(contentWidth, content.sizeDelta.y);
        }
        else if (direction == Direction.Vertical)
        {
            content.pivot = new Vector2(0.5f, 1);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.offsetMin = new Vector2(content.offsetMin.x, 0);
            content.offsetMax = new Vector2(content.offsetMax.x, 0);
            var contentHeight = (viewPort.sizeDelta.y * TotalChildren) + (spacing * (TotalChildren - 1));
            content.sizeDelta = new Vector2(content.sizeDelta.x, contentHeight);
        }

        content.anchoredPosition = Vector2.zero;

        if (!content.TryGetComponent(out HorizontalOrVerticalLayoutGroup layoutGroup))
        {
            if (direction == Direction.Horizontal)
            {
                layoutGroup = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            }
            else
            {
                layoutGroup = content.gameObject.AddComponent<VerticalLayoutGroup>();
            }
        }
        
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.spacing = spacing;

        for (var i = 0; i < TotalChildren; i++)
        {
            var item = Instantiate(itemPrefab, content);
            item.name = $"Item {i}: {items[i]}";
            item.GetComponentInChildren<TMP_Text>().SetText(items[i].ToString());
        }
    }

    public void SetupList(List<T> list)
    {
        items = list;
    }

    protected void Start()
    {
        previousButton.onClick.AddListener(MovePrevious);
        nextButton.onClick.AddListener(MoveNext);
        UpdateButtonInteractable();
    }

    protected void OnDestroy()
    {
        _tweener?.Kill();
    }

    protected void MovePrevious()
    {
        if (_currentSelectedItem <= 0) return;
        SoundManager.Instance.PlaySFX(navigateSfx);
        _currentSelectedItem--;
        Move();
    }

    protected void MoveNext()
    {
        if (_currentSelectedItem >= TotalChildren - 1) return;
        SoundManager.Instance.PlaySFX(navigateSfx);
        _currentSelectedItem++;
        Move();
    }

    protected void Move()
    {
        _tweener?.Kill();
        Vector2 targetPosition = Vector2.zero;

        if (direction == Direction.Horizontal)
        {
            float targetX = -(_currentSelectedItem * (viewPort.sizeDelta.x + spacing));
            targetPosition = new Vector2(targetX, content.anchoredPosition.y);
        }
        else if (direction == Direction.Vertical)
        {
            float targetY = _currentSelectedItem * (viewPort.sizeDelta.y + spacing);
            targetPosition = new Vector2(content.anchoredPosition.x, targetY);
        }

        _tweener = content.DOAnchorPos(targetPosition, duration)
            .SetEase(easeType)
            .OnComplete(UpdateButtonInteractable);
        OnItemSelected?.Invoke(items[_currentSelectedItem]);
        
        UpdateButtonInteractable();
    }

    protected void JumpToItem(int index)
    {
        if (index < 0 || index >= TotalChildren) return;
        _currentSelectedItem = index;
        Vector2 targetPosition = Vector2.zero;

        if (direction == Direction.Horizontal)
        {
            float targetX = -(_currentSelectedItem * (viewPort.sizeDelta.x + spacing));
            targetPosition = new Vector2(targetX, content.anchoredPosition.y);
        }
        else if (direction == Direction.Vertical)
        {
            float targetY = _currentSelectedItem * (viewPort.sizeDelta.y + spacing);
            targetPosition = new Vector2(content.anchoredPosition.x, targetY);
        }

        content.anchoredPosition = targetPosition;
        OnItemSelected?.Invoke(items[_currentSelectedItem]);
        UpdateButtonInteractable();
    }

    protected void UpdateButtonInteractable()
    {
        previousButton.interactable = _currentSelectedItem > 0;
        nextButton.interactable = _currentSelectedItem < TotalChildren - 1;
    }

    public void SetSelectedItem(T item, bool animate = true)
    {
        int index = items.IndexOf(item);
        if (index == -1) return;
        _currentSelectedItem = index;
        if (animate)
        {
            Move();
        }
        else
        {
            JumpToItem(index);
        }
    }

    public enum Direction { Horizontal, Vertical }
}