using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : ABasePopup {
    [SerializeField] private Transform panel;

    [Header("Buttons")] 
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button homeButton;

    [Header("Toggle")]
    [SerializeField] private TwoStateToggle generalSoundToggle;
    [SerializeField] private TwoStateToggle musicToggle;
    [SerializeField] private TwoStateToggle sfxToggle;

    private void OnEnable() {
        resumeButton.onClick.AddListener(Close);
        homeButton.onClick.AddListener(ReturnHome);
        generalSoundToggle.OnValueChanged += ToggleSound;
        musicToggle.OnValueChanged += ToggleMusic;
        sfxToggle.OnValueChanged += ToggleSfx;
    }

    void OnDisable()
    {
        resumeButton.onClick.RemoveListener(Close);
        homeButton.onClick.RemoveListener(ReturnHome);
        generalSoundToggle.OnValueChanged -= ToggleSound;
        musicToggle.OnValueChanged -= ToggleMusic;
        sfxToggle.OnValueChanged -= ToggleSfx;
    }

    private void ToggleSound(bool on)
    {
        SoundManager.Instance.EnableMasterVolume(on);
    }

    private void ToggleMusic(bool on)
    {
        SoundManager.Instance.EnableMusicVolume(on);
    }

    private void ToggleSfx(bool on)
    {
        SoundManager.Instance.EnableSfxVolume(on);
    }

    private void Close()
    {
        Close(null).Forget();
        GameFacade.Instance.Resume();
    }

    private void ReturnHome()
    {
        GameFacade.Instance.ReturnToMenu().Forget();
    }

    public override async UniTask Open(Action onOpenFinish = null)
    {
        musicToggle.SetStateImmediately(SoundManager.Instance.IsMusicVolumeEnabled());
        generalSoundToggle.SetStateImmediately(SoundManager.Instance.IsMasterVolumeEnabled());
        sfxToggle.SetStateImmediately(SoundManager.Instance.IsSfxVolumeEnabled());
        
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        canvasGroup.interactable = false;
        canvasGroup.alpha = 0;
        onOpen?.Invoke();

        DOVirtual.Float(0.8f, 1, 0.4f, value =>
        {
            panel.localScale = new Vector3(panel.localScale.x, value, panel.localScale.z);
        }).SetEase(Ease.OutElastic).OnComplete(() => panel.localScale = new Vector3(panel.localScale.x, 1, panel.localScale.z));
        canvasGroup.DOFade(1, 0.4f);

        canvasGroup.interactable = true;
        onOpenFinish?.Invoke();
    }

    public override async UniTask Close(Action onCloseFinish = null)
    {
        canvasGroup.interactable = false;
        canvasGroup.alpha = 1;
        onClose?.Invoke();

        DOVirtual.Float(1, 0.8f, 0.4f, value =>
        {
            panel.localScale = new Vector3(panel.localScale.x, value, panel.localScale.z);
        }).SetEase(Ease.OutElastic).OnComplete(() => panel.localScale = new Vector3(panel.localScale.x, 1, panel.localScale.z));
        await canvasGroup.DOFade(0, 0.4f).AsyncWaitForCompletion();

        onCloseFinish?.Invoke();
        gameObject.SetActive(false);
    }
}