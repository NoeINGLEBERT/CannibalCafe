using UnityEngine;
using UnityEngine.UI;

public class RoundButton : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] Image background;
    [SerializeField] Image icon;

    [SerializeField] Sprite normalIcon;
    [SerializeField] Sprite variantIcon;

    [SerializeField] float disabledIconAlpha = 0.25f;

    public void SetState(bool interactable, bool variant)
    {
        button.interactable = interactable;

        icon.sprite = variant ? variantIcon : normalIcon;

        Color c = icon.color;
        c.a = interactable ? 1f : disabledIconAlpha;
        icon.color = c;
    }
}
