using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DuckSelection : MonoBehaviour
{
    [SerializeField] private Image thumbnail;
    [SerializeField] private Button selectButton;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private GameObject buyGameObject;
    [SerializeField] private AudioClip buySuccessSound;
    [SerializeField] private AudioClip buyFailSound;

    private bool isBought;
    private int price;
    private DuckBaseData data;
    public DuckBaseData Data => data;
    private Action onBuySuccessAction;
    private DuckSelectionMenu duckSelectionMenu;

    void OnEnable()
    {
        buyButton.onClick.AddListener(Buy);
        UpdateUI();
    }
    void OnDisable()
    {
        buyButton.onClick.RemoveListener(Buy);
    }

    public void Setup(DuckSelectionMenu duckSelectionMenu, Sprite thumbnail, DuckBaseData data, bool isBought, int price, Action onSelect = null, Action onBuySuccess = null)
    {
        this.duckSelectionMenu = duckSelectionMenu;
        this.thumbnail.sprite = thumbnail;
        this.data = data;
        this.isBought = isBought;
        this.price = price;

        priceText.SetText(price.ToString());
        selectButton.onClick.AddListener(() => onSelect?.Invoke());
        this.onBuySuccessAction = onBuySuccess;

        UpdateUI();
    }

    public void Buy()
    {
        if (DataManager.Instance.CanBuySkin(price))
        {
            // TODO: Play effect: sound, vfx
            DataManager.Instance.SaveCurrency(ConstantString.COIN, GetCurrency() - price);
            isBought = true;
            DataManager.Instance.SaveUnlockedSkin(data.SkinId, true);
            UpdateUI();
            SoundManager.Instance.PlaySFX(buySuccessSound);
            onBuySuccessAction?.Invoke();
        }
        else
        {
            SoundManager.Instance.PlaySFX(buyFailSound);
            duckSelectionMenu.ShakeCoinText();
        }
    }

    private void UpdateUI()
    {
        if (isBought)
        {
            buyGameObject.gameObject.SetActive(false);
        }
        else
        {
            buyGameObject.gameObject.SetActive(true);
        }
    }

    public void SetSelected(bool selected)
    {
        if (!isBought)
        {
            return;
        }

        if (selected)
        {
            selectButton.image.material = null;
            selectButton.GetComponentInChildren<TMP_Text>().SetText("Select");
        }
        else
        {
            selectButton.image.material = GameManager.Instance.GrayScaleMat;
            selectButton.GetComponentInChildren<TMP_Text>().SetText("Selected");
        }
    }

    private int GetCurrency()
    {
        return DataManager.Instance.GetCurrency(ConstantString.COIN);
    }
}