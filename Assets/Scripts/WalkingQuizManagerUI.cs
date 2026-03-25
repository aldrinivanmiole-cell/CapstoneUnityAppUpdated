using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// Quiz Manager — clickable answer boxes, no player movement.
/// Flow: Tap answer box → YES/NO Confirmation dialog → Correct/Wrong panel → (wrong) mini-game
/// </summary>
public class WalkingQuizManagerUI : MonoBehaviour
{
    private const string LegacyFlappySceneName = "GameScreen";
    private const string DefaultFlappySceneName = "GameScreen";
    private const string PunishmentSceneName = DefaultFlappySceneName;

    [Header("Answer Boxes")]
    [SerializeField] private AnswerBoxUI[] answerBoxes;

    [Header("UI - Question")]
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("UI - Confirmation Dialog")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private TextMeshProUGUI confirmationText;
    [SerializeField] private TextMeshProUGUI selectedAnswerPreview;
    [SerializeField] private Image selectedAnswerColor;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("UI - Correct Panel")]
    [SerializeField] private GameObject correctPanel;
    [SerializeField] private Button correctProceedButton;

    [Header("UI - Wrong Panel")]
    [SerializeField] private GameObject wrongPanel;
    [SerializeField] private float wrongPanelDuration = 2f;

    [Header("UI - Finish Panel")]
    [SerializeField] private GameObject finishPanel;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Button returnButton;

    [Header("Mini Game")]
    [SerializeField] private StarCollectionMiniGameUI miniGame;
    [SerializeField] private List<string> punishmentMinigameScenes = new List<string> { "PacmanMinigame", "MemoryButtonsMinigame", "MatchTheShape", "GameScreen" };

    [Header("Background Themes")]
    [SerializeField] private GameObject[] backgroundThemes;

    [Header("Settings")]
    [SerializeField] private string apiBaseUrl = "https://your-api.com";

    private List<QuestionData> questions = new List<QuestionData>();
    private int currentQuestionIndex = 0;
    private int score = 0;
    private int totalQuestions = 0;
    private AnswerBoxUI selectedAnswerBox;
    private bool isQuizActive = false;
    private string timerSessionKey = string.Empty;
    private bool completionRecorded;

    [System.Serializable]
    public class QuestionData
    {
        public int id;
        public string question;
        public string[] answers;
        public int correctIndex;
        public string tutorial_link;
        public string wrong_minigame;
        public string punishment_minigame;
    }

    private void Start()
    {
        SetupButtons();
        HideAllPanels();
        StartCoroutine(LoadQuestionsAndStart());
    }

    private void SetupButtons()
    {
        if (yesButton != null)            yesButton.onClick.AddListener(OnYesClicked);
        if (noButton != null)             noButton.onClick.AddListener(OnNoClicked);
        if (correctProceedButton != null) correctProceedButton.onClick.AddListener(OnProceedClicked);
        if (returnButton != null)         returnButton.onClick.AddListener(OnReturnClicked);

        foreach (var box in answerBoxes)
            if (box != null) box.SetQuizManager(this);
    }

    private void HideAllPanels()
    {
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (correctPanel != null)      correctPanel.SetActive(false);
        if (wrongPanel != null)        wrongPanel.SetActive(false);
        if (finishPanel != null)       finishPanel.SetActive(false);
    }

    private IEnumerator LoadQuestionsAndStart()
    {
        int timerStudentId = SessionManager.Instance != null ? SessionManager.Instance.StudentId : 0;
        int timerAssignmentId = CurrentClassSession.SelectedCategoryId;
        timerSessionKey = BuildTimerSessionKey(timerStudentId, timerAssignmentId);

        if (questionText != null)
            questionText.text = "Loading questions...";

        yield return StartCoroutine(FetchQuestionsFromAPI());

        if (questions.Count == 0)
            LoadTestQuestions();

        totalQuestions = questions.Count;

        int resumeIndex = PlayerPrefs.GetInt(PacmanManager.ResumeQuestionIndexKey, -1);
        if (resumeIndex >= 0)
        {
            // Allow totalQuestions as a sentinel meaning "finish immediately after minigame".
            currentQuestionIndex = Mathf.Clamp(resumeIndex, 0, questions.Count);
            PlayerPrefs.DeleteKey(PacmanManager.ResumeQuestionIndexKey);
            PlayerPrefs.Save();
        }

        if (questions.Count > 0)
        {
            isQuizActive = true;
            completionRecorded = false;
            QuizSessionTimer.StartOrResume(timerSessionKey);
            ShowQuestion(currentQuestionIndex);
        }
        else
        {
            if (questionText != null)
                questionText.text = "No questions available!";
        }
    }

    private IEnumerator FetchQuestionsFromAPI()
    {
        int studentId = 1;
        int categoryId = 1;

        if (SessionManager.Instance != null)
            studentId = SessionManager.Instance.StudentId;

        categoryId = CurrentClassSession.SelectedCategoryId;

        string url = $"{apiBaseUrl}/api/questions?student_id={studentId}&category_id={categoryId}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string raw = request.downloadHandler.text;
                List<MCQuestion> parsed = ParseApiQuestions(raw);
                questions = MapToQuestionData(parsed);
                Debug.Log($"[QuizManager] Loaded {questions.Count} teacher question(s) from API.");
            }
            else
                Debug.LogWarning($"[QuizManager] API failed: {request.error}");
        }
    }

    private List<MCQuestion> ParseApiQuestions(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<MCQuestion>();

        string raw = json.Trim();
        try
        {
            if (raw.StartsWith("["))
                return JsonUtilityWrapper.FromJsonList<MCQuestion>(raw) ?? new List<MCQuestion>();

            MCQuestionEnvelope envelope = JsonUtility.FromJson<MCQuestionEnvelope>(raw);
            if (envelope != null)
            {
                if (envelope.questions != null && envelope.questions.Count > 0)
                    return envelope.questions;
                if (envelope.items != null && envelope.items.Count > 0)
                    return envelope.items;
                if (envelope.data != null && envelope.data.Count > 0)
                    return envelope.data;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("WalkingQuizManagerUI: Parse API questions failed: " + ex.Message);
        }

        return new List<MCQuestion>();
    }

    private List<QuestionData> MapToQuestionData(List<MCQuestion> source)
    {
        List<QuestionData> mapped = new List<QuestionData>();
        if (source == null)
            return mapped;

        for (int i = 0; i < source.Count; i++)
        {
            MCQuestion q = source[i];
            if (q == null)
                continue;

            List<MCAnswer> options = q.choices != null && q.choices.Count > 0
                ? q.choices
                : (q.answers ?? new List<MCAnswer>());
            if (options.Count == 0)
                continue;

            string[] answerTexts = new string[options.Count];
            int detectedCorrect = 0;
            for (int a = 0; a < options.Count; a++)
            {
                MCAnswer opt = options[a];
                answerTexts[a] = opt != null ? (opt.answer_description ?? string.Empty) : string.Empty;
                if (opt != null && opt.correct_answer == 1)
                    detectedCorrect = a;
            }

            mapped.Add(new QuestionData
            {
                id = q.id,
                question = string.IsNullOrWhiteSpace(q.question_description) ? "Question unavailable" : q.question_description,
                answers = answerTexts,
                correctIndex = Mathf.Clamp(detectedCorrect, 0, Mathf.Max(0, answerTexts.Length - 1)),
                tutorial_link = q.tutorial_link,
                wrong_minigame = q.wrong_minigame,
                punishment_minigame = q.punishment_minigame
            });
        }

        return mapped;
    }

    private void LoadTestQuestions()
    {
        questions.Clear();
        questions.Add(new QuestionData { id=1, question="What is 2 + 2?",           answers=new[]{"3","4","5","6"},                    correctIndex=1 });
        questions.Add(new QuestionData { id=2, question="What color is the sky?",    answers=new[]{"Green","Red","Blue","Yellow"},       correctIndex=2 });
        questions.Add(new QuestionData { id=3, question="How many days in a week?",  answers=new[]{"5","6","7","8"},                    correctIndex=2 });
        questions.Add(new QuestionData { id=4, question="Capital of Japan?",         answers=new[]{"Seoul","Beijing","Tokyo","Bangkok"}, correctIndex=2 });
        questions.Add(new QuestionData { id=5, question="Which animal says 'meow'?", answers=new[]{"Dog","Cat","Bird","Fish"},           correctIndex=1 });
        Debug.Log($"[QuizManager] Loaded {questions.Count} test questions");
    }

    private void ShowQuestion(int index)
    {
        if (index >= questions.Count) { ShowFinishPanel(); return; }

        currentQuestionIndex = index;
        QuestionData q = questions[index];

        if (questionText != null)  questionText.text  = q.question;
        if (progressText != null)  progressText.text  = $"Question {index + 1}/{totalQuestions}";

        for (int i = 0; i < answerBoxes.Length && i < q.answers.Length; i++)
        {
            if (answerBoxes[i] != null)
            {
                answerBoxes[i].SetAnswerText(q.answers[i]);
                answerBoxes[i].SetCorrect(i == q.correctIndex);
                answerBoxes[i].SetAnswerIndex(i);
            }
        }

        ChangeBackgroundTheme(index);
    }

    private void ChangeBackgroundTheme(int questionIndex)
    {
        if (backgroundThemes == null || backgroundThemes.Length == 0) return;
        int themeIndex = questionIndex % backgroundThemes.Length;
        for (int i = 0; i < backgroundThemes.Length; i++)
            if (backgroundThemes[i] != null)
                backgroundThemes[i].SetActive(i == themeIndex);
    }

    /// <summary>Called by AnswerBoxUI when the player taps a box.</summary>
    public void ShowConfirmationDialog(AnswerBoxUI answerBox)
    {
        if (!isQuizActive) return;

        if (selectedAnswerBox != null) selectedAnswerBox.SetHighlight(false);
        selectedAnswerBox = answerBox;
        selectedAnswerBox.SetHighlight(true);

        if (confirmationText != null)
            confirmationText.text = "Are you sure you want to choose this answer?";

        if (selectedAnswerPreview != null)
            selectedAnswerPreview.text = answerBox.GetAnswerText();

        if (selectedAnswerColor != null && answerBox.GetComponent<Image>() != null)
            selectedAnswerColor.color = answerBox.GetComponent<Image>().color;

        if (confirmationPanel != null) confirmationPanel.SetActive(true);

        Debug.Log($"[QuizManager] Confirmation for: {answerBox.GetAnswerText()}");
    }

    private void OnYesClicked()
    {
        if (selectedAnswerBox == null) return;
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        selectedAnswerBox.SetHighlight(false);

        QuestionData currentQuestion = (currentQuestionIndex >= 0 && currentQuestionIndex < questions.Count)
            ? questions[currentQuestionIndex]
            : null;
        string selectedAnswer = selectedAnswerBox.GetAnswerText();
        string correctAnswer = GetCorrectAnswerText(currentQuestion);
        bool isCorrect = selectedAnswerBox.IsCorrect();
        RecordAnswerHistory(currentQuestion, selectedAnswer, correctAnswer, isCorrect);

        if (isCorrect) OnCorrectAnswer();
        else           OnWrongAnswer();
    }

    private void OnNoClicked()
    {
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (selectedAnswerBox != null) selectedAnswerBox.SetHighlight(false);
        selectedAnswerBox = null;
    }

    private void OnCorrectAnswer()
    {
        score++;
        if (correctPanel != null) correctPanel.SetActive(true);
        Debug.Log($"[QuizManager] Correct! {score}/{totalQuestions}");
    }

    private void OnWrongAnswer()
    {
        Debug.Log("[QuizManager] Wrong!");
        StartCoroutine(ShowWrongThenMiniGame());
    }

    private IEnumerator ShowWrongThenMiniGame()
    {
        if (wrongPanel != null) wrongPanel.SetActive(true);
        yield return new WaitForSeconds(wrongPanelDuration);
        if (wrongPanel != null) wrongPanel.SetActive(false);

        // Keep the true next index so final-question wrong answers finish after minigame.
        int nextQuestionIndex = currentQuestionIndex + 1;
        string activeSceneName = SceneManager.GetActiveScene().name;
        MinigameData.SetNextQuestion(activeSceneName, nextQuestionIndex);

        PlayerPrefs.SetString(PacmanManager.ReturnSceneKey, activeSceneName);
        PlayerPrefs.SetInt(PacmanManager.NextQuestionIndexKey, nextQuestionIndex);
        PlayerPrefs.Save();

        QuestionData currentQuestion = (currentQuestionIndex >= 0 && currentQuestionIndex < questions.Count)
            ? questions[currentQuestionIndex]
            : null;
        
        Debug.Log($"[QuizManager] Current question: {(currentQuestion != null ? currentQuestion.question : "NULL")}");
        Debug.Log($"[QuizManager] wrong_minigame: {(currentQuestion != null ? currentQuestion.wrong_minigame : "NULL")}");
        Debug.Log($"[QuizManager] punishment_minigame: {(currentQuestion != null ? currentQuestion.punishment_minigame : "NULL")}");
        
        string sceneName = GetPunishmentSceneForQuestion(currentQuestion);
        Debug.Log($"[QuizManager] Loading scene: {sceneName}");
        
        SceneManager.LoadScene(sceneName);
    }


    private string GetPunishmentSceneForQuestion(QuestionData question)
    {
        string requestedChoice = ResolveWrongMinigameChoice(question);
        Debug.Log($"[DEBUG GetPunishmentSceneForQuestion] Resolved wrong minigame choice: {requestedChoice}");
        string selected = ResolvePunishmentSceneForChoice(requestedChoice);
        Debug.Log($"[DEBUG GetPunishmentSceneForQuestion] ResolvePunishmentSceneForChoice returned: {selected}");
        if (!string.IsNullOrWhiteSpace(selected))
            return selected;

        return GetRandomPunishmentScene();
    }

    private string GetRandomPunishmentScene()
    {
        if (punishmentMinigameScenes == null || punishmentMinigameScenes.Count == 0)
        {
            return "PacmanMinigame";
        }

        List<string> validScenes = new List<string>();
        for (int i = 0; i < punishmentMinigameScenes.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(punishmentMinigameScenes[i]))
            {
                validScenes.Add(punishmentMinigameScenes[i].Trim());
            }
        }

        if (Application.CanStreamedLevelBeLoaded(LegacyFlappySceneName) && !validScenes.Contains(LegacyFlappySceneName))
        {
            validScenes.Add(LegacyFlappySceneName);
        }

        if (Application.CanStreamedLevelBeLoaded(PunishmentSceneName) && !validScenes.Contains(PunishmentSceneName))
        {
            validScenes.Add(PunishmentSceneName);
        }

        if (validScenes.Count == 0)
        {
            return "PacmanMinigame";
        }

        int index = Random.Range(0, validScenes.Count);
        return validScenes[index];
    }

    private string ResolveWrongMinigameChoice(QuestionData question)
    {
        string raw = question != null && !string.IsNullOrWhiteSpace(question.wrong_minigame)
            ? question.wrong_minigame
            : (question != null ? question.punishment_minigame : string.Empty);

        if (string.IsNullOrWhiteSpace(raw))
            return "randomized";

        string normalized = raw.Trim().ToLowerInvariant().Replace("-", "_").Replace(" ", "_");
        if (normalized == "random" || normalized == "randomized" || normalized == "randomise" || normalized == "randomize")
            return "randomized";
        if (normalized == "pacman" || normalized == "pacmanminigame")
            return "pacman";
        if (normalized == "match_the_shape" || normalized == "matchshape" || normalized == "match")
            return "match_the_shape";
        if (normalized == "flappy_bird" || normalized == "flappy" || normalized == "gamescreen" || normalized == "flappyminigame")
            return "flappy_bird";
        if (normalized == "memory_button" || normalized == "memory_buttons" || normalized == "memory" || normalized == "memorybuttonsminigame")
            return "memory_button";
        return "randomized";
    }

    private string ResolvePunishmentSceneForChoice(string choice)
    {
        Debug.Log($"[DEBUG ResolvePunishmentSceneForChoice] Input choice: '{choice}'");
        if (string.IsNullOrWhiteSpace(choice) || string.Equals(choice, "randomized", System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"[DEBUG ResolvePunishmentSceneForChoice] Choice is null/empty/randomized, returning empty");
            return string.Empty;
        }

        string[] desiredScenes;
        if (choice == "pacman") desiredScenes = new[] { "PacmanMinigame" };
        else if (choice == "match_the_shape") desiredScenes = new[] { "MatchTheShape" };
        else if (choice == "flappy_bird") desiredScenes = new[] { DefaultFlappySceneName, LegacyFlappySceneName, "FlappyMinigame" };
        else if (choice == "memory_button") desiredScenes = new[] { "MemoryButtonsMinigame" };
        else desiredScenes = System.Array.Empty<string>();

        Debug.Log($"[DEBUG ResolvePunishmentSceneForChoice] Desired scenes for '{choice}': {string.Join(", ", desiredScenes)}");

        for (int d = 0; d < desiredScenes.Length; d++)
        {
            string desired = desiredScenes[d];
            if (string.IsNullOrWhiteSpace(desired))
                continue;

            if (punishmentMinigameScenes != null)
            {
                for (int i = 0; i < punishmentMinigameScenes.Count; i++)
                {
                    if (string.Equals(punishmentMinigameScenes[i], desired, System.StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log($"[DEBUG ResolvePunishmentSceneForChoice] Found in punishmentMinigameScenes: {punishmentMinigameScenes[i]}");
                        return punishmentMinigameScenes[i];
                    }
                }
            }

            if (Application.CanStreamedLevelBeLoaded(desired))
            {
                Debug.Log($"[DEBUG ResolvePunishmentSceneForChoice] Scene can be streamed: {desired}");
                return desired;
            }
        }

        Debug.Log($"[DEBUG ResolvePunishmentSceneForChoice] No scene found, returning empty");
        return string.Empty;
    }

    private void OnMiniGameComplete()
    {
        Debug.Log("[QuizManager] Mini-game complete!");
    }

    private void OnProceedClicked()
    {
        if (correctPanel != null) correctPanel.SetActive(false);
        ProceedToNextQuestion();
    }

    public void ProceedToNextQuestion()
    {
        currentQuestionIndex++;
        if (currentQuestionIndex >= questions.Count)
            ShowFinishPanel();
        else
            ShowQuestion(currentQuestionIndex);
    }

    private void ShowFinishPanel()
    {
        isQuizActive = false;
        if (scoreText != null)   scoreText.text = $"Your Score: {score}/{totalQuestions}";
        if (finishPanel != null) finishPanel.SetActive(true);
        Debug.Log($"[QuizManager] Complete! {score}/{totalQuestions}");

        if (!completionRecorded)
        {
            int elapsedSeconds = QuizSessionTimer.GetElapsedSeconds(timerSessionKey);
            int globalScore = totalQuestions > 0 ? Mathf.RoundToInt((score / (float)totalQuestions) * 100f) : 0;

            int studentId = SessionManager.Instance != null ? SessionManager.Instance.StudentId : 0;
            string studentName = SessionManager.Instance != null ? SessionManager.Instance.StudentDisplayName : string.Empty;
            studentName = SessionManager.ToFirstAndLastName(studentName);
            if (string.IsNullOrWhiteSpace(studentName))
            {
                studentName = "Student";
            }

            LeaderboardStore.UpsertCompletion(studentId.ToString(), studentName, globalScore, elapsedSeconds);
            completionRecorded = true;
            QuizSessionTimer.EndSession(timerSessionKey);
        }

        StartCoroutine(SubmitScore());
    }

    private IEnumerator SubmitScore()
    {
        int studentId = ResolveStudentId();
        int assignmentId = ResolveAssignmentId();
        int total = Mathf.Max(totalQuestions, 1);
        int percentageScore = Mathf.Clamp(Mathf.RoundToInt((score / (float)total) * 100f), 0, 100);

        PlayerPrefs.SetInt("PlayerScore", percentageScore);
        PlayerPrefs.SetInt("TotalQuestions", total);
        PlayerPrefs.Save();

        HistoryLocalStore.AddEntry(
            studentId,
            "Quiz completion",
            $"{score}/{total} ({percentageScore}%)",
            "Completed",
            true);

        if (studentId <= 0 || assignmentId <= 0)
            yield break;

        string baseApi = ResolveApiBaseUrl();

        WWWForm scoreForm = new WWWForm();
        scoreForm.AddField("student_id", studentId);
        scoreForm.AddField("assignment_id", assignmentId);
        scoreForm.AddField("score", score);
        scoreForm.AddField("total_points", total);

        bool submitted = false;
        using (UnityWebRequest request = UnityWebRequest.Post(baseApi + "/submit_score2", scoreForm))
        {
            yield return request.SendWebRequest();
            submitted = LegacyApiResponseValidator.WasSuccessful(request, "WalkingQuizManagerUI.submit_score2");
        }

        if (!submitted)
        {
            using (UnityWebRequest fallback = UnityWebRequest.Post(baseApi + "/submit_score", scoreForm))
            {
                yield return fallback.SendWebRequest();
                LegacyApiResponseValidator.WasSuccessful(fallback, "WalkingQuizManagerUI.submit_score");
            }
        }

        WWWForm summaryHistory = new WWWForm();
        summaryHistory.AddField("student_id", studentId);
        summaryHistory.AddField("assignment_id", assignmentId);
        summaryHistory.AddField("question_id", 0);
        summaryHistory.AddField("question_text", "Quiz completion");
        summaryHistory.AddField("student_answer", $"{score}/{total} ({percentageScore}%)");
        summaryHistory.AddField("correct_answer", "Completed");
        summaryHistory.AddField("is_correct", 1);

        using (UnityWebRequest request = UnityWebRequest.Post(baseApi + "/save_history", summaryHistory))
        {
            yield return request.SendWebRequest();
        }
    }

    private void OnReturnClicked()
    {
        QuizSessionTimer.EndSession(timerSessionKey);
        UnityEngine.SceneManagement.SceneManager.LoadScene("Classroom");
    }

    private string BuildTimerSessionKey(int studentId, int assignmentId)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        return $"{sceneName}_student{studentId}_assignment{assignmentId}";
    }

    private void RecordAnswerHistory(QuestionData question, string selectedAnswer, string correctAnswer, bool isCorrect)
    {
        int studentId = ResolveStudentId();
        int assignmentId = ResolveAssignmentId();
        if (question == null || studentId <= 0)
            return;

        string questionTextValue = string.IsNullOrWhiteSpace(question.question) ? "Question" : question.question;
        HistoryLocalStore.AddEntry(studentId, questionTextValue, selectedAnswer ?? string.Empty, correctAnswer ?? string.Empty, isCorrect);
        StartCoroutine(PostAnswerHistory(studentId, assignmentId, question.id, questionTextValue, selectedAnswer ?? string.Empty, correctAnswer ?? string.Empty, isCorrect ? 1 : 0));
    }

    private IEnumerator PostAnswerHistory(int studentId, int assignmentId, int questionId, string questionTextValue, string selectedAnswer, string correctAnswer, int isCorrect)
    {
        if (studentId <= 0 || assignmentId <= 0)
            yield break;

        WWWForm form = new WWWForm();
        form.AddField("student_id", studentId);
        form.AddField("assignment_id", assignmentId);
        form.AddField("question_id", Mathf.Max(0, questionId));
        form.AddField("question_text", questionTextValue ?? string.Empty);
        form.AddField("student_answer", selectedAnswer ?? string.Empty);
        form.AddField("correct_answer", correctAnswer ?? string.Empty);
        form.AddField("is_correct", isCorrect);

        using (UnityWebRequest request = UnityWebRequest.Post(ResolveApiBaseUrl() + "/save_history", form))
        {
            yield return request.SendWebRequest();
        }
    }

    private string GetCorrectAnswerText(QuestionData question)
    {
        if (question == null || question.answers == null || question.correctIndex < 0 || question.correctIndex >= question.answers.Length)
            return string.Empty;

        return question.answers[question.correctIndex] ?? string.Empty;
    }

    private int ResolveStudentId()
    {
        if (SessionManager.Instance != null && SessionManager.Instance.StudentId > 0)
            return SessionManager.Instance.StudentId;

        int id = PlayerPrefs.GetInt("SESSION_STUDENT_ID", 0);
        if (id > 0)
            return id;

        id = PlayerPrefs.GetInt("StudentID", 0);
        if (id > 0)
            return id;

        return PlayerPrefs.GetInt("student_id", 0);
    }

    private int ResolveAssignmentId()
    {
        if (CurrentClassSession.SelectedCategoryId > 0)
            return CurrentClassSession.SelectedCategoryId;

        int id = PlayerPrefs.GetInt("CategoryId", 0);
        if (id > 0)
            return id;

        id = PlayerPrefs.GetInt("AssignmentID", 0);
        if (id > 0)
            return id;

        return 0;
    }

    private string ResolveApiBaseUrl()
    {
        if (!string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            string trimmed = apiBaseUrl.Trim().TrimEnd('/');
            if (!string.Equals(trimmed, "https://your-api.com", System.StringComparison.OrdinalIgnoreCase))
                return trimmed;
        }

        string configured = PlayerPrefs.GetString("ApiBaseUrl", string.Empty);
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.Trim().TrimEnd('/');

        return "https://homequest-c3k7.onrender.com";
    }
}
