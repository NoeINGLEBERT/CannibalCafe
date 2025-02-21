using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class RoomCardUI : MonoBehaviour
{
    public TMP_Text roomNameText;
    public TMP_Text playerCountText;
    private RoomInfo roomInfo;
    private RoomManager roomManager;

    public void Setup(RoomInfo room, RoomManager manager)
    {
        roomInfo = room;
        roomManager = manager;

        roomNameText.text = room.Name;
        playerCountText.text = $"{room.PlayerCount} / {room.MaxPlayers}";
    }

    public void JoinRoom()
    {
        roomManager.JoinRoom(roomInfo.Name);
    }
}