using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TownSettingsPanel : MonoBehaviour
{
    [Header("UI References")]

    [SerializeField] private TMP_InputField townNameInput;
    [SerializeField] private TMP_Text errorText;

    [SerializeField] private TMP_Text headerText;
    [SerializeField] private string[] subtitleTexts;

    [SerializeField] private Slider inhabitantsSlider;
    [SerializeField] private TMP_Text inhabitantsValueText;
    [SerializeField] private int[] minInhabitantsValues;
    [SerializeField] private int[] maxInhabitantsValues;

    [SerializeField] private Switch secretInviteToggle;

    private void OnEnable()
    {
        SetupFromPlayerCount();
        UpdateSliderText();
        errorText.gameObject.SetActive(false);
    }

    void SetupFromPlayerCount()
    {
        int playerCount = LobbyFlowManager.Instance.Settings.PlayerCount;

        townNameInput.text = "";

        headerText.text = subtitleTexts[playerCount-2];

        inhabitantsSlider.minValue = minInhabitantsValues[playerCount - 2];
        inhabitantsSlider.maxValue = maxInhabitantsValues[playerCount - 2];

        inhabitantsSlider.value = minInhabitantsValues[playerCount - 2];

        secretInviteToggle.SetStateImmediate(false);
    }

    public void OnSliderChanged()
    {
        UpdateSliderText();
    }

    void UpdateSliderText()
    {
        inhabitantsValueText.text = Mathf.RoundToInt(inhabitantsSlider.value).ToString();
    }

    public void ConfirmSettings()
    {
        string townName = townNameInput.text.Trim();

        if (string.IsNullOrEmpty(townName))
        {
            errorText.gameObject.SetActive(true);
            errorText.text = "Town name must be entered.";
            return;
        }

        var settings = LobbyFlowManager.Instance.Settings;

        settings.TownName = townName;
        settings.Inhabitants = Mathf.RoundToInt(inhabitantsSlider.value);
        settings.SecretInvite = secretInviteToggle.IsOn;

        LobbyFlowManager.Instance.OpenCharacterPanel();
    }

    public void Back()
    {
        LobbyFlowManager.Instance.OpenCreationPanel();
    }
}