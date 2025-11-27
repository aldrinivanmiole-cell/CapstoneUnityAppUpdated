using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class LogoutImage : MonoBehaviour, IPointerClickHandler
{
    [Header("Scene to load after logout")]
    public string loginSceneName = "LoginScene"; // ← change this to your login scene name

    [Header("Confirmation UI")]
    public GameObject confirmationPanel; // panel with YES/NO images
    public GameObject yesImage;           // image for confirm logout
    public GameObject noImage;            // image for cancel logout

    void Start()
    {
        // Hide confirmation popup at start
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);

        // Add click events to the images (if they have colliders or are UI Images)
        AddClickHandler(yesImage, ConfirmLogout);
        AddClickHandler(noImage, CancelLogout);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Show confirmation popup when logout image is clicked
        if (confirmationPanel != null)
            confirmationPanel.SetActive(true);
    }

    void AddClickHandler(GameObject imageObject, System.Action action)
    {
        if (imageObject == null) return;

        var trigger = imageObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = imageObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerClick
        };
        entry.callback.AddListener((data) => { action(); });
        trigger.triggers.Add(entry);
    }

    // ✅ Called when user confirms logout
    public void ConfirmLogout()
    {
        PlayerPrefs.DeleteKey("student_id");
        PlayerPrefs.DeleteKey("username");
        PlayerPrefs.Save();

        Debug.Log("✅ Logged out successfully");
        SceneManager.LoadScene(loginSceneName);
    }

    // ❌ Called when user cancels logout
    public void CancelLogout()
    {
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }
}
