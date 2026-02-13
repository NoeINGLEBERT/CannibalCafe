using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class OpenRouterChat : MonoBehaviour
{
    private string apiUrl = "https://openrouter.ai/api/v1/chat/completions";
    private string apiKey = "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";

    // Example free / cheap models:
    // "mistralai/mistral-7b-instruct"
    // "meta-llama/llama-3-8b-instruct"
    private string modelName = "openai/gpt-4o-mini";

    public void SendMessageToAI(string playerMessage, System.Action<string> callback)
    {
        Debug.Log("[OpenRouterChat] Sending message: " + playerMessage);
        StartCoroutine(SendRequest(playerMessage, callback));
    }

    private IEnumerator SendRequest(string prompt, System.Action<string> callback)
    {
        // Build request payload
        ChatRequest requestData = new ChatRequest
        {
            model = modelName,
            messages = new Message[]
            {
                new Message { role = "user", content = prompt }
            }
        };

        string jsonData = JsonUtility.ToJson(requestData);
        Debug.Log("[OpenRouterChat] JSON Payload: " + jsonData);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            // OpenRouter requires these headers (can be anything meaningful)
            request.SetRequestHeader("HTTP-Referer", "https://yourgame.com");
            request.SetRequestHeader("X-Title", "Unity AI Prototype");

            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                Debug.LogError("[OpenRouterChat] Error: " + request.error);
                Debug.LogError(request.downloadHandler.text);
                callback?.Invoke("Error contacting AI");
            }
            else
            {
                string responseText = request.downloadHandler.text;
                Debug.Log("[OpenRouterChat] Raw Response: " + responseText);

                try
                {
                    ChatResponse response = JsonUtility.FromJson<ChatResponse>(responseText);
                    string aiText = response.choices[0].message.content;
                    callback?.Invoke(aiText);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("[OpenRouterChat] Parse Error: " + e.Message);
                    callback?.Invoke("Failed to parse AI response");
                }
            }
        }
    }

    // =========================
    // Data Models
    // =========================

    [System.Serializable]
    private class ChatRequest
    {
        public string model;
        public Message[] messages;
    }

    [System.Serializable]
    private class Message
    {
        public string role;
        public string content;
    }

    [System.Serializable]
    private class ChatResponse
    {
        public Choice[] choices;
    }

    [System.Serializable]
    private class Choice
    {
        public Message message;
    }
}
