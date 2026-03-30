using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RoomUIController : MonoBehaviour
{
    [SerializeField] private GameObject front;
    [SerializeField] private GameObject back;
    [SerializeField] private TMP_InputField filterInput;
    [SerializeField] private Transform refreshIcon;
    [SerializeField] private TMP_Text noResultText;

    private RoomManager manager;

    private CanvasGroup frontGroup;
    private Coroutine frontFade;

    private const float FadeDuration = 0.125f;

    // Cached state for post-fade back activation
    private bool shouldShowBack;
    private AvailableVillager nextVillager;

    private string currentFilter = "";

    private Coroutine rotateRoutine;

    private void Update()
    {
        Debug.Log(currentFilter);
    }

    public void Init(RoomManager manager)
    {
        this.manager = manager;
        frontGroup = GetOrAddCanvasGroup(front);
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        if (!obj.TryGetComponent(out CanvasGroup group))
            group = obj.AddComponent<CanvasGroup>();

        return group;
    }

    public void UpdateRoomCards(List<AvailableVillager> allVillagers, int index)
    {
        List<AvailableVillager> filteredVillagers = ApplyFilters(allVillagers);

        if (filteredVillagers.Count == 0 && allVillagers.Count > 0 && !string.IsNullOrEmpty(currentFilter))
        {
            ShowNoResults("No Town found with this filter");
            HideRoomCards();
            return;
        }

        if (filteredVillagers.Count == 0)
        {
            ShowNoResults("No rooms available");
            HideRoomCards();
            return;
        }

        HideNoResults();

        // BACK: decide state but DON'T show yet
        shouldShowBack = index + 1 < filteredVillagers.Count;
        nextVillager = shouldShowBack ? filteredVillagers[index + 1] : null;

        // Always hide back during transition
        back.SetActive(false);

        // FRONT
        if (index < filteredVillagers.Count)
        {
            front.GetComponent<RoomCardUI>().Setup(filteredVillagers[index], manager);
            StartFrontFade(1f);
        }
        else
        {
            StartFrontFade(0f);
        }
    }

    private void ShowNoResults(string message)
    {
        if (noResultText == null) return;

        noResultText.text = message;
        noResultText.gameObject.SetActive(true);
    }

    private void HideNoResults()
    {
        if (noResultText == null) return;

        noResultText.gameObject.SetActive(false);
    }

    public void RefreshFilter()
    {
        if (filterInput == null)
        {
            Debug.LogWarning("Filter input not assigned!");
            currentFilter = "";
            return;
        }

        currentFilter = filterInput.text;

        if (rotateRoutine != null)
            StopCoroutine(rotateRoutine);

        rotateRoutine = StartCoroutine(RotateButton());
    }

    private IEnumerator RotateButton()
    {
        float duration = .5f; // quick spin
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            refreshIcon.rotation = Quaternion.Euler(0f, 0f, -t * 360f);
            yield return null;
        }

        refreshIcon.rotation = Quaternion.identity;
    }

    private List<AvailableVillager> ApplyFilters(List<AvailableVillager> input)
    {
        List<AvailableVillager> filtered = new List<AvailableVillager>();

        foreach (var av in input)
        {
            if (av.roomData.settings.secretInvite)
            {
                if (currentFilter == av.roomData.settings.townName)
                    filtered.Add(av);
            }
            else
            {
                if (currentFilter == "")
                    filtered.Add(av);
            }
        }

        return filtered;
    }

    private void StartFrontFade(float target)
    {
        if (frontFade != null)
            StopCoroutine(frontFade);

        frontFade = StartCoroutine(FadeFrontRoutine(target));
    }

    private IEnumerator FadeFrontRoutine(float target)
    {
        front.SetActive(true);

        float start = frontGroup.alpha;
        float time = 0f;

        back.SetActive(false);

        while (time < FadeDuration)
        {
            time += Time.deltaTime;
            frontGroup.alpha = Mathf.Lerp(start, target, time / FadeDuration);
            yield return null;
        }

        frontGroup.alpha = target;

        if (target == 0f)
        {
            front.SetActive(false);
            yield break;
        }

        if (shouldShowBack)
        {
            back.SetActive(true);
            back.GetComponent<RoomCardUI>().Setup(nextVillager, manager);
        }
    }

    public void HideRoomCards()
    {
        if (frontFade != null)
            StopCoroutine(frontFade);

        frontFade = StartCoroutine(FadeFrontRoutine(0f));
        back.SetActive(false);
    }
}