using UnityEngine;

public static class PortraitGenerator
{
    public static PortraitData Generate(PortraitDatabase db)
    {
        PortraitData data = new PortraitData();

        data.bodyIndex = Random.Range(0, db.bodyPrefabs.Length);

        data.backgroundIndex = Random.Range(0, db.backgrounds.Length);
        data.backgroundRedness = Random.value;

        data.hasEyes = Random.value > 0.05f;
        data.hasMouth = Random.value > 0.05f;
        data.hasBlood = Random.value > 0.7f;

        if (data.hasEyes)
        {
            data.eyesIndex = Random.Range(0, db.eyes.Length);
            data.eyesRedness = Random.value;
        }

        if (data.hasMouth)
        {
            data.mouthIndex = Random.Range(0, db.mouths.Length);
            data.mouthRedness = Random.value;
        }

        if (data.hasBlood)
        {
            data.bloodIndex = Random.Range(0, db.bloodStains.Length);
        }

        return data;
    }
}
