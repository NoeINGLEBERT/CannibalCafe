using System;

[Serializable]
public class LobbySettings
{
    public int PlayerCount { get; set; }
    public string TownName { get; set; }
    public int Inhabitants { get; set; }
    public bool SecretInvite { get; set; }

    public void Reset()
    {
        PlayerCount = 0;
        TownName = "";
        Inhabitants = 0;
        SecretInvite = false;
    }
}