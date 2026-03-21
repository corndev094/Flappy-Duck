using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

public class JuicyObject : MonoBehaviour
{
    [SerializeField] private bool applyX;
    [SerializeField] private bool applyY = true;
    [SerializeField] private float scaleFactorX = 1.1f;
    [SerializeField] private float scaleFactorY = 1.1f;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Ease easeType = Ease.InOutSine;
    [SerializeField] private bool playOnAwake = true;

    private Tweener _tweener;
    private Vector2 _initialScale;

    private void Awake()
    {
        _initialScale = transform.localScale;

        if (playOnAwake)
        {
            Play();
        }
    }

    [Button]
    public void Play()
    {
        if (_tweener != null && _tweener.IsPlaying()) return;
        _tweener = transform
            .DOScale(new Vector3(_initialScale.x * (applyX ? scaleFactorX : 1), _initialScale.y * (applyY ? scaleFactorY : 1)), duration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(easeType);
    }

    [Button]
    public void Stop()
    {
        _tweener?.Kill();
        transform.localScale = _initialScale;
    }

    [Button]
    public void Pause()
    {
        _tweener?.Pause();
    }

    [Button]
    public void Resume()
    {
        _tweener?.Play();
    }

    public void SetSpeed(float speed) => _tweener.timeScale = speed;

    private void OnDisable()
    {
        Stop();
    }
}