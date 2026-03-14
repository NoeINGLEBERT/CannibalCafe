using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public enum CharacterButtonState
{
    Offline,      // Not generated yet
    Generating,   // Typing animation
    Generated     // Character ready
}

public class CharacterButton : MonoBehaviour
{
    public Image background;
    public Image border;
    public Button button;
    public TMP_Text text;

    public GameObject typingAnimation;
    public Transform[] dots;

    public float dotAnimDuration = 0.4f;
    public float delayBetweenDots = 0.15f;

    public Color offlineBackground;
    public Color offlineTextColor;

    public Color generatingBackground = Color.black;

    public Color generatedBackground = Color.white;
    public Color generatedTextColor = Color.black;

    public float transitionDuration = 0.25f;

    Coroutine transitionRoutine;
    Coroutine typingRoutine;

    private CharacterButtonState currentState;

    public void SetState(CharacterButtonState state, string characterName = "")
    {
        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(TransitionToState(state, characterName));
    }

    IEnumerator TransitionToState(CharacterButtonState state, string characterName)
    {
        currentState = state;

        Color startColor = background.color;
        Color targetColor = startColor;

        switch (state)
        {
            case CharacterButtonState.Offline:
                button.interactable = false;

                text.gameObject.SetActive(true);
                text.text = "Offline";
                text.color = offlineTextColor;

                border.enabled = false;

                StopTyping();

                targetColor = offlineBackground;

                break;

            case CharacterButtonState.Generating:
                button.interactable = false;

                text.gameObject.SetActive(false);
                border.enabled = false;

                StartTyping();

                targetColor = generatingBackground;

                break;

            case CharacterButtonState.Generated:
                button.interactable = true;

                text.gameObject.SetActive(true);
                text.text = characterName;
                text.color = generatedTextColor;

                border.enabled = true;

                StopTyping();

                targetColor = generatedBackground;

                break;
        }

        float t = 0;

        while (t < transitionDuration)
        {
            t += Time.deltaTime;

            background.color = Color.Lerp(startColor, targetColor, t / transitionDuration);

            yield return null;
        }

        background.color = targetColor;
    }

    void StartTyping()
    {
        typingAnimation.SetActive(true);

        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        typingRoutine = StartCoroutine(TypingDotsAnimation());
    }

    void StopTyping()
    {
        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        typingAnimation.SetActive(false);
    }

    IEnumerator TypingDotsAnimation()
    {
        Vector3 baseScale = Vector3.one;
        Vector3 smallScale = Vector3.one * 0.6f;

        while (true)
        {
            for (int i = 0; i < dots.Length; i++)
            {
                StartCoroutine(AnimateDot(dots[i], baseScale, smallScale));
                yield return new WaitForSeconds(delayBetweenDots);
            }

            yield return new WaitForSeconds(dotAnimDuration);
        }
    }

    IEnumerator AnimateDot(Transform dot, Vector3 big, Vector3 small)
    {
        float t = 0;

        while (t < dotAnimDuration)
        {
            t += Time.deltaTime;

            float curve = Mathf.Sin((t / dotAnimDuration) * Mathf.PI);

            dot.localScale = Vector3.Lerp(small, big, curve);

            yield return null;
        }

        dot.localScale = small;
    }
}