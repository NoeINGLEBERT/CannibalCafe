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
    [SerializeField] private PortraitRenderer portrait;
    [SerializeField] private Image background;
    [SerializeField] private Image border;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text text;

    [SerializeField] private GameObject typingAnimation;
    [SerializeField] private Transform[] dots;

    [SerializeField] private float dotAnimDuration = 0.4f;
    [SerializeField] private float delayBetweenDots = 0.15f;

    [SerializeField] private Color offlineBackground;
    [SerializeField] private Color offlineTextColor;

    [SerializeField] private Color generatingBackground = Color.black;

    [SerializeField] private Color generatedBackground = Color.white;
    [SerializeField] private Color generatedTextColor = Color.black;

    [SerializeField] private float transitionDuration = 0.25f;

    Coroutine transitionRoutine;
    Coroutine typingRoutine;

    private CharacterButtonState currentState;
    public CharacterButtonState State => currentState;
    public bool IsGenerated => currentState == CharacterButtonState.Generated;

    private VillagerData villager;
    public VillagerData Villager => villager;

    private VillagerPanel villagerPanel;
    private int index;

    public void Initialize(int index, VillagerData villager, VillagerPanel panel, VillagerAIGenerator generator)
    {
        this.index = index;
        this.villager = villager;
        villagerPanel = panel;

        button.onClick.AddListener(OnClick);

        generator.OnVillagerGenerationStarted += HandleGenerationStarted;
        generator.OnVillagerGenerated += HandleVillagerGenerated;
    }

    void OnClick()
    {
        if (currentState != CharacterButtonState.Generated)
            return;

        villagerPanel.SetCurrentIndex(index);
        villagerPanel.Open();
    }

    private void HandleGenerationStarted(int index)
    {
        if (this.index == index)
            SetState(CharacterButtonState.Generating);
    }

    private void HandleVillagerGenerated(int index, VillagerData data)
    {
        if (this.index == index)
                SetState(CharacterButtonState.Generated);
    }

    public void SetState(CharacterButtonState state)
    {
        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(TransitionToState(state));
    }

    IEnumerator TransitionToState(CharacterButtonState state)
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

                portrait.Render(new PortraitData(), PortraitRenderMode.Default);

                StopTyping();

                targetColor = offlineBackground;

                break;

            case CharacterButtonState.Generating:
                button.interactable = false;

                text.gameObject.SetActive(false);
                border.enabled = false;

                portrait.Render(PortraitCoder.Decode(villager.portraitCode), PortraitRenderMode.Partial);

                StartTyping();

                targetColor = generatingBackground;

                break;

            case CharacterButtonState.Generated:
                button.interactable = true;

                text.gameObject.SetActive(true);
                text.text = villager.name;
                text.color = generatedTextColor;

                border.enabled = true;

                portrait.Render(PortraitCoder.Decode(villager.portraitCode), PortraitRenderMode.Full);

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