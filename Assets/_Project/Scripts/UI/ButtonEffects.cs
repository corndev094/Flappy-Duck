using Coffee.UIEffects;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonEffects : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {
    [SerializeField] private Button btn;
    [SerializeField] private Image image;
    [SerializeField] private bool swapSprite;
    [SerializeField, ShowIf("swapSprite")] private Sprite clickedSprite;
    [SerializeField, ShowIf("swapSprite")] private Sprite normalSprite;
    [Space(20)]
    [SerializeField] private AudioClip defaultClickSound;
    [SerializeField] private bool randomSound;
    [SerializeField, ShowIf("randomSound")] private AudioClip[] clickSounds;

    void OnValidate()
    {
        if (btn == null) btn = GetComponent<Button>();
        if (image == null) image = GetComponent<Image>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (randomSound) SoundManager.Instance.PlaySFX(clickSounds[Random.Range(0, clickSounds.Length)]);
        else SoundManager.Instance.PlaySFX(defaultClickSound);
        if (!swapSprite || clickedSprite == null) return;
        image.sprite = clickedSprite;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!swapSprite || normalSprite == null) return;
        image.sprite = normalSprite;
    }

    public Button Button => btn;
}