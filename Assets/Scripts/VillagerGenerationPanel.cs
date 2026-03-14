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

    private void OnEnable()
    {
        int villagerCount = LobbyFlowManager.Instance.Settings.Inhabitants;

        CreateButtons(villagerCount);

        aiGenerator.OnVillagerGenerated += OnVillagerGenerated;
        aiGenerator.OnGenerationComplete += OnGenerationComplete;

        generator.GenerateCharacters(villagerCount);
    }

    private void OnDisable()
    {
        aiGenerator.OnVillagerGenerated -= OnVillagerGenerated;
        aiGenerator.OnGenerationComplete -= OnGenerationComplete;
    }

    void CreateButtons(int count)
    {
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);

        buttons.Clear();

        for (int i = 0; i < count; i++)
        {
            CharacterButton btn = Instantiate(buttonPrefab, buttonContainer);

            btn.SetState(i == 0 ? CharacterButtonState.Generating : CharacterButtonState.Offline);

            buttons.Add(btn);
        }
    }

    void OnVillagerGenerated(int index, VillagerData villager)
    {
        if (index < 0 || index >= buttons.Count)
            return;

        buttons[index].SetState(CharacterButtonState.Generated, villager.name);

        if (index + 1 < 0 || index + 1 >= buttons.Count)
            return;

        buttons[index + 1].SetState(CharacterButtonState.Generating, villager.name);
    }

    void OnGenerationComplete(List<VillagerData> villagers)
    {
        Debug.Log("All villagers generated.");
    }
}