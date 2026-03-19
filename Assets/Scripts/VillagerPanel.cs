using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VillagerPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] PortraitRenderer portrait;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text ageText;
    [SerializeField] TMP_Text occupationText;
    [SerializeField] TMP_Text traitsText;
    [SerializeField] TMP_Text bioText;
    [SerializeField] GameObject blackPanel;

    [Header("Buttons")]
    [SerializeField] RoundButton nextButton;
    [SerializeField] RoundButton prevButton;
    [SerializeField] RoundButton closeButton;
    [SerializeField] RoundButton selectButton;
    [SerializeField] RoundButton resetButton;

    [SerializeField] private VillagerAIGenerator generator;

    private List<CharacterButton> buttons;
    private int currentIndex;

    private void OnEnable()
    {
        generator.OnVillagerGenerationStarted += HandleGenerationStarted;
        generator.OnVillagerGenerated += HandleVillagerGenerated;
        generator.OnGenerationComplete += HandleGenerationComplete;
    }

    private void OnDisable()
    {
        generator.OnVillagerGenerated -= HandleVillagerGenerated;
        generator.OnGenerationComplete -= HandleGenerationComplete;
    }

    public void Initialize(List<CharacterButton> buttons)
    {
        this.buttons = buttons;
        SetGenerationState(true);
    }

    public void SetCurrentIndex(int index)
    {
        this.currentIndex = index;
        UpdatePanel();
    }

    public void UpdatePanel()
    {
        if (buttons == null || buttons.Count == 0)
            return;

        CharacterButton btn = buttons[currentIndex];

        if (!btn.IsGenerated)
            return;

        VillagerData v = btn.Villager;

        portrait.Render(PortraitCoder.Decode(v.portraitCode), PortraitRenderMode.Full);
        nameText.text = $"{v.name},";
        ageText.text = $" {v.age}";
        occupationText.text = v.occupation;

        traitsText.text = string.Join(", ", v.personalityTraits);
        bioText.text = v.bio;

        TryNext(false);
        TryNext(true);
    }

    private void HandleGenerationStarted(int index)
    {
        UpdatePanel();
    }

    private void HandleVillagerGenerated(int index, VillagerData data)
    {
        UpdatePanel();
    }

    private void HandleGenerationComplete(List<VillagerData> villagers)
    {
        SetGenerationState(false);
    }

    public void Next(bool isNext)
    {
        int delta = isNext ? 1 : -1;

        bool skipped;
        int nextIndex = FindNextIndex(currentIndex, delta, out skipped);

        if (nextIndex != -1)
            SetCurrentIndex(nextIndex);
    }

    public void TryNext(bool isNext)
    {
        RoundButton button = isNext ? nextButton : prevButton;
        int delta = isNext ? 1 : -1;

        bool skipped;
        int nextIndex = FindNextIndex(currentIndex, delta, out skipped);

        if (nextIndex == -1)
            button.SetState(false, false);
        else
            button.SetState(true, skipped);
    }

    private int FindNextIndex(int startIndex, int delta, out bool skipped)
    {
        skipped = false;
        int nextIndex = startIndex + delta;

        if (nextIndex < 0 || nextIndex >= buttons.Count)
            return -1;

        if (buttons[nextIndex].IsGenerated)
            return nextIndex;

        nextIndex += delta;
        skipped = true;

        if (nextIndex >= 0 && nextIndex < buttons.Count && buttons[nextIndex].IsGenerated)
            return nextIndex;

        return -1;
    }

    public void Open()
    {
        blackPanel.SetActive(true);
        gameObject.SetActive(true);
    }

    public void Close()
    {
        blackPanel.SetActive(false);
        gameObject.SetActive(false);
    }

    public void RegenerateBio()
    {
        generator.OnVillagerGenerated += HandleVillagerRegenerated;

        generator.GenerateVillagerAt(currentIndex);

        buttons[currentIndex].SetState(CharacterButtonState.Generating);

        SetGenerationState(true);
    }

    private void HandleVillagerRegenerated(int index, VillagerData data)
    {
        generator.OnVillagerGenerated -= HandleVillagerRegenerated;

        HandleGenerationComplete(generator.villagers);
    }

    public void SetGenerationState(bool generating)
    {
        selectButton.SetState(!generating, false);
        resetButton.SetState(!generating, false);
    }
}
