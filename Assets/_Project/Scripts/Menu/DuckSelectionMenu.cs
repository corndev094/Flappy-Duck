using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DanielLochner.Assets.SimpleScrollSnap;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Sirenix.OdinInspector;

public class DuckSelectionMenu : ABaseMenu {
    [SerializeField] private ShopData shopData;
    [Header("References")] 
    [SerializeField] private DuckList duckList;
    [SerializeField] private SimpleScrollSnap scrollSnap;
    [SerializeField] private Transform content;
    [SerializeField] private ToggleGroup toggleGroup;
    [SerializeField] private Button returnBtn;
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private DuckSelection duckSelectionPerfab;
    [SerializeField] private Toggle togglePrefab;

    private Sequence shakeSequence;

    void Awake()
    {
        for (var i = 0; i < duckList.List.Count; i++)
        {
            var thumb = Instantiate(duckSelectionPerfab, content);
            var toggle = Instantiate(togglePrefab, toggleGroup.transform);
            toggle.group = toggleGroup;
            var duckData = duckList.List[i];
            int skinIndex = shopData.skins.FindIndex(_ => _.SkinId == duckData.SkinId);
            if (skinIndex < 0)
            {
                Debug.LogWarning($"Missing shop data for duck skin {duckData.SkinId}", this);
                continue;
            }
            var skin = shopData.skins[skinIndex];
            thumb.Setup(this, duckData.Thumbnail, duckData, IsSkinBought(duckData.SkinId), skin.Price, SelectDuck, UpdateUI);
        }
    }

    void OnEnable()
    {
        if (duckList == null || duckList.List.Count == 0) return;
        returnBtn.onClick.AddListener(Return);
        UpdateUI();
    }

    void OnDisable()
    {
        returnBtn.onClick.RemoveListener(Return);
    }

    private void UpdateUI()
    {
        coinText.SetText(DataManager.Instance.GetCurrency(ConstantString.COIN).ToString());
        if (GameFacade.Instance.CurrentSelectedDuck == null)
        {
            GameFacade.Instance.CurrentSelectedDuck = duckList.List[0];
        }
        for (var i = 0; i < duckList.List.Count; i++)
        {
            if (duckList.List[i].SkinId == GameFacade.Instance.CurrentSelectedDuck.SkinId)
            {
                scrollSnap.Content.GetChild(i).GetComponent<DuckSelection>().SetSelected(true);
                continue;
            }
            scrollSnap.Content.GetChild(i).GetComponent<DuckSelection>().SetSelected(false);
        }
    }

    private void Return()
    {
        UIManager.Instance.SwitchToMenu(Menu.Main).Forget();
    }

    public void SelectDuck(DuckBaseData duckData)
    {
        GameFacade.Instance.CurrentSelectedDuck = duckData;
        DataManager.Instance.SaveLastSelectedDuck((int)(duckData.SkinId));
        UpdateUI();
    }

    public void UpdateCoinText(int value)
    {
        DataManager.Instance.SaveCurrency(ConstantString.COIN, value);
    }

    private bool IsSkinBought(DuckSkinID skinId)
    {
        return DataManager.Instance.IsSkinUnlocked(skinId);
    }

    public void ShakeCoinText()
    {
        if (shakeSequence != null && shakeSequence.IsActive() && shakeSequence.IsPlaying())
            return;

        var originalColor = coinText.color;
        Color targetColor = Color.red;
        float duration = 0.75f;

        shakeSequence?.Complete();
        shakeSequence?.Kill();
        shakeSequence = DOTween.Sequence()
            .Append(coinText.transform.DOShakePosition(duration, new Vector3(5f, 0, 0), vibrato: 20))
            .Join(coinText.DOColor(targetColor, duration/3).SetLoops(3))
            .SetEase(Ease.InOutFlash)
            .OnComplete(() => coinText.color = originalColor);
    }
}
