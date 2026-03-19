using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject profilePanel;
    [SerializeField] private GameObject mainLobbyPanel;
    [SerializeField] private GameObject chatManager;

    private AccountManager accountManager;
    private RoomManager roomManager;
    private PreviousRoomsManager previousRoomsManager;

    private void Start()
    {
        accountManager = GetComponent<AccountManager>();
        roomManager = GetComponent<RoomManager>();
        previousRoomsManager = GetComponent<PreviousRoomsManager>();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master Server!");
        PhotonNetwork.JoinLobby();
        loginPanel.SetActive(false);
        profilePanel.SetActive(true);
        mainLobbyPanel.SetActive(true);
        chatManager.SetActive(true);

        accountManager.FetchAccountInfo();
        previousRoomsManager.LoadPreviousRooms();
    }
}
