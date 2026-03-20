using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VillagerFactory : MonoBehaviour
{
    [Header("Pools")]
    public TextAsset maleNamesFile;
    public TextAsset femaleNamesFile;
    public TextAsset surnamesFile;
    public TextAsset occupationsFile;

    public string[] allowedLocations;

    private List<string> maleNames = new();
    private List<string> femaleNames = new();
    private List<string> surnames = new();
    private List<string> occupations = new();

    private HashSet<string> usedFullNames = new();
    private HashSet<string> usedOccupations = new();
    private Dictionary<int, string> familySurnames = new();

    public void Initialize()
    {
        maleNames = LoadLines(maleNamesFile);
        femaleNames = LoadLines(femaleNamesFile);
        surnames = LoadLines(surnamesFile);
        occupations = LoadLines(occupationsFile);
    }

    public List<VillagerData> CreateVillagers(List<CharacterConstraints> constraints)
    {
        usedFullNames.Clear();
        usedOccupations.Clear();
        familySurnames.Clear();

        var familyMap = BuildFamilyMap(constraints);

        List<VillagerData> villagers = new();

        int i = 0;

        foreach (var c in constraints)
        {
            VillagerData v = new();

            v.index = i;
            i++;

            Gender chosenGender =
                c.AllowedGenders[UnityEngine.Random.Range(0, c.AllowedGenders.Length)];

            v.gender = chosenGender.ToString();

            int familyId = familyMap[c.Index];

            if (!familySurnames.ContainsKey(familyId))
                familySurnames[familyId] = GetRandom(surnames);

            string surname = familySurnames[familyId];

            v.name = GetUniqueName(v.gender, surname);
            v.age = UnityEngine.Random.Range(c.MinAge, c.MaxAge + 1);
            v.occupation = GetUniqueOccupation();

            v.location = allowedLocations.Length > 0
                ? allowedLocations[UnityEngine.Random.Range(0, allowedLocations.Length)]
                : "Village";

            villagers.Add(v);
        }

        return villagers;
    }

    private string GetUniqueName(string gender, string surname)
    {
        List<string> pool = gender == "Male" ? maleNames : femaleNames;

        for (int i = 0; i < 50; i++)
        {
            string first = GetRandom(pool);
            string full = $"{first} {surname}";

            if (!usedFullNames.Contains(full))
            {
                usedFullNames.Add(full);
                return full;
            }
        }

        return $"{gender} {surname}";
    }

    private string GetUniqueOccupation()
    {
        for (int i = 0; i < 50; i++)
        {
            string occ = GetRandom(occupations);

            if (!usedOccupations.Contains(occ))
            {
                usedOccupations.Add(occ);
                return occ;
            }
        }

        return GetRandom(occupations);
    }

    private string GetRandom(List<string> list)
    {
        if (list.Count == 0) return "Unknown";
        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    private List<string> LoadLines(TextAsset file)
    {
        if (!file) return new();

        return file.text
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToList();
    }

    private Dictionary<int, int> BuildFamilyMap(List<CharacterConstraints> constraints)
    {
        int count = constraints.Count;

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

        for (int i = 0; i < constraints.Count; i++)
        {
            foreach (var r in constraints[i].Relations)
            {
                if (r.CharacterTarget == null) continue;

                bool isFamily =
                    r.Template is FamilialRelation ||
                    r.Template is MaritalRelation;

                if (isFamily)
                    Union(i, r.CharacterTarget.Index);
            }
        }

        Dictionary<int, int> map = new();
        for (int i = 0; i < count; i++)
            map[i] = Find(i);

        return map;
    }
}