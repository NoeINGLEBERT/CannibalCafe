using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;

public class FirebaseManager : MonoBehaviour
{
    private DatabaseReference dbReference;
    public static FirebaseManager Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                FirebaseApp app = FirebaseApp.DefaultInstance;
                dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                Debug.Log("🔥 Firebase is ready!");
            }
            else
            {
                Debug.LogError($"❌ Firebase setup failed: {task.Result}");
            }
        });
    }

    // Method to create a room in Firebase
    public void CreateRoom(string roomName, int maxPlayers, string masterPlayFabID)
    {
        string roomPath = "rooms/" + roomName;

        RoomData newRoom = new RoomData
        {
            roomName = roomName,
            maxPlayers = maxPlayers,
            players = new List<string> { masterPlayFabID }
        };

        string json = JsonUtility.ToJson(newRoom);
        dbReference.Child(roomPath).SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log($"✅ Room '{roomName}' created successfully.");
            }
            else
            {
                Debug.LogError($"❌ Failed to create room '{roomName}': {task.Exception}");
            }
        });
    }

    // Method to join a room
    public void JoinRoom(string roomName, string playerPlayFabID)
    {
        string roomPlayersPath = $"rooms/{roomName}/players";

        dbReference.Child(roomPlayersPath).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                List<string> players = new List<string>();

                if (snapshot.Exists)
                {
                    foreach (var child in snapshot.Children)
                    {
                        players.Add(child.Value.ToString());
                    }
                }

                if (!players.Contains(playerPlayFabID))
                {
                    players.Add(playerPlayFabID);
                    dbReference.Child(roomPlayersPath).SetValueAsync(players).ContinueWithOnMainThread(updateTask =>
                    {
                        if (updateTask.IsCompleted)
                        {
                            Debug.Log($"✅ Player '{playerPlayFabID}' joined room '{roomName}'.");
                        }
                        else
                        {
                            Debug.LogError($"❌ Failed to join room '{roomName}': {updateTask.Exception}");
                        }
                    });
                }
                else
                {
                    Debug.LogWarning($"⚠️ Player '{playerPlayFabID}' is already in room '{roomName}'.");
                }
            }
            else
            {
                Debug.LogError($"❌ Failed to retrieve room '{roomName}': {task.Exception}");
            }
        });
    }
}

// RoomData structure
[System.Serializable]
public class RoomData
{
    public string roomName;
    public int maxPlayers;
    public List<string> players;
}
