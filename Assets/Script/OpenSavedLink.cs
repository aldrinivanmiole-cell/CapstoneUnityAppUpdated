using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

public class OpenSavedLink : MonoBehaviour
{
    [Header("PHP Endpoint URL")]
    public string getLinkUrl = "https://homequest-c3k7.onrender.com/get_link"; // Flask API endpoint

    private string savedLink = "";

    void Start()
    {
        StartCoroutine(LoadLinkFromServer());
    }

    IEnumerator LoadLinkFromServer()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(getLinkUrl))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                LinkResponse res = JsonUtility.FromJson<LinkResponse>(www.downloadHandler.text);
                if (res.status == "success")
                {
                    savedLink = res.url;
                    Debug.Log("✅ Loaded link: " + savedLink);
                }
                else
                {
                    Debug.LogWarning("⚠️ No link found on server.");
                }
            }
            else
            {
                Debug.LogError("❌ Error loading link: " + www.error);
            }
        }
    }

    // Call this on image click
    public void OnImageClick()
    {
        if (!string.IsNullOrEmpty(savedLink))
        {
#if UNITY_ANDROID
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.VIEW");
                    AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri");
                    AndroidJavaObject uri = uriClass.CallStatic<AndroidJavaObject>("parse", savedLink);

                    intent.Call<AndroidJavaObject>("setData", uri);
                    intent.Call<AndroidJavaObject>("setPackage", "com.android.chrome"); // Force open in Chrome
                    currentActivity.Call("startActivity", intent);
                }
            }
            catch (System.Exception)
            {
                Debug.LogWarning("⚠️ Chrome not found, opening with default browser instead.");
                Application.OpenURL(savedLink);
            }
#else
            // Fallback for PC/iOS/WebGL
            Application.OpenURL(savedLink);
#endif
        }
        else
        {
            Debug.LogWarning("⚠️ No link loaded yet!");
        }
    }

    [System.Serializable]
    public class LinkResponse
    {
        public string status;
        public string url;
    }
}
