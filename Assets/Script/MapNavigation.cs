using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MapNavigation : MonoBehaviour
{
    [Header("Center Icon Image")]
    public Image centerIconImage;           // Single icon image in the center that changes

    [Header("Section Label (Text under icon)")]
    public TMP_Text sectionLabelText;      // Text that displays "Enter School", "Enter Leaderboard", etc.

    [Header("Instruction Dialog (Below section label)")]
    public TMP_Text instructionDialogText; // Dialog text that explains instructions with typewriter effect
    public float typewriterSpeed = 0.05f;  // Speed of typewriter animation (seconds per character)

    [Header("Icon Sprites")]
    public Sprite schoolGuildIcon;          // School/Guild icon sprite
    public Sprite leaderboardIcon;          // Leaderboard icon sprite
    public Sprite historyIcon;              // History icon sprite
    public Sprite feedbackIcon;             // Feedback icon sprite

    [Header("Navigation Buttons")]
    public Button rightArrowButton;         // Arrow to go to next section (right)
    public Button leftArrowButton;          // Arrow to go to previous section (left)

    [Header("Student Info (Top Left)")]
    public TMP_Text studentIdText;          // Text to display student ID
    public TMP_Text studentNameText;         // Text to display student name
    public TMP_Text schoolNumberText;       // Text to display school number (optional)

    [Header("Logout Button (Top Right)")]
    public Button logoutButton;             // Logout button
    public string loginSceneName = "login"; // Scene to load after logout

    [Header("Scene Names")]
    public string schoolGuildSceneName = "classroom";  // Scene name for school/guild
    public string leaderboardSceneName = "gameresult";  // Scene name for leaderboard
    public string historySceneName = "historyScene";    // Scene name for history
    public string feedbackSceneName = "feedBack";       // Scene name for feedback

    [Header("Center Icon Button (Optional - to load scene)")]
    public Button centerIconButton;         // Button on the center icon to load the scene

    [Header("Icon Animation Settings")]
    public float animationDuration = 0.3f;  // Duration of icon transition animation
    public bool useFadeAnimation = true;     // Use fade in/out animation
    public bool useScaleAnimation = true;    // Use scale (pulse) animation
    public float scaleAmount = 1.2f;         // How much to scale during animation (1.2 = 20% larger)

    private Coroutine iconAnimationCoroutine; // Track current animation
    private Coroutine typewriterCoroutine;    // Track typewriter animation

    private enum MapSection
    {
        SchoolGuild = 0,
        Leaderboard = 1,
        History = 2,
        Feedback = 3
    }

    private MapSection currentSection = MapSection.SchoolGuild;

    void Awake()
    {
        // Auto-connect navigation buttons
        if (rightArrowButton != null)
        {
            rightArrowButton.onClick.RemoveAllListeners();
            rightArrowButton.onClick.AddListener(GoToNextSection);
            rightArrowButton.interactable = true; // Ensure button is interactable
            Debug.Log("✅ Right Arrow Button connected");
        }
        else
        {
            Debug.LogWarning("⚠️ Right Arrow Button not assigned!");
        }

        if (leftArrowButton != null)
        {
            leftArrowButton.onClick.RemoveAllListeners();
            leftArrowButton.onClick.AddListener(GoToPreviousSection);
            leftArrowButton.interactable = true; // Ensure button is interactable
            Debug.Log("✅ Left Arrow Button connected");
        }
        else
        {
            Debug.LogWarning("⚠️ Left Arrow Button not assigned!");
        }

        // Auto-connect logout button
        if (logoutButton != null)
        {
            logoutButton.onClick.RemoveAllListeners();
            logoutButton.onClick.AddListener(OnLogout);
        }

        // Auto-connect center icon button
        if (centerIconButton != null)
        {
            centerIconButton.onClick.RemoveAllListeners();
            centerIconButton.onClick.AddListener(LoadCurrentSectionScene);
        }
    }

    void Start()
    {
        // Initialize: Show School/Guild icon
        ShowSection(MapSection.SchoolGuild);
        UpdateArrowButtons();
        UpdateStudentInfo();
        ShowInstructionDialog(MapSection.SchoolGuild);
    }

    public void GoToNextSection()
    {
        if (currentSection < MapSection.Feedback)
        {
            currentSection++;
            ShowSectionWithAnimation(currentSection);
            UpdateArrowButtons();
        }
    }

    public void GoToPreviousSection()
    {
        Debug.Log($"🔄 GoToPreviousSection called. Current section: {currentSection}");
        if (currentSection > MapSection.SchoolGuild)
        {
            currentSection--;
            ShowSectionWithAnimation(currentSection);
            UpdateArrowButtons();
            Debug.Log($"✅ Moved to previous section: {currentSection}");
        }
        else
        {
            Debug.LogWarning($"⚠️ Cannot go back - already at first section: {currentSection}");
        }
    }

    void ShowSection(MapSection section)
    {
        currentSection = section;

        // Update center icon based on current section (no animation - for initial load)
        if (centerIconImage != null)
        {
            switch (section)
            {
                case MapSection.SchoolGuild:
                    if (schoolGuildIcon != null) centerIconImage.sprite = schoolGuildIcon;
                    break;
                case MapSection.Leaderboard:
                    if (leaderboardIcon != null) centerIconImage.sprite = leaderboardIcon;
                    break;
                case MapSection.History:
                    if (historyIcon != null) centerIconImage.sprite = historyIcon;
                    break;
                case MapSection.Feedback:
                    if (feedbackIcon != null) centerIconImage.sprite = feedbackIcon;
                    break;
            }
        }

        // Update section label text
        UpdateSectionLabel(section);
    }

    void UpdateSectionLabel(MapSection section)
    {
        if (sectionLabelText != null)
        {
            switch (section)
            {
                case MapSection.SchoolGuild:
                    sectionLabelText.text = "Enter School";
                    break;
                case MapSection.Leaderboard:
                    sectionLabelText.text = "Enter Leaderboard";
                    break;
                case MapSection.History:
                    sectionLabelText.text = "Enter History";
                    break;
                case MapSection.Feedback:
                    sectionLabelText.text = "Enter Feedback";
                    break;
            }
        }
    }

    void ShowInstructionDialog(MapSection section)
    {
        // Stop any existing typewriter animation
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }

        // Get instruction text for current section
        string instructionText = GetInstructionText(section);

        // Start typewriter animation
        if (instructionDialogText != null && !string.IsNullOrEmpty(instructionText))
        {
            typewriterCoroutine = StartCoroutine(TypewriterEffect(instructionText));
        }
    }

    string GetInstructionText(MapSection section)
    {
        switch (section)
        {
            case MapSection.SchoolGuild:
                return "WELCOME TO THE SCHOOL! HERE YOU CAN ACCESS DIFFERENT CLASSROOMS AND LEARNING ACTIVITIES. CLICK THE ICON TO ENTER.";
            case MapSection.Leaderboard:
                return "CHECK YOUR RANKING AND SEE HOW YOU COMPARE WITH OTHER STUDENTS. VIEW YOUR SCORES AND ACHIEVEMENTS HERE.";
            case MapSection.History:
                return "REVIEW YOUR PAST ACTIVITIES, ANSWERS, AND PROGRESS. SEE YOUR LEARNING JOURNEY AND TRACK YOUR IMPROVEMENT.";
            case MapSection.Feedback:
                return "SHARE YOUR THOUGHTS AND FEEDBACK WITH YOUR TEACHER. YOUR INPUT HELPS IMPROVE THE LEARNING EXPERIENCE.";
            default:
                return "";
        }
    }

    System.Collections.IEnumerator TypewriterEffect(string text)
    {
        if (instructionDialogText == null) yield break;

        instructionDialogText.text = "";

        foreach (char c in text)
        {
            instructionDialogText.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }

        typewriterCoroutine = null;
    }

    void ShowSectionWithAnimation(MapSection section)
    {
        // Stop any existing animation
        if (iconAnimationCoroutine != null)
        {
            StopCoroutine(iconAnimationCoroutine);
        }

        // Start new animation
        iconAnimationCoroutine = StartCoroutine(AnimateIconChange(section));
    }

    System.Collections.IEnumerator AnimateIconChange(MapSection newSection)
    {
        if (centerIconImage == null) yield break;

        RectTransform iconRect = centerIconImage.rectTransform;
        Vector3 originalScale = Vector3.one;
        Color originalColor = centerIconImage.color;
        float halfDuration = animationDuration / 2f;

        // Get the new sprite
        Sprite newSprite = null;
        switch (newSection)
        {
            case MapSection.SchoolGuild:
                newSprite = schoolGuildIcon;
                break;
            case MapSection.Leaderboard:
                newSprite = leaderboardIcon;
                break;
            case MapSection.History:
                newSprite = historyIcon;
                break;
            case MapSection.Feedback:
                newSprite = feedbackIcon;
                break;
        }

        if (newSprite == null) yield break;

        // Phase 1: Fade out and scale down
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;

            if (useFadeAnimation)
            {
                centerIconImage.color = Color.Lerp(originalColor, new Color(originalColor.r, originalColor.g, originalColor.b, 0f), t);
            }

            if (useScaleAnimation)
            {
                float scale = Mathf.Lerp(1f, 0.8f, t);
                iconRect.localScale = originalScale * scale;
            }

            yield return null;
        }

        // Change sprite in the middle (when invisible)
        centerIconImage.sprite = newSprite;
        currentSection = newSection;
        
        // Update section label and instruction dialog
        UpdateSectionLabel(newSection);
        ShowInstructionDialog(newSection);

        // Phase 2: Fade in and scale up (with slight bounce)
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;

            if (useFadeAnimation)
            {
                centerIconImage.color = Color.Lerp(new Color(originalColor.r, originalColor.g, originalColor.b, 0f), originalColor, t);
            }

            if (useScaleAnimation)
            {
                // Bounce effect: go slightly larger then back to normal
                float scale;
                if (t < 0.6f)
                {
                    // Scale up to max
                    scale = Mathf.Lerp(0.8f, scaleAmount, t / 0.6f);
                }
                else
                {
                    // Scale back to normal
                    scale = Mathf.Lerp(scaleAmount, 1f, (t - 0.6f) / 0.4f);
                }
                iconRect.localScale = originalScale * scale;
            }

            yield return null;
        }

        // Ensure final state
        centerIconImage.color = originalColor;
        iconRect.localScale = originalScale;
        iconAnimationCoroutine = null;
    }

    void UpdateArrowButtons()
    {
        // Show/hide arrow buttons based on current section
        if (leftArrowButton != null)
        {
            // Hide left arrow when at School/Guild (first section)
            bool shouldShow = currentSection != MapSection.SchoolGuild;
            leftArrowButton.gameObject.SetActive(shouldShow);
            leftArrowButton.interactable = shouldShow; // Also ensure interactable matches visibility
            Debug.Log($"Left Arrow Button - Active: {shouldShow}, Section: {currentSection}");
        }

        if (rightArrowButton != null)
        {
            // Hide right arrow when at Feedback (last section)
            bool shouldShow = currentSection != MapSection.Feedback;
            rightArrowButton.gameObject.SetActive(shouldShow);
            rightArrowButton.interactable = shouldShow; // Also ensure interactable matches visibility
        }
    }

    void UpdateStudentInfo()
    {
        if (SessionManager.Instance == null)
        {
            Debug.LogWarning("⚠️ SessionManager not found.");
            if (studentIdText != null) studentIdText.text = "ID: N/A";
            if (studentNameText != null) studentNameText.text = "Name: N/A";
            if (schoolNumberText != null) schoolNumberText.text = "";
            return;
        }

        // Get data from SessionManager
        int studentId = SessionManager.Instance.StudentId;
        string username = SessionManager.Instance.Username;

        // Display student info
        if (studentIdText != null)
            studentIdText.text = $"ID: {(studentId > 0 ? studentId.ToString() : "N/A")}";

        if (studentNameText != null)
            studentNameText.text = string.IsNullOrEmpty(username) ? "N/A" : username;

        // School number can be added if available in SessionManager
        // For now, you can manually set it or add it to SessionManager
        if (schoolNumberText != null)
        {
            // If you have school number in PlayerPrefs or SessionManager, set it here
            // schoolNumberText.text = "School #: " + schoolNumber;
        }
    }

    void LoadCurrentSectionScene()
    {
        switch (currentSection)
        {
            case MapSection.SchoolGuild:
                SceneManager.LoadScene(schoolGuildSceneName);
                break;
            case MapSection.Leaderboard:
                SceneManager.LoadScene(leaderboardSceneName);
                break;
            case MapSection.History:
                SceneManager.LoadScene(historySceneName);
                break;
            case MapSection.Feedback:
                SceneManager.LoadScene(feedbackSceneName);
                break;
        }
    }

    void OnLogout()
    {
        // Clear session
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.ClearSession();
        }

        // Clear PlayerPrefs
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Debug.Log("✅ Logged out successfully");
        SceneManager.LoadScene(loginSceneName);
    }
}

