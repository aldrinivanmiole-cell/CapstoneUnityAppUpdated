using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GoToFeedback : MonoBehaviour

{
    [Header("Target Scene Name")]
    public string sceneName = "feedBack"; // Default target scene

    private void Start()
    {
        // Check if this GameObject has an Image and needs a Button
        Image img = GetComponent<Image>();
        if (img != null && GetComponent<Button>() == null)
        {
            // Add Button if missing (so clicks work)
            gameObject.AddComponent<Button>();
        }

        // Add click listener
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnImageClick);
        }
        else
        {
            Debug.LogWarning("No Button or Image component found on this GameObject.");
        }
    }

    void OnImageClick()
    {
        Debug.Log("Image clicked! Loading " + sceneName);
        SceneManager.LoadScene(sceneName);
    }
}
