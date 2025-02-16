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
    private NetworkManager networkManager;

    public void Setup(RoomInfo room, NetworkManager manager)
    {
        roomInfo = room;
        networkManager = manager;

        roomNameText.text = room.Name;
        playerCountText.text = $"{room.PlayerCount} / {room.MaxPlayers}";
    }

    public void JoinRoom()
    {
        networkManager.JoinRoom(roomInfo.Name);
    }
}