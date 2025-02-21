using Photon.Pun;
using Photon.Realtime;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PreviousRoomsManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private Transform previousRoomsContent;
    [SerializeField] private GameObject previousRoomItemPrefab;
    [SerializeField] private int maxPlayer = 4; // used for recreating rooms

    public List<string> previousRoomsList = new List<string>();
    private string currentRoomName;

    public void LoadPreviousRooms()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), OnUserDataSuccess, OnUserDataFailure);
    }

    private void OnUserDataSuccess(GetUserDataResult result)
    {
        if (result.Data != null && result.Data.ContainsKey("PreviousRooms"))
        {
            previousRoomsList = new List<string>(result.Data["PreviousRooms"].Value.Split(','));
            UpdatePreviousRoomsUI();
        }
    }

    private void OnUserDataFailure(PlayFabError error)
    {
        Debug.LogError("Failed to fetch previous rooms: " + error.ErrorMessage);
    }

    private void UpdatePreviousRoomsUI()
    {
        foreach (Transform child in previousRoomsContent)
            Destroy(child.gameObject);

        foreach (string roomName in previousRoomsList)
        {
            GameObject roomItem = Instantiate(previousRoomItemPrefab, previousRoomsContent);
            TMP_Text roomText = roomItem.GetComponentInChildren<TMP_Text>();
            if (roomText != null)
                roomText.text = roomName;

            Button joinButton = roomItem.GetComponent<Button>();
            if (joinButton != null)
                joinButton.onClick.AddListener(() => TryRejoinRoom(roomName));
        }
    }

    private void TryRejoinRoom(string roomName)
    {
        currentRoomName = roomName;
        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log($"Failed to join room: {message} for room {currentRoomName}");
        if (message.Contains("Game does not exist"))
        {
            Debug.Log("Room doesn't exist, recreating room.");
            RecreateRoom(currentRoomName);
        }
        else
        {
            Debug.Log("Join room failed due to a different reason, not recreating.");
        }
    }

    public void RecreateRoom(string roomName)
    {
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayer,
            CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { "GameStarted", true } },
            CustomRoomPropertiesForLobby = new string[] { "GameStarted" }
        };
        PhotonNetwork.CreateRoom(roomName, options);
    }

    public void SavePreviousRooms()
    {
        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { "PreviousRooms", string.Join(",", previousRoomsList) } }
        },
        result => Debug.Log("Previous rooms saved."),
        error => Debug.LogError("Failed to save previous rooms: " + error.ErrorMessage));
    }

    public void RemoveRoomFromHistory(string roomName)
    {
        if (previousRoomsList.Contains(roomName))
        {
            previousRoomsList.Remove(roomName);
            SavePreviousRooms();
        }
    }

    public void AddRoomToHistory(string roomName)
    {
        if (!previousRoomsList.Contains(roomName))
        {
            previousRoomsList.Add(roomName);
            SavePreviousRooms();
        }
    }

    public bool RoomExists(string roomName)
    {
        return previousRoomsList.Contains(roomName);
    }
}
