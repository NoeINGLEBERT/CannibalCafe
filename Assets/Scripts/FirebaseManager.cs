using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.Messaging;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Newtonsoft.Json;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

public class FirebaseManager : MonoBehaviour
{
    private DatabaseReference dbReference;
    public static FirebaseManager Instance;

    [SerializeField] private RoomManager roomManager;

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
        var options = new AppOptions
        {
            ProjectId = "cannibalcafe", // project_id from google-services.json
            AppId = "1:347799429101:android:cfe1b8338e5ea8cf1851e4",  // mobilesdk_app_id from google-services.json
            ApiKey = "AIzaSyAAq9Yrlnb2mpgye7HQG54UkU117wpaJPk", // current_key from google-services.json
            DatabaseUrl = new System.Uri("https://cannibalcafe-default-rtdb.europe-west1.firebasedatabase.app"), // firebase_url from google-services.json
            StorageBucket = "cannibalcafe.firebasestorage.app", // storage_bucket from google-services.json
            MessageSenderId = "347799429101"  // project_number from google-services.json
        };

        FirebaseApp app = FirebaseApp.Create(options);

        dbReference = FirebaseDatabase.GetInstance(app).RootReference;

        roomManager.InitializeFirebase(FirebaseDatabase.GetInstance(app).GetReference("rooms"));

        FirebaseMessaging.TokenReceived += OnTokenReceived;
        FirebaseMessaging.MessageReceived += OnMessageReceived;

        RequestNotificationPermission();
    }

    void RequestNotificationPermission()
    {
    #if UNITY_ANDROID && !UNITY_EDITOR
    const string ANDROID_NOTIFICATION_PERMISSION = "android.permission.POST_NOTIFICATIONS";

    if (!Permission.HasUserAuthorizedPermission(ANDROID_NOTIFICATION_PERMISSION))
    {
        Permission.RequestUserPermission(ANDROID_NOTIFICATION_PERMISSION);
    }
    #endif
    }

    public static void Subscribe(string playFabId)
    {
        FirebaseMessaging.SubscribeAsync("cannibalcafe");
    }

    public void SendNotification(string playFabId)
    {
        StartCoroutine(SendingNotification(playFabId, "TEST", "Hello world!"));
    }


    private IEnumerator SendingNotification(string playFabId, string title, string body)
    {
        string functionUrl = "https://us-central1-cannibalcafe.cloudfunctions.net/sendNotification";

        var data = new
        {
            recipient = "cannibalcafe",
            title = title,
            body = body
        };

        string jsonPayload = JsonConvert.SerializeObject(data); // using Newtonsoft here!
        Debug.Log("JSON Payload: " + jsonPayload);

        UnityWebRequest request = new UnityWebRequest(functionUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Notification sent successfully: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("❌ Notification sending failed: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
        }
    }

    void OnTokenReceived(object sender, TokenReceivedEventArgs token)
    {
        Debug.Log($"📲 FCM Token: {token.Token}");
        // Optional: Send this token to your backend for targeted notifications
    }

    void OnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        Debug.Log("📩 FCM Message received");
        if (e.Message.Notification != null)
        {
            Debug.Log($"Title: {e.Message.Notification.Title}");
            Debug.Log($"Body: {e.Message.Notification.Body}");
        }
        else
        {
            Debug.Log("Received a data message");
        }
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
