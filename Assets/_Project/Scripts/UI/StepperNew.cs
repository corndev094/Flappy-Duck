using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class StepperNew : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Configs")]
    [SerializeField, Min(0)] private float spacing;
    [SerializeField] private Direction direction = Direction.Horizontal;
    [SerializeField]private Ease easeType = Ease.Linear;
    [SerializeField] private float duration;
    [SerializeField] private float dragThreshold = 50f; // Ngưỡng để xác định khi nào vuốt đủ để chuyển item
    [SerializeField] private bool enableDragScrolling = true; // Bật/tắt tính năng cuộn bằng cảm ứng

    [Header("References")]
    [SerializeField] private RectTransform content;
    [SerializeField] private RectTransform itemPrefab;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private RectTransform viewport; // Khung nhìn của stepper
    [SerializeField] private List<string> items = new();

    // Delegate cho sự kiện khi item thay đổi
    public delegate void OnItemChanged(int newIndex);
    public event OnItemChanged OnItemChangedEvent;

    private int TotalChildren => items.Count;
    private int _currentSelectedItem;
    private Tweener _tweener;
    private Vector2 _startDragPosition;
    private bool _isDragging;
    private int _targetItemIndex = -1;

    private void Awake()
    {
        // Kiểm tra các tham số bắt buộc
        if (content == null || itemPrefab == null || previousButton == null || nextButton == null)
        {
            Debug.LogError("Missing required references in Stepper component!");
            enabled = false;
            return;
        }

        // Nếu viewport không được thiết lập, sử dụng transform của chính component này
        if (viewport == null)
        {
            viewport = GetComponent<RectTransform>();
        }

        // Thiết lập layout ban đầu
        SetupLayout();

        // Tạo các item ban đầu
        CreateItems();
    }

    /// <summary>
    /// Thiết lập layout cho stepper
    /// </summary>
    private void SetupLayout()
    {
        if (direction == Direction.Horizontal)
        {
            content.pivot = new Vector2(0, 0.5f);
            content.anchorMin = new Vector2(0, 0);
            content.anchorMax = new Vector2(0, 1);
            var itemWidth = itemPrefab.sizeDelta.x;
            var contentWidth = (itemWidth * TotalChildren) + (spacing * (TotalChildren - 1));
            content.sizeDelta = new Vector2(contentWidth, itemPrefab.sizeDelta.y);
        }
        else if (direction == Direction.Vertical)
        {
            content.pivot = new Vector2(0.5f, 1);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            var itemHeight = itemPrefab.sizeDelta.y;
            var contentHeight = (itemHeight * TotalChildren) + (spacing * (TotalChildren - 1));
            content.sizeDelta = new Vector2(itemPrefab.sizeDelta.x, contentHeight);
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
    }

    /// <summary>
    /// Tạo các item từ danh sách
    /// </summary>
    private void CreateItems()
    {
        // Xóa các item cũ nếu có
        DestroyItems();

        // Kiểm tra nếu không có items
        if (items.Count == 0)
        {
            Debug.LogWarning("No items found in Stepper!");
            return;
        }

        // Tạo các item mới
        for (var i = 0; i < TotalChildren; i++)
        {
            var item = Instantiate(itemPrefab, content);

            // Nếu item có component Text, cập nhật nội dung
            if (item.TryGetComponent<Text>(out var textComponent))
            {
                textComponent.text = items[i];
            }

            // Nếu item có component Button, thêm sự kiện click
            if (item.TryGetComponent<Button>(out var buttonComponent))
            {
                int index = i; // Capture index for closure
                buttonComponent.onClick.AddListener(() => SelectItem(index));
            }
        }
    }

    private void Start()
    {
        previousButton?.onClick.AddListener(MovePrevious);
        nextButton?.onClick.AddListener(MoveNext);
        UpdateButtonInteractable();

        // Nếu có items, chọn item đầu tiên
        if (items.Count > 0)
        {
            SelectItem(0);
        }
    }

    private void OnDestroy()
    {
        // Hủy các animation đang hoạt động
        _tweener?.Kill();

        // Hủy các item để tránh rò rỉ bộ nhớ
        DestroyItems();

        // Hủy các sự kiện
        previousButton?.onClick.RemoveListener(MovePrevious);
        nextButton?.onClick.RemoveListener(MoveNext);
    }

    /// <summary>
    /// Chọn một item cụ thể
    /// </summary>
    /// <param name="index">Index của item cần chọn</param>
    public void SelectItem(int index)
    {
        // Kiểm tra index hợp lệ
        if (index < 0 || index >= TotalChildren)
        {
            Debug.LogWarning($"Invalid item index: {index}. Must be between 0 and {TotalChildren - 1}.");
            return;
        }

        // Nếu đã chọn item này, không làm gì cả
        if (index == _currentSelectedItem)
        {
            return;
        }

        _currentSelectedItem = index;
        Move();

        // Kích hoạt sự kiện khi item thay đổi
        OnItemChangedEvent?.Invoke(_currentSelectedItem);
    }

    private void MovePrevious()
    {
        if (_currentSelectedItem <= 0) return;
        _currentSelectedItem--;
        Move();
    }

    private void MoveNext()
    {
        if (_currentSelectedItem >= TotalChildren - 1) return;
        _currentSelectedItem++;
        Move();
    }

    private void Move()
    {
        // Hủy animation cũ nếu đang chạy
        _tweener?.Kill();

        // Xác định vị trí mục tiêu
        Vector2 targetPosition = Vector2.zero;

        if (direction == Direction.Horizontal)
        {
            float targetX = -(_currentSelectedItem * (itemPrefab.sizeDelta.x + spacing));
            targetPosition = new Vector2(targetX, content.anchoredPosition.y);
        }
        else if (direction == Direction.Vertical)
        {
            float targetY = _currentSelectedItem * (itemPrefab.sizeDelta.y + spacing);
            targetPosition = new Vector2(content.anchoredPosition.x, targetY);
        }

        _tweener = content.DOAnchorPos(targetPosition, duration)
            .SetEase(easeType)
            .OnComplete(() => UpdateButtonInteractable());
    }

    private void UpdateButtonInteractable()
    {
        if (previousButton != null) previousButton.interactable = _currentSelectedItem > 0;
        if (nextButton != null) nextButton.interactable = _currentSelectedItem < TotalChildren - 1;
    }

    private void DestroyItems()
    {
        if (content == null) return;
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            if (content.GetChild(i) != null)
            {
                DestroyImmediate(content.GetChild(i).gameObject);
            }
        }
    }

    // Phương thức xử lý khi bắt đầu kéo
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!enableDragScrolling) return;

        _isDragging = true;
        _startDragPosition = eventData.position;
        _targetItemIndex = -1;

        // Hủy animation đang chạy
        _tweener?.Kill();
    }

    // Phương thức xử lý khi đang kéo
    public void OnDrag(PointerEventData eventData)
    {
        if (!enableDragScrolling || !_isDragging) return;

        // Tính toán khoảng cách kéo
        Vector2 currentPosition = eventData.position;
        float dragDistance = Vector2.Distance(_startDragPosition, currentPosition);

        // Nếu kéo đủ ngưỡng, xác định item mục tiêu
        if (dragDistance > dragThreshold)
        {
            // Xác định hướng kéo
            Vector2 dragDirection = currentPosition - _startDragPosition;

            // Xác định item tiếp theo
            if (direction == Direction.Horizontal)
            {
                if (dragDirection.x > 0) // Kéo sang phải
                {
                    _targetItemIndex = Mathf.Max(0, _currentSelectedItem - 1);
                }
                else // Kéo sang trái
                {
                    _targetItemIndex = Mathf.Min(TotalChildren - 1, _currentSelectedItem + 1);
                }
            }
            else // Vertical
            {
                if (dragDirection.y > 0) // Kéo xuống dưới
                {
                    _targetItemIndex = Mathf.Max(0, _currentSelectedItem - 1);
                }
                else // Kéo lên trên
                {
                    _targetItemIndex = Mathf.Min(TotalChildren - 1, _currentSelectedItem + 1);
                }
            }

            // Nếu có item mục tiêu hợp lệ, chọn nó
            if (_targetItemIndex != -1 && _targetItemIndex != _currentSelectedItem)
            {
                _isDragging = false;
                SelectItem(_targetItemIndex);
            }
        }
    }

    // Phương thức xử lý khi kết thúc kéo
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!enableDragScrolling) return;

        _isDragging = false;
        _targetItemIndex = -1;

        // Đảm bảo các nút được cập nhật
        UpdateButtonInteractable();
    }

    /// <summary>
    /// Cập nhật danh sách items và làm mới UI
    /// </summary>
    /// <param name="newItems">Danh sách items mới</param>
    public void UpdateItems(List<string> newItems)
    {
        items = newItems ?? new List<string>();

        // Cập nhật layout
        SetupLayout();

        // Tạo lại các item
        CreateItems();

        // Đảm bảo chọn item hợp lệ
        if (_currentSelectedItem >= items.Count)
        {
            _currentSelectedItem = Mathf.Max(0, items.Count - 1);
        }

        // Di chuyển đến item hiện tại
        Move();
    }

    public enum Direction { Horizontal, Vertical }
}
