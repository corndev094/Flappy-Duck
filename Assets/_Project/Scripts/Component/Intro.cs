using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Intro : MonoBehaviour {
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text pressAnyKeyText;
    [SerializeField] private Ease textEase;
    [SerializeField] private AudioClip pressButtonSfx;

    private Tween textTween;

    void Start()
    {
        Play();
    }

    public async void Play()
    {
        gameObject.SetActive(true);
        background.gameObject.SetActive(true);
        pressAnyKeyText.gameObject.SetActive(true);
        await background.DOFade(0, 1f).OnComplete(() => background.gameObject.SetActive(false)).AsyncWaitForCompletion();
        PlayTextEffect();
        WaitForAnyKey();
    }

    private async void EnterMainMenu()
    {
        SoundManager.Instance.PlaySFX(pressButtonSfx);
        await pressAnyKeyText.DOFade(0, 1f).AsyncWaitForCompletion();
        background.gameObject.SetActive(false);
        pressAnyKeyText.gameObject.SetActive(false);
        textTween.Kill();
        gameObject.SetActive(false);
        await UIManager.Instance.OpenMenu(Menu.Main);
    }

    private async void PlayTextEffect()
    {
        await pressAnyKeyText.transform.DOScale(1, 1).From(0).AsyncWaitForCompletion();
        textTween = pressAnyKeyText.transform.DOScale(1.2f, 1).SetLoops(-1, LoopType.Yoyo).SetEase(textEase);
        await textTween.AsyncWaitForKill();
    }

    private async void WaitForAnyKey()
    {
        await UniTask.WaitUntil(() => Keyboard.current.anyKey.wasPressedThisFrame);
        if (pressButtonSfx != null)SoundManager.Instance.PlaySFX(pressButtonSfx);
        EnterMainMenu();
    }
}