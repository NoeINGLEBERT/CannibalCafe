using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

[Serializable]
public class VillagerData
{
    public string name;
    public int age;
    public string gender;
    public string occupation;
    public string location;

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

public class NPCGenerator : MonoBehaviour
{
    [Header("AI")]
    public OpenRouterChat aiClient;

    [Header("Generation Settings")]
    public int villagerCount = 10;

    [Header("Random Pools")]
    public TextAsset maleNamesFile;
    public TextAsset femaleNamesFile;
    public TextAsset surnamesFile;
    public TextAsset occupationsFile;

    [TextArea(4, 10)]
    public string[] allowedLocations;

    [Header("Database")]
    public VillageDatabase database;

    private List<string> maleNames = new();
    private List<string> femaleNames = new();
    private List<string> surnames = new();
    private List<string> occupations = new();

    private List<VillagerData> workingVillagers = new();
    private HashSet<string> usedFullNames = new();
    private HashSet<string> usedOccupations = new();

    private int currentIndex = 0;

    private void Start()
    {
        LoadPools();
        GenerateVillagers();
    }

    private void LoadPools()
    {
        maleNames = LoadLinesFromTextAsset(maleNamesFile);
        femaleNames = LoadLinesFromTextAsset(femaleNamesFile);
        surnames = LoadLinesFromTextAsset(surnamesFile);
        occupations = LoadLinesFromTextAsset(occupationsFile);

        if (maleNames.Count == 0 || femaleNames.Count == 0 || surnames.Count == 0 || occupations.Count == 0)
            Debug.LogWarning("One or more text pools are empty!");
    }

    private List<string> LoadLinesFromTextAsset(TextAsset file)
    {
        if (file == null) return new List<string>();
        return file.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim()).ToList();
    }

    public void GenerateVillagers()
    {
        workingVillagers.Clear();
        usedFullNames.Clear();
        usedOccupations.Clear();

        for (int i = 0; i < villagerCount; i++)
        {
            VillagerData v = new VillagerData();
            v.gender = UnityEngine.Random.value > 0.5f ? "Male" : "Female";
            v.name = GetRandomUniqueFullName(v.gender);
            v.age = GenerateWeightedAge();
            v.occupation = GetRandomUniqueOccupation();
            v.location = allowedLocations.Length > 0
                ? allowedLocations[UnityEngine.Random.Range(0, allowedLocations.Length)]
                : "Village";

            workingVillagers.Add(v);
        }

        GenerateNextVillager(0);
    }

    private string GetRandomUniqueFullName(string gender)
    {
        int attempts = 0;
        while (attempts < 50)
        {
            string firstName = gender == "Male" ? GetRandomFromList(maleNames) : GetRandomFromList(femaleNames);
            string surname = GetRandomFromList(surnames);
            string fullName = $"{firstName} {surname}";

            if (!usedFullNames.Contains(fullName))
            {
                usedFullNames.Add(fullName);
                return fullName;
            }

            attempts++;
        }

        // Fallback
        return $"Name{UnityEngine.Random.Range(0, 1000)}";
    }

    private string GetRandomUniqueOccupation()
    {
        int attempts = 0;
        while (attempts < 50)
        {
            string occ = GetRandomFromList(occupations);
            if (!usedOccupations.Contains(occ))
            {
                usedOccupations.Add(occ);
                return occ;
            }
            attempts++;
        }

        // fallback
        return GetRandomFromList(occupations);
    }

    private string GetRandomFromList(List<string> list)
    {
        if (list.Count == 0) return "Unknown";
        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    private int GenerateWeightedAge()
    {
        // Weighted but spread out: 18-25 (15%), 26-35 (20%), 36-50 (35%), 51-65 (20%), 66-80 (10%)
        float roll = UnityEngine.Random.value;
        if (roll < 0.15f) return UnityEngine.Random.Range(18, 26);
        if (roll < 0.35f) return UnityEngine.Random.Range(26, 36);
        if (roll < 0.70f) return UnityEngine.Random.Range(36, 51);
        if (roll < 0.90f) return UnityEngine.Random.Range(51, 66);
        return UnityEngine.Random.Range(66, 81);
    }

    // ---------------- AI Generation ----------------

    private void GenerateNextVillager(int index)
    {
        if (index >= workingVillagers.Count)
        {
            SaveVillagers(workingVillagers);
            Debug.Log("[NPCGenerator] All villagers generated.");
            return;
        }

        currentIndex = index;
        VillagerData v = workingVillagers[index];

        VillagerBatch batch = new VillagerBatch
        {
            villagers = new List<VillagerData> { v }
        };

        string jsonSeed = JsonUtility.ToJson(batch, true);

        string prompt = $@"
You are generating data for a fictional NPC in a darkly comedic village simulation.

Focus especially on bio:

Write a dating-app style bio with very dark humor.

Tone rules for bio:
- Casual and friendly on the surface
- Slightly unhinged
- Morbid but playful
- Imply cannibalism, murder, or taboo appetites only through euphemisms
- Never explicitly state crimes
- Use food metaphors, hunger, guilty pleasures, professional perks, or unsettling phrasing
- 2–4 short sentences
- Written in first person
- Sounds like flirting while confessing something horrific
- The character’s occupation must strongly influence the wordings

IMPORTANT:
- Keep everything subtle—no explicit violence, gore, or crimes.
- Do not change name, age, gender, occupation, or location.
- Only fill in: bio.
- Return valid JSON in exactly the same structure as provided below.

Return JSON in the same structure:
{jsonSeed}
";

        aiClient.SendMessageToAI(prompt, OnSingleVillagerGenerated);
    }

    private void OnSingleVillagerGenerated(string response)
    {
        Debug.Log("[NPCGenerator] AI Returned:\n" + response);

        try
        {
            string cleaned = ExtractJson(response);

            VillagerBatch batch = JsonUtility.FromJson<VillagerBatch>(cleaned);

            if (batch == null || batch.villagers == null || batch.villagers.Count == 0)
            {
                Debug.LogError("Failed to parse villager.");
                GenerateNextVillager(currentIndex + 1);
                return;
            }

            VillagerData generated = batch.villagers[0];

            workingVillagers[currentIndex].bio = generated.bio;
            workingVillagers[currentIndex].personalityTraits = generated.personalityTraits;
            workingVillagers[currentIndex].interests = generated.interests;
            workingVillagers[currentIndex].motivation = generated.motivation;

            GenerateNextVillager(currentIndex + 1);
        }
        catch (Exception e)
        {
            Debug.LogError("NPC Parse Exception: " + e);
            GenerateNextVillager(currentIndex + 1);
        }
    }

    private string ExtractJson(string input)
    {
        int firstBrace = input.IndexOf('{');
        int lastBrace = input.LastIndexOf('}');

        if (firstBrace == -1 || lastBrace == -1)
        {
            Debug.LogError("No JSON object found in AI output.");
            return input;
        }

        return input.Substring(firstBrace, lastBrace - firstBrace + 1).Trim();
    }

#if UNITY_EDITOR
    private void SaveVillagers(List<VillagerData> villagers)
    {
        if (!database)
        {
            Debug.LogError("No VillageDatabase assigned.");
            return;
        }

        database.villagers.Clear();

        foreach (var v in villagers)
            database.villagers.Add(v);

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        Debug.Log($"[NPCGenerator] Saved {villagers.Count} villagers into database.");
    }
#endif
}
