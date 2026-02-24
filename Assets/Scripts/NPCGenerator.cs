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

    public string situations;
    public string roles;
    [TextArea] public string relations;

    public List<DarkTriad> DarkTriad;
    public TraitState HonestyHumility;
    public TraitState Emotionality;
    public TraitState Extraversion;
    public TraitState Agreeableness;
    public TraitState Conscientiousness;
    public TraitState Openness;

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


            string situations = "";
            string roles = "";

            foreach (PoltiRoleInstance role in c.AssignedRoles)
            {
                situations += role.Situation.Name + ", ";
                roles += role.Name + ", ";
            }

            situations = situations.Substring(0, situations.Length - 2);
            roles = roles.Substring(0, roles.Length - 2);

            v.situations = situations;
            v.roles = roles;

            AssignRandomTraits(v);
            v.personalityTraits = GeneratePersonalityTraits(v);

            workingVillagers.Add(v);
        }

        ApplyRelationsFromConstraints(constraints);

        GenerateNextVillager(0);
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

    private List<string> GeneratePersonalityTraits(VillagerData v)
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

        List<string> result = new();

        for (int i = 0; i < 3; i++)
        {
            var t1 = allTraits[i * 2];
            var t2 = allTraits[i * 2 + 1];

            string adj = GetAdjectiveForPair(t1, t2);
            if (!string.IsNullOrEmpty(adj))
                result.Add(adj);
        }

        return result;
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
                            v.relations += "    -" + ResolveMurder(c, v, r.RoleTarget, workingVillagers) + "\n";
                            deadRoles.Add(r.RoleTarget);
                        }
                        else
                        {
                            var relation = r.RoleTarget.outRelations.Where(r =>
                                        r.Template is not CrimeRelation { Type: CrimeType.Murder } &&
                                        r.Template is not CrimeRelation { Type: CrimeType.Murdered })
                                    .OrderBy(GetRelationPriority)
                                    .FirstOrDefault();

                            if (relation == null)
                            {
                                v.relations += "    -" + (v.gender == "Male" ? "His " : "Her ") + "acquaintance was murdered by " + workingVillagers[murderedRelation.CharacterTarget.Index].name + "\n";
                                deadRoles.Add(r.RoleTarget);
                            }

                            v.relations += "    -" + (v.gender == "Male" ? "His " : "Her ") + GetRelationLabel(relation, r.RoleTarget.Template.AllowedGenders[0] == Gender.Male) + " was murdered by " + workingVillagers[murderedRelation.CharacterTarget.Index].name + "\n";
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
                    relationText = $"{label} of {targetVillager.name}";
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
                            relationText = $"Divorced to {targetVillager.name}";
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
                            relationText = $"Rivaled by {targetVillager.name}";
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
                            relationText = $"Committed adultery with {targetVillager.name}";
                            break;
                    }
                }

                // =====================================================
                // Append readable text
                // =====================================================
                if (!string.IsNullOrEmpty(relationText))
                {
                    v.relations += "    -" + relationText + "\n";
                }
            }
        }
    }

    private string ResolveMurder(CharacterConstraints killer, VillagerData killerData, PoltiRoleInstance killed, List<VillagerData> villagers)
    {
        bool killedMale = killed.Template.AllowedGenders[0] == Gender.Male;
        bool killerMale = killerData.gender == Gender.Male.ToString();
        string possessive = killerMale ? "his" : "her";

        // Direct relation to killer
        var direct = killed.outRelations
            .Where(r =>
                r.CharacterTarget == killer &&
                r.Template is not CrimeRelation { Type: CrimeType.Murder } &&
                r.Template is not CrimeRelation { Type: CrimeType.Murdered })
            .OrderBy(GetRelationPriority)
            .FirstOrDefault();

        if (direct != null)
        {
            string label = GetRelationLabel(direct, killedMale);
            return $"Murdered {possessive} {label}";
        }

        // Fallback: strongest relation to anyone
        var fallback = killed.outRelations
            .Where(r =>
                r.Template is not CrimeRelation { Type: CrimeType.Murder } &&
                r.Template is not CrimeRelation { Type: CrimeType.Murdered })
            .OrderBy(GetRelationPriority)
            .FirstOrDefault();

        if (fallback != null && fallback.CharacterTarget != null)
        {
            var otherVillager = villagers[fallback.CharacterTarget.Index];
            string label = GetRelationLabel(fallback, killedMale);

            return $"Murdered {otherVillager.name}'s {label}";
        }

        return "Murdered a stranger";
    }

    private string GetRelationLabel(PoltiRelationInstance relation, bool male)
    {
        switch (relation.Template)
        {
            case FamilialRelation fam when fam.Type != FamilialRelationType.Unrelated:
                return GetFamilialLabel(fam.Type, male).ToLower();

            case MaritalRelation mar:
                return mar.Type switch
                {
                    MaritalType.Married => male ? "husband" : "wife",
                    MaritalType.Divorced => male ? "ex-husband" : "ex-wife",
                    _ => "acquaintance"
                };

            case IncomingRelation inRel:
                return inRel.Type switch
                {
                    IncomingType.Loved => "loved one",
                    IncomingType.Liked => "friend",
                    IncomingType.Hated => "enemy",
                    IncomingType.Rivaled => "rival",
                    _ => "acquaintance"
                };

            case OutgoingRelation outRel:
                return outRel.Type switch
                {
                    OutgoingType.Love => "loved one",
                    OutgoingType.Like => "friend",
                    OutgoingType.Hate => "enemy",
                    OutgoingType.Rivalry => "rival",
                    _ => "acquaintance"
                };

            case CrimeRelation crime:
                return crime.Type switch
                {
                    CrimeType.Adultery => male ? "lover" : "mistress",
                    _ => "acquaintance"
                };

            default:
                return "acquaintance";
        }
    }

    private int GetRelationPriority(PoltiRelationInstance r)
    {
        return r.Template switch
        {
            FamilialRelation fam when fam.Type != FamilialRelationType.Unrelated => 0,
            MaritalRelation => 1,
            CrimeRelation => 2,
            IncomingRelation => 3,
            OutgoingRelation => 4,
            _ => 100
        };
    }

    private string GetFamilialLabel(FamilialRelationType type, bool male)
    {
        return type switch
        {
            FamilialRelationType.Parent => male ? "Father" : "Mother",
            FamilialRelationType.Child => male ? "Son" : "Daughter",
            FamilialRelationType.Grandparent => male ? "Grandfather" : "Grandmother",
            FamilialRelationType.Grandchild => male ? "Grandson" : "Granddaughter",
            FamilialRelationType.Sibling => male ? "Brother" : "Sister",
            FamilialRelationType.Avuncular => male ? "uncle" : "Aunt",
            FamilialRelationType.Nibling => male ? "Nephew" : "Niece",
            FamilialRelationType.GrandAvuncular => male ? "Great-uncle" : "Great-aunt",
            FamilialRelationType.GrandNibling => male ? "Great-Nephew" : "Great-niece",
            FamilialRelationType.Cousin => "Cousin",
            _ => "Relative" // PLACEHOLDER SHOULD BE REPLACED BY REAL STEP-PARENT DETECTION LOGIC
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

The bio is NOT generic personality flavor text.
It must transform the following structured data into diegetic backstory:

- Polti Situation(s): {v.situations}
- Role(s) in that situation: {v.roles}
- Explicit named relations:
{v.relations}

CRITICAL REQUIREMENTS:

1. The Polti situation must be clearly implied in the bio (conflict, betrayal, revenge, rivalry, adultery, murder, etc.).
2. The role must be expressed as part of the character's identity (e.g., wronged spouse, secret lover, rival heir, grieving parent, hidden culprit, etc.).
3. Named character from the relations list must be mentioned directly in the bio at least ONCE.
4. Relations must be reinterpreted emotionally (jealousy, grief, pride, obsession, resentment, etc.).
5. The character must speak as if these events are part of their recent personal history.

Do NOT invent new dramatic events.
Only reinterpret what is given.
Do NOT summarize mechanically.
Transform it into personality.

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
