using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BackButton : MonoBehaviour
{
    [Header("Scene to go back to")]
    public string previousSceneName = "NEWMAP"; // Scene to go back to

    private Button backButton;

    void Start()
    {
        // ✅ Get the Button component attached to this GameObject
        backButton = GetComponent<Button>();

        if (backButton != null)
        {
            backButton.onClick.AddListener(GoBackToPreviousScene);
        }
        else
        {
            Debug.LogWarning("⚠️ No Button component found on this GameObject.");
        }
    }

    public void GoBackToPreviousScene()
    {
        Debug.Log($"🔙 Going back to {previousSceneName} scene...");
        SceneManager.LoadScene(previousSceneName);
    }
}
