using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Dreamteck.Splines;
using QFSW.QC;
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
    [SerializeField] private ParticleSystem bubblePs; // Hiệu ứng bong bóng khi di chuyển

    [Header("Asset References")]
    [SerializeField] private LevelItem levelThumbPrefab;
    [SerializeField] private LevelListSO levelListData;
    

    [Header("Configuration")]
    [SerializeField] private float moveDuration = 1.5f; // Thời gian di chuyển của con vịt giữa các level
    [SerializeField] private Ease moveEase = Ease.InOutQuad;
    
    [Header("Audio")] 
    [SerializeField] private AudioClip whooshClip;

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

        if (bubblePs != null)
        {
            bubblePs.Stop();
        }

        InitializeMap();
    }

    private void OnDisable()
    {
        returnBtn.onClick.RemoveListener(Return);
        playBtn.onClick.RemoveListener(OnPlay);
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
            thumb.SetActiveAura(levelData.ID == highestLevel);
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
        int originalLastDuckLevel = DataManager.Instance.GetLastDuckLevelPos();

        // Handle post-level-win state: if the current level was just unlocked,
        // ensure the duck position is updated correctly
        HandlePostLevelWinState(highestLevel, originalLastDuckLevel);

        // Position the duck at the start level of the transition (originalLastDuckLevel)
        UpdateDuckPosition(originalLastDuckLevel);

        // Check if we need to move to the newly unlocked level
        bool levelExists = levelListData.List.Exists(l => l.ID == highestLevel);
        if (highestLevel > originalLastDuckLevel && levelExists)
        {
            Debug.Log($"{originalLastDuckLevel} -> {highestLevel}");
            MoveDuckToLevel(originalLastDuckLevel, highestLevel).Forget();
        }
        else
        {
            int currentLastDuckLevel = DataManager.Instance.GetLastDuckLevelPos();
            UpdateDuckPosition(currentLastDuckLevel);
        }
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
                pointData.isCollected = false;
            }
            else
            {
                pointData.pointType = SplinePointType.Special;
                pointData.pointId = i + 1;
                pointData.isCollected = false;
            }

            splinePointDataList.Add(pointData);
        }
    }

    /// <summary>
    /// Handle post-level-win state to ensure proper duck positioning
    /// when returning from a won level
    /// </summary>
    private void HandlePostLevelWinState(int highestLevel, int lastDuckLevel)
    {
        // If the highest level was just unlocked (highestLevel > lastDuckLevel),
        // ensure the level actually exists in levelListData.List before saving
        bool levelExists = levelListData != null && levelListData.List.Exists(l => l.ID == highestLevel);

        if (highestLevel > lastDuckLevel && levelExists)
        {
            // Ensure the duck position is updated to the newly unlocked level
            DataManager.Instance.SaveLastDuckLevelPos(highestLevel);
            
            // Update the spline data to reflect the new level state
            UpdateData();
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
        if (startIndex >= pathSpline.pointCount || endIndex >= pathSpline.pointCount || startIndex < 0 || endIndex < 0)
        {
            Debug.LogWarning("Không đủ điểm trên Spline để di chuyển vịt.");
            isDuckMoving = false;
            return;
        }

        double startPercent = pathSpline.GetPointPercent(startIndex);
        double endPercent = pathSpline.GetPointPercent(endIndex);

        UpdateDuckFlip(endIndex > startIndex);

        if (bubblePs != null) bubblePs.Play();
        SoundManager.Instance.PlaySFX(whooshClip, 1.5f);

        await DOVirtual.Float((float)startPercent, (float)endPercent, moveDuration, value =>
        {
            Vector3 pos = pathSpline.EvaluatePosition(value);
            ScrollToDuck(pos, smooth: false);
            duckIcon.position = pos;
            Vector3 localPos = duckIcon.localPosition;
            localPos.z = 0;
            duckIcon.localPosition = localPos;
        }).SetEase(moveEase).AsyncWaitForCompletion();

        if (bubblePs != null) bubblePs.Stop();

        DataManager.Instance.SaveLastDuckLevelPos(endLevelId);
        currentPointIndex = endIndex;
        UpdateData();
        isDuckMoving = false;
    }
    
    /// <summary>
    /// Move duck along the spline from start index to end index.
    /// </summary>
    private async UniTask MoveDuckToPointIndex(int startIndex, int endIndex, Func<Task> beforeMoveTask = null)
    {
        if (isDuckMoving) return;
        isDuckMoving = true;

        await UniTask.WaitForSeconds(0.2f);

        if (startIndex >= pathSpline.pointCount || endIndex >= pathSpline.pointCount || startIndex < 0 || endIndex < 0)
        {
            Debug.LogWarning("Index không hợp lệ để di chuyển vịt.");
            isDuckMoving = false;
            return;
        }

        double startPercent = pathSpline.GetPointPercent(startIndex);
        double endPercent = pathSpline.GetPointPercent(endIndex);

        UpdateDuckFlip(endIndex > startIndex);

        if (bubblePs != null) bubblePs.Play();
        SoundManager.Instance.PlaySFX(whooshClip, 1.5f);

        if (beforeMoveTask != null) await beforeMoveTask();
        await DOVirtual.Float((float)startPercent, (float)endPercent, moveDuration, value =>
        {
            Vector3 pos = pathSpline.EvaluatePosition(value);
            ScrollToDuck(pos, smooth: false);
            duckIcon.position = pos;
            Vector3 localPos = duckIcon.localPosition;
            localPos.z = 0;
            duckIcon.localPosition = localPos;
        }).SetEase(moveEase).AsyncWaitForCompletion();

        if (bubblePs != null) bubblePs.Stop();

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
        Vector3 targetPos = pathSpline.EvaluatePosition(percent);
        ScrollToDuck(targetPos, smooth: false);
        duckIcon.position = targetPos;
        Vector3 localPos = duckIcon.localPosition;
        localPos.z = 0;
        duckIcon.localPosition = localPos;
    }

    private void UpdateDuckFlip(bool facingRight)
    {
        if (duckIcon != null)
        {
            Vector3 scale = duckIcon.localScale;
            scale.x = facingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            duckIcon.DOScale(scale, 0.2f);
        }
    }

    private void ScrollToDuck(Vector3 worldPosition, bool smooth = false)
    {
        if (scrollRect == null || levelsContainer == null) return;

        RectTransform contentRect = levelsContainer as RectTransform;
        if (contentRect == null) return;

        Vector3 duckLocalPos = contentRect.InverseTransformPoint(worldPosition);
        float viewportWidth = scrollRect.viewport.rect.width;
        float contentWidth = contentRect.rect.width;

        float targetX = -duckLocalPos.x + viewportWidth * 0.5f;

        float minX = viewportWidth - contentWidth;
        float maxX = 0;
        targetX = Mathf.Clamp(targetX, minX, maxX);

        if (smooth)
        {
            contentRect.DOAnchorPosX(targetX, 0.5f).SetEase(Ease.OutQuad);
        }
        else
        {
            contentRect.DOKill();
            Vector2 anchoredPos = contentRect.anchoredPosition;
            anchoredPos.x = targetX;
            contentRect.anchoredPosition = anchoredPos;
        }
    }

    #endregion
}