using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Dreamteck.Splines;
using UnityEngine;
using UnityEngine.UI;

public class LevelMapMenu : ABaseMenu
{
    #region Fields

    [Header("Component References")]
    [SerializeField] private SplineComputer pathSpline; // Đường dẫn spline để định vị các level
    [SerializeField] private Transform levelsContainer; // Container chứa các nút level (thường là con của content trong ScrollRect)
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform duckIcon; // Icon con vịt di chuyển
    [SerializeField] private Button returnBtn, playBtn;
    [SerializeField] private CoinCollectionUI coinCollectionUI; // UI hiện thông báo nhận coin

    [Header("Asset References")]
    [SerializeField] private LevelItem levelThumbPrefab;
    [SerializeField] private LevelListSO levelListData;

    [Header("Configuration")]
    [SerializeField] private float moveDuration = 1.5f; // Thời gian di chuyển của con vịt giữa các level
    [SerializeField] private Ease moveEase = Ease.InOutQuad;
    [SerializeField] private int coinRewardAmount = 1; // lượng coin nhận được mỗi lần thu thập

    private List<LevelItem> spawnedLevels = new List<LevelItem>();
    private bool isDuckMoving = false;
    private int currentPointIndex = 0; // Index của điểm hiện tại trên spline
    private List<SplinePointData> splinePointDataList = new List<SplinePointData>(); // Dữ liệu cho từng điểm trên spline

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        // Xóa các level cũ nếu có
        foreach (Transform child in levelsContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void OnEnable()
    {
        returnBtn.onClick.AddListener(Return);
        playBtn.onClick.AddListener(OnPlay);

        if (coinCollectionUI != null)
        {
            coinCollectionUI.OnCoinCollected += OnCoinCollected_Handler;
            coinCollectionUI.Hide(); // ẩn đi cho lần đầu vào map
        }

        InitializeMap();
    }

    private void OnDisable()
    {
        returnBtn.onClick.RemoveListener(Return);
        playBtn.onClick.RemoveListener(OnPlay);
        if (coinCollectionUI != null)
            coinCollectionUI.OnCoinCollected -= OnCoinCollected_Handler;
    }

    #endregion

    #region Menu Transitions

    protected override async UniTask PlayOpenTransition()
    {
        CanvasGroup.alpha = 0;
        RectTransform returnRect = returnBtn.GetComponent<RectTransform>();
        RectTransform playRect = playBtn.GetComponent<RectTransform>();
        returnRect.anchoredPosition = new Vector2(-150, -150);
        playRect.anchoredPosition = Vector2.down * 150;
        gameObject.SetActive(true);
        DOTween.Sequence()
            .Join(CanvasGroup.DOFade(1, 0.5f))
            .Append(playRect.DOAnchorPos(Vector2.up * 50, 0.5f).SetEase(Ease.OutBack))
            .AppendInterval(0.01f)
            .Append(returnRect.DOAnchorPos(new Vector2(50, 75), 0.5f).SetEase(Ease.OutBack));
    }

    protected override async UniTask PlayCloseTransition()
    {
        await CanvasGroup.DOFade(0, 0.5f).AsyncWaitForCompletion();
    }

    #endregion

    #region UI Callbacks

    [ContextMenu("Refresh Data")]
    public void UpdateData()
    {
        if (levelListData == null) return;

        if (!Application.isPlaying)
        {
            while (levelsContainer.childCount > 0)
            {
                DestroyImmediate(levelsContainer.GetChild(0).gameObject);
            }
            spawnedLevels.Clear();
        }

        int highestLevel = DataManager.Instance.GetHighestLevel();
        int pointCount = pathSpline.pointCount;

        for (var i = 0; i < levelListData.List.Count; i++)
        {
            if (i >= pointCount)
            {
                Debug.LogWarning($"Không đủ điểm trên Spline cho Level {i + 1}. Hãy thêm điểm vào Spline.");
                break;
            }

            var levelData = levelListData.List[i];
            Vector3 position = pathSpline.GetPoint(i).position;

            LevelItem thumb;
            if (spawnedLevels.Count > i && spawnedLevels[i] != null)
            {
                thumb = spawnedLevels[i];
            }
            else
            {
                thumb = Instantiate(levelThumbPrefab, levelsContainer);
                spawnedLevels.Add(thumb);
            }

            thumb.transform.position = position;
            thumb.name = $"Level_{levelData.ID}";

            bool isUnlocked = levelData.ID <= highestLevel;
            thumb.Setup(levelData, OnLevelItemClick, isUnlocked);
            thumb.SetLevelMapMenu(this);
        }
    }

    private async void OnPlay()
    {
        if (isDuckMoving) return;

        int currentDuckLevel = currentPointIndex + 1;
        if (currentDuckLevel < 1 || currentDuckLevel > levelListData.List.Count)
        {
            Debug.LogWarning("Duck không đang ở một điểm level hợp lệ.");
            return;
        }

        int index = currentDuckLevel - 1;
        LevelSO data = levelListData.List[index];

        int highestLevel = DataManager.Instance.GetHighestLevel();
        if (data.ID > highestLevel)
        {
            Debug.LogWarning($"Level {data.ID} chưa được mở khóa.");
            return;
        }

        GameManager.Instance.CurrentPlayingLevel = data;
        await GameFacade.Instance.PlayLevel(data);
    }

    private async void Return()
    {
        await UIManager.Instance.SwitchToMenu(Menu.Main);
    }

    private void OnLevelItemClick(LevelSO data)
    {
        if (isDuckMoving) return;

        int highestLevel = DataManager.Instance.GetHighestLevel();
        if (data.ID > highestLevel)
        {
            Debug.LogWarning($"Level {data.ID} chưa được mở khóa.");
            return;
        }

        int levelIndex = levelListData.List.FindIndex(l => l.ID == data.ID);
        if (levelIndex == -1)
        {
            Debug.LogWarning($"Không tìm thấy Level {data.ID} trong danh sách.");
            return;
        }

        int currentDuckLevel = currentPointIndex + 1;
        MoveDuckToLevel(currentDuckLevel, data.ID).Forget();
    }

    private void OnCoinCollected_Handler(int amount)
    {
        DataManager.Instance.SaveCurrency(ConstantString.COIN, DataManager.Instance.GetCurrency(ConstantString.COIN) + amount);
        
        if (splinePointDataList[currentPointIndex].pointType == SplinePointType.Coin)
        {
            splinePointDataList[currentPointIndex].isCollected = true;
        }
        
        if (coinCollectionUI != null)
        {
            coinCollectionUI.Hide();
        }

        // Sau khi nhận coin, tiếp tục chu trình di chuyển
        CheckNextPointType();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Khởi tạo bản đồ, sinh các nút level và đặt vị trí con vịt.
    /// </summary>
    private void InitializeMap()
    {
        if (levelListData == null || pathSpline == null) return;

        InitializeSplinePointData();
        UpdateData();

        int highestLevel = DataManager.Instance.GetHighestLevel();
        int lastDuckLevel = DataManager.Instance.GetLastDuckLevelPos();

        UpdateDuckPosition(lastDuckLevel);
        ScrollToDuck();

        // if (highestLevel > lastDuckLevel)
        // {
        //     MoveDuckToLevel(lastDuckLevel, highestLevel).Forget();
        // }
        // else
        // {
        //     UpdateDuckPosition(highestLevel);
            // Sau khi đặt vịt, kiểm tra xem có thể di chuyển tiếp không (ví dụ: tới coin)
            // CheckNextPointType();
        // }
    }

    /// <summary>
    /// Khởi tạo dữ liệu cho từng điểm trên spline
    /// </summary>
    private void InitializeSplinePointData()
    {
        splinePointDataList.Clear();

        for (int i = 0; i < pathSpline.pointCount; i++)
        {
            SplinePointData pointData = new SplinePointData();

            if (i < levelListData.List.Count)
            {
                pointData.pointType = SplinePointType.Level;
                pointData.pointId = levelListData.List[i].ID;
            }
            else
            {
                pointData.pointType = SplinePointType.Coin;
                pointData.pointId = i + 1; // ID cho coin có thể là index
            }

            pointData.isCollected = false; // Cần logic load trạng thái đã thu thập coin từ DataManager
            splinePointDataList.Add(pointData);
        }
    }

    #endregion

    #region Duck Movement

    /// <summary>
    /// Di chuyển con vịt từ level start đến level end dọc theo spline.
    /// </summary>
    private async UniTask MoveDuckToLevel(int startLevelId, int endLevelId)
    {
        if (isDuckMoving) return;
        isDuckMoving = true;

        await UniTask.WaitForSeconds(0.5f);

        int startIndex = startLevelId - 1;
        int endIndex = endLevelId - 1;

        if (endIndex >= levelListData.List.Count)
        {
            Debug.LogWarning($"Không thể di chuyển đến level {endLevelId} vì không có trong levelListData.");
            isDuckMoving = false;
            return;
        }
        if (startIndex >= pathSpline.pointCount || endIndex >= pathSpline.pointCount)
        {
            Debug.LogWarning("Không đủ điểm trên Spline để di chuyển vịt.");
            isDuckMoving = false;
            return;
        }

        double startPercent = pathSpline.GetPointPercent(startIndex);
        double endPercent = pathSpline.GetPointPercent(endIndex);

        UpdateDuckFlip(endIndex > startIndex);

        await DOVirtual.Float((float)startPercent, (float)endPercent, moveDuration, value =>
        {
            Vector3 pos = pathSpline.EvaluatePosition(value);
            duckIcon.position = pos;
            ScrollToDuck();
        }).SetEase(moveEase).AsyncWaitForCompletion();

        DataManager.Instance.SaveLastDuckLevelPos(endLevelId);
        currentPointIndex = endIndex;
        UpdateData();
        isDuckMoving = false;

        // CheckNextPointType();
    }
    
    /// <summary>
    /// Di chuyển duck theo spline dựa trên index. Chỉ di chuyển, không có logic đi kèm.
    /// </summary>
    private async UniTask MoveDuckToPointIndex(int startIndex, int endIndex, Func<Task> beforeMoveTask = null)
    {
        if (isDuckMoving) return;
        isDuckMoving = true;

        await UniTask.WaitForSeconds(0.2f); // Thời gian chờ ngắn hơn cho các bước di chuyển nhỏ

        if (startIndex >= pathSpline.pointCount || endIndex >= pathSpline.pointCount || startIndex < 0 || endIndex < 0)
        {
            Debug.LogWarning("Index không hợp lệ để di chuyển vịt.");
            isDuckMoving = false;
            return;
        }

        double startPercent = pathSpline.GetPointPercent(startIndex);
        double endPercent = pathSpline.GetPointPercent(endIndex);

        UpdateDuckFlip(endIndex > startIndex);

        if (beforeMoveTask != null) await beforeMoveTask();
        await DOVirtual.Float((float)startPercent, (float)endPercent, moveDuration, value =>
        {
            Vector3 pos = pathSpline.EvaluatePosition(value);
            duckIcon.position = pos;
            ScrollToDuck();
        }).SetEase(moveEase).AsyncWaitForCompletion();

        currentPointIndex = endIndex;
        if (splinePointDataList[endIndex].pointType == SplinePointType.Level)
        {
            DataManager.Instance.SaveLastDuckLevelPos(endIndex + 1);
        }

        isDuckMoving = false;
    }

    private void UpdateDuckPosition(int levelId)
    {
        if (levelListData.List.Count == 0 || pathSpline.pointCount == 0) return;

        int index = Mathf.Clamp(levelId - 1, 0, pathSpline.pointCount - 1);
        currentPointIndex = index;

        double percent = pathSpline.GetPointPercent(index);
        duckIcon.position = pathSpline.EvaluatePosition(percent);
    }

    private void UpdateDuckFlip(bool facingRight)
    {
        if (duckIcon != null)
        {
            Vector3 scale = duckIcon.localScale;
            scale.x = facingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            duckIcon.localScale = scale;
        }
    }

    private void ScrollToDuck()
    {
        if (scrollRect == null || levelsContainer == null) return;

        RectTransform contentRect = levelsContainer as RectTransform;
        if (contentRect == null) return;

        Vector3 duckLocalPos = contentRect.InverseTransformPoint(duckIcon.position);
        float viewportWidth = scrollRect.viewport.rect.width;
        float contentWidth = contentRect.rect.width;

        float targetX = -duckLocalPos.x + viewportWidth * 0.5f;

        float minX = viewportWidth - contentWidth;
        float maxX = 0;
        targetX = Mathf.Clamp(targetX, minX, maxX);

        contentRect.DOAnchorPosX(targetX, 0.5f).SetEase(Ease.OutQuad);
    }

    #endregion

    #region Gameplay Logic

    /// <summary>
    /// Kiểm tra xem điểm tiếp theo sau vị trí hiện tại là gì và xử lý.
    /// </summary>
    private void CheckNextPointType()
    {
        int nextPointIndex = currentPointIndex + 1;

        if (nextPointIndex < splinePointDataList.Count)
        {
            SplinePointData nextPoint = splinePointDataList[nextPointIndex];

            if (nextPoint.pointType == SplinePointType.Coin && !nextPoint.isCollected)
            {
                HandleCoinPoint(nextPointIndex).Forget();
            }
            else if (nextPoint.pointType == SplinePointType.Level)
            {
                int nextLevelId = nextPoint.pointId;
                int highestLevel = DataManager.Instance.GetHighestLevel();
                if (nextLevelId <= highestLevel)
                {
                    HandleLevelPoint(nextPointIndex).Forget();
                }
            }
        }
    }
    
    /// <summary>
    /// Xử lý logic khi duck cần di chuyển đến một điểm Level.
    /// </summary>
    private async UniTaskVoid HandleLevelPoint(int pointIndex)
    {
        if (isDuckMoving) return;
        
        await MoveDuckToPointIndex(currentPointIndex, pointIndex);
        
        // Sau khi di chuyển xong, kiểm tra điểm tiếp theo
        CheckNextPointType();
    }

    /// <summary>
    /// Xử lý logic khi duck cần di chuyển đến một điểm Coin.
    /// </summary>
    private async UniTaskVoid HandleCoinPoint(int pointIndex)
    {
        if (isDuckMoving) return;

        await MoveDuckToPointIndex(currentPointIndex, pointIndex);

        if (coinCollectionUI != null)
        {
            coinCollectionUI.Show(coinRewardAmount);
        }
    }

    #endregion
}