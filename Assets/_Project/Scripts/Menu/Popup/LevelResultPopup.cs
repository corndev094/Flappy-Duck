using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public class LevelResultPopup : ABasePopup {
    [Header("Win Window")]
    [SerializeField] private GameObject winWindow;
    [SerializeField] private Image coinIcon;
    [SerializeField] private LocalizeStringEvent winCoinCountText;
    [SerializeField] private Button winHomeButton;
    [SerializeField] private Button nextButton;

    [Space]
    [Header("Lose Window")]
    [SerializeField] private GameObject loseWindow;
    [SerializeField] private Image heartIcon;
    [SerializeField] private LocalizeStringEvent loseCoinCountText;
    [SerializeField] private Button loseHomeButton;
    [SerializeField] private Button replayButton;

    [Header("References")]
    [SerializeField] private DOTweenAnimation openTween;

    protected override void Awake() {
        if (openTween == null) openTween = GetComponent<DOTweenAnimation>();
        base.Awake();
    }

    protected override async UniTask PlayOpenTransition() {
        if (openTween == null || openTween.tween == null) return;
        openTween.DORestart();
        await openTween.tween.AsyncWaitForCompletion();
    }

     protected override async UniTask PlayCloseTransition() {
        if (openTween == null || openTween.tween == null) return;
        openTween.DOPlayBackwards();
        await openTween.tween.AsyncWaitForCompletion();
    }

    void Start()
    {
        winHomeButton?.onClick.AddListener(Return);
        loseHomeButton?.onClick.AddListener(Return);
        replayButton?.onClick.AddListener(Replay);
        nextButton?.onClick.AddListener(Next);
    }

    void OnDestroy()
    {
        winHomeButton?.onClick.RemoveListener(Return);
        loseHomeButton?.onClick.RemoveListener(Return);
        replayButton?.onClick.RemoveListener(Replay);
        nextButton?.onClick.RemoveListener(Next);
    }

    public void Setup(bool isWin, int coin = 0)
    {
        if (isWin)
        {
            winWindow?.SetActive(true);
            loseWindow?.SetActive(false);
        }
        else
        {
            winWindow?.SetActive(false);
            loseWindow?.SetActive(true);
        }

        if (GameManager.Instance.IsOnlineMode)
        {
            replayButton?.gameObject.SetActive(false);
            nextButton?.gameObject.SetActive(false);
        }
        else
        {
            replayButton?.gameObject.SetActive(true);
            nextButton?.gameObject.SetActive(true);
        }

        if (winCoinCountText != null) winCoinCountText.StringReference.Arguments = new object[] {coin.ToString()};
        if (loseCoinCountText != null) loseCoinCountText.StringReference.Arguments = new object[] {coin.ToString()};
    }

    private async void Return()
    {
        Debug.Log("Returning to menu...");
        await UIManager.Instance.CloseTopPopup();
        await GameFacade.Instance.ReturnToMenu();
        if (MatchmakingManager.Instance != null)
        {
            await MatchmakingManager.Instance.LeaveLobby();
        }
    }

    private async void Replay()
    {
        // if (GameManager.Instance.IsOnlineMode) await GameFacade.Instance.ReplayLevel();
        Return();
    }

    private async void Next()
    {
        // await GameFacade.Instance.NextLevel();
        Return();
    }
}
