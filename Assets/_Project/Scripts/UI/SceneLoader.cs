using UnityEngine;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine.UI;

public class SceneLoader : Singleton<SceneLoader>
{   
    [SerializeField] public HoleCircleTransition transition;

    private Material transitionMaterial;

    public async UniTask LoadMainMenu()
    {
        await FadeIn();
        await UIManager.Instance.OpenMenu(Menu.Main);
    }

    public async UniTask FadeIn()
    {
        gameObject.SetActive(true);
        GetCanvasGroup().blocksRaycasts = true;
        await transition.FadeIn();
    }

    public async UniTask FadeOut()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        await transition.FadeOut();
        GetCanvasGroup().blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    public void SetCutoutPoint(Vector2 screenPosition)
    {
        Vector2 viewportPoint = Camera.main.ScreenToViewportPoint(screenPosition);
        transitionMaterial.SetVector("_CutoutPoint", new Vector4(viewportPoint.x, viewportPoint.y, 0, 0));
    }

    private CanvasGroup GetCanvasGroup() => GetComponent<CanvasGroup>();
}