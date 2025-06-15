using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class StarRatingDisplay : MonoBehaviour
{
    public List<Image> stars;             // Assign 5 star Image components in Inspector
    public Material grayscaleMat;
    public Material normalMat;
    public float popDuration = 0.3f;
    public bool animateSequentially = true;

    public void SetStarsFromXP(int totalXP)
    {
        int filledCount = Mathf.Clamp(totalXP / 50, 0, 5);

        // Set all stars to grayscale first
        foreach (var star in stars)
        {
            star.material = grayscaleMat;
            star.transform.localScale = Vector3.one;
        }

        // Then animate selected ones
        if (animateSequentially)
            StartCoroutine(AnimateStarsSequentially(filledCount));
        else
            AnimateAllAtOnce(filledCount);
    }

    void AnimateAllAtOnce(int filledCount)
    {
        for (int i = 0; i < stars.Count; i++)
        {
            var star = stars[i];
            if (i < filledCount)
            {
                star.material = normalMat;
                star.transform
                    .DOScale(1.3f, popDuration / 2f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() => star.transform.DOScale(1f, popDuration / 2f));
            }
        }
    }

    IEnumerator AnimateStarsSequentially(int filledCount)
    {
        for (int i = 0; i < filledCount; i++)
        {
            var star = stars[i];
            yield return new WaitForSeconds(0.1f);

            star.material = normalMat;

            star.transform
                .DOScale(1.3f, popDuration / 2f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => star.transform.DOScale(1f, popDuration / 2f));
        }
    }
}
