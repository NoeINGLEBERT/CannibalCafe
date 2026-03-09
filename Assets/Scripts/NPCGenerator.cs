using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

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

    [Header("Personality CSV")]
    public TextAsset personalityCSV;
    private Dictionary<(TraitState, HEXACO), Dictionary<(TraitState, HEXACO), string>> traitAdjectiveLookup;

    private List<VillagerData> workingVillagers = new();
    private Dictionary<int, string> familySurnames = new();
    private HashSet<string> usedFullNames = new();
    private HashSet<string> usedOccupations = new();

    private int currentIndex = 0;

    [SerializeField] private PoltiCharacterGenerator poltiCharacterGenerator;

    private void Start()
    {
        LoadPools();
        LoadTraitAdjectives();
    }

    private void Awake()
    {
        if (poltiCharacterGenerator != null)
        {
            poltiCharacterGenerator.OnCharactersGenerated += GenerateVillagersFromConstraints;
        }
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

    private void LoadTraitAdjectives()
    {
        traitAdjectiveLookup = new();

        if (personalityCSV == null)
        {
            Debug.LogWarning("No personality CSV assigned!");
            return;
        }

        string[] lines = personalityCSV.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        // The first line is headers
        string[] headers = lines[0].Split(',');

        for (int i = 1; i < lines.Length; i++)
        {
            string[] cols = lines[i].Split(',');

            if (cols.Length < 13) continue;

            // Determine row trait
            HEXACO rowTrait = ParseHexaco(cols[0]);
            TraitState rowState = cols[0].Contains("Low") ? TraitState.Low : TraitState.High;

            for (int j = 1; j < cols.Length; j++)
            {
                if (string.IsNullOrWhiteSpace(cols[j])) continue;

                HEXACO colTrait = ParseHexaco(headers[j]);
                TraitState colState = headers[j].Contains("Low") ? TraitState.Low : TraitState.High;

                if (!traitAdjectiveLookup.ContainsKey((rowState, rowTrait)))
                    traitAdjectiveLookup[(rowState, rowTrait)] = new Dictionary<(TraitState, HEXACO), string>();

                traitAdjectiveLookup[(rowState, rowTrait)][(colState, colTrait)] = cols[j].Trim();
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
        return HEXACO.HonestyHumility; // fallback
    }

    private List<string> LoadLinesFromTextAsset(TextAsset file)
    {
        if (file == null) return new List<string>();
        return file.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim()).ToList();
    }

    public void GenerateVillagersFromConstraints(List<CharacterConstraints> constraints)
    {
        workingVillagers.Clear();
        usedFullNames.Clear();
        usedOccupations.Clear();

        var familyMap = BuildFamilyMap(constraints);

        foreach (var c in constraints)
        {
            VillagerData v = new VillagerData();

            // -------- Gender --------
            Gender chosenGender = c.AllowedGenders[
                UnityEngine.Random.Range(0, c.AllowedGenders.Length)
            ];

            v.gender = chosenGender.ToString();

            // -------- Name --------
            int familyId = familyMap[c.Index];

            if (!familySurnames.ContainsKey(familyId))
                familySurnames[familyId] = GetRandomFromList(surnames);

            string surname = familySurnames[familyId];

            v.name = GetRandomUniqueFullNameWithSurname(v.gender, surname);

            // -------- Age --------
            v.age = UnityEngine.Random.Range(c.MinAge, c.MaxAge + 1);

            // -------- Occupation --------
            v.occupation = GetRandomUniqueOccupation();

            // -------- Location --------
            v.location = allowedLocations.Length > 0
                ? allowedLocations[UnityEngine.Random.Range(0, allowedLocations.Length)]
                : "Village";

            AssignRandomTraits(v);
            GeneratePersonalityTraits(v);

            workingVillagers.Add(v);
        }

        for (int i = 0; i < constraints.Count; i++)
        {
            CharacterConstraints c = constraints[i];
            VillagerData v = workingVillagers[i];

            string situations = "";

            foreach (PoltiRoleInstance role in c.AssignedRoles)
            {
                string resolvedSentence = ResolveSituationSentence(role, workingVillagers);

                situations += resolvedSentence + "\n";
            }

            v.situations = situations;
        }

        ApplyRelationsFromConstraints(constraints);

        GenerateNextVillager(0);
    }

    private string ResolveSituationSentence(PoltiRoleInstance role, List<VillagerData> villagers)
    {
        if (string.IsNullOrEmpty(role.Template.Sentence))
            return role.Template.Sentence;

        return System.Text.RegularExpressions.Regex.Replace(
            role.Template.Sentence,
            @"\{(\dX|\d{2})\}",
            match =>
            {
                string token = match.Groups[1].Value;

                int currentUnits = role.Index % 10;

                foreach (var r in role.Situation.Roles)
                {
                    int lastTwoDigits = r.Index % 100;
                    int tens = lastTwoDigits / 10;
                    int units = lastTwoDigits % 10;

                // Normal case: {01}, {12}, etc.
                if (token.Length == 2 && char.IsDigit(token[1]))
                    {
                        if (lastTwoDigits.ToString("D2") == token)
                        {
                            return r.GetDisplayName(
                                role.AssignedCharacter,
                                villagers
                            );
                        }
                    }
                // Special case: {1X}
                else if (token.Length == 2 && token[1] == 'X')
                    {
                        int tokenTens = token[0] - '0';

                        if (tens == tokenTens && units != currentUnits)
                        {
                            return r.GetDisplayName(
                                role.AssignedCharacter,
                                villagers
                            );
                        }
                    }
                }

                Debug.LogWarning($"No role found for token {{{token}}}");
                return match.Value; // fallback
        });
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

        // Shuffle
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

    private string GetAdjectiveForPair((HEXACO trait, TraitState state) t1, (HEXACO trait, TraitState state) t2)
    {
        if (traitAdjectiveLookup.TryGetValue((t1.state, t1.trait), out var inner))
        {
            if (inner.TryGetValue((t2.state, t2.trait), out var adj))
                return adj;
        }

        // fallback: try reverse
        if (traitAdjectiveLookup.TryGetValue((t2.state, t2.trait), out var inner2))
        {
            if (inner2.TryGetValue((t1.state, t1.trait), out var adj2))
                return adj2;
        }

        return null;
    }

    private void ApplyRelationsFromConstraints(List<CharacterConstraints> constraints)
    {
        for (int i = 0; i < constraints.Count; i++)
        {
            CharacterConstraints c = constraints[i];
            VillagerData v = workingVillagers[i];
            List<PoltiRoleInstance> deadRoles = new List<PoltiRoleInstance>();

            foreach (PoltiRelationInstance r in c.Relations)
            {
                CharacterConstraints target = r.CharacterTarget;

                if (target == null)
                {
                    if (!deadRoles.Contains(r.RoleTarget))
                    {
                        PoltiRelationInstance murderedRelation = r.RoleTarget.outRelations.FirstOrDefault(rel => rel.Template is CrimeRelation cr && cr.Type == CrimeType.Murdered);

                        if (murderedRelation == null)
                            continue;

                        if (murderedRelation.CharacterTarget == c)
                        {
                            v.relations += "-Murdered " + r.RoleTarget.GetDisplayName(c, workingVillagers) + "\n";
                            deadRoles.Add(r.RoleTarget);
                        }
                        else
                        {
                            v.relations += "-" + workingVillagers[murderedRelation.CharacterTarget.Index].name + " murdered " + r.RoleTarget.GetDisplayName(c, workingVillagers) + "\n";
                            deadRoles.Add(r.RoleTarget);
                        }
                    }

                    continue;
                }

                VillagerData targetVillager = workingVillagers[target.Index];

                string relationText = "";

                // =====================================================
                // Familial relations
                // =====================================================
                if (r.Template is FamilialRelation fam)
                {
                    string label = GetFamilialLabel(fam.Type, v.gender == "Male");
                    relationText = $"{targetVillager.name}'s {label}";
                }

                // =====================================================
                // Marriage
                // =====================================================
                else if (r.Template is MaritalRelation mar)
                {
                    switch (mar.Type)
                    {
                        case MaritalType.Married:
                            relationText = $"Married to {targetVillager.name}";
                            break;
                        case MaritalType.Divorced:
                            relationText = $"Divorced {targetVillager.name}";
                            break;
                    }
                }

                // =====================================================
                // Outgoing feelings
                // =====================================================
                else if (r.Template is OutgoingRelation outRel)
                {
                    relationText = $"{outRel.Type} : {targetVillager.name}";

                    switch (outRel.Type)
                    {
                        case OutgoingType.Love:
                            relationText = $"In love with {targetVillager.name}";
                            break;
                        case OutgoingType.Like:
                            relationText = $"Friends with {targetVillager.name}";
                            break;
                        case OutgoingType.Hate:
                            relationText = $"Hates {targetVillager.name}";
                            break;
                        case OutgoingType.Rivalry:
                            relationText = $"Rivals with {targetVillager.name}";
                            break;
                    }
                }

                // =====================================================
                // Incoming feelings
                // =====================================================
                else if (r.Template is IncomingRelation inRel)
                {
                    relationText = $"{inRel.Type} : {targetVillager.name}";

                    switch (inRel.Type)
                    {
                        case IncomingType.Loved:
                            relationText = $"Loved by {targetVillager.name}";
                            break;
                        case IncomingType.Liked:
                            relationText = $"{targetVillager.name}'s friend";
                            break;
                        case IncomingType.Hated:
                            relationText = $"Hated by {targetVillager.name}";
                            break;
                        case IncomingType.Rivaled:
                            relationText = $"{targetVillager.name}'s rival";
                            break;
                    }
                }

                // =====================================================
                // Crimes
                // =====================================================
                else if (r.Template is CrimeRelation crime)
                {
                    switch (crime.Type)
                    {
                        case CrimeType.Murder:
                            relationText = $"Murdered {targetVillager.name}";
                            break;
                        case CrimeType.Adultery:
                            relationText = $"Had an affair with {targetVillager.name}";
                            break;
                    }
                }

                // =====================================================
                // Append readable text
                // =====================================================
                if (!string.IsNullOrEmpty(relationText))
                {
                    v.relations += "-" + relationText + "\n";
                }
            }
        }
    }

    private string GetFamilialLabel(FamilialRelationType type, bool male)
    {
        return type switch
        {
            FamilialRelationType.Parent => male ? "father" : "mother",
            FamilialRelationType.Child => male ? "son" : "daughter",
            FamilialRelationType.Grandparent => male ? "grandfather" : "grandmother",
            FamilialRelationType.Grandchild => male ? "grandson" : "granddaughter",
            FamilialRelationType.Sibling => male ? "brother" : "sister",
            FamilialRelationType.Avuncular => male ? "uncle" : "aunt",
            FamilialRelationType.Nibling => male ? "nephew" : "niece",
            FamilialRelationType.GrandAvuncular => male ? "great-uncle" : "great-aunt",
            FamilialRelationType.GrandNibling => male ? "great-Nephew" : "great-niece",
            FamilialRelationType.Cousin => "cousin",
            _ => "relative" // PLACEHOLDER SHOULD BE REPLACED BY REAL STEP-PARENT DETECTION LOGIC
        };
    }

    private Dictionary<int, int> BuildFamilyMap(List<CharacterConstraints> constraints)
    {
        int count = constraints.Count;

        // Union-Find structure
        int[] parent = new int[count];
        for (int i = 0; i < count; i++)
            parent[i] = i;

        int Find(int x)
        {
            if (parent[x] != x)
                parent[x] = Find(parent[x]);
            return parent[x];
        }

        void Union(int a, int b)
        {
            int pa = Find(a);
            int pb = Find(b);
            if (pa != pb)
                parent[pb] = pa;
        }

        // Link characters by familial or marital relations
        for (int i = 0; i < constraints.Count; i++)
        {
            foreach (var r in constraints[i].Relations)
            {
                if (r.CharacterTarget == null) continue;

                bool isFamilyRelation =
                    r.Template is FamilialRelation ||
                    r.Template is MaritalRelation;

                if (isFamilyRelation)
                    Union(i, r.CharacterTarget.Index);
            }
        }

        // Final mapping: villager index = family ID
        Dictionary<int, int> map = new();
        for (int i = 0; i < count; i++)
            map[i] = Find(i);

        return map;
    }

    private string GetRandomUniqueFullNameWithSurname(string gender, string surname)
    {
        int attempts = 0;

        while (attempts < 50)
        {
            string firstName = gender == "Male"
                ? GetRandomFromList(maleNames)
                : GetRandomFromList(femaleNames);

            string fullName = $"{firstName} {surname}";

            if (!usedFullNames.Contains(fullName))
            {
                usedFullNames.Add(fullName);
                return fullName;
            }

            attempts++;
        }

        return $"{gender} {surname}";
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

    // ---------------- AI Generation ----------------

    private void GenerateNextVillager(int index)
    {
        if (index >= workingVillagers.Count)
        {
#if UNITY_EDITOR
            SaveVillagers(workingVillagers);
#endif
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

        string ComedicTone = v.Emotionality == TraitState.Low ? "dry" : "dark";

        // Key: (Extraversion, Agreeableness)
        Dictionary<(TraitState, TraitState), string> SurfaceTone = new()
        {
            { (TraitState.Low, TraitState.Low), "Cold-hearted and critical" },              // COLD-HEARTED KILLER
            { (TraitState.High, TraitState.Low), "Forcefully assertive and outgoing" },     // DOMINATION ADDICT
            { (TraitState.Low, TraitState.High), "Overly gentle and polite" },              // REFINED HANNIBAL LECTER
            { (TraitState.High, TraitState.High), "Casual and friendly on the surface" }    // ALLURING PREDATOR
        };

        // Key: (Conscientiousness, Openness)
        Dictionary<(TraitState, TraitState), string> SpeechTone = new()
        {
            { (TraitState.Low, TraitState.Low), "asserting control" },      // DOMINATION ADDICT
            { (TraitState.High, TraitState.Low), "assessing" },             // COLD-HEARTED KILLER
            { (TraitState.Low, TraitState.High), "flirting" },              // ALLURING PREDATOR
            { (TraitState.High, TraitState.High), "lecturing elegantly" }   // REFINED HANNIBAL LECTER
        };

        // Key: (Emotionality, Agreeableness)
        Dictionary<(TraitState, TraitState), string> HumorTone = new()
        {
            { (TraitState.Low, TraitState.Low), "Use body-part metaphors, skin, flesh, cuts, obsession with touch, butcher outlook, or clinical phrasing" },        // COLD-HEARTED KILLER
            { (TraitState.High, TraitState.Low), "Use possessive phrasing, 'you're mine', breaking someone apart, becoming one forever, or domination wording" },   // DOMINATION ADDICT
            { (TraitState.Low, TraitState.High), "Use disturbing animal cannibalism fun facts, natural selection metaphors, or the beauty of silence" },            // REFINED HANNIBAL LECTER
            { (TraitState.High, TraitState.High), "Use food metaphors, hunger, guilty pleasures, professional perks, or unsettling phrasing" }                      // ALLURING PREDATOR
        };

        string WordingTone = v.Conscientiousness == TraitState.Low ? "personality" : "occupation";

        string prompt = $@"
You are generating data for a fictional NPC in a {ComedicTone}ly comedic village simulation.

Focus especially on bio:

Write a roleplaying game character backstory in the style of a dating-app bio with very {ComedicTone} humor.
You must ROLEPLAY this character authentically.
Do not write as a narrator.
Think as them.

IDENTITY:
Name: {v.name}
Age: {v.age}
Gender: {v.gender}
Occupation: {v.occupation}
Personality: {string.Join(", ", v.personalityTraits)}

DRAMATIC CONTEXT:
Recent personal events (Not visible to the reader, must appear in bio): 
{v.situations}
Relationships:
{v.relations}
Recent personal events must clearly appear in the bio, the named character must be mentionned through the eyes of the character writting the bio
- Do not create new relations
- Use all recents personal events in the bio
- If this character feels a certain way towards another, the cause must be made apparent
A reader unfamiliar with subtext must still understand:
- Who they are related to
- What happened recently
- Their emotional stance toward it


TONE RULES FOR BIO:
- {SurfaceTone[(v.Extraversion, v.Agreeableness)]}
- Slightly {v.hexacoTraitsMap[HEXACO.Conscientiousness].ToLower()}
- Morbid but {v.hexacoTraitsMap[HEXACO.Openness].ToLower()}
- Imply cannibalism, murder, or taboo appetites only through euphemisms but keep recent events and relationships explicit and clear.
- Never explicitly state crimes
- {HumorTone[(v.Emotionality, v.Agreeableness)]}
- 3–6 short sentences
- Written in first person by a {v.hexacoTraitsMap[HEXACO.Agreeableness].ToLower()} writer
- Sounds like {SpeechTone[(v.Conscientiousness, v.Openness)]} while confessing something horrific
- The character’s {WordingTone} must strongly influence the wordings

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
