using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public abstract class ABasePopup : MonoBehaviour
{
    [Header("Base Popup Settings")]
    [SerializeField] private Button closeBtn;
    [SerializeField] private Button backgroundBtn;
    [SerializeField] private bool closeOnBackgroundClick = true;

    public Action onOpen;
    public Action onClose;

    protected CanvasGroup canvasGroup;
    private UniTaskCompletionSource<object> completionSource;

    #region Unity Lifecycle Methods
    protected virtual void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (closeBtn != null)
        {
            closeBtn.onClick.AddListener(OnCloseBtnClick);
        }

        if (backgroundBtn != null)
        {
            backgroundBtn.onClick.AddListener(OnBackgroundClick);
        }
    }
    #endregion

    #region Public Methods
    public virtual async UniTask Open(Action onOpenFinish = null)
    {
        if (gameObject.activeSelf) return;

        gameObject.SetActive(true);
        canvasGroup.interactable = false;
        onOpen?.Invoke();

        await PlayOpenTransition();

        canvasGroup.interactable = true;
        onOpenFinish?.Invoke();
    }

    public virtual async UniTask Close(Action onCloseFinish = null)
    {
        if (!gameObject.activeSelf) return;
        completionSource?.TrySetResult(null);

        canvasGroup.interactable = false;
        onClose?.Invoke();
        // if (transtionSound != null) SoundManager.Instance.PlaySFX(transtionSound, randomPitch: true);

        await PlayCloseTransition();

        onCloseFinish?.Invoke();
        gameObject.SetActive(false);
    }

    public void CloseImmediate()
    {
        completionSource?.TrySetResult(null);
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Opens the popup, waits for a result, and closes it.
    /// </summary>
    public virtual async UniTask<object> Show(object data = null)
    {
        // Cancel previous task if it exists (though unlikely for a single instance popup)
        completionSource?.TrySetCanceled();
        completionSource = new UniTaskCompletionSource<object>();

        OnInitialize(data);

        await Open();

        return await completionSource.Task;
    }

    /// <summary>
    /// Helper to show and cast result to specific type
    /// </summary>
    public async UniTask<T> Show<T>(object data = null)
    {
        var result = await Show(data);
        if (result is T tResult)
        {
            return tResult;
        }
        return default;
    }
    #endregion

    #region Protected Methods
    protected virtual UniTask PlayOpenTransition()
    {
        return UniTask.CompletedTask;
    }

    protected virtual UniTask PlayCloseTransition()
    {
        return UniTask.CompletedTask;
    }

    /// <summary>
    /// Override to initialize popup with data
    /// </summary>
    protected virtual void OnInitialize(object data)
    {
    }

    protected virtual void OnCloseBtnClick()
    {
        Dismiss(null);
    }

    protected virtual void OnBackgroundClick()
    {
        if (closeOnBackgroundClick)
        {
            Dismiss(null);
        }
    }

    /// <summary>
    /// Call this to close the popup with a result
    /// </summary>
    protected void Dismiss(object result)
    {
        completionSource?.TrySetResult(result);
        Close().Forget();
    }
    #endregion
}
