using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelResultMenu : ABasePopup {
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

    void OnEnable()
    {
    }

    void OnDisable()
    {
    }

    public override UniTask<object> Show(object data = null)
    {
        return base.Show(data);
    }

    public void Setup(bool isWin)
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
    }

    private async void Return()
    {
        await GameFacade.Instance.ReturnToMenu();
    }
}