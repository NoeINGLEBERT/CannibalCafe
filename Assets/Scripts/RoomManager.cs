using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections.Generic;

public class RoomManager : MonoBehaviourPunCallbacks
{
    [Header("Room UI References")]
    [SerializeField] private GameObject frontRoomCard;
    [SerializeField] private GameObject backRoomCard;

    [Header("Gameplay Settings")]
    [SerializeField] private int maxPlayer = 4;

    [Header("Scene UI References")]
    [SerializeField] private GameObject lobbyManager;
    [SerializeField] private GameObject mainLobbyPanel;
    [SerializeField] private GameObject playerListUI;

    [Header("External Managers")]
    [SerializeField] private PreviousRoomsManager previousRoomsManager; 

    private int currentRoomIndex = 0;
    private List<RoomInfo> availableRooms = new List<RoomInfo>();

    public void CreateRoom()
    {
        string roomName = "Room_" + Random.Range(1000, 9999);
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayer,
            CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { "GameStarted", false } },
            CustomRoomPropertiesForLobby = new string[] { "GameStarted" }
        };

        PhotonNetwork.CreateRoom(roomName, options);
    }

    public void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        availableRooms.Clear();

        foreach (RoomInfo room in roomList)
        {
            if (!room.RemovedFromList && room.CustomProperties.TryGetValue("GameStarted", out object started) && !(bool)started)
            {
                availableRooms.Add(room);
            }
        }

        if (availableRooms.Count > 0)
        {
            currentRoomIndex = 0;
            UpdateRoomCards();
        }
        else
        {
            HideRoomCards();
        }
    }

    private void UpdateRoomCards()
    {
        if (availableRooms.Count == 0)
        {
            HideRoomCards();
            return;
        }

        // Assign the first two rooms
        if (currentRoomIndex < availableRooms.Count)
        {
            frontRoomCard.SetActive(true);
            frontRoomCard.GetComponent<RoomCardUI>().Setup(availableRooms[currentRoomIndex], this);
        }
        else
        {
            frontRoomCard.SetActive(false);
        }

        if (currentRoomIndex + 1 < availableRooms.Count)
        {
            backRoomCard.SetActive(true);
            backRoomCard.GetComponent<RoomCardUI>().Setup(availableRooms[currentRoomIndex + 1], this);
        }
        else
        {
            backRoomCard.SetActive(false);
        }
    }

    public void AdvanceToNextRoom()
    {
        if (currentRoomIndex < availableRooms.Count - 1)
        {
            currentRoomIndex++;
            UpdateRoomCards();
        }
        else
        {
            HideRoomCards();
        }
    }

    private void HideRoomCards()
    {
        frontRoomCard.SetActive(false);
        backRoomCard.SetActive(false);
    }

    public void RefreshRoomCards()
    {
        currentRoomIndex = 0;
        UpdateRoomCards();
    }

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
            Debug.LogWarning("GameStarted property not found. Defaulting to 'RoomLobby' scene...");
            PhotonNetwork.LoadLevel("RoomLobby");
        }

        mainLobbyPanel.SetActive(false);

        if (playerListUI != null)
        {
            playerListUI.GetComponent<PlayerListUI>().enabled = true;
        }
        else
        {
            Debug.LogWarning("PlayerListUI reference not assigned.");
        }

        ChatManager chatManager = FindFirstObjectByType<ChatManager>();
        if (chatManager != null)
        {
            chatManager.OnJoinedRoom();
        }
        else
        {
            Debug.LogWarning("ChatManager not found in scene.");
        }
    }
}
