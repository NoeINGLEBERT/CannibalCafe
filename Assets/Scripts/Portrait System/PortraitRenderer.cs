using UnityEngine;
using UnityEngine.UI;

public class PortraitRenderer : MonoBehaviour
{
    public PortraitDatabase database;

    public Image background;
    private GameObject currentBody;

    public void Render(PortraitData data)
    {
        // background
        background.sprite = database.backgrounds[data.backgroundIndex];
        background.color = Color.Lerp(Color.white, Color.red, data.backgroundRedness);

        // remove previous body
        if (currentBody != null)
            Destroy(currentBody);

        // spawn body prefab
        currentBody = Instantiate(
            database.bodyPrefabs[data.bodyIndex],
            transform
        );

        PortraitBody body = currentBody.GetComponent<PortraitBody>();

        // apply features
        if (data.hasEyes)
        {
            body.eyes.sprite = database.eyes[data.eyesIndex];
            body.eyes.color = Color.Lerp(Color.white, Color.red, data.eyesRedness);
        }
        else body.eyes.gameObject.SetActive(false);

        if (data.hasMouth)
        {
            body.mouth.sprite = database.mouths[data.mouthIndex];
            body.mouth.color = Color.Lerp(Color.white, Color.red, data.mouthRedness);
        }
        else body.mouth.gameObject.SetActive(false);

        if (data.hasBlood)
        {
            body.blood.sprite = database.bloodStains[data.bloodIndex];
        }
        else body.blood.gameObject.SetActive(false);
    }
}