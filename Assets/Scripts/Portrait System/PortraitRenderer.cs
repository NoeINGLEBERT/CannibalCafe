using UnityEngine;
using UnityEngine.UI;

public enum PortraitRenderMode { Full, Partial, Default }

public class PortraitRenderer : MonoBehaviour
{
    public PortraitDatabase database;

    public Image background;
    private GameObject currentBody;

    [SerializeField] GameObject defaultBody;
    [SerializeField] Sprite defaultBackground;
    [SerializeField] Color defaultBackgroundColor;

    public void Render(PortraitData data, PortraitRenderMode mode)
    {
        if (mode == PortraitRenderMode.Default)
        {
            RenderDefault();
            return;
        }

        if (mode == PortraitRenderMode.Partial)
        {
            RenderPartial(data);
        }
        else
        {
            RenderFull(data);
        }
    }

    public void RenderFull(PortraitData data)
    {
        background.sprite = database.backgrounds[data.backgroundIndex];
        background.color = Color.Lerp(Color.white, Color.red, data.backgroundRedness);

        SpawnBody(data);

        PortraitBody body = currentBody.GetComponent<PortraitBody>();

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

    void RenderPartial(PortraitData data)
    {
        background.sprite = defaultBackground;
        background.color = Color.red;

        SpawnBody(data);

        PortraitBody body = currentBody.GetComponent<PortraitBody>();

        body.eyes.gameObject.SetActive(false);
        body.mouth.gameObject.SetActive(false);
        body.blood.gameObject.SetActive(false);
    }

    void RenderDefault()
    {
        background.sprite = defaultBackground;
        background.color = defaultBackgroundColor;

        if (currentBody != null)
            Destroy(currentBody);

        currentBody = Instantiate(defaultBody, transform);

        PortraitBody body = currentBody.GetComponent<PortraitBody>();

        body.eyes.gameObject.SetActive(false);
        body.mouth.gameObject.SetActive(false);
        body.blood.gameObject.SetActive(false);
    }

    void SpawnBody(PortraitData data)
    {
        if (currentBody != null)
            Destroy(currentBody);

        currentBody = Instantiate(
            database.bodyPrefabs[data.bodyIndex],
            transform
        );
    }
}