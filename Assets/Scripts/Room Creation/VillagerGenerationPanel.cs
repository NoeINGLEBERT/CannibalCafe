using System.Collections.Generic;
using UnityEngine;

public class VillagerGenerationPanel : MonoBehaviour
{
    [Header("References")]

    [SerializeField] private CharacterButton buttonPrefab;
    [SerializeField] private Transform buttonContainer;

    [SerializeField] private PoltiCharacterGenerator generator;
    [SerializeField] private VillagerAIGenerator aiGenerator;

    private List<CharacterButton> buttons = new();

    [SerializeField] private VillagerPanel villagerPanel;

    private void OnEnable()
    {
        int villagerCount = LobbyFlowManager.Instance.Settings.Population;

        aiGenerator.OnVillagersGenerationStarted += CreateButtons;
        aiGenerator.OnGenerationComplete += OnGenerationComplete;

        generator.GenerateCharacters(villagerCount);
    }

    private void OnDisable()
    {
        aiGenerator.OnVillagersGenerationStarted -= CreateButtons;
        aiGenerator.OnGenerationComplete -= OnGenerationComplete;
    }

    void CreateButtons(List<VillagerData> villagers)
    {
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);

        buttons.Clear();

        int i = 0;

        foreach (VillagerData villager in villagers)
        {
            CharacterButton btn = Instantiate(buttonPrefab, buttonContainer);

            btn.Initialize(i, villager, villagerPanel, aiGenerator);

            btn.SetState(CharacterButtonState.Offline);

            buttons.Add(btn);

            i++;
        }

        villagerPanel.Initialize(buttons);
    }

    void OnGenerationComplete(List<VillagerData> villagers)
    {
        LobbyFlowManager.Instance.Settings.Villagers = villagers;
    }
}