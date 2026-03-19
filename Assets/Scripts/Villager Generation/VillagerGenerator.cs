using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

public class VillagerGenerator : MonoBehaviour
{
    public PoltiCharacterGenerator poltiCharacterGenerator;
    public VillagerFactory factory;
    public VillagerPersonalityGenerator personality;
    public VillagerNarrativeBuilder narrative;
    public VillagerPortraitGenerator portraits;
    public VillagerAIGenerator ai;

    public VillageDatabase database;

    private void Start()
    {
        factory.Initialize();
        personality.Initialize();
    }

    private void Awake()
    {
        if (poltiCharacterGenerator != null)
        {
            poltiCharacterGenerator.OnCharactersGenerated += Generate;
        }
    }

    public void Generate(List<CharacterConstraints> constraints)
    {
        List<VillagerData> villagers = factory.CreateVillagers(constraints);

        personality.ApplyPersonality(villagers);

        narrative.ApplySituations(villagers, constraints);
        narrative.ApplyRelations(villagers, constraints);

        portraits.GeneratePortraits(villagers);

        ai.GenerateVillagers(villagers);
    }
}
