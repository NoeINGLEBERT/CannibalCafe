using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Cannibal Cafe/Village Database")]
public class VillageDatabase : ScriptableObject
{
    public List<VillagerData> villagers = new();
}
