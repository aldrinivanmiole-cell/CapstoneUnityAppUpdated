using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DisplayStudentInfo : MonoBehaviour
{
    [Header("TMP Text References")]
    public TMP_Text studentIdText;
    public TMP_Text usernameText;

    [Header("Gender Images")]
    public Image maleImage;   // 👈 Drag your male image here
    public Image femaleImage; // 👈 Drag your female image here

    void Start()
    {
        UpdateStudentInfo();
    }

    void UpdateStudentInfo()
    {
        if (SessionManager.Instance == null)
        {
            Debug.LogWarning("⚠️ SessionManager not found.");
            if (studentIdText != null) studentIdText.text = "ID: N/A";
            if (usernameText != null) usernameText.text = "Username: N/A";
            return;
        }

        // ✅ Get data from SessionManager
        int studentId = SessionManager.Instance.StudentId;
        string username = SessionManager.Instance.Username;
        string gender = SessionManager.Instance.gender != null 
                        ? SessionManager.Instance.gender.ToLower().Trim() 
                        : "";

        // ✅ Display info
        if (studentIdText != null)
            studentIdText.text = $"ID: {(studentId > 0 ? studentId.ToString() : "N/A")}";

        if (usernameText != null)
            usernameText.text = $"Username: {(string.IsNullOrEmpty(username) ? "N/A" : username)}";

        // ✅ Show correct gender image
        if (maleImage != null && femaleImage != null)
        {
            maleImage.gameObject.SetActive(gender == "male");
            femaleImage.gameObject.SetActive(gender == "female");
        }

        Debug.Log($"🧠 Student Info Loaded — ID: {studentId}, Username: {username}, Gender: {gender}");
    }
}
