using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class ActiveRoomData
{
    public RoomSettings settings;

    public Dictionary<string, PlayerState> players;
    public List<VillagerData> villagers;

    public Dictionary<int, int> villagerToHouse; // Implement directly into VillagerData ?

    public Dictionary<string, ConversationLog> conversations;

    public static string GetConversationKey(int a, int b)
    {
        return a < b ? $"{a}_{b}" : $"{b}_{a}";
    }
}

[Serializable]
public class ConversationLog
{
    public List<Message> messages;
}

public class Message
{
    public int senderIndex;
    public string text;
    public long timestamp;
}

[Serializable]
public class PlayerState
{
    public int villagerIndex;

    public Match kill;
}

[Serializable]
public class Match
{
    public int day;
    public int eaterIndex;
    public int eatenIndex;
}
