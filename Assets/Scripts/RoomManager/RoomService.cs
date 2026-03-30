using System.Collections.Generic;

public class RoomService
{
    private LobbyFlowManager lobbyFlowManager;
    private VillagerPanel villagerPanel;

    public RoomService(LobbyFlowManager lobbyFlowManager, VillagerPanel villagerPanel)
    {
        this.lobbyFlowManager = lobbyFlowManager;
        this.villagerPanel = villagerPanel;
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

    public void SelectVillager(RoomData roomData, int villagerIndex, bool tryStartRoom)
    {
        if (tryStartRoom)
        {
            roomData.players[PlayFabAuth.PlayFabId].selectedCharacters.Add(villagerIndex);
            StartRoom(roomData);
        }
        else
        {
            string path = $"rooms/{roomData.settings.townName}/players/{PlayFabAuth.PlayFabId}/selectedCharacters";
            FirebaseManager.Instance.AddToArray(path, villagerIndex);
        }
    }

    public void RejectVillager(string roomName, int villagerIndex)
    {
        string path = $"rooms/{roomName}/players/{PlayFabAuth.PlayFabId}/rejectedCharacters";
        FirebaseManager.Instance.AddToArray(path, villagerIndex);
    }

    private void StartRoom(RoomData roomData)
    {
        string roomName = roomData.settings.townName;

        ActiveRoomData activeRoom = RoomAssignmentService.ConvertToActiveRoom(roomData);

        string activePath = $"activeRooms/{roomName}";
        string waitingPath = $"rooms/{roomName}";

        FirebaseManager.Instance.SetValueAtPath(activePath, activeRoom, (success) =>
        {
            if (success)
                FirebaseManager.Instance.SetValueAtPath(waitingPath, null);
        });
    }
}