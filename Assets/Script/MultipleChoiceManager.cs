using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

[System.Serializable]
public class MCAnswer
{
    public string answer_description;
    public int correct_answer;
}

[System.Serializable]
public class MCQuestion
{
    public int id;
    public int assignment_id;  // Assignment ID from database
    public string question_description;
    public string tutorial_link; // Tutorial link from database
    public string difficulty;  // NEW: "easy", "medium", or "hard"
    public List<MCAnswer> choices;
}

[System.Serializable]
public class SavedProgress
{
    public int currentQuestionIndex;
    public List<string> answers;  // Stored answers
    public List<int> lockedQuestions;  // Which questions are locked
}

public class MultipleChoiceManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text questionText;
    public TMP_Text progressText;
    public List<Button> choiceButtons;
    public GameObject finishPanel;
    public TMP_Text scoreText;

    [Header("Navigation Buttons - NEW!")]
    public Button previousButton;
    public Button nextButton;
    public Button submitButton;

    [Header("Tutorial")]
    public Button tutorialButton;

    [Header("Confirmation Dialog - NEW!")]
    [SerializeField] private GameObject confirmationDialog;
    [SerializeField] private TMP_Text confirmationText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;

    [Header("Warning Dialog - Unanswered Questions")]
    [SerializeField] private GameObject warningDialog;
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private Button warningOkButton;

    [Header("Difficulty Badge - NEW!")]
    public TMP_Text difficultyBadge;

    [Header("Animation Settings")]
    [SerializeField] private float fadeSpeed = 3f;
    [SerializeField] private float levelCompleteDuration = 2.5f;
    [SerializeField] private float scaleAnimationSpeed = 2f;

    private List<MCQuestion> questions = new List<MCQuestion>();
    private int currentIndex = 0;
    private int correctCount = 0;
    private int studentId;
    private int assignmentId;
    private bool isTransitioning = false;

    // Programmatically created UI elements
    private CanvasGroup mainCanvasGroup;
    private GameObject levelTransitionPanel;
    private TMP_Text levelTransitionText;
    private Image levelTransitionImage;

    // NEW: Progress tracking
    private List<string> studentAnswers = new List<string>();  // Store all answers
    private List<int> lockedQuestions = new List<int>();  // Track locked question indices
    private MCAnswer pendingAnswer = null;  // Answer waiting for confirmation
    private string saveKey = "";  // PlayerPrefs key for saving progress

    void Start()
    {
        studentId = SessionManager.Instance.StudentId;
        assignmentId = CurrentClassSession.SelectedCategoryId;
        saveKey = $"assignment_progress_{assignmentId}_{studentId}";

        // Initialize animation system
        InitializeAnimationSystem();

        finishPanel.SetActive(false);
        
        // NEW: Hide confirmation dialog initially
        if (confirmationDialog != null)
            confirmationDialog.SetActive(false);

        // NEW: Hide warning dialog initially
        if (warningDialog != null)
            warningDialog.SetActive(false);

        // Setup warning dialog button
        if (warningOkButton != null)
            warningOkButton.onClick.AddListener(OnWarningOkClicked);

        // Setup tutorial button
        if (tutorialButton != null)
            tutorialButton.onClick.AddListener(OpenTutorialLink);

        // NEW: Setup navigation buttons
        if (previousButton != null)
            previousButton.onClick.AddListener(OnPreviousClicked);
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);
        if (submitButton != null)
            submitButton.onClick.AddListener(OnSubmitClicked);

        // NEW: Setup confirmation buttons
        if (confirmYesButton != null)
            confirmYesButton.onClick.AddListener(OnConfirmYes);
        if (confirmNoButton != null)
            confirmNoButton.onClick.AddListener(OnConfirmNo);

        StartCoroutine(LoadQuestions());
    }

    IEnumerator LoadQuestions()
    {
        string url = $"https://homequest-c3k7.onrender.com/get_questions?student_id={studentId}&assignment_id={assignmentId}";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to load questions: " + www.error);
                yield break;
            }

            string json = www.downloadHandler.text;
            questions = JsonUtilityWrapper.FromJsonList<MCQuestion>(json);

            if (questions.Count > 0)
            {
                // NEW: Initialize answers list
                studentAnswers = new List<string>();
                for (int i = 0; i < questions.Count; i++)
                    studentAnswers.Add("");  // Empty = not answered yet

                // NEW: Try to load saved progress
                LoadProgress();

                ShowQuestion();
                UpdateNavigationButtons();
            }
        }
    }

    void ShowQuestion()
    {
        if (currentIndex >= questions.Count)
        {
            StartCoroutine(SaveScore());
            return;
        }

        MCQuestion q = questions[currentIndex];
        questionText.text = q.question_description;
        progressText.text = $"Question {currentIndex + 1} of {questions.Count}";

        // NEW: Show difficulty badge
        if (difficultyBadge != null)
        {
            string diff = string.IsNullOrEmpty(q.difficulty) ? "easy" : q.difficulty.ToLower();
            switch (diff)
            {
                case "easy":
                    difficultyBadge.text = "Easy 😊";
                    difficultyBadge.color = new Color(0.3f, 0.8f, 0.3f); // Green
                    break;
                case "medium":
                    difficultyBadge.text = "Medium 🤔";
                    difficultyBadge.color = new Color(1f, 0.6f, 0f); // Orange
                    break;
                case "hard":
                    difficultyBadge.text = "Hard 🔥";
                    difficultyBadge.color = new Color(0.9f, 0.2f, 0.2f); // Red
                    break;
            }
        }

        for (int i = 0; i < choiceButtons.Count; i++)
        {
            if (i < q.choices.Count)
            {
                MCAnswer choice = q.choices[i];
                choiceButtons[i].gameObject.SetActive(true);
                
                TMP_Text buttonText = choiceButtons[i].GetComponentInChildren<TMP_Text>();
                buttonText.text = choice.answer_description;
                
                // NEW: Highlight if this answer was previously selected
                if (studentAnswers[currentIndex] == choice.answer_description)
                {
                    choiceButtons[i].GetComponent<Image>().color = new Color(0.7f, 0.9f, 1f); // Light blue
                }
                else
                {
                    choiceButtons[i].GetComponent<Image>().color = Color.white;
                }

                choiceButtons[i].onClick.RemoveAllListeners();
                int capturedIndex = i; // Closure capture
                choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(q, q.choices[capturedIndex]));
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }

        // Show tutorial button if link exists
        if (tutorialButton != null)
            tutorialButton.gameObject.SetActive(!string.IsNullOrEmpty(q.tutorial_link));

        // NEW: Update navigation buttons
        UpdateNavigationButtons();
    }

    void OnChoiceSelected(MCQuestion question, MCAnswer selectedChoice)
    {
        // NEW: Show confirmation dialog instead of immediate submit
        pendingAnswer = selectedChoice;
        
        if (confirmationDialog != null && confirmationText != null)
        {
            // Show the selected answer in the confirmation message
            confirmationText.text = $"Are you sure to pick \"{selectedChoice.answer_description}\" as your answer? 🤔";
            confirmationDialog.SetActive(true);
        }
    }

    // NEW: Confirmation dialog handlers
    void OnConfirmYes()
    {
        if (pendingAnswer == null) return;

        // Save the answer (NO FEEDBACK SHOWN!)
        MCQuestion question = questions[currentIndex];
        studentAnswers[currentIndex] = pendingAnswer.answer_description;

        // Save answer to history but DON'T show if correct/incorrect
        bool isCorrect = pendingAnswer.correct_answer == 1;
        string correctAnswer = "";
        foreach (var c in question.choices)
        {
            if (c.correct_answer == 1)
            {
                correctAnswer = c.answer_description;
                break;
            }
        }

        StartCoroutine(SaveAnswerToHistory(studentId, question.id, question.question_description, 
            pendingAnswer.answer_description, correctAnswer, isCorrect ? 1 : 0));

        // Lock previous difficulty questions if moving to new difficulty
        LockPreviousDifficultyQuestions();

        // Save progress
        SaveProgress();

        // Hide dialog
        confirmationDialog.SetActive(false);
        pendingAnswer = null;

        // Refresh current question to show selection
        ShowQuestion();
        
        // Update navigation buttons (to check if button text should change)
        UpdateNavigationButtons();
    }

    void OnConfirmNo()
    {
        // User clicked "Let me think more"
        confirmationDialog.SetActive(false);
        pendingAnswer = null;
    }

    IEnumerator SaveAnswerToHistory(int studentId, int questionId, string question, string playerAnswer, string correctAnswer, int isCorrect)
    {
        int assignmentId = (questions.Count > 0) ? questions[0].assignment_id : 0;
        
        WWWForm form = new WWWForm();
        form.AddField("student_id", studentId);
        form.AddField("assignment_id", assignmentId);
        form.AddField("question_id", questionId);
        form.AddField("question_text", question);
        form.AddField("student_answer", playerAnswer);
        form.AddField("correct_answer", correctAnswer);
        form.AddField("is_correct", isCorrect);

        using (UnityWebRequest www = UnityWebRequest.Post("https://homequest-c3k7.onrender.com/save_history", form))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
                Debug.LogError("Error saving history: " + www.error);
            else
                Debug.Log("✅ History saved successfully");
        }
    }

   IEnumerator SaveScore()
{
    // Show finish panel with score
    finishPanel.SetActive(true);
    
    if (scoreText != null)
    {
        scoreText.text = $"You got {correctCount} out of {questions.Count} correct!";
        scoreText.color = Color.white;
        scoreText.fontSize = 36;
        scoreText.gameObject.SetActive(true);
    }
    
    Debug.Log($"Final Score: {correctCount}/{questions.Count}");

    // Calculate percentage score for trophy system
    int percentageScore = (questions.Count > 0) ? (correctCount * 100) / questions.Count : 0;
    PlayerPrefs.SetInt("PlayerScore", percentageScore);
    PlayerPrefs.Save();
    Debug.Log($"✅ Score saved to PlayerPrefs: {percentageScore}%");

    int assignmentId = (questions.Count > 0) ? questions[0].assignment_id : 0;

    WWWForm form = new WWWForm();
    form.AddField("student_id", studentId);
    form.AddField("assignment_id", assignmentId);
    form.AddField("score", correctCount); // Send the total correct answers as score

    using (UnityWebRequest www = UnityWebRequest.Post("https://homequest-c3k7.onrender.com/submit_score", form))
    {
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
            Debug.LogError("❌ Error saving score: " + www.error);
        else
            Debug.Log("✅ Score saved successfully: " + www.downloadHandler.text);
    }

    // Wait 5 seconds then navigate to gameresult scene
    yield return new WaitForSeconds(5f);
    SceneManager.LoadScene("gameresult");
}

    void OpenTutorialLink()
    {
        if (currentIndex < questions.Count)
        {
            string link = questions[currentIndex].tutorial_link;
            if (!string.IsNullOrEmpty(link))
            {
                Application.OpenURL(link);
                Debug.Log("Opening tutorial link: " + link);
            }
        }
    }

    // ========== NEW NAVIGATION FUNCTIONS ==========
    
    void OnPreviousClicked()
    {
        if (isTransitioning) return;
        if (currentIndex > 0 && !lockedQuestions.Contains(currentIndex - 1))
        {
            StartCoroutine(AnimateQuestionTransition(currentIndex - 1));
        }
    }

    void OnNextClicked()
    {
        if (isTransitioning) return;
        if (currentIndex >= questions.Count - 1) return;

        // Get current difficulty
        string currentDifficulty = questions[currentIndex].difficulty?.ToLower() ?? "easy";
        
        // Check if trying to move to next difficulty
        string nextDifficulty = questions[currentIndex + 1].difficulty?.ToLower() ?? "easy";
        
        if (currentDifficulty != nextDifficulty)
        {
            // Moving to next difficulty - check if all questions in current difficulty are answered
            if (!AreAllQuestionsInDifficultyAnswered(currentDifficulty))
            {
                ShowWarningDialog(currentDifficulty);
                return;
            }
            
            // Show level complete animation before moving to next difficulty
            StartCoroutine(ShowLevelCompleteAnimation(currentDifficulty, nextDifficulty));
        }
        else
        {
            // Same difficulty - animate transition
            StartCoroutine(AnimateQuestionTransition(currentIndex + 1));
        }
    }

    bool AreAllQuestionsInDifficultyAnswered(string difficulty)
    {
        for (int i = 0; i < questions.Count; i++)
        {
            string qDifficulty = questions[i].difficulty?.ToLower() ?? "easy";
            if (qDifficulty == difficulty)
            {
                // Check if this question is answered
                if (string.IsNullOrEmpty(studentAnswers[i]))
                {
                    return false;
                }
            }
        }
        return true;
    }

    void ShowWarningDialog(string difficulty)
    {
        if (warningDialog == null || warningText == null) return;

        // Count unanswered questions in current difficulty
        int unansweredCount = 0;
        List<int> unansweredIndices = new List<int>();
        
        for (int i = 0; i < questions.Count; i++)
        {
            string qDifficulty = questions[i].difficulty?.ToLower() ?? "easy";
            if (qDifficulty == difficulty && string.IsNullOrEmpty(studentAnswers[i]))
            {
                unansweredCount++;
                unansweredIndices.Add(i + 1); // Question numbers (1-indexed)
            }
        }

        string difficultyName = difficulty.Substring(0, 1).ToUpper() + difficulty.Substring(1);
        warningText.text = $"⚠️ Warning!\n\nYou have {unansweredCount} unanswered question(s) in the {difficultyName} difficulty level.\n\nPlease answer all questions before proceeding to the next level.";
        
        warningDialog.SetActive(true);
    }

    void OnWarningOkClicked()
    {
        if (warningDialog != null)
            warningDialog.SetActive(false);
    }

    void OnSubmitClicked()
    {
        // Check if all questions answered
        foreach (string answer in studentAnswers)
        {
            if (string.IsNullOrEmpty(answer))
            {
                Debug.LogWarning("Please answer all questions before submitting!");
                return;
            }
        }

        // Calculate score now (at end)
        correctCount = 0;
        for (int i = 0; i < questions.Count; i++)
        {
            MCQuestion q = questions[i];
            string studentAnswer = studentAnswers[i];
            
            foreach (var choice in q.choices)
            {
                if (choice.answer_description == studentAnswer && choice.correct_answer == 1)
                {
                    correctCount++;
                    break;
                }
            }
        }

        // Clear saved progress
        PlayerPrefs.DeleteKey(saveKey);
        PlayerPrefs.Save();

        StartCoroutine(SaveScore());
    }

    void UpdateNavigationButtons()
    {
        // Previous button: disabled if at first question OR previous question is locked
        if (previousButton != null)
        {
            bool canGoPrevious = currentIndex > 0 && !lockedQuestions.Contains(currentIndex - 1);
            previousButton.interactable = canGoPrevious;
        }

        // Next button: disabled if at last question
        if (nextButton != null)
        {
            bool canGoNext = currentIndex < questions.Count - 1;
            nextButton.interactable = canGoNext;

            // Update button text based on whether all questions in current difficulty are answered
            if (canGoNext)
            {
                TMP_Text nextButtonText = nextButton.GetComponentInChildren<TMP_Text>();
                if (nextButtonText != null)
                {
                    string currentDifficulty = questions[currentIndex].difficulty?.ToLower() ?? "easy";
                    
                    // Check if moving to next difficulty
                    if (currentIndex < questions.Count - 1)
                    {
                        string nextDifficulty = questions[currentIndex + 1].difficulty?.ToLower() ?? "easy";
                        
                        if (currentDifficulty != nextDifficulty)
                        {
                            // About to move to next difficulty - check if all answered
                            if (AreAllQuestionsInDifficultyAnswered(currentDifficulty))
                            {
                                nextButtonText.text = "Proceed to Next Level";
                            }
                            else
                            {
                                nextButtonText.text = "Next";
                            }
                        }
                        else
                        {
                            // Same difficulty - just show "Next"
                            nextButtonText.text = "Next";
                        }
                    }
                    else
                    {
                        nextButtonText.text = "Next";
                    }
                }
            }
        }

        // Submit button: only show on last question
        if (submitButton != null)
        {
            submitButton.gameObject.SetActive(currentIndex == questions.Count - 1);
        }
    }

    void LockPreviousDifficultyQuestions()
    {
        if (currentIndex >= questions.Count) return;

        string currentDifficulty = questions[currentIndex].difficulty?.ToLower() ?? "easy";

        // Determine difficulty ranges
        Dictionary<string, List<int>> difficultyRanges = new Dictionary<string, List<int>>();
        difficultyRanges["easy"] = new List<int>();
        difficultyRanges["medium"] = new List<int>();
        difficultyRanges["hard"] = new List<int>();

        for (int i = 0; i < questions.Count; i++)
        {
            string diff = questions[i].difficulty?.ToLower() ?? "easy";
            if (difficultyRanges.ContainsKey(diff))
                difficultyRanges[diff].Add(i);
        }

        // Lock questions from previous difficulties
        if (currentDifficulty == "medium")
        {
            foreach (int idx in difficultyRanges["easy"])
            {
                if (!lockedQuestions.Contains(idx))
                    lockedQuestions.Add(idx);
            }
        }
        else if (currentDifficulty == "hard")
        {
            foreach (int idx in difficultyRanges["easy"])
            {
                if (!lockedQuestions.Contains(idx))
                    lockedQuestions.Add(idx);
            }
            foreach (int idx in difficultyRanges["medium"])
            {
                if (!lockedQuestions.Contains(idx))
                    lockedQuestions.Add(idx);
            }
        }
    }

    // ========== PROGRESS SAVE/LOAD ==========

    void SaveProgress()
    {
        SavedProgress progress = new SavedProgress
        {
            currentQuestionIndex = currentIndex,
            answers = studentAnswers,
            lockedQuestions = lockedQuestions
        };

        string json = JsonUtility.ToJson(progress);
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.Save();
        
        Debug.Log($"✅ Progress saved: Question {currentIndex + 1}/{questions.Count}");
    }

    void LoadProgress()
    {
        if (PlayerPrefs.HasKey(saveKey))
        {
            string json = PlayerPrefs.GetString(saveKey);
            SavedProgress progress = JsonUtility.FromJson<SavedProgress>(json);

            currentIndex = progress.currentQuestionIndex;
            studentAnswers = progress.answers;
            lockedQuestions = progress.lockedQuestions;

            Debug.Log($"✅ Progress loaded: Resuming at Question {currentIndex + 1}/{questions.Count}");
        }
        else
        {
            Debug.Log("No saved progress found. Starting fresh.");
        }
    }

    // ========== ANIMATION SYSTEM (CODE-BASED) ==========

    void InitializeAnimationSystem()
    {
        // Add CanvasGroup to main panel for fade effects
        Transform mainPanel = questionText?.transform.parent;
        if (mainPanel != null && mainCanvasGroup == null)
        {
            mainCanvasGroup = mainPanel.gameObject.GetComponent<CanvasGroup>();
            if (mainCanvasGroup == null)
            {
                mainCanvasGroup = mainPanel.gameObject.AddComponent<CanvasGroup>();
            }
        }

        // Create level transition overlay panel programmatically
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null && levelTransitionPanel == null)
        {
            // Create overlay panel
            levelTransitionPanel = new GameObject("LevelTransitionPanel");
            levelTransitionPanel.transform.SetParent(canvas.transform, false);
            
            RectTransform rectTransform = levelTransitionPanel.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            
            // Add background image
            levelTransitionImage = levelTransitionPanel.AddComponent<Image>();
            levelTransitionImage.color = new Color(0, 0, 0, 0); // Start transparent
            
            // Add canvas group for fading
            CanvasGroup cg = levelTransitionPanel.AddComponent<CanvasGroup>();
            cg.alpha = 0;
            cg.blocksRaycasts = false;
            
            // Create text element
            GameObject textObj = new GameObject("LevelCompleteText");
            textObj.transform.SetParent(levelTransitionPanel.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(800, 400);
            textRect.anchoredPosition = Vector2.zero;
            
            levelTransitionText = textObj.AddComponent<TextMeshProUGUI>();
            levelTransitionText.fontSize = 48;
            levelTransitionText.fontStyle = FontStyles.Bold;
            levelTransitionText.alignment = TextAlignmentOptions.Center;
            levelTransitionText.color = Color.white;
            
            // Set high sort order to appear on top
            levelTransitionPanel.transform.SetAsLastSibling();
            levelTransitionPanel.SetActive(false);
        }
    }

    IEnumerator AnimateQuestionTransition(int targetIndex)
    {
        isTransitioning = true;

        // Fade out current question
        if (mainCanvasGroup != null)
        {
            float elapsed = 0f;
            float duration = 0.3f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                mainCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                yield return null;
            }
            mainCanvasGroup.alpha = 0f;
        }

        // Change question
        currentIndex = targetIndex;
        ShowQuestion();

        // Small delay
        yield return new WaitForSeconds(0.1f);

        // Fade in new question
        if (mainCanvasGroup != null)
        {
            float elapsed = 0f;
            float duration = 0.3f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                mainCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }
            mainCanvasGroup.alpha = 1f;
        }

        isTransitioning = false;
    }

    IEnumerator ShowLevelCompleteAnimation(string completedDifficulty, string nextDifficulty)
    {
        isTransitioning = true;

        if (levelTransitionPanel != null && levelTransitionText != null && levelTransitionImage != null)
        {
            // Setup text
            string difficultyName = char.ToUpper(completedDifficulty[0]) + completedDifficulty.Substring(1);
            string nextDifficultyName = char.ToUpper(nextDifficulty[0]) + nextDifficulty.Substring(1);
            
            levelTransitionText.text = $"🎉 {difficultyName} Level Complete! 🎉\n\nPreparing {nextDifficultyName} Level...";
            
            // Set background color based on next difficulty
            Color bgColor = GetDifficultyColor(nextDifficulty);
            levelTransitionImage.color = new Color(bgColor.r, bgColor.g, bgColor.b, 0f);
            
            // Show panel
            levelTransitionPanel.SetActive(true);
            CanvasGroup panelCG = levelTransitionPanel.GetComponent<CanvasGroup>();
            panelCG.blocksRaycasts = true;
            
            // Fade in with scale animation
            float elapsed = 0f;
            float duration = 0.5f;
            Vector3 startScale = new Vector3(0.8f, 0.8f, 1f);
            Vector3 endScale = Vector3.one;
            
            levelTransitionText.transform.localScale = startScale;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Smooth fade in
                panelCG.alpha = Mathf.Lerp(0f, 1f, t);
                levelTransitionImage.color = new Color(bgColor.r, bgColor.g, bgColor.b, Mathf.Lerp(0f, 0.9f, t));
                
                // Scale animation with bounce
                float bounceT = Mathf.Sin(t * Mathf.PI * 0.5f);
                levelTransitionText.transform.localScale = Vector3.Lerp(startScale, endScale, bounceT);
                
                yield return null;
            }
            
            // Hold for a moment
            yield return new WaitForSeconds(levelCompleteDuration);
            
            // Fade out
            elapsed = 0f;
            duration = 0.4f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                panelCG.alpha = Mathf.Lerp(1f, 0f, t);
                levelTransitionImage.color = new Color(bgColor.r, bgColor.g, bgColor.b, Mathf.Lerp(0.9f, 0f, t));
                
                yield return null;
            }
            
            panelCG.alpha = 0f;
            panelCG.blocksRaycasts = false;
            levelTransitionPanel.SetActive(false);
        }

        // Move to next question
        currentIndex++;
        ShowQuestion();

        isTransitioning = false;
    }

    Color GetDifficultyColor(string difficulty)
    {
        switch (difficulty.ToLower())
        {
            case "easy":
                return new Color(0.2f, 0.8f, 0.2f); // Green
            case "medium":
                return new Color(1f, 0.6f, 0f); // Orange
            case "hard":
                return new Color(0.9f, 0.2f, 0.2f); // Red
            default:
                return new Color(0.3f, 0.3f, 0.8f); // Blue
        }
    }
}
