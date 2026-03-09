using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VillagerPersonalityGenerator : MonoBehaviour
{
    public TextAsset personalityCSV;

    private Dictionary<(TraitState, HEXACO), Dictionary<(TraitState, HEXACO), string>> lookup;

    public void Initialize()
    {
        LoadTraitAdjectives();
    }

    public void ApplyPersonality(List<VillagerData> villagers)
    {
        foreach (var v in villagers)
        {
            AssignRandomTraits(v);
            GeneratePersonalityTraits(v);
        }
    }

    private void AssignRandomTraits(VillagerData v)
    {
        v.HonestyHumility = UnityEngine.Random.value < 0.6f ? TraitState.Low : TraitState.High;
        v.Emotionality = UnityEngine.Random.value < 0.5f ? TraitState.Low : TraitState.High;
        v.Extraversion = UnityEngine.Random.value < 0.5f ? TraitState.Low : TraitState.High;
        v.Agreeableness = UnityEngine.Random.value < 0.6f ? TraitState.Low : TraitState.High;
        v.Conscientiousness = UnityEngine.Random.value < 0.5f ? TraitState.Low : TraitState.High;
        v.Openness = UnityEngine.Random.value < 0.4f ? TraitState.Low : TraitState.High;
    }

    private void GeneratePersonalityTraits(VillagerData v)
    {
        List<(HEXACO trait, TraitState state)> allTraits = new()
        {
            (HEXACO.HonestyHumility, v.HonestyHumility),
            (HEXACO.Emotionality, v.Emotionality),
            (HEXACO.Extraversion, v.Extraversion),
            (HEXACO.Agreeableness, v.Agreeableness),
            (HEXACO.Conscientiousness, v.Conscientiousness),
            (HEXACO.Openness, v.Openness)
        };

        allTraits = allTraits.OrderBy(_ => UnityEngine.Random.value).ToList();

        for (int i = 0; i < 3; i++)
        {
            var t1 = allTraits[i * 2];
            var t2 = allTraits[i * 2 + 1];

            string adj = GetAdjectiveForPair(t1, t2);

            if (!string.IsNullOrEmpty(adj))
            {
                v.personalityTraits.Add(adj);
                v.hexacoTraitsMap[t1.trait] = adj;
                v.hexacoTraitsMap[t2.trait] = adj;
            }
        }
    }

    private string GetAdjectiveForPair(
        (HEXACO trait, TraitState state) t1,
        (HEXACO trait, TraitState state) t2)
    {
        if (lookup.TryGetValue((t1.state, t1.trait), out var inner))
        {
            if (inner.TryGetValue((t2.state, t2.trait), out var adj))
                return adj;
        }

        if (lookup.TryGetValue((t2.state, t2.trait), out var inner2))
        {
            if (inner2.TryGetValue((t1.state, t1.trait), out var adj2))
                return adj2;
        }

        return null;
    }

    private void LoadTraitAdjectives()
    {
        lookup = new();
        if (!personalityCSV) return;

        string[] lines = personalityCSV.text.Split('\n');

        string[] headers = lines[0].Split(',');

        for (int i = 1; i < lines.Length; i++)
        {
            var cols = lines[i].Split(',');

            HEXACO rowTrait = ParseHexaco(cols[0]);
            TraitState rowState = cols[0].Contains("Low") ? TraitState.Low : TraitState.High;

            for (int j = 1; j < cols.Length; j++)
            {
                if (string.IsNullOrWhiteSpace(cols[j])) continue;

                HEXACO colTrait = ParseHexaco(headers[j]);
                TraitState colState = headers[j].Contains("Low") ? TraitState.Low : TraitState.High;

                if (!lookup.ContainsKey((rowState, rowTrait)))
                    lookup[(rowState, rowTrait)] = new();

                lookup[(rowState, rowTrait)][(colState, colTrait)] = cols[j].Trim();
            }
        }
    }

    private HEXACO ParseHexaco(string s)
    {
        if (s.Contains("Honesty")) return HEXACO.HonestyHumility;
        if (s.Contains("Emotionality")) return HEXACO.Emotionality;
        if (s.Contains("Extraversion")) return HEXACO.Extraversion;
        if (s.Contains("Agreeableness")) return HEXACO.Agreeableness;
        if (s.Contains("Conscientiousness")) return HEXACO.Conscientiousness;
        if (s.Contains("Openness")) return HEXACO.Openness;

        return HEXACO.HonestyHumility;
    }
}