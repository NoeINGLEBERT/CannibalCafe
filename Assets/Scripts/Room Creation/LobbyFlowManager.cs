using UnityEngine;

public class LobbyFlowManager : MonoBehaviour
{
    public static LobbyFlowManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject creationPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject characterPanel;

    public RoomData roomData { get; private set; } = new RoomData();

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
        creationPanel.SetActive(true);
        settingsPanel.SetActive(false);
        characterPanel.SetActive(false);
    }

    public void OpenSettingsPanel(int playerCount)
    {
        roomData.Reset();
        roomData.settings.playerCount = playerCount;

        creationPanel.SetActive(false);
        settingsPanel.SetActive(true);
        characterPanel.SetActive(false);
    }

    public void OpenCharacterPanel()
    {
        creationPanel.SetActive(false);
        settingsPanel.SetActive(false);
        characterPanel.SetActive(true);
    }
}