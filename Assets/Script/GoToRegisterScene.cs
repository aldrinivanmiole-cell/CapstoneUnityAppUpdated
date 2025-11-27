using UnityEngine;
using UnityEngine.SceneManagement; // for SceneManager
using UnityEngine.UI; // for Button

public class GoToRegisterScene : MonoBehaviour
{
    [Header("Scene Settings")]
    public string registerSceneName = "register"; // 👈 Change to your actual scene name

    [Header("Button")]
    public Button registerButton; // 👈 Assign your button here in the Inspector

    void Start()
    {
        // Connect the button click to the function
        if (registerButton != null)
            registerButton.onClick.AddListener(GoToRegister);
        else
            Debug.LogWarning("⚠️ No button assigned to GoToRegisterScene script!");
    }

    public void GoToRegister()
    {
        Debug.Log("Loading Register Scene...");
        SceneManager.LoadScene(registerSceneName);
    }
}
