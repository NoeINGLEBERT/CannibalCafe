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
using System;
using System.Linq;
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

        FirebaseMessaging.TokenReceived += OnTokenReceived;
        FirebaseMessaging.MessageReceived += OnMessageReceived;

        RequestNotificationPermission();
    }

    public void OnLogin()
    {
        roomManager.InitializeFirebase(dbReference.Child("rooms"));
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
            Debug.Log("Notification sent successfully: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Notification sending failed: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
        }
    }

    void OnTokenReceived(object sender, TokenReceivedEventArgs token)
    {
        Debug.Log($"FCM Token: {token.Token}");
        // Optional: Send this token to your backend for targeted notifications
    }

    void OnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        Debug.Log("FCM Message received");
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

    public void CheckIfPathExists(string path, Action<bool> callback)
    {
        dbReference.Child(path).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                bool exists = snapshot.Exists;
                callback?.Invoke(exists);
            }
            else
            {
                Debug.LogError($"Failed to check path '{path}': {task.Exception}");
                callback?.Invoke(false);
            }
        });
    }

    public void SetValueAtPath(string path, object data, Action<bool> callback = null)
    {
        string json = JsonConvert.SerializeObject(data);

        dbReference.Child(path).SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log($"Data written at '{path}'");
                callback?.Invoke(true);
            }
            else
            {
                Debug.LogError($"Failed to write at '{path}': {task.Exception}");
                callback?.Invoke(false);
            }
        });
    }

    public void AddToArray(string path, int data)
    {
        dbReference.Child(path).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompleted)
            {
                Debug.LogError("Failed to read data: " + task.Exception);
                return;
            }

            List<int> list = new List<int>();

            DataSnapshot snapshot = task.Result;

            // Rebuild list from Firebase
            if (snapshot.Exists)
            {
                foreach (var child in snapshot.Children)
                {
                    list.Add(Convert.ToInt32(child.Value));
                }
            }

            // Avoid duplicates (optional)
            if (!list.Contains(data))
            {
                list.Add(data);
            }

            // Write full array back
            dbReference.Child(path).SetValueAsync(list);
        });
    }
}

