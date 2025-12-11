using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DoneButton : MonoBehaviour
{
    public Button goBackButton;

    void Start()
    {
        if (goBackButton != null)
        {
            goBackButton.onClick.AddListener(GoToMainMenu);
        }
        else
        {
            Debug.LogWarning("⚠️ Done button not assigned!");
        }
    }

    public void GoToMainMenu()
    {
        Debug.Log("🏠 Returning to main menu...");
        SceneManager.LoadScene("map"); // Navigate to the map (main menu) scene
    }
}
