using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class SwipeHandler : MonoBehaviour
{
    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;
    private RoomCardUI roomCardUI;

    private InputAction touchPress;
    private InputAction touchPosition;

    private float swipeThreshold = 100f;
    private float swipeDuration = 0.4f;
    private float rotationAngle = 30f;
    private float exitDistance = Screen.width * 1.5f;

    private bool isSwiping = false; // Flag to prevent multiple swipes at once

    private void Awake()
    {
        var playerInput = new InputActionMap("SwipeControls");
        touchPress = playerInput.AddAction("Press", binding: "<Pointer>/press");
        touchPosition = playerInput.AddAction("Position", binding: "<Pointer>/position");

        touchPress.started += ctx => StartTouch();
        touchPress.canceled += ctx => EndTouch();

        playerInput.Enable();
    }

    private void Start()
    {
        roomCardUI = GetComponent<RoomCardUI>();
        if (roomCardUI == null)
            Debug.LogError("RoomCardUI component not found!");
    }

    private void StartTouch()
    {
        if (isSwiping) return; // Prevent new swipe if already swiping
        startTouchPosition = touchPosition.ReadValue<Vector2>();
    }

    private void EndTouch()
    {
        if (isSwiping) return; // Prevent new swipe if already swiping
        endTouchPosition = touchPosition.ReadValue<Vector2>();
        HandleSwipe();
    }

    private void HandleSwipe()
    {
        float deltaX = endTouchPosition.x - startTouchPosition.x;

        if (Mathf.Abs(deltaX) > swipeThreshold)
        {
            isSwiping = true; // Block new swipes
            if (deltaX > 0)
                StartCoroutine(SwipeRightAnimation());
            else
                StartCoroutine(SwipeLeftAnimation());
        }
    }

    private IEnumerator SwipeLeftAnimation()
    {
        yield return SwipeAnimation(-exitDistance, rotationAngle);
        FindFirstObjectByType<RoomManager>().AdvanceToNextRoom();
        isSwiping = false; // Allow new swipes after animation
    }

    private IEnumerator SwipeRightAnimation()
    {
        yield return SwipeAnimation(exitDistance, -rotationAngle);
        roomCardUI.JoinRoom();
        FindFirstObjectByType<RoomManager>().AdvanceToNextRoom();
        isSwiping = false; // Allow new swipes after animation
    }

    private IEnumerator SwipeAnimation(float distance, float rotation)
    {
        float elapsedTime = 0f;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + new Vector3(distance, Mathf.Abs(distance * 0.3f), 0);
        Quaternion startRotation = transform.rotation;
        Quaternion endRotation = Quaternion.Euler(0, 0, rotation);

        while (elapsedTime < swipeDuration)
        {
            float t = elapsedTime / swipeDuration;
            transform.position = Vector3.Lerp(startPos, endPos, EaseOutQuad(t));
            transform.rotation = Quaternion.Lerp(startRotation, endRotation, EaseOutQuad(t));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        transform.position = startPos;
        transform.rotation = startRotation;
    }

    private float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }
}
