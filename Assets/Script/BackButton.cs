using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BackButton : MonoBehaviour
{
    [Header("Scene to go back to")]
    public string loginSceneName = ""; // Change to your login scene name

    private Button backButton;

    void Start()
    {
        // ✅ Get the Button component attached to this GameObject
        backButton = GetComponent<Button>();

        if (backButton != null)
        {
            backButton.onClick.AddListener(GoBackToLogin);
        }
        else
        {
            Debug.LogWarning("⚠️ No Button component found on this GameObject.");
        }
    }

    public void GoBackToLogin()
    {
        Debug.Log("🔙 Going back to login scene...");
        SceneManager.LoadScene(loginSceneName);
    }
}
