using System.Collections.Generic;
using System;
using UnityEngine;

public enum HEXACO { HonestyHumility, Emotionality, Extraversion, Agreeableness, Conscientiousness, Openness }

public enum TraitState { Low, High }

public enum DarkTriad { Machiavellianism, Narcissism, Psychopathy }

[Serializable]
public class VillagerData
{
    public string name;
    public int age;
    public string gender;
    public string occupation;
    public string location;

    [TextArea] public string situations;
    [TextArea] public string relations;

    public List<DarkTriad> DarkTriad;
    public TraitState HonestyHumility;
    public TraitState Emotionality;
    public TraitState Extraversion;
    public TraitState Agreeableness;
    public TraitState Conscientiousness;
    public TraitState Openness;

    public Dictionary<HEXACO, string> hexacoTraitsMap = new();

    [TextArea] public string bio;
    public List<string> personalityTraits = new();
    public List<string> interests = new();

    public List<string> friends = new();
    public List<string> blocks = new();

    [TextArea] public string motivation;
}

[Serializable]
public class VillagerBatch
{
    public List<VillagerData> villagers;
}
