using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class Switch : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform handle;
    [SerializeField] private Image background;
    [SerializeField] private Image handleImage;

    [Header("Positions")]
    [SerializeField] private Vector2 handleOffPosition;
    [SerializeField] private Vector2 handleOnPosition;

    [Header("Colors")]
    [SerializeField] private Color backgroundOffColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private Color backgroundOnColor = new Color(0.2f, 0.8f, 0.4f);

    [SerializeField] private Color handleOffColor = new Color(0.9f, 0.9f, 0.9f);
    [SerializeField] private Color handleOnColor = Color.white;

    [Header("Animation")]
    [SerializeField] private float animationDuration = 0.18f;

    [Header("State")]
    [SerializeField] private bool isOn = false;

    public UnityEvent<bool> OnValueChanged;

    public bool IsOn => isOn;

    Coroutine animationRoutine;

    private void Start()
    {
        SetStateImmediate(false);
    }

    public void Toggle()
    {
        SetValue(!isOn);
    }

    public void SetValue(bool value)
    {
        if (isOn == value) return;

        isOn = value;

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(AnimateSwitch());

        OnValueChanged?.Invoke(isOn);
    }

    public void SetStateImmediate(bool on)
    {
        isOn = on;

        if (handle != null)
            handle.anchoredPosition = isOn ? handleOnPosition : handleOffPosition;

        if (background != null)
            background.color = isOn ? backgroundOnColor : backgroundOffColor;

        if (handleImage != null)
            handleImage.color = isOn ? handleOnColor : handleOffColor;
    }

    IEnumerator AnimateSwitch()
    {
        Vector2 startPos = handle.anchoredPosition;
        Vector2 targetPos = isOn ? handleOnPosition : handleOffPosition;

        Color startBg = background.color;
        Color targetBg = isOn ? backgroundOnColor : backgroundOffColor;

        Color startHandle = handleImage.color;
        Color targetHandle = isOn ? handleOnColor : handleOffColor;

        float time = 0f;

        while (time < animationDuration)
        {
            time += Time.deltaTime;
            float t = time / animationDuration;

            // Smooth easing
            t = Mathf.SmoothStep(0, 1, t);

            handle.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            background.color = Color.Lerp(startBg, targetBg, t);
            handleImage.color = Color.Lerp(startHandle, targetHandle, t);

            yield return null;
        }

        handle.anchoredPosition = targetPos;
        background.color = targetBg;
        handleImage.color = targetHandle;
    }
}