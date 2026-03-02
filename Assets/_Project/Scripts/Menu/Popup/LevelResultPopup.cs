using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LevelResultPopup : ABasePopup {
    [Header("Win Window")]
    [SerializeField] private GameObject winWindow;
    [SerializeField] private Image coinIcon;
    [SerializeField] private TMP_Text winCoinCountText;
    [SerializeField] private Button winHomeButton;
    [SerializeField] private Button nextButton;

    [Space]
    [Header("Lose Window")]
    [SerializeField] private GameObject loseWindow;
    [SerializeField] private Image heartIcon;
    [SerializeField] private TMP_Text loseCoinCountText;
    [SerializeField] private Button loseHomeButton;
    [SerializeField] private Button replayButton;

    [Header("References")]
    [SerializeField] private DOTweenAnimation openTween;

    protected override void Awake() {
        if (openTween == null) openTween = GetComponent<DOTweenAnimation>();
        base.Awake();
    }

    protected override async UniTask PlayOpenTransition() {
        openTween.DORestart();
        await openTween.tween.AsyncWaitForCompletion();
    }

     protected override async UniTask PlayCloseTransition() {
        openTween.DOPlayBackwards();
        await openTween.tween.AsyncWaitForCompletion();
    }

    void Start()
    {
        winHomeButton.onClick.AddListener(Return);
        loseHomeButton.onClick.AddListener(Return);
        replayButton.onClick.AddListener(Replay);
        nextButton.onClick.AddListener(Next);
    }

    public void Setup(bool isWin, int coin = 0)
    {
        if (isWin)
        {
            winWindow.SetActive(true);
            loseWindow.SetActive(false);
        }
        else
        {
            winWindow.SetActive(false);
            loseWindow.SetActive(true);
        }

        if (GameManager.Instance.IsOnlineMode)
        {
            replayButton.gameObject.SetActive(false);
            nextButton.gameObject.SetActive(false);
        }
        else
        {
            replayButton.gameObject.SetActive(true);
            nextButton.gameObject.SetActive(true);
        }

        winCoinCountText.text = coin.ToString();
        loseCoinCountText.text = coin.ToString();
    }

    private async void Return()
    {
        await UIManager.Instance.CloseTopPopup();
        await GameFacade.Instance.ReturnToMenu();
        Debug.Log(MatchmakingManager.Instance);
        await MatchmakingManager.Instance.LeaveLobby();
    }

    private async void Replay()
    {
        // if (GameManager.Instance.IsOnlineMode) await GameFacade.Instance.ReplayLevel();
        await GameFacade.Instance.ReturnToMenu();
    }

    private async void Next()
    {
        // await GameFacade.Instance.NextLevel();
        await GameFacade.Instance.ReturnToMenu();
    }
}