using UnityEngine;

public class LobbyFlowManager : MonoBehaviour
{
    public static LobbyFlowManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject step1Panel;
    [SerializeField] private GameObject step2Panel;
    [SerializeField] private GameObject step3Panel;

    public LobbySettings Settings { get; private set; } = new LobbySettings();

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
        OpenStep1();
    }

    public void OpenStep1()
    {
        step1Panel.SetActive(true);
        step2Panel.SetActive(false);
        step3Panel.SetActive(false);
    }

    public void OpenStep2(int playerCount)
    {
        Settings.PlayerCount = playerCount;

        step1Panel.SetActive(false);
        step2Panel.SetActive(true);
        step3Panel.SetActive(false);
    }

    public void OpenStep3()
    {
        step1Panel.SetActive(false);
        step2Panel.SetActive(false);
        step3Panel.SetActive(true);
    }
}