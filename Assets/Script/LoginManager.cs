using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public Button loginButton;
    public GameObject loginErrorPanel; // optional - show errors
    public TMP_Text loginErrorText;
    public GameObject loadingPanel; // optional - show while waiting

    [Header("Server")]
    public string loginURL = "https://homeworkquest.site/login.php"; // <- change to your PHP login endpoint

    [Header("Scene")]
    public string afterLoginScene = "intro1"; // <- change to your scene name

    void Start()
    {
        if (loginButton != null)
            loginButton.onClick.AddListener(OnLoginClicked);

        if (loginErrorPanel != null)
            loginErrorPanel.SetActive(false);

        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    void OnLoginClicked()
    {
        StartCoroutine(LoginCoroutine());
    }

    IEnumerator LoginCoroutine()
    {
        string username = usernameInput != null ? usernameInput.text.Trim() : "";
        string password = passwordInput != null ? passwordInput.text.Trim() : "";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Please enter username and password.");
            yield break;
        }

        if (loadingPanel != null) loadingPanel.SetActive(true);

        WWWForm form = new WWWForm();
        form.AddField("username", username);
        form.AddField("password", password);

        using (UnityWebRequest www = UnityWebRequest.Post(loginURL, form))
        {
            yield return www.SendWebRequest();

            if (loadingPanel != null) loadingPanel.SetActive(false);

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Network error: " + www.error);
                ShowError("Network error: " + www.error);
            }
            else
            {
                string resp = www.downloadHandler.text.Trim();
                Debug.Log("Login response: " + resp);

                // Expecting JSON like: {"status":"SUCCESS","id":"123","username":"jdoe"}
                LoginResponse data;
                try
                {
                    data = JsonUtility.FromJson<LoginResponse>(resp);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("JSON parse error: " + ex.Message);
                    ShowError("Invalid server response.");
                    yield break;
                }

                if (data == null || string.IsNullOrEmpty(data.status))
                {
                    ShowError("Invalid server response.");
                    yield break;
                }

                if (data.status.ToUpper() == "SUCCESS")
                {
                    int id = -1;
                    int.TryParse(data.id, out id);

                    // Save session in SessionManager (persist to PlayerPrefs)
                    if (SessionManager.Instance != null)
                    {
                        SessionManager.Instance.SetSession(id, data.username, data.gender, true);
                    }
                    else
                    {
                        // fallback: create a temporary session manager object if missing
                        GameObject go = new GameObject("SessionManager");
                        go.AddComponent<SessionManager>();
                        // wait one frame for Awake to run
                        yield return null;
                        SessionManager.Instance.SetSession(id, data.username, data.gender, true);
                    }

                    // Optionally show success UI, then load next scene
                    SceneManager.LoadScene(afterLoginScene);
                }
                else
                {
                    // server should send status like "FAIL" and maybe a message field
                    string msg = string.IsNullOrEmpty(data.message) ? "Login failed." : data.message;
                    ShowError(msg);
                }
            }
        }
    }

    void ShowError(string message)
    {
        Debug.LogWarning(message);
        if (loginErrorText != null)
            loginErrorText.text = message;
        if (loginErrorPanel != null)
            StartCoroutine(ShowPanelForSeconds(loginErrorPanel, 2f));
    }

    IEnumerator ShowPanelForSeconds(GameObject panel, float seconds)
    {
        panel.SetActive(true);
        yield return new WaitForSeconds(seconds);
        panel.SetActive(false);
    }

    [System.Serializable]
    private class LoginResponse
    {
        public string status;
        public string id;
        public string username;
        public string gender; // added to fix compile error
        public string message; // optional
    }
}
