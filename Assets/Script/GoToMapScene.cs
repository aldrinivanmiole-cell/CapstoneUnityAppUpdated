using UnityEngine;
using UnityEngine.SceneManagement; // for SceneManager
using UnityEngine.UI; // for Button

public class GoToMapScene : MonoBehaviour
{
    [Header("Scene Settings")]
    public string mapSceneName = "map"; // 👈 Change to your actual scene name

    [Header("Button")]
    public Button continueButton; // 👈 Assign your button here in the Inspector

    void Start()
    {
        // Connect the button click to the function
        if (continueButton != null)
            continueButton.onClick.AddListener(GoToMap);
        else
            Debug.LogWarning("⚠️ No button assigned to GoToRegisterScene script!");
    }

    public void GoToMap()
    {
        Debug.Log("Loading Map Scene...");
        SceneManager.LoadScene(mapSceneName);
    }
}
