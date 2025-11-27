using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DisplaySessionInfo : MonoBehaviour
{
    [Header("TMP Text References")]
    public TMP_Text idText;        // drag your TMP_Text for ID
    public TMP_Text usernameText;  // drag your TMP_Text for Username

    [Header("Gender Images")]
    public Image maleImage;        // drag your male image here
    public Image femaleImage;      // drag your female image here

    void Start()
    {
        UpdateSessionDisplay();
    }

    void UpdateSessionDisplay()
    {
        // ✅ Make sure SessionManager exists
        if (SessionManager.Instance == null)
        {
            Debug.LogWarning("⚠️ SessionManager not found in scene.");
            if (idText != null) idText.text = "ID: N/A";
            if (usernameText != null) usernameText.text = "Username: N/A";

            if (maleImage != null) maleImage.gameObject.SetActive(false);
            if (femaleImage != null) femaleImage.gameObject.SetActive(false);
            return;
        }

        int studentId = SessionManager.Instance.StudentId;
        string username = SessionManager.Instance.Username;
        string gender = SessionManager.Instance.gender?.ToLower().Trim() ?? "";

        // ✅ Display ID and username
        if (idText != null)
            idText.text = $"ID: {(studentId > 0 ? studentId.ToString() : "N/A")}";

        if (usernameText != null)
            usernameText.text = $"{(string.IsNullOrEmpty(username) ? "N/A" : username)}";

        // ✅ Show correct gender image
        if (maleImage != null && femaleImage != null)
        {
            bool isMale = gender == "male";
            bool isFemale = gender == "female";

            maleImage.gameObject.SetActive(isMale);
            femaleImage.gameObject.SetActive(isFemale);

            Debug.Log($"🧠 Gender detected: {gender} → Showing {(isMale ? "Male" : isFemale ? "Female" : "None")} image");
        }
    }

    // Optional: call this if session data changes later
    public void RefreshDisplay()
    {
        UpdateSessionDisplay();
    }
}
