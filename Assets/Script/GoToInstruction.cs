using UnityEngine;
using UnityEngine.SceneManagement; // for SceneManager
using UnityEngine.UI; // for Button

public class GoToInstruction : MonoBehaviour
{
    [Header("Scene Settings")]
    public string instructionSceneName = "instruction"; // 👈 Change to your actual scene name

    [Header("Button")]
    public Button loginButton; // 👈 Assign your button here in the Inspector

    void Start()
    {
        // Connect the button click to the function
        if (loginButton != null)
            loginButton.onClick.AddListener(GoTonstruct);
        else
            Debug.LogWarning("⚠️ No button assigned to GoToRegisterScene script!");
    }

    public void GoTonstruct()
    {
        Debug.Log("Loading Intruction Scene...");
        SceneManager.LoadScene(instructionSceneName);
    }
}
