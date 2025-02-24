using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;

public class RoomCardUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text roomNameText;
    public TMP_Text playerCountText;

    private string roomName;
    private RoomManager roomManager;
    private DatabaseReference roomRef;

    /// <summary>
    /// Initializes the room card with data from Firebase.
    /// </summary>
    public void Setup(string room, RoomManager manager)
    {
        roomName = room;
        roomManager = manager;

        // Set the name immediately
        roomNameText.text = roomName;

        // Fetch additional room data from Firebase
        roomRef = FirebaseDatabase.DefaultInstance.GetReference("rooms").Child(roomName);
        FetchRoomData();
    }

    /// <summary>
    /// Fetch room data from Firebase and update UI.
    /// </summary>
    private void FetchRoomData()
    {
        roomRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                DataSnapshot snapshot = task.Result;
                int currentPlayers = (int)snapshot.Child("players").ChildrenCount;  // Get the count of players in the "players" array
                int maxPlayers = int.Parse(snapshot.Child("maxPlayers").Value.ToString());

                playerCountText.text = $"{currentPlayers} / {maxPlayers}";
            }
            else
            {
                Debug.LogError($"❌ Room {roomName} not found or failed to load.");
                playerCountText.text = "N/A";
            }
        });
    }

    /// <summary>
    /// Called when the player clicks the "Join" button.
    /// </summary>
    public void JoinRoom()
    {
        roomManager.JoinRoom(roomName);
    }
}
