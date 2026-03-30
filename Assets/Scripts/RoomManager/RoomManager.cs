using Firebase.Database;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviourPunCallbacks
{
    [Header("Scene UI References")]
    [SerializeField] private GameObject lobbyManager;
    [SerializeField] private GameObject mainLobbyPanel;
    [SerializeField] private GameObject playerListUI;

    [Header("External Managers")]
    [SerializeField] private PreviousRoomsManager previousRoomsManager;
    [SerializeField] private LobbyFlowManager lobbyFlowManager;
    [SerializeField] private VillagerPanel villagerPanel;

    private DatabaseReference roomsRef;

    private RoomService roomService;
    private RoomRepository roomRepository;

    [SerializeField] private RoomUIController uiController;

    private List<AvailableVillager> availableVillagers = new();
    public int currentRoomIndex = 0;

    public void InitializeFirebase(DatabaseReference databaseRef)
    {
        roomsRef = databaseRef;

        roomService = new RoomService(lobbyFlowManager, villagerPanel);
        roomRepository = new RoomRepository(roomsRef, this);

        uiController.Init(this);

        roomRepository.OnRoomsUpdated += OnRoomsUpdated;
        roomRepository.StartListening();
    }

    private void OnRoomsUpdated(List<AvailableVillager> villagers)
    {
        availableVillagers = villagers;
        currentRoomIndex = 0;
        uiController.UpdateRoomCards(availableVillagers, currentRoomIndex);
    }

    public void CreateRoom() => roomService.CreateRoom();

    public void SelectVillager(RoomData roomData, int index, bool tryStartRoom = false)
        => roomService.SelectVillager(roomData, index, tryStartRoom);

    public void RejectVillager(string roomName, int index)
        => roomService.RejectVillager(roomName, index);

    public void AdvanceToNextRoom()
    {
        if (currentRoomIndex < availableVillagers.Count - 1)
        {
            currentRoomIndex++;
            uiController.UpdateRoomCards(availableVillagers, currentRoomIndex);
        }
        else
        {
            uiController.HideRoomCards();
        }
    }

    public void RefreshRoomCards()
    {
        currentRoomIndex = 0;
        uiController.RefreshFilter();
        uiController.UpdateRoomCards(availableVillagers, currentRoomIndex);
    }

    // 🔥 Photon logic untouched
    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room successfully!");

        var playerProperties = new ExitGames.Client.Photon.Hashtable
        {
            { "AvatarUrl", PlayFabAuth.AvatarUrl },
            { "Username", PhotonNetwork.NickName }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("GameStarted", out object gameStartedObj))
        {
            bool gameStarted = (bool)gameStartedObj;
            if (gameStarted)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    var roomProperties = new ExitGames.Client.Photon.Hashtable { { "GameStarted", true } };
                    PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);

                    if (!previousRoomsManager.RoomExists(PhotonNetwork.CurrentRoom.Name))
                    {
                        previousRoomsManager.AddRoomToHistory(PhotonNetwork.CurrentRoom.Name);
                    }
                }

                PhotonNetwork.Instantiate("PlayerPrefab", Vector3.zero, Quaternion.identity);
            }
            else
            {
                lobbyManager.SetActive(true);
            }
        }
        else
        {
            Debug.LogWarning("GameStarted property not found.");
            PhotonNetwork.LoadLevel("RoomLobby");
        }

        mainLobbyPanel.SetActive(false);

        if (playerListUI != null)
            playerListUI.GetComponent<PlayerListUI>().enabled = true;

        ChatManager chatManager = FindFirstObjectByType<ChatManager>();
        chatManager?.OnJoinedRoom();
    }
}

public class AvailableVillager
{
    public int readyPlayers;
    public int interestsNumber;
    public VillagerData villager;
    public RoomData roomData;
}