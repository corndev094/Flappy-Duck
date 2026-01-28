using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using DG.Tweening.Core;
using Cysharp.Threading.Tasks;
using System;

public class PreGameCountdown : MonoBehaviour {
    [SerializeField] private TMP_Text text;
    [SerializeField] private Image bg;
    [SerializeField] private AudioClip sfx;

    [ContextMenu("Test")]
    private async void Test() => await StartCountdown(3);

    public async UniTask StartCountdown(int from)
    {
        var rect = bg.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector3(0, 200);
        text.transform.localScale = Vector3.zero;
        gameObject.SetActive(true);
        DOVirtual.Float(0, 1920, .75f, value =>
        {
            rect.sizeDelta = new Vector3(value, rect.sizeDelta.y);
        }).SetEase(Ease.OutCirc);
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        SoundManager.Instance.PlaySFX(sfx, 0.1f);
        for (var i = from; i > 0; i--)
        {
            TweenText(i.ToString());
            await UniTask.Delay(TimeSpan.FromSeconds(1f));
        }
        DOVirtual.Float(200, 0, 0.5f, value =>
        {
            rect.sizeDelta = new Vector3(rect.sizeDelta.x, value);
        });
        await UniTask.Delay(TimeSpan.FromSeconds(2f));
        gameObject.SetActive(false);
    }

    private Tween TweenText(string content)
    {
        text.transform.localScale = Vector3.zero;
        text.SetText(content);
        Tween tween = text.transform.DOScale(1, 0.5f).SetEase(Ease.OutCubic)
        .OnComplete(() =>
        {
            text.transform.DOScale(0, 0.5f).SetEase(Ease.InCubic);
        });
        return tween;
    }
}