using UnityEngine;
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Extensions;
using System;
using Newtonsoft.Json;
public class RoomRepository
{
    private DatabaseReference roomsRef;
    private RoomManager manager;

    public Action<List<AvailableVillager>> OnRoomsUpdated;

    public RoomRepository(DatabaseReference roomsRef, RoomManager manager)
    {
        this.roomsRef = roomsRef;
        this.manager = manager;
    }

    public void StartListening()
    {
        roomsRef.ChildChanged += HandleUpdate;
        roomsRef.ChildAdded += HandleUpdate;
        roomsRef.ChildRemoved += HandleUpdate;

        Fetch();
    }

    private void HandleUpdate(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        Fetch();
    }

    private void Fetch()
    {
        List<AvailableVillager> availableVillagers = new List<AvailableVillager>();

        roomsRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompleted)
            {
                Debug.LogError($"Failed to fetch rooms: {task.Exception}");
                return;
            }

            int? currentVillagerIndex = null;
            if (availableVillagers.Count > 0 && manager.currentRoomIndex < availableVillagers.Count)
            {
                currentVillagerIndex = availableVillagers[manager.currentRoomIndex].villager.index;
            }

            availableVillagers.Clear();

            string playerId = PlayFabAuth.PlayFabId;

            foreach (var room in task.Result.Children)
            {
                string roomName = room.Key;

                int interestsNumber = 0;

                HashSet<int> seenIndexes = new HashSet<int>();

                var playerSnap = room.Child($"players/{playerId}");

                var selectedSnap = playerSnap.Child("selectedCharacters");
                if (selectedSnap.Exists)
                {
                    foreach (var i in selectedSnap.Children)
                    {
                        seenIndexes.Add(Convert.ToInt32(i.Value));
                    }

                    interestsNumber = (int)selectedSnap.ChildrenCount;
                }

                var rejectedSnap = playerSnap.Child("rejectedCharacters");
                if (rejectedSnap.Exists)
                {
                    foreach (var i in rejectedSnap.Children)
                    {
                        seenIndexes.Add(Convert.ToInt32(i.Value));
                    }
                }


                RoomData roomData = JsonConvert.DeserializeObject<RoomData>(room.GetRawJsonValue());

                int readyPlayers = 1;

                var playersSnap = room.Child("players");

                if (playersSnap.Exists)
                {
                    foreach (var player in playersSnap.Children)
                    {
                        var selected = player.Child("selectedCharacters");

                        if (selected.Exists)
                        {
                            int selectedCount = (int)selected.ChildrenCount;

                            if (selectedCount >= roomData.settings.playerCount)
                            {
                                readyPlayers++;
                            }
                        }
                    }
                }

                var villagersSnap = room.Child("villagers");

                if (!villagersSnap.Exists)
                    continue;

                foreach (var villagerSnap in villagersSnap.Children)
                {
                    VillagerData villager =
                        JsonUtility.FromJson<VillagerData>(villagerSnap.GetRawJsonValue());

                    int villagerIndex = villager.index;

                    if (seenIndexes.Contains(villagerIndex))
                        continue;

                    AvailableVillager av = new AvailableVillager
                    {
                        villager = villager,
                        roomData = roomData,
                        interestsNumber = interestsNumber,
                        readyPlayers = readyPlayers
                    };

                    availableVillagers.Add(av);
                }
            }

            if (currentVillagerIndex.HasValue)
            {
                int foundIndex = availableVillagers.FindIndex(v => v.villager.index == currentVillagerIndex.Value);

                if (foundIndex > 0)
                {
                    var currentVillager = availableVillagers[foundIndex];
                    availableVillagers.RemoveAt(foundIndex);
                    availableVillagers.Insert(0, currentVillager);
                }
            }

            manager.currentRoomIndex = 0;

            OnRoomsUpdated?.Invoke(availableVillagers);
        });
    }
}