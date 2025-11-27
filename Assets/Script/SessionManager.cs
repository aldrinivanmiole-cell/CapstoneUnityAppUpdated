using UnityEngine;

/// <summary>
/// Holds current session info for the logged-in student.
/// Other scripts can read SessionManager.StudentId and SessionManager.Username.
/// Optionally persists session to PlayerPrefs (so login survives app restart).
/// Alerts/logs issues when session info is missing or invalid.
/// </summary>
public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    // In-memory values
    public int StudentId { get; private set; } = -1;
    public string Username { get; private set; } = "";
    public string gender { get; private set; } = "";

    // PlayerPrefs keys
    const string PREF_ID = "SESSION_STUDENT_ID";
    const string PREF_USERNAME = "SESSION_USERNAME";
    const string PREF_GENDER = "SESSION_GENDER";

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Try load persisted session
        LoadSessionFromPrefs();

        // Alert if session is invalid
        CheckSessionValidity();
    }

    public bool IsLoggedIn()
    {
        return StudentId > 0 && !string.IsNullOrEmpty(Username);
    }

    /// <summary>
    /// Set session values in memory and optionally persist.
    /// </summary>
    public void SetSession(int studentId, string username, string gender, bool persist = true)
    {
        StudentId = studentId;
        Username = username ?? "";
        this.gender = gender ?? "";

        if (persist)
        {
            PlayerPrefs.SetInt(PREF_ID, StudentId);
            PlayerPrefs.SetString(PREF_USERNAME, Username);
            PlayerPrefs.SetString(PREF_GENDER, this.gender);
            PlayerPrefs.Save();
        }

        CheckSessionValidity();
    }

    public void ClearSession(bool clearPrefs = true)
    {
        StudentId = -1;
        Username = "";
        gender = "";
        if (clearPrefs)
        {
            PlayerPrefs.DeleteKey(PREF_ID);
            PlayerPrefs.DeleteKey(PREF_USERNAME);
            PlayerPrefs.DeleteKey(PREF_GENDER);
            PlayerPrefs.Save();
        }

        Debug.LogWarning("Session cleared.");
    }

    void LoadSessionFromPrefs()
    {
        if (PlayerPrefs.HasKey(PREF_ID))
        {
            StudentId = PlayerPrefs.GetInt(PREF_ID, -1);
            Username = PlayerPrefs.GetString(PREF_USERNAME, "");
            gender = PlayerPrefs.GetString(PREF_GENDER, "");
        }
        else
        {
            Debug.LogWarning("No saved session found in PlayerPrefs.");
        }
    }

    /// <summary>
    /// Check if the session is valid and alert if anything is missing.
    /// </summary>
    void CheckSessionValidity()
    {
        if (StudentId <= 0)
        {
            Debug.LogWarning("SessionManager: StudentId is invalid or missing.");
        }
        if (string.IsNullOrEmpty(Username))
        {
            Debug.LogWarning("SessionManager: Username is missing.");
        }
        if (string.IsNullOrEmpty(gender))
        {
            Debug.LogWarning("SessionManager: Gender is missing.");
        }
    }
}
