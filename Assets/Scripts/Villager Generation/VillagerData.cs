using System.Collections.Generic;
using System;
using UnityEngine;

public enum HEXACO { HonestyHumility, Emotionality, Extraversion, Agreeableness, Conscientiousness, Openness }

public enum TraitState { Low, High }

[Serializable]
public class VillagerData
{
    public int index;

    public string name;
    public int age;
    public string gender;
    public string occupation;
    public string location;

    [TextArea] public string situations;
    [TextArea] public string relations;

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

    public ulong portraitCode;
}

[Serializable]
public class VillagerBatch
{
    public List<VillagerData> villagers;
}
