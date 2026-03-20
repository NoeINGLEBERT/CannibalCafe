using NUnit.Framework;
using System;
using System.Collections.Generic;

// RoomData structure
[Serializable]
public class RoomData
{
    public RoomSettings settings;
    public Dictionary<string, PlayerRoomData> players;
    public List<VillagerData> villagers;

    public void Reset()
    {
        settings = new RoomSettings();
        players = new Dictionary<string, PlayerRoomData>();
        villagers = new List<VillagerData>();
    }
}

[Serializable]
public class RoomSettings
{
    public string townName;
    public int playerCount;
    public int population;
    public bool secretInvite;
}

[Serializable]
public class PlayerRoomData
{
    public List<int> selectedCharacters = new();
    public List<int> rejectedCharacters = new();
}