using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // 👈 Needed for scene loading

public class RegisterStudent : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField fnameInput;
    public TMP_InputField mnameInput;
    public TMP_InputField lnameInput;
    public TMP_Dropdown genderDropdown; // 👈 Dropdown for gender
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;

    [Header("UI Elements")]
    public Button registerButton;
    public GameObject accountExistsPanel; // 👈 show if username exists
    public GameObject successPanel; // 👈 optional: show success before switching

    [Header("Server URL")]
    public string registerURL = "https://homeworkquest.site/registers.php"; // 👈 change this to your actual PHP file

    [Header("Scene Settings")]
    public string loginSceneName = "login"; // 👈 your login scene name

    void Start()
    {
        if (registerButton != null)
            registerButton.onClick.AddListener(OnRegisterButtonClicked);

        if (accountExistsPanel != null)
            accountExistsPanel.SetActive(false);

        if (successPanel != null)
            successPanel.SetActive(false);
    }

    void OnRegisterButtonClicked()
    {
        StartCoroutine(RegisterUser());
    }

    IEnumerator RegisterUser()
    {
        // Validation
        if (string.IsNullOrEmpty(fnameInput.text) ||
            string.IsNullOrEmpty(lnameInput.text) ||
            string.IsNullOrEmpty(usernameInput.text) ||
            string.IsNullOrEmpty(passwordInput.text))
        {
            Debug.LogWarning("⚠️ Please fill in all required fields.");
            yield break;
        }

        // Get selected gender from dropdown
        string selectedGender = genderDropdown.options[genderDropdown.value].text;

        WWWForm form = new WWWForm();
        form.AddField("fname", fnameInput.text.Trim());
        form.AddField("mname", mnameInput.text.Trim());
        form.AddField("lname", lnameInput.text.Trim());
        form.AddField("gender", selectedGender); // 👈 Changed from "age" to "gender"
        form.AddField("username", usernameInput.text.Trim());
        form.AddField("password", passwordInput.text.Trim());

        using (UnityWebRequest www = UnityWebRequest.Post(registerURL, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("❌ Network error: " + www.error);
            }
            else
            {
                string response = www.downloadHandler.text.Trim();

                if (response == "SUCCESS")
                {
                    Debug.Log("✅ Registration successful!");
                    if (successPanel != null)
                        StartCoroutine(ShowSuccessAndGoToLogin());
                }
                else if (response == "EXISTS")
                {
                    Debug.LogWarning("⚠️ Username already exists.");
                    if (accountExistsPanel != null)
                        StartCoroutine(ShowPanelForSeconds(accountExistsPanel, 2f));
                }
                else
                {
                    Debug.LogError("❌ Registration failed: " + response);
                }
            }
        }
    }

    IEnumerator ShowPanelForSeconds(GameObject panel, float duration)
    {
        panel.SetActive(true);
        yield return new WaitForSeconds(duration);
        panel.SetActive(false);
    }

    IEnumerator ShowSuccessAndGoToLogin()
    {
        if (successPanel != null)
        {
            successPanel.SetActive(true);
            yield return new WaitForSeconds(1.5f);
        }

        // Load login scene after short delay
        SceneManager.LoadScene(loginSceneName);
    }
}
