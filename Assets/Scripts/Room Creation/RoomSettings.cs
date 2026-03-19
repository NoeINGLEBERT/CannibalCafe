using NUnit.Framework;
using System;
using System.Collections.Generic;

[Serializable]
public class RoomSettings
{
    public string TownName { get; set; }
    public int PlayerCount { get; set; }
    public int Population { get; set; }
    public List<VillagerData> Villagers { get; set; }
    public bool SecretInvite { get; set; }

    public void Reset()
    {
        TownName = "";
        PlayerCount = 0;
        Population = 0;
        Villagers = new List<VillagerData>();
        SecretInvite = false;
    }
}