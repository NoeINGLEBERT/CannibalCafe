using PlayFab;
using PlayFab.ClientModels;
using Photon.Pun;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

public class AccountManager : MonoBehaviour
{
    [Header("Profile Panel References")]
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private TMP_Text mmrText;
    [SerializeField] private Image avatarImage;

    private string playerUsername = "Unknown";
    private int playerMMR = 0;
    private string avatarUrl = "";

    public void FetchAccountInfo()
    {
        PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest(), OnAccountInfoSuccess, OnAccountInfoFailure);
    }

    private void OnAccountInfoSuccess(GetAccountInfoResult result)
    {
        Debug.Log("Successfully retrieved PlayFab account info.");
        playerUsername = result.AccountInfo.TitleInfo.DisplayName ?? "Unknown";
        avatarUrl = result.AccountInfo.TitleInfo.AvatarUrl ?? "";

        if (!string.IsNullOrEmpty(avatarUrl))
        {
            var playerProperties = new ExitGames.Client.Photon.Hashtable { { "AvatarUrl", avatarUrl } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
        }

        FetchPlayerMMR();
    }

    private void OnAccountInfoFailure(PlayFabError error)
    {
        Debug.LogError($"Failed to retrieve account info: {error.ErrorMessage}");
    }

    private void FetchPlayerMMR()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), OnUserDataSuccess, OnUserDataFailure);
    }

    private void OnUserDataSuccess(GetUserDataResult result)
    {
        if (result.Data != null && result.Data.ContainsKey("MMR"))
            int.TryParse(result.Data["MMR"].Value, out playerMMR);

        UpdateProfileUI();
    }

    private void OnUserDataFailure(PlayFabError error)
    {
        Debug.LogError($"Failed to retrieve MMR: {error.ErrorMessage}");
    }

    private void UpdateProfileUI()
    {
        if (usernameText != null)
        {
            usernameText.text = $"{playerUsername}";
        }
        else
        {
            Debug.LogWarning("UsernameText is not assigned in the inspector.");
        }

        if (mmrText != null)
        {
            mmrText.text = $"MMR: {playerMMR}";
        }
        else
        {
            Debug.LogWarning("MMRText is not assigned in the inspector.");
        }

        if (avatarImage != null && !string.IsNullOrEmpty(avatarUrl))
        {
            StartCoroutine(LoadAvatarImage(avatarUrl));
        }
        else
        {
            Debug.LogWarning("AvatarImage is not assigned in the inspector or Avatar URL is empty.");
        }
    }

    private IEnumerator LoadAvatarImage(string url)
    {
        using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(webRequest);
                avatarImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
            }
            else
            {
                Debug.LogError($"Failed to load avatar image from URL: {webRequest.error}");
            }
        }
    }
}
