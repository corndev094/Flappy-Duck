using Cysharp.Threading.Tasks;
using DG.Tweening;
// using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class HoleCircleTransition : MonoBehaviour {
    [SerializeField] private float duration = 0.75f;
    [SerializeField] private Ease easeType;

    private Image image;
    private Material mat;
    private Tween currentTween;

    void Awake()
    {
        image ??= GetComponent<Image>();
        if (image == null) return;
        image.material = new Material(image.material);
        mat = image.material;
    }

    public async UniTask FadeIn()
    {
        if (image == null) return;
        if (currentTween != null) currentTween.Kill();
        currentTween = DOVirtual.Float(1, 0, duration, value =>
        {
            mat.SetFloat("_Radius", value);
        }).SetEase(easeType);
        await currentTween.AsyncWaitForCompletion();
    }

    public async UniTask FadeOut()
    {
        if (image == null) return;
        if (currentTween != null) currentTween.Kill();
        currentTween = DOVirtual.Float(0, 1, duration, value =>
        {
            mat.SetFloat("_Radius", value);
        }).SetEase(easeType);
        await currentTween.AsyncWaitForCompletion();
    }
}