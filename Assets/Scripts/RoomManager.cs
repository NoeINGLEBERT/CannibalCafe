using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Extensions;

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


    private DatabaseReference roomsRef;
    private List<string> availableRooms = new List<string>();
    private int currentRoomIndex = 0;

    public void InitializeFirebase(DatabaseReference databaseRef)
    {
        // Get Firebase reference
        roomsRef = databaseRef;

        // Start listening for room updates
        StartListeningForRoomUpdates();
    }

    public void CreateRoom()
    {
        string roomName = "Room_" + Random.Range(1000, 9999);
        FirebaseManager.Instance.CreateRoom(roomName, maxPlayer, PlayFabAuth.PlayFabId);
    }

    public void JoinRoom(string roomName)
    {
        FirebaseManager.Instance.JoinRoom(roomName, PlayFabAuth.PlayFabId);
    }

    private void StartListeningForRoomUpdates()
    {
        roomsRef.ChildChanged += HandleRoomUpdated;
        roomsRef.ChildAdded += HandleRoomUpdated;
        //roomsRef.ChildRemoved += HandleRoomUpdated;

        // Initial fetch
        FetchAvailableRooms();
    }

    private void HandleRoomUpdated(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError($"❌ Firebase error: {args.DatabaseError.Message}");
            return;
        }

        // Refresh the room list whenever there is a change
        FetchAvailableRooms();
    }

    private void FetchAvailableRooms()
    {
        roomsRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                availableRooms.Clear();
                foreach (var room in task.Result.Children)
                {
                    string roomName = room.Key;

                    // Fetch the list of players in the room
                    var playersRef = roomsRef.Child(roomName).Child("players");

                    playersRef.GetValueAsync().ContinueWithOnMainThread(playerTask =>
                    {
                        if (playerTask.IsCompleted)
                        {
                            List<string> playerList = new List<string>();
                            foreach (var player in playerTask.Result.Children)
                            {
                                playerList.Add(player.Key); // Assuming player IDs/names are the keys
                            }

                            int playerCount = playerList.Count; 

                            // If player count is equal to the max player count, trigger the action
                            if (playerCount == maxPlayer)
                            {
                                // Example of calling your method (you can replace it with actual code)
                                Debug.Log($"Room {roomName} has reached max players. Deleting room...");
                                DeleteRoom(roomName, playerList); // Placeholder for your method
                            }
                        }
                        else
                        {
                            Debug.LogError($"❌ Failed to fetch players in room {roomName}: {playerTask.Exception}");
                        }
                    });

                    availableRooms.Add(roomName);
                }

                // Update the room cards
                UpdateRoomCards();
            }
            else
            {
                Debug.LogError($"❌ Failed to fetch rooms: {task.Exception}");
            }
        });
    }

    private void DeleteRoom(string roomName, List<string> players)
    {
        DatabaseReference roomRef = roomsRef.Child(roomName); // Reference to the room node in the database
        roomRef.RemoveValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log($"Room {roomName} successfully deleted from the Firebase database.");
                
                if (availableRooms.IndexOf(roomName) < currentRoomIndex)
                {
                    currentRoomIndex--;
                }
                availableRooms.Remove(roomName);
                
                UpdateRoomCards();
            }
            else
            {
                Debug.LogError($"❌ Failed to delete room {roomName} from Firebase: {task.Exception}");
            }
        });
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
