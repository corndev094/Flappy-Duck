using Cysharp.Threading.Tasks;
using Nguyen.Event;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour {
    [SerializeField] private GameObject hpContainer;
    [SerializeField] private GameObject staminaContainer;
    [SerializeField] private Image staminaImage;
    [SerializeField] private Image hpImage;
    [SerializeField] private Image hpBackgroundImage; // Thanh HP nền
    [SerializeField] private Button pauseButton;
    [SerializeField] private FloatEventChannelSO staminaEvent;
    [SerializeField] private FloatEventChannelSO hpEvent;

    [SerializeField] private Color hpFlashColor = Color.white;
    [SerializeField] private float hpFlashDuration = 0.15f;
    [SerializeField] private float hpBackgroundDelay = 0.1f;
    [SerializeField] private float hpBackgroundLerpDuration = 0.4f;

    private float staminaTarget;
    private float hpTarget;
    private UniTask staminaTweenTask;
    private UniTask hpTweenTask;
    private bool staminaTweening;
    private bool hpTweening;
    private bool hpFlashing;
    private bool hpBackgroundTweening;
    private Color hpOriginalColor;
    private const float tweenDuration = 0.25f;

    void OnEnable()
    {
        hpImage.fillAmount = 1;
        staminaImage.fillAmount = 1;
        if (hpBackgroundImage != null)
        {
            hpBackgroundImage.fillAmount = 1;
        }
        staminaTarget = 1;
        hpTarget = 1;
        staminaEvent.OnEventRaised += OnStaminaChanged;
        hpEvent.OnEventRaised += OnHpChanged;
        pauseButton.onClick.AddListener(Pause);
        hpOriginalColor = hpImage.color;
        if (GameFacade.Instance.CurrentSelectedDuck != null 
        && GameFacade.Instance.CurrentSelectedDuck.UseMP)
        {
            staminaContainer.SetActive(true);
        }
        else
        {
            staminaContainer.SetActive(false);
        }
    }

    void OnDisable()
    {
        staminaEvent.OnEventRaised -= OnStaminaChanged;
        hpEvent.OnEventRaised -= OnHpChanged;
        pauseButton.onClick.RemoveListener(Pause);
    }

    private void OnStaminaChanged(float value)
    {
        staminaTarget = value;
        if (!staminaTweening)
        {
            TweenStamina().Forget();
        }
    }

    private void OnHpChanged(float value)
    {
        hpTarget = value;
        if (!hpTweening)
        {
            TweenHp().Forget();
        }
        if (!hpBackgroundTweening && hpBackgroundImage != null)
        {
            TweenHpBackground().Forget();
        }
        if (!hpFlashing)
        {
            FlashHp().Forget();
        }
    }

    private async UniTaskVoid TweenStamina()
    {
        staminaTweening = true;
        while (Mathf.Abs(staminaImage.fillAmount - staminaTarget) > 0.001f)
        {
            float start = staminaImage.fillAmount;
            float end = staminaTarget;
            float elapsed = 0f;
            while (elapsed < tweenDuration && Mathf.Abs(staminaImage.fillAmount - staminaTarget) > 0.001f)
            {
                end = staminaTarget; // update in case target changed
                elapsed += Time.unscaledDeltaTime;
                staminaImage.fillAmount = Mathf.Lerp(start, end, elapsed / tweenDuration);
                await UniTask.Yield();
            }
            staminaImage.fillAmount = staminaTarget;
        }
        staminaTweening = false;
    }

    private async UniTaskVoid TweenHp()
    {
        hpTweening = true;
        while (Mathf.Abs(hpImage.fillAmount - hpTarget) > 0.001f)
        {
            float start = hpImage.fillAmount;
            float end = hpTarget;
            float elapsed = 0f;
            while (elapsed < tweenDuration && Mathf.Abs(hpImage.fillAmount - hpTarget) > 0.001f)
            {
                end = hpTarget; // update in case target changed
                elapsed += Time.unscaledDeltaTime;
                hpImage.fillAmount = Mathf.Lerp(start, end, elapsed / tweenDuration);
                await UniTask.Yield();
            }
            hpImage.fillAmount = hpTarget;
        }
        hpTweening = false;
    }

    private async UniTaskVoid FlashHp()
    {
        hpFlashing = true;
        hpImage.color = hpFlashColor;
        await UniTask.Delay((int)(hpFlashDuration * 1000));
        hpImage.color = hpOriginalColor;
        hpFlashing = false;
    }

    private async UniTaskVoid TweenHpBackground()
    {
        hpBackgroundTweening = true;
        await UniTask.Delay((int)(hpBackgroundDelay * 1000));
        while (Mathf.Abs(hpBackgroundImage.fillAmount - hpTarget) > 0.001f)
        {
            float start = hpBackgroundImage.fillAmount;
            float end = hpTarget;
            float elapsed = 0f;
            while (elapsed < hpBackgroundLerpDuration && Mathf.Abs(hpBackgroundImage.fillAmount - hpTarget) > 0.001f)
            {
                end = hpTarget;
                elapsed += Time.unscaledDeltaTime;
                hpBackgroundImage.fillAmount = Mathf.Lerp(start, end, elapsed / hpBackgroundLerpDuration);
                await UniTask.Yield();
            }
            hpBackgroundImage.fillAmount = hpTarget;
        }
        hpBackgroundTweening = false;
    }

    private async void Pause()
    {
        if (GameManager.Instance.IsGameOver) return;
        UIManager.Instance.OpenMenu(Menu.Pause).Forget();
        GameFacade.Instance.Pause();
    }
}