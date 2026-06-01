using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace _Project.Scripts.Component
{
    public class BeginnerTut : MonoBehaviour
    {
        [SerializeField] private DOTweenAnimation animation;
        [SerializeField] private CanvasGroup canvasGroup;

        private void Awake()
        {
            if (animation == null) animation = GetComponent<DOTweenAnimation>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Start()
        {
            DataManager.Save(ConstantString.SHOWN_BEGINNER_TUT, 1);
            StartCoroutine(ShowTutorials());
        }

        IEnumerator ShowTutorials()
        {
            animation.DORestart();
            yield return animation.tween;
            yield return new WaitForSeconds(2);
            canvasGroup.DOFade(0, 1).OnComplete(() => gameObject.SetActive(false));
        }
    }
}