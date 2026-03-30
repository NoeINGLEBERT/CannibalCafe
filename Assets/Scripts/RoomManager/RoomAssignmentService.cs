using UnityEngine;
using System.Collections.Generic;


public static class RoomAssignmentService
{
    public static ActiveRoomData ConvertToActiveRoom(RoomData roomData)
    {
        ActiveRoomData active = new ActiveRoomData();

        active.settings = roomData.settings;
        active.players = new Dictionary<string, PlayerState>();
        active.villagers = roomData.villagers;

        active.villagerToHouse = new Dictionary<int, int>();
        active.conversations = new Dictionary<string, ConversationLog>();

        Dictionary<string, int> assignments = AssignUniqueCharacters(roomData.players);

        foreach (var kvp in assignments)
        {
            active.players[kvp.Key] = new PlayerState
            {
                villagerIndex = kvp.Value,
                kill = null
            };
        }

        return active;
    }

    public static Dictionary<string, int> AssignUniqueCharacters(Dictionary<string, PlayerRoomData> players)
    {
        var rng = new System.Random();
        List<string> playerIds = new(players.Keys);

        for (int attempt = 0; attempt < 50; attempt++)
        {
            var used = new HashSet<int>();
            var result = new Dictionary<string, int>();

            Shuffle(playerIds, rng);

            bool success = true;

            foreach (var playerId in playerIds)
            {
                var choices = players[playerId].selectedCharacters;
                List<int> available = choices.FindAll(c => !used.Contains(c));

                if (available.Count == 0)
                {
                    success = false;
                    break;
                }

                int chosen = available[rng.Next(available.Count)];
                result[playerId] = chosen;
                used.Add(chosen);
            }

            if (success)
                return result;
        }

        Debug.LogError("Failed to assign unique characters.");
        return new Dictionary<string, int>();
    }

    private static void Shuffle<T>(List<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}