//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using UnityEngine;

//[System.Serializable]
//public class PoltiSituation
//{
//    public string Id;
//    public string Category;
//    public string Expression;
//}

//[Serializable]
//public class CharacterConstraints
//{
//    public bool IsDead;
//    public int MinAge;
//    public int MaxAge;
//    public Gender[] AllowedGenders;

//    public List<PoltiRole> Roles; // All roles applied to this character
//    public List<PoltiRelation> Relations; // Final concrete relations after generation

//    public CharacterConstraints(bool isDead, int minAge = 18, int maxAge = 81, Gender[] allowedGenders = null)
//    {
//        IsDead = isDead;
//        MinAge = minAge;
//        MaxAge = maxAge;
//        AllowedGenders = allowedGenders ?? new Gender[] { Gender.Male, Gender.Female };
//        Relations = new List<PoltiRelation>();
//    }

//    public void ApplyRoles(List<PoltiRole> roles)
//    {
//        Roles.AddRange(roles);

//        foreach (var role in roles)
//        {
//            // Merge relations
//            foreach (var r in role.Relations)
//            {
//                if (!Relations.Contains(r))
//                    Relations.Add(r);
//            }

//            // Merge age and gender constraints
//            MinAge = Math.Max(MinAge, role.MinAge);
//            MaxAge = Math.Min(MaxAge, role.MaxAge);
//            AllowedGenders = IntersectGenders(AllowedGenders, role.AllowedGenders);

//            // Merge death state
//            if (role.IsDead)
//                IsDead = true;
//        }
//    }

//    private Gender[] IntersectGenders(Gender[] g1, Gender[] g2)
//    {
//        List<Gender> intersection = new List<Gender>();
//        foreach (var gender in g1)
//        {
//            if (Array.Exists(g2, g => g == gender))
//                intersection.Add(gender);
//        }
//        return intersection.ToArray();
//    }

//    public bool IsCompatible(PoltiRole candidate)
//    {
//        if (IsDead || candidate.IsDead) return false;
//        if (candidate.MinAge > MaxAge || candidate.MaxAge < MinAge) return false;

//        bool genderMatch = Array.Exists(AllowedGenders, g => Array.Exists(candidate.AllowedGenders, cg => cg == g));
//        if (!genderMatch) return false;

//        // Check relations
//        foreach (var rel in candidate.Relations)
//        {
//            if (!rel.IsCompatible(Relations))
//                return false;
//        }

//        return true;
//    }
//}

//public class PoltiGenerator : MonoBehaviour
//{
//    private List<PoltiSituation> situations = new List<PoltiSituation>();

//    void Awake()
//    {
//        LoadSituationsFromCSV();
//    }

//    public void LoadSituationsFromCSV()
//    {
//        TextAsset csvFile = Resources.Load<TextAsset>("polti_situations");

//        if (csvFile == null)
//        {
//            Debug.LogError("CSV file not found in Resources!");
//            return;
//        }

//        string[] lines = csvFile.text.Split('\n');

//        for (int i = 1; i < lines.Length; i++) // Skip header
//        {
//            if (string.IsNullOrWhiteSpace(lines[i]))
//                continue;

//            string line = lines[i];

//            // Basic CSV split (safe because expression is quoted)
//            string[] parts = SplitCSVLine(line);

//            if (parts.Length < 3)
//                continue;

//            if (string.IsNullOrWhiteSpace(parts[0]))
//                continue;

//            PoltiSituation situation = new PoltiSituation
//            {
//                Id = parts[0],
//                Category = parts[1],
//                Expression = parts[2]
//            };

//            situations.Add(situation);
//        }

//        Debug.Log($"Loaded {situations.Count} Polti situations.");
//    }

//    string[] SplitCSVLine(string line)
//    {
//        List<string> result = new List<string>();
//        bool insideQuotes = false;
//        string current = "";

//        foreach (char c in line)
//        {
//            if (c == '"')
//            {
//                insideQuotes = !insideQuotes;
//                continue;
//            }

//            if ((c == ',' || c == ';') && !insideQuotes)
//            {
//                result.Add(current);
//                current = "";
//            }
//            else
//            {
//                current += c;
//            }
//        }

//        result.Add(current);
//        return result.ToArray();
//    }

