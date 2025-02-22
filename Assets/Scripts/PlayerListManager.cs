using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using Photon.Pun;
using Photon.Realtime;

public class PlayerListManager : MonoBehaviourPunCallbacks
{
    private const string OnlineGroupId = "StartingRoomPlayers";
    private string _playFabId;
    private string _displayName;
    private bool _isInStartingRoom = false;

    public void Initialize()
    {
        _playFabId = PlayFabAuth.PlayFabId;  // Fetch PlayFab ID from PlayFabAuth
        _displayName = PhotonNetwork.NickName;

        if (string.IsNullOrEmpty(_playFabId))
        {
            Debug.LogError("PlayFab ID not found. Make sure you're logged in.");
            return;
        }

        Debug.Log("PlayerListManager initialized successfully.");
    }

    // 🔹 Called when the MasterClient starts the game
    public void RoomCreated()
    {
        if (!PhotonNetwork.InRoom) return;

        _isInStartingRoom = true;  // Mark this player as part of a "starting" game
        CreateOnlineGroup();
    }

    void OnApplicationQuit()
    {
        RemovePlayerFromOnlineGroup();
    }

    void OnDestroy()
    {
        RemovePlayerFromOnlineGroup();
    }

    // 🔹 Step 1: Create Online Group (if it doesn’t exist)
    private void CreateOnlineGroup()
    {
        PlayFabClientAPI.CreateSharedGroup(new CreateSharedGroupRequest { SharedGroupId = OnlineGroupId },
            result =>
            {
                Debug.Log("StartingRoomPlayers group created.");
                AddPlayerToOnlineGroup(); // ✅ Now we add the player only after the group is created
        },
            error =>
            {
                Debug.Log("StartingRoomPlayers group likely already exists.");
                AddPlayerToOnlineGroup(); // ✅ If group exists, still try adding player
        });
    }

    // 🔹 Step 2: Add Player to Online Group ONLY IF Room is Starting
    private void AddPlayerToOnlineGroup()
    {
        if (!_isInStartingRoom) return;  // Only add if game is starting

        PlayFabClientAPI.AddSharedGroupMembers(new AddSharedGroupMembersRequest
        {
            SharedGroupId = OnlineGroupId,
            PlayFabIds = new List<string> { _playFabId }
        },
        result =>
        {
            Debug.Log($"Player {_playFabId} added to StartingRoomPlayers");
            UpdateSharedGroupData();
        },
        error => Debug.LogError("Failed to add player to StartingRoomPlayers: " + error.GenerateErrorReport()));
    }

    // 🔹 Step 3: Store Player's Name in Shared Data
    private void UpdateSharedGroupData()
    {
        PlayFabClientAPI.UpdateSharedGroupData(new UpdateSharedGroupDataRequest
        {
            SharedGroupId = OnlineGroupId,
            Data = new Dictionary<string, string> { { _playFabId, _displayName } }
        },
        result => Debug.Log("Shared Group Data updated"),
        error => Debug.LogError("Failed to update Shared Group Data: " + error.GenerateErrorReport()));
    }

    // 🔹 Step 4: Fetch Players from the "Starting Room"
    public void GetStartingRoomPlayers()
    {
        PlayFabClientAPI.GetSharedGroupData(new GetSharedGroupDataRequest
        {
            SharedGroupId = OnlineGroupId
        },
        result =>
        {
            Debug.Log("Starting Room Players:");
            foreach (var entry in result.Data)
            {
                Debug.Log($"PlayFabID: {entry.Key}, Name: {entry.Value.Value}");
            }
        },
        error => Debug.LogError("Failed to fetch Starting Room players: " + error.GenerateErrorReport()));
    }

    // 🔹 Step 5: Remove Player from Online Group when the Game Ends
    public void OnGameEnded()
    {
        _isInStartingRoom = false; // Game is over, mark player as out
        RemovePlayerFromOnlineGroup();
    }

    private void RemovePlayerFromOnlineGroup()
    {
        if (string.IsNullOrEmpty(_playFabId) || !_isInStartingRoom) return;

        PlayFabClientAPI.RemoveSharedGroupMembers(new RemoveSharedGroupMembersRequest
        {
            SharedGroupId = OnlineGroupId,
            PlayFabIds = new List<string> { _playFabId }
        },
        result => Debug.Log($"Player {_playFabId} removed from StartingRoomPlayers"),
        error => Debug.LogError("Failed to remove player from StartingRoomPlayers: " + error.GenerateErrorReport()));
    }

    // 🔹 Optional: If a player disconnects, remove them
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (otherPlayer.IsMasterClient) OnGameEnded();
    }
}