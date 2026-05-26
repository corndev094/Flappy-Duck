using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class DisconnectedInMatchPopup : ABasePopup {
    [SerializeField] private DOTweenAnimation openTween;
    [SerializeField] private Button returnButton;

    protected override void Awake()
    {
        if (openTween == null) openTween = GetComponent<DOTweenAnimation>();
    }

    void Start()
    {
        returnButton?.onClick.AddListener(Return);
    }

    void OnDestroy()
    {
        returnButton?.onClick.RemoveListener(Return);
    }

    protected override async UniTask PlayOpenTransition()
    {
        if (openTween == null || openTween.tween == null) return;
        openTween.DORestart();
        await openTween.tween.AsyncWaitForCompletion();
    }

    protected override UniTask PlayCloseTransition()
    {
        gameObject.SetActive(false);
        transform.localScale = Vector3.zero;
        return UniTask.CompletedTask;
    }

    void Return()
    {
        if (GameFacade.Instance != null)
        {
            GameFacade.Instance.ReturnToMenu().Forget();
        }
        else
        {
            UIManager.Instance.CloseAllPopupImmediately();
            UIManager.Instance.SwitchToMenu(Menu.Main).Forget();
        }
    }
}