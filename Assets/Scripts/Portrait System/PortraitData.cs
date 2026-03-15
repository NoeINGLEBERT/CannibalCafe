using UnityEngine;

[System.Serializable]
public struct PortraitData
{
    public int bodyIndex;

    public int backgroundIndex;
    public float backgroundRedness;

    public bool hasEyes;
    public int eyesIndex;
    public float eyesRedness;

    public bool hasMouth;
    public int mouthIndex;
    public float mouthRedness;

    public bool hasBlood;
    public int bloodIndex;
}

public static class PortraitCoder
{
    public static ulong Encode(PortraitData data)
    {
        ulong value = 0;
        int shift = 0;

        value |= ((ulong)data.bodyIndex) << shift;
        shift += 6;

        value |= ((ulong)data.backgroundIndex) << shift;
        shift += 6;

        value |= ((ulong)(data.backgroundRedness * 255)) << shift;
        shift += 8;

        value |= (data.hasEyes ? 1UL : 0UL) << shift;
        shift += 1;

        value |= ((ulong)data.eyesIndex) << shift;
        shift += 6;

        value |= ((ulong)(data.eyesRedness * 255)) << shift;
        shift += 8;

        value |= (data.hasMouth ? 1UL : 0UL) << shift;
        shift += 1;

        value |= ((ulong)data.mouthIndex) << shift;
        shift += 6;

        value |= ((ulong)(data.mouthRedness * 255)) << shift;
        shift += 8;

        value |= (data.hasBlood ? 1UL : 0UL) << shift;
        shift += 1;

        value |= ((ulong)data.bloodIndex) << shift;

        return value;
    }

    public static PortraitData Decode(ulong value)
    {
        PortraitData data = new PortraitData();
        int shift = 0;

        data.bodyIndex = (int)((value >> shift) & 0b111111);
        shift += 6;

        data.backgroundIndex = (int)((value >> shift) & 0b111111);
        shift += 6;

        data.backgroundRedness = ((value >> shift) & 0xFF) / 255f;
        shift += 8;

        data.hasEyes = ((value >> shift) & 1) == 1;
        shift += 1;

        data.eyesIndex = (int)((value >> shift) & 0b111111);
        shift += 6;

        data.eyesRedness = ((value >> shift) & 0xFF) / 255f;
        shift += 8;

        data.hasMouth = ((value >> shift) & 1) == 1;
        shift += 1;

        data.mouthIndex = (int)((value >> shift) & 0b111111);
        shift += 6;

        data.mouthRedness = ((value >> shift) & 0xFF) / 255f;
        shift += 8;

        data.hasBlood = ((value >> shift) & 1) == 1;
        shift += 1;

        data.bloodIndex = (int)((value >> shift) & 0b111111);

        return data;
    }
}