//    public void DebugOutput()
//    {
//        List<PoltiRole> roles = GenerateSituation();

//        string formatted = "=== POLTI SITUATION GENERATED ===\n";

//        for (int i = 0; i < roles.Count; i++)
//        {
//            formatted += $"Role {i + 1}: {roles[i]}\n";
//        }

//        Debug.Log(formatted);
//    }

//    public List<PoltiRole> GenerateSituation()
//    {
//        if (situations.Count == 0)
//            return new List<PoltiRole>();

//        PoltiSituation selected = situations[UnityEngine.Random.Range(0, situations.Count)];
//        return CollapseExpression(selected.Expression);
//    }

//    private List<PoltiRole> CollapseExpression(string expression)
//    {
//        expression = TrimOuterParentheses(expression.Trim());

//        // Split top-level OR
//        List<string> orSplit = SplitTopLevel(expression, "||");

//        if (orSplit.Count > 1)
//        {
//            string chosen = orSplit[UnityEngine.Random.Range(0, orSplit.Count)];
//            return CollapseExpression(chosen);
//        }

//        // Split top-level AND
//        List<string> andSplit = SplitTopLevel(expression, "&&");

//        List<PoltiRole> results = new List<PoltiRole>();

//        foreach (var part in andSplit)
//        {
//            string trimmed = part.Trim();

//            if (trimmed.StartsWith("("))
//            {
//                results.AddRange(CollapseExpression(trimmed));
//            }
//            else
//            {
//                results.Add(ParseRole(trimmed));
//            }
//        }

//        return results;
//    }

//    private PoltiRole ParseRole(string token)
//    {
//        token = token.Trim();

//        bool isDead = false;

//        if (token.StartsWith("!"))
//        {
//            isDead = true;
//            token = token.Substring(1).Trim();
//        }

//        token = TrimOuterParentheses(token).Trim();

//        var role = ScriptableObject.CreateInstance<PoltiRole>();
//        role.Name = token;
//        role.IsDead = isDead;
//        return role;
//    }

//    private List<string> SplitTopLevel(string input, string delimiter)
//    {
//        List<string> result = new List<string>();
//        int depth = 0;
//        int lastIndex = 0;

//        for (int i = 0; i < input.Length; i++)
//        {
//            if (input[i] == '(') depth++;
//            else if (input[i] == ')') depth--;

//            if (depth == 0 && input.Substring(i).StartsWith(delimiter))
//            {
//                result.Add(input.Substring(lastIndex, i - lastIndex));
//                lastIndex = i + delimiter.Length;
//            }
//        }

//        result.Add(input.Substring(lastIndex));
//        return result;
//    }

//    private string TrimOuterParentheses(string input)
//    {
//        while (input.StartsWith("(") && input.EndsWith(")"))
//        {
//            int depth = 0;
//            bool isBalanced = true;

//            for (int i = 0; i < input.Length - 1; i++)
//            {
//                if (input[i] == '(') depth++;
//                if (input[i] == ')') depth--;

//                if (depth == 0 && i < input.Length - 2)
//                {
//                    isBalanced = false;
//                    break;
//                }
//            }

//            if (isBalanced)
//                input = input.Substring(1, input.Length - 2);
//            else
//                break;
//        }

//        return input;
//    }

//    public HashSet<string> ExtractAllRoles()
//    {
//        HashSet<string> roles = new HashSet<string>();

//        foreach (var situation in situations)
//        {
//            if (string.IsNullOrWhiteSpace(situation.Expression))
//                continue;

//            string expr = situation.Expression;

//            string[] tokens = expr
//                .Replace("(", "")
//                .Replace(")", "")
//                .Replace("&&", "|")
//                .Replace("||", "|")
//                .Split('|');

//            foreach (var raw in tokens)
//            {
//                string token = raw.Trim();

//                if (string.IsNullOrWhiteSpace(token))
//                    continue;

//                bool isDead = token.StartsWith("!");

//                if (isDead)
//                    token = token.Substring(1).Trim();

//                if (!string.IsNullOrWhiteSpace(token))
//                {
//                    roles.Add("ALIVE: " + token);
//                    roles.Add("DEAD: " + token);
//                }
//            }
//        }

//        return roles;
//    }
//}
