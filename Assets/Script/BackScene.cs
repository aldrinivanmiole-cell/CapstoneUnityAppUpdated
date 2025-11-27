using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BackScene : MonoBehaviour
{
    public string loginSceneName = ""; // 👈 change to your actual login scene name
    public Image backImage; // Drag your UI Image here in the Inspector

    void Start()
    {
        // ✅ Add click listener to the image (requires Button or EventTrigger)
        if (backImage != null)
        {
            // Make sure the image has a Button component
            Button button = backImage.GetComponent<Button>();
            if (button == null)
            {
                button = backImage.gameObject.AddComponent<Button>();
            }

            button.onClick.AddListener(GoBackToLogin);
        }
        else
        {
            Debug.LogWarning("⚠️ Back Image not assigned in Inspector.");
        }
    }

    public void GoBackToLogin()
    {
        Debug.Log("🔁 Returning to Login Scene...");
        SceneManager.LoadScene(loginSceneName);
    }
}
