using UnityEngine;
using System.Collections;

public class BioPanel : MonoBehaviour
{
    [Header("Read More Animation")]
    [SerializeField] RectTransform contentAnchor;
    [SerializeField] CanvasGroup readMoreGroup;
    [SerializeField] CanvasGroup bioGroup;

    [SerializeField] float slideDuration = 0.4f;
    [SerializeField] float fadeDuration = 0.25f;
    [SerializeField] float slideDistance = 465f;

    private Vector2 initialAnchorPos;
    private bool isExpanded = false;
    private Coroutine currentAnim;

    void Start()
    {
        initialAnchorPos = contentAnchor.anchoredPosition;

        readMoreGroup.alpha = 1;
        readMoreGroup.interactable = true;
        readMoreGroup.blocksRaycasts = true;

        bioGroup.alpha = 0;
        bioGroup.interactable = false;
        bioGroup.blocksRaycasts = false;
    }

    public void OnReadMore()
    {
        if (isExpanded) return;

        if (currentAnim != null)
            StopCoroutine(currentAnim);

        currentAnim = StartCoroutine(ExpandRoutine());
    }

    public void OnCollapse()
    {
        if (!isExpanded) return;

        if (currentAnim != null)
            StopCoroutine(currentAnim);

        currentAnim = StartCoroutine(CollapseRoutine());
    }

    private IEnumerator ExpandRoutine()
    {
        isExpanded = true;

        float time = 0f;
        Vector2 startPos = contentAnchor.anchoredPosition;
        Vector2 targetPos = initialAnchorPos + Vector2.up * slideDistance;

        // Step 1: Slide + fade out button
        while (time < slideDuration)
        {
            time += Time.deltaTime;
            float t = time / slideDuration;

            contentAnchor.anchoredPosition = Vector2.Lerp(startPos, targetPos, EaseOut(t));
            readMoreGroup.alpha = 1 - t;

            yield return null;
        }

        readMoreGroup.interactable = false;
        readMoreGroup.blocksRaycasts = false;

        // Step 2: Fade in bio
        time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeDuration;

            bioGroup.alpha = t;

            yield return null;
        }

        bioGroup.interactable = true;
        bioGroup.blocksRaycasts = true;
    }

    private IEnumerator CollapseRoutine()
    {
        isExpanded = false;

        float time = 0f;

        // Step 1: Fade out bio
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeDuration;

            bioGroup.alpha = 1 - t;

            yield return null;
        }

        bioGroup.interactable = false;
        bioGroup.blocksRaycasts = false;

        // Step 2: Slide back + fade in button
        time = 0f;
        Vector2 startPos = contentAnchor.anchoredPosition;
        Vector2 targetPos = initialAnchorPos;

        while (time < slideDuration)
        {
            time += Time.deltaTime;
            float t = time / slideDuration;

            contentAnchor.anchoredPosition = Vector2.Lerp(startPos, targetPos, EaseOut(t));
            readMoreGroup.alpha = t;

            yield return null;
        }

        readMoreGroup.interactable = true;
        readMoreGroup.blocksRaycasts = true;
    }

    private float EaseOut(float t)
    {
        return Mathf.Pow(t, 2f);
    }

    public void SetStateInstant(bool expanded)
    {
        // Stop any running animation
        if (currentAnim != null)
            StopCoroutine(currentAnim);

        isExpanded = expanded;

        if (expanded)
        {
            // Final EXPANDED state
            contentAnchor.anchoredPosition = initialAnchorPos + Vector2.up * slideDistance;

            readMoreGroup.alpha = 0;
            readMoreGroup.interactable = false;
            readMoreGroup.blocksRaycasts = false;

            bioGroup.alpha = 1;
            bioGroup.interactable = true;
            bioGroup.blocksRaycasts = true;
        }
        else
        {
            // Final COLLAPSED state
            contentAnchor.anchoredPosition = initialAnchorPos;

            readMoreGroup.alpha = 1;
            readMoreGroup.interactable = true;
            readMoreGroup.blocksRaycasts = true;

            bioGroup.alpha = 0;
            bioGroup.interactable = false;
            bioGroup.blocksRaycasts = false;
        }
    }
}
