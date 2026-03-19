using System;
using System.Collections.Generic;
using UnityEngine;

public class VillagerAIGenerator : MonoBehaviour
{
    public OpenRouterChat aiClient;

    public List<VillagerData> villagers;
    private int currentIndex;

    public Action<List<VillagerData>> OnVillagersGenerationStarted;
    public Action<int> OnVillagerGenerationStarted;
    public Action<int, VillagerData> OnVillagerGenerated;
    public Action<List<VillagerData>> OnGenerationComplete;

    [SerializeField] bool disableAI = true;

    public void GenerateVillagers(List<VillagerData> villagers)
    {
        this.villagers = villagers;

        OnVillagersGenerationStarted?.Invoke(villagers);

        int index = 0;

        void Handler(int i, VillagerData v)
        {
            index++;

            if (index >= this.villagers.Count)
            {
                Debug.Log("[VillagerAIGenerator] All villagers generated.");

                OnVillagerGenerated -= Handler;
                OnGenerationComplete?.Invoke(this.villagers);
                return;
            }

            GenerateVillagerAt(index);
        }

        OnVillagerGenerated += Handler;

        GenerateVillagerAt(index);
    }

    public void GenerateVillagerAt(int index)
    {
        if (index < 0 || index >= villagers.Count)
            return;

        currentIndex = index;
        VillagerData v = villagers[index];

        OnVillagerGenerationStarted?.Invoke(currentIndex);

        if (disableAI)
        {
            Debug.Log("[VillagerAIGenerator] AI Disabled - Using placeholder bio");

            villagers[index].bio = villagers[index].situations;

            OnVillagerGenerated?.Invoke(index, villagers[index]);
            return;
        }

        string prompt = BuildPrompt(v);
        aiClient.SendMessageToAI(prompt, OnSingleVillagerGenerated);
    }

    private string BuildPrompt(VillagerData v)
    {
        string comedicTone = v.Emotionality == TraitState.Low ? "dry" : "dark";

        Dictionary<(TraitState, TraitState), string> SurfaceTone = new()
        {
            { (TraitState.Low, TraitState.Low), "Cold-hearted and critical" },              // SERIAL KILLER
            { (TraitState.High, TraitState.Low), "Forcefully assertive and outgoing" },     // CRAZY YANDERE
            { (TraitState.Low, TraitState.High), "Overly gentle and polite" },              // REFINED PSYCHOPATH
            { (TraitState.High, TraitState.High), "Casual and friendly on the surface" }    // BLACK WIDOW
        };

        Dictionary<(TraitState, TraitState), string> SpeechTone = new()
        {
            { (TraitState.Low, TraitState.Low), "asserting control" },      // CRAZY YANDERE
            { (TraitState.High, TraitState.Low), "assessing" },             // SERIAL KILLER
            { (TraitState.Low, TraitState.High), "flirting" },              // BLACK WIDOW
            { (TraitState.High, TraitState.High), "lecturing elegantly" }   // REFINED PSYCHOPATH
        };

        Dictionary<(TraitState, TraitState), string> HumorTone = new()
        {
            { (TraitState.Low, TraitState.Low), "Use body-part metaphors, skin, flesh, cuts, obsession with touch, butcher outlook, or clinical phrasing" },        // SERIAL KILLER
            { (TraitState.High, TraitState.Low), "Use possessive phrasing, 'you're mine', breaking someone apart, becoming one forever, or domination wording" },   // CRAZY YANDERE
            { (TraitState.Low, TraitState.High), "Use disturbing animal cannibalism fun facts, natural selection metaphors, or the beauty of silence" },            // REFINED PSYCHOPATH
            { (TraitState.High, TraitState.High), "Use food metaphors, hunger, guilty pleasures, professional perks, or unsettling phrasing" }                      // BLACK WIDOW
        };

        string wordingTone = v.Conscientiousness == TraitState.Low ? "personality" : "occupation";

        return $@"
You are generating data for a fictional NPC in a {comedicTone}ly comedic village simulation.

Focus especially on bio:

Write a roleplaying game character backstory in the style of a dating-app bio with very {comedicTone} humor.
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
- The character’s {wordingTone} must strongly influence the wordings

IMPORTANT:
- Keep everything subtle—no explicit violence, gore, or crimes.
- Do not change name, age, gender, occupation, or location.
- Return only the bio text
- Only include name if relevant
- Only include age if relevant
- Only include occupation if relavant
- Avoid direcly including traits, they should influence writing style and not simply be stated in the text

Just return the bio paragraph.
";
    }

    private void OnSingleVillagerGenerated(string response)
    {
        Debug.Log("[VillagerAIGenerator] AI Returned:\n" + response);

        string cleanedBio = CleanBio(response);

        villagers[currentIndex].bio = cleanedBio;

        OnVillagerGenerated?.Invoke(currentIndex, villagers[currentIndex]);
    }

    private string CleanBio(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "";

        string text = input.Trim();

        // Remove markdown code blocks
        text = text.Replace("```", "");

        // Convert **bold**
        while (text.Contains("**"))
        {
            int start = text.IndexOf("**");
            int end = text.IndexOf("**", start + 2);

            if (end == -1)
                break;

            string content = text.Substring(start + 2, end - start - 2);

            text = text.Remove(start, (end + 2) - start)
                       .Insert(start, "<b>" + content + "</b>");
        }

        // Convert *italic*
        while (text.Contains("*"))
        {
            int start = text.IndexOf("*");
            int end = text.IndexOf("*", start + 1);

            if (end == -1)
                break;

            string content = text.Substring(start + 1, end - start - 1);

            text = text.Remove(start, (end + 1) - start)
                       .Insert(start, "<i>" + content + "</i>");
        }

        // Remove quotes
        text = text.Replace("\"", "");

        // Normalize whitespace
        text = text.Replace("\n", " ").Replace("\r", " ");

        return text.Trim();
    }


}