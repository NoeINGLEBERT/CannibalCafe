using UnityEngine;

[CreateAssetMenu(menuName = "Portrait/Database")]
public class PortraitDatabase : ScriptableObject
{
    public GameObject[] bodyPrefabs;

    public Sprite[] hairMale;
    public Sprite[] hairFemale;

    public Sprite[] backgrounds;
    public Sprite[] eyes;
    public Sprite[] mouths;
    public Sprite[] bloodStains;
}
