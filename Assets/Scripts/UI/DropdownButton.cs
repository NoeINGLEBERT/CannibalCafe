using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;

public class DropdownButton : MonoBehaviour
{
    private static readonly System.Collections.Generic.List<DropdownButton> allDropdowns
    = new System.Collections.Generic.List<DropdownButton>();

    private RectTransform root;

    [Header("UI References")]
    [SerializeField] private RectTransform dropdownButton;
    [SerializeField] private RectTransform dropdownPanel;
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private TMP_Text dropdownText;

    private CanvasGroup canvasGroup;
    private Image buttonImage;

    [Header("Content")]
    [TextArea]
    [SerializeField] private string dropdownMessage;

    [SerializeField] private string buttonLabel;

    [Header("Button Colors")]
    [SerializeField] private Color closedColor = Color.gray;
    [SerializeField] private Color openColor = Color.white;

    [Header("Events")]
    public UnityEvent onValidate;

    [Header("Animation")]
    [SerializeField] private float animationTime = 0.25f;

    private bool isOpen = false;
    private Coroutine anim;

    void OnEnable()
    {
        if (!allDropdowns.Contains(this))
            allDropdowns.Add(this);
    }

    void OnDisable()
    {
        allDropdowns.Remove(this);
    }

    void Awake()
    {
        root = GetComponent<RectTransform>();
        canvasGroup = dropdownPanel.GetComponent<CanvasGroup>();
        buttonImage = dropdownButton.GetComponent<Image>();

        if (canvasGroup == null)
            canvasGroup = dropdownPanel.gameObject.AddComponent<CanvasGroup>();

        // Apply text
        if (buttonText) buttonText.text = buttonLabel;
        if (dropdownText) dropdownText.text = dropdownMessage;

        // Start closed
        dropdownPanel.anchoredPosition = Vector2.zero;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float buttonHeight = dropdownButton.rect.height;
        root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, buttonHeight);
    }

    public void Toggle()
    {
        if (!isOpen)
        {
            // Close all other dropdowns
            foreach (var dropdown in allDropdowns)
            {
                if (dropdown != this)
                    dropdown.ForceClose();
            }
        }

        if (anim != null)
            StopCoroutine(anim);

        anim = StartCoroutine(Animate(!isOpen));
        isOpen = !isOpen;
    }

    public void ForceClose()
    {
        if (!isOpen) return;

        if (anim != null)
            StopCoroutine(anim);

        anim = StartCoroutine(Animate(false));
        isOpen = false;
    }

    public void Validate()
    {
        onValidate?.Invoke();
    }

    IEnumerator Animate(bool open)
    {
        float panelHeight = dropdownPanel.rect.height;
        float buttonHeight = dropdownButton.rect.height;

        float startPanelY = dropdownPanel.anchoredPosition.y;
        float targetPanelY = open ? -panelHeight : 0;

        float startHeight = root.rect.height;
        float targetHeight = open ? buttonHeight + panelHeight : buttonHeight;

        float startAlpha = canvasGroup.alpha;
        float targetAlpha = open ? 1f : 0f;

        Color startColor = buttonImage ? buttonImage.color : closedColor;
        Color targetColor = open ? openColor : closedColor;

        if (open)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / animationTime;
            float eased = Mathf.SmoothStep(0, 1, t);

            dropdownPanel.anchoredPosition =
                new Vector2(0, Mathf.Lerp(startPanelY, targetPanelY, eased));

            float newHeight = Mathf.Lerp(startHeight, targetHeight, eased);
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, eased);
            buttonImage.color = Color.Lerp(startColor, targetColor, eased);

            yield return null;
        }

        dropdownPanel.anchoredPosition = new Vector2(0, targetPanelY);
        root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
        canvasGroup.alpha = targetAlpha;
        buttonImage.color = targetColor;

        if (!open)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}