using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Extensions;
using System;

public class RoomManager : MonoBehaviourPunCallbacks
{
    [Header("Room UI References")]
    [SerializeField] private GameObject frontRoomCard;
    [SerializeField] private GameObject backRoomCard;

    [Header("Scene UI References")]
    [SerializeField] private GameObject lobbyManager;
    [SerializeField] private GameObject mainLobbyPanel;
    [SerializeField] private GameObject playerListUI;

    [Header("External Managers")]
    [SerializeField] private PreviousRoomsManager previousRoomsManager;
    [SerializeField] private LobbyFlowManager lobbyFlowManager;
    [SerializeField] private VillagerPanel villagerPanel;


    private DatabaseReference roomsRef;
    private List<AvailableVillager> availableVillagers = new List<AvailableVillager>();
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
        PlayerRoomData hostData = new PlayerRoomData();

        string playerId = PlayFabAuth.PlayFabId;

        int selectedIndex = villagerPanel.GetCurrentIndex();
        hostData.selectedCharacters = new List<int> { selectedIndex };

        List<int> rejected = new List<int>();
        int totalVillagers = lobbyFlowManager.roomData.settings.population;

        for (int i = 0; i < totalVillagers; i++)
            if (i != selectedIndex)
                rejected.Add(i);

        hostData.rejectedCharacters = rejected;

        if (lobbyFlowManager.roomData.players == null)
            lobbyFlowManager.roomData.players = new Dictionary<string, PlayerRoomData>();

        lobbyFlowManager.roomData.players[playerId] = hostData;

        string path = $"rooms/{lobbyFlowManager.roomData.settings.townName}";

        FirebaseManager.Instance.SetValueAtPath(path, lobbyFlowManager.roomData);
    }

    public void SelectVillager(string roomName, int villagerIndex)
    {
        string path = $"rooms/{roomName}/players/{PlayFabAuth.PlayFabId}/selectedCharacters";

        FirebaseManager.Instance.AddToArray(path, villagerIndex);
    }

    public void RejectVillager(string roomName, int villagerIndex)
    {
        string path = $"rooms/{roomName}/players/{PlayFabAuth.PlayFabId}/rejectedCharacters";

        FirebaseManager.Instance.AddToArray(path, villagerIndex);
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
            Debug.LogError($"Firebase error: {args.DatabaseError.Message}");
            return;
        }

        FetchAvailableRooms();
    }

    private void FetchAvailableRooms()
    {
        roomsRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompleted)
            {
                Debug.LogError($"Failed to fetch rooms: {task.Exception}");
                return;
            }

            int? currentVillagerIndex = null;
            if (availableVillagers.Count > 0 && currentRoomIndex < availableVillagers.Count)
            {
                currentVillagerIndex = availableVillagers[currentRoomIndex].villager.index;
            }

            availableVillagers.Clear();

            availableVillagers.Clear();

            string playerId = PlayFabAuth.PlayFabId;

            foreach (var room in task.Result.Children)
            {
                string roomName = room.Key;

                HashSet<int> seenIndexes = new HashSet<int>();

                var playerSnap = room.Child($"players/{playerId}");

                var selectedSnap = playerSnap.Child("selectedCharacters");
                if (selectedSnap.Exists)
                {
                    foreach (var i in selectedSnap.Children)
                    {
                        seenIndexes.Add(Convert.ToInt32(i.Value));
                    }

                }

                var rejectedSnap = playerSnap.Child("rejectedCharacters");
                if (rejectedSnap.Exists)
                {
                    foreach (var i in rejectedSnap.Children)
                    {
                        seenIndexes.Add(Convert.ToInt32(i.Value));
                    }
                }

                RoomSettings settings = new RoomSettings();
                var settingsSnap = room.Child("settings");

                if (settingsSnap.Exists)
                {
                    settings.townName = settingsSnap.Child("townName").Value?.ToString();

                    if (settingsSnap.Child("playerCount").Value != null)
                        int.TryParse(settingsSnap.Child("playerCount").Value.ToString(), out settings.playerCount);

                    if (settingsSnap.Child("population").Value != null)
                        int.TryParse(settingsSnap.Child("population").Value.ToString(), out settings.population);

                    if (settingsSnap.Child("secretInvite").Value != null)
                        bool.TryParse(settingsSnap.Child("secretInvite").Value.ToString(), out settings.secretInvite);
                }

                if (settings.secretInvite)
                    continue;

                var villagersSnap = room.Child("villagers");

                if (!villagersSnap.Exists)
                    continue;

                foreach (var villagerSnap in villagersSnap.Children)
                {
                    VillagerData villager =
                        JsonUtility.FromJson<VillagerData>(villagerSnap.GetRawJsonValue());

                    int villagerIndex = villager.index;

                    if (seenIndexes.Contains(villagerIndex))
                        continue;

                    AvailableVillager av = new AvailableVillager
                    {
                        villager = villager,
                        settings = settings
                    };

                    availableVillagers.Add(av);
                }
            }

            if (currentVillagerIndex.HasValue)
            {
                int foundIndex = availableVillagers.FindIndex(v => v.villager.index == currentVillagerIndex.Value);

                if (foundIndex > 0)
                {
                    var currentVillager = availableVillagers[foundIndex];
                    availableVillagers.RemoveAt(foundIndex);
                    availableVillagers.Insert(0, currentVillager);
                }
            }

            currentRoomIndex = 0;

            UpdateRoomCards();
        });
    }

    //private void DeleteRoom(string roomName, List<string> players)
    //{
    //    DatabaseReference roomRef = roomsRef.Child(roomName); // Reference to the room node in the database
    //    roomRef.RemoveValueAsync().ContinueWithOnMainThread(task =>
    //    {
    //        if (task.IsCompleted)
    //        {
    //            Debug.Log($"Room {roomName} successfully deleted from the Firebase database.");
                
    //            if (availableVillagers.IndexOf(roomName) < currentRoomIndex)
    //            {
    //                currentRoomIndex--;
    //            }
    //            availableVillagers.Remove(roomName);
                
    //            UpdateRoomCards();
    //        }
    //        else
    //        {
    //            Debug.LogError($"Failed to delete room {roomName} from Firebase: {task.Exception}");
    //        }
    //    });
    //}

    private void UpdateRoomCards()
    {
        if (availableVillagers.Count == 0)
        {
            HideRoomCards();
            return;
        }

        // Assign the first two rooms
        if (currentRoomIndex < availableVillagers.Count)
        {
            frontRoomCard.SetActive(true);
            frontRoomCard.GetComponent<RoomCardUI>().Setup(availableVillagers[currentRoomIndex], this);
        }
        else
        {
            frontRoomCard.SetActive(false);
        }

        if (currentRoomIndex + 1 < availableVillagers.Count)
        {
            backRoomCard.SetActive(true);
            backRoomCard.GetComponent<RoomCardUI>().Setup(availableVillagers[currentRoomIndex + 1], this);
        }
        else
        {
            backRoomCard.SetActive(false);
        }
    }

    public void AdvanceToNextRoom()
    {
        if (currentRoomIndex < availableVillagers.Count - 1)
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

public class AvailableVillager
{
    public VillagerData villager;
    public RoomSettings settings;
}