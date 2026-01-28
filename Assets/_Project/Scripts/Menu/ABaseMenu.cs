using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public abstract class ABaseMenu : MonoBehaviour {
    [SerializeField] private AudioClip transtionSound;
    protected CanvasGroup canvasGroup;
    protected DOTweenAnimation dotweenAnim;
    public CanvasGroup CanvasGroup
    {
        get
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            return canvasGroup;
        }
    }
    public DOTweenAnimation DOTweenAnim
    {
        get
        {
            if (dotweenAnim == null) dotweenAnim = GetComponent<DOTweenAnimation>();
            return dotweenAnim;
        }
    }
    public Action onOpen;
    public Action onClose;

    public virtual async UniTask Open(Action onOpenFinish = null)
    {
        gameObject.SetActive(true);
        CanvasGroup.interactable = false;
        onOpen?.Invoke();

        await PlayOpenTransition();

        CanvasGroup.interactable = true;
        onOpenFinish?.Invoke();
    }

    public virtual async UniTask Close(Action onCloseFinish = null)
    {
        CanvasGroup.interactable = false;
        onClose?.Invoke();
        if (transtionSound != null) SoundManager.Instance.PlaySFX(transtionSound, randomPitch: true);

        await PlayCloseTransition();

        onCloseFinish?.Invoke();
        gameObject.SetActive(false);
    }

    protected virtual async UniTask PlayOpenTransition()
    {
        if (DOTweenAnim != null)
        {
            DOTweenAnim.DORestart();
            await UniTask.WaitForSeconds(DOTweenAnim.duration);
        }
    }

    protected virtual async UniTask PlayCloseTransition()
    {
        if (DOTweenAnim != null)
        {
            DOTweenAnim.DOPlayBackwards();
            await UniTask.WaitForSeconds(DOTweenAnim.duration);
        }
    }
}