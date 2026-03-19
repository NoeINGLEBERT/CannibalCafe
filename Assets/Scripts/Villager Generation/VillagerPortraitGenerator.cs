using System.Collections.Generic;
using UnityEngine;

public class VillagerPortraitGenerator : MonoBehaviour
{
    public PortraitDatabase database;

    public void GeneratePortraits(List<VillagerData> villagers)
    {
        foreach (var villager in villagers)
        {
            PortraitData data = PortraitGenerator.Generate(database);
            villager.portraitCode = PortraitCoder.Encode(data);
        }
    }
}
