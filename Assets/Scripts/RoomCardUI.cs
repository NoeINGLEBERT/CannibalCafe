using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;

public class RoomCardUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TMP_Text matchbakersText;
    [SerializeField] TMP_Text interestsText;
    [SerializeField] TMP_Text roomNameText;
    [SerializeField] TMP_Text populationText;
    [SerializeField] PortraitRenderer portrait;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text ageText;
    [SerializeField] TMP_Text occupationText;
    [SerializeField] TMP_Text traitsText;
    [SerializeField] TMP_Text bioText;

    [SerializeField] BioPanel bioPanel;

    private RoomManager roomManager;
    private AvailableVillager villagerRef;

    public void Setup(AvailableVillager villager, RoomManager manager)
    {
        roomManager = manager;
        villagerRef = villager;

        VillagerData v = villager.villager;

        roomNameText.text = villager.settings.townName;
        populationText.text = $"{villager.settings.population} inhabitants";
        portrait.Render(PortraitCoder.Decode(v.portraitCode), PortraitRenderMode.Full);
        nameText.text = $"{v.name},";
        ageText.text = $" {v.age}";
        occupationText.text = v.occupation;

        traitsText.text = string.Join(", ", v.personalityTraits);
        bioText.text = v.bio;

        bioPanel.SetStateInstant(false);
    }

    public void Slash()
    {
        roomManager.SelectVillager(villagerRef.settings.townName, villagerRef.villager.index);
    }

    public void Pass()
    {
        roomManager.RejectVillager(villagerRef.settings.townName, villagerRef.villager.index);
    }
}
