using System.Collections;
using System;
using MoonPull.Ads;
using MoonPull.Localization;
using UnityEngine.UI;
using UnityEngine;

namespace MoonPull.UI
{
    public sealed class StarRatingView : MonoBehaviour
    {
        [SerializeField] private Image[] stars = new Image[3];
        [SerializeField] private Sprite filled;
        [SerializeField] private Sprite empty;
        [SerializeField, Min(0f)] private float interval = 0.25f;

        public void SetImmediate(int count)
        {
            StopAllCoroutines();
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].sprite = i < count ? filled : empty;
                stars[i].transform.localScale = Vector3.one;
            }
        }

        public void Animate(int count)
        {
            SetImmediate(0);
            StartCoroutine(Reveal(count));
        }

        private IEnumerator Reveal(int count)
        {
            for (int i = 0; i < count && i < stars.Length; i++)
            {
                yield return new WaitForSecondsRealtime(interval);
                stars[i].sprite = filled;
                UiTween.PopIn(stars[i].transform, 0.3f);
            }
        }
    }
}
