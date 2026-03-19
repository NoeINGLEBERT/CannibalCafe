using UnityEngine;

public class LobbyFlowManager : MonoBehaviour
{
    public static LobbyFlowManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject CreationPanel;
    [SerializeField] private GameObject SettingsPanel;
    [SerializeField] private GameObject CharacterPanel;

    public RoomSettings Settings { get; private set; } = new RoomSettings();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        OpenCreationPanel();
    }

    public void OpenCreationPanel()
    {
        CreationPanel.SetActive(true);
        SettingsPanel.SetActive(false);
        CharacterPanel.SetActive(false);
    }

    public void OpenSettingsPanel(int playerCount)
    {
        Settings.PlayerCount = playerCount;

        CreationPanel.SetActive(false);
        SettingsPanel.SetActive(true);
        CharacterPanel.SetActive(false);
    }

    public void OpenCharacterPanel()
    {
        CreationPanel.SetActive(false);
        SettingsPanel.SetActive(false);
        CharacterPanel.SetActive(true);
    }
}