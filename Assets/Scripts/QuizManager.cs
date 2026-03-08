using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System;
using System.Text;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

[System.Serializable]
public class QuizApiAnswer
{
    public string answer_description;
    public int correct_answer;
}

[System.Serializable]
public class QuizApiQuestion
{
    public int id;
    public int assignment_id;
    public string question_description;
    public string tutorial_link;
    public List<QuizApiAnswer> answers;
}

[System.Serializable]
public class QuizApiQuestionEnvelope
{
    public List<QuizApiQuestion> questions;
    public List<QuizApiQuestion> items;
    public List<QuizApiQuestion> data;
}

[System.Serializable]
public class SelectedActivityQuestion
{
    public int id;
    public string question_text;
    public string question_type;
    public int points;
    public string help_video_url;
    public List<string> options;
    public int correct_answer_index;
    public List<string> correct_answers;
}

[System.Serializable]
public class SelectedActivityQuestionWrapper
{
    public List<SelectedActivityQuestion> items;
}

[System.Serializable]
public class AssignmentEndpointResponse
{
    public string status;
    public List<SelectedActivityQuestion> questions;
}

public class QuizManager : MonoBehaviour
{
    [Header("Answer Buttons")]
    public Button answerA;
    public Button answerB;
    public Button answerC;
    public Button answerD;

    [Header("Answer Button Images")]
    public Image imgA;
    public Image imgB;
    public Image imgC;
    public Image imgD;

    [Header("Question UI")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI questionNum;

    [Header("Panels")]
    public GameObject confirmPanel;
    public TextMeshProUGUI confirmLabel;
    public Button confirmYes;
    public Button confirmNo;

    public GameObject resultPanel;
    public GameObject correctPanel;
    public GameObject wrongPanel;
    public Button correctProceedButton;

    [Header("Timer")]
    public TimerManager timerManager;

    [Header("Help")]
    public Button helpButton;

    [Header("Navigation")]
    public Button backButton;
    public string mapSceneName = "NewMap";

    [Header("Animations & Effects")]
    public ParticleSystem confettiParticles;
    public Animator correctTextAnimator;
    public ParticleSystem sadSparkles;
    public Animator wrongTextAnimator;

    [Header("Mini Games")]
    public string[] miniGameSceneNames = { "MemoryButtonsMinigame", "PacmanMinigame" };

    [Header("Story Intro")]
    [SerializeField] private bool enableStoryIntro = true;
    [SerializeField] private float storyFadeDuration = 0.22f;
    [SerializeField] private float storyTypeSpeed = 0.02f;

    // State
    private string selectedLetter = "";
    private int selectedIndex = -1;
    private int correctIndex = 0; // 0 = A, 1 = B, 2 = C, 3 = D

    private Color[] originalColors;
    private Button[] allButtons;
    private Image[] allImages;
    private readonly string[] letters = { "A", "B", "C", "D" };
    private readonly List<QuizApiQuestion> questions = new List<QuizApiQuestion>();
    private int currentQuestionIndex = 0;
    private int correctAnswerCount = 0;
    private int studentId = 0;
    private int assignmentId = 0;
    private string currentTutorialLink = "";
    private string timerSessionKey = string.Empty;
    private bool completionSubmitted = false;
    private bool isStoryIntroActive = false;
    private int storyIntroIndex = 0;
    private bool storyLineFullyShown = false;
    private readonly List<string> storyIntroLines = new List<string>();
    private Coroutine storyTypingCoroutine;
    private Coroutine storyPromptBlinkCoroutine;
    private GameObject storyIntroPanel;
    private CanvasGroup storyIntroCanvasGroup;
    private TextMeshProUGUI storyIntroTitleText;
    private TextMeshProUGUI storyIntroBodyText;
    private TextMeshProUGUI storyIntroHintText;
    private TextMeshProUGUI storyLineCounterText;
    private Button storyNextButton;
    private Button storyStartButton;
    private TMP_FontAsset storyFontAsset;
    private Material storyFontMaterial;
    private const string LastMiniGameSceneKey = "Quiz_LastMiniGameScene";

    void Start()
    {
        allButtons = new Button[] { answerA, answerB, answerC, answerD };
        allImages  = new Image[]  { imgA, imgB, imgC, imgD };
        originalColors = new Color[4];
        for (int i = 0; i < 4; i++)
            originalColors[i] = allImages[i].color;

        answerA.onClick.AddListener(() => OnAnswerSelected(0));
        answerB.onClick.AddListener(() => OnAnswerSelected(1));
        answerC.onClick.AddListener(() => OnAnswerSelected(2));
        answerD.onClick.AddListener(() => OnAnswerSelected(3));

        confirmYes.onClick.AddListener(OnConfirmYes);
        confirmNo.onClick.AddListener(OnConfirmNo);

        if (helpButton != null)
            helpButton.onClick.AddListener(OpenCurrentTutorial);

        if (correctProceedButton == null)
        {
            if (correctPanel != null)
                correctProceedButton = correctPanel.transform.Find("NextButton")?.GetComponent<Button>();

            if (correctProceedButton == null && resultPanel != null)
                correctProceedButton = resultPanel.transform.Find("CorrectPanel/NextButton")?.GetComponent<Button>();
        }

        if (correctProceedButton != null)
        {
            correctProceedButton.onClick.RemoveListener(OnNextQuestion);
            correctProceedButton.onClick.AddListener(OnNextQuestion);
        }
        else
        {
            Debug.LogWarning("QuizManager: Correct proceed button was not found. 'PROCEED' cannot advance questions.");
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(GoToMap);
            AddScaleAnimation(backButton.gameObject);
        }

        // Auto-wire animations if not set in Inspector
        if (confettiParticles == null && correctPanel != null)
            confettiParticles = correctPanel.transform.Find("ConfettiParticles")?.GetComponent<ParticleSystem>();
        
        if (correctTextAnimator == null && correctPanel != null)
            correctTextAnimator = correctPanel.transform.Find("CorrectText")?.GetComponent<Animator>();
        
        if (sadSparkles == null && wrongPanel != null)
            sadSparkles = wrongPanel.transform.Find("SadSparkles")?.GetComponent<ParticleSystem>();
        
        if (wrongTextAnimator == null && wrongPanel != null)
            wrongTextAnimator = wrongPanel.transform.Find("WrongText")?.GetComponent<Animator>();

        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (resultPanel != null)  resultPanel.SetActive(false);
        if (correctPanel != null) correctPanel.SetActive(false);
        if (wrongPanel != null)   wrongPanel.SetActive(false);

        studentId = ResolveStudentId();
        assignmentId = ResolveAssignmentId();
        timerSessionKey = BuildTimerSessionKey();
        Debug.Log($"QuizManager: resolved studentId={studentId}, assignmentId={assignmentId}");
        StartCoroutine(LoadTeacherQuestions());
    }

    IEnumerator LoadTeacherQuestions()
    {
        if (studentId <= 0 || assignmentId <= 0)
        {
            SetNoQuestionsState("Missing session/activity context. Please re-open the teacher activity.");
            yield break;
        }

        string baseApi = ResolveApiBaseUrl();
        string[] urls =
        {
            baseApi + $"/get_questions?student_id={studentId}&assignment_id={assignmentId}",
            baseApi + $"/api/questions?student_id={studentId}&category_id={assignmentId}"
        };

        string lastError = "";
        for (int i = 0; i < urls.Length && questions.Count == 0; i++)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(urls[i]))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    lastError = request.error;
                    continue;
                }

                List<QuizApiQuestion> parsed = ParseQuestions(request.downloadHandler.text);
                Debug.Log($"QuizManager: {urls[i]} -> parsed {parsed.Count} question(s)");
                if (parsed != null && parsed.Count > 0)
                {
                    questions.Clear();
                    questions.AddRange(parsed);
                    break;
                }
            }
        }

        if (questions.Count == 0)
        {
            yield return StartCoroutine(LoadQuestionsFromAssignmentEndpoint(baseApi));
        }

        if (questions.Count == 0)
        {
            List<QuizApiQuestion> cached = LoadQuestionsFromSelectedActivityCache();
            if (cached != null && cached.Count > 0)
            {
                questions.Clear();
                questions.AddRange(cached);
            }
        }

        if (questions.Count == 0)
        {
            SetNoQuestionsState("No teacher questions found for this activity.");
            if (!string.IsNullOrWhiteSpace(lastError))
                Debug.LogWarning("QuizManager: question request failed: " + lastError);
            yield break;
        }

        int resumeIndex = PlayerPrefs.GetInt(PacmanManager.ResumeQuestionIndexKey, -1);
        if (resumeIndex >= 0)
        {
            // Allow questions.Count as a sentinel meaning "quiz finished while in minigame".
            currentQuestionIndex = Mathf.Clamp(resumeIndex, 0, questions.Count);
            PlayerPrefs.DeleteKey(PacmanManager.ResumeQuestionIndexKey);
            PlayerPrefs.Save();
            Debug.Log($"QuizManager: resuming at question index {currentQuestionIndex}");
        }
        else
        {
            currentQuestionIndex = 0;
        }

        QuizSessionTimer.StartOrResume(timerSessionKey);
        if (timerManager != null)
            timerManager.BindSession(timerSessionKey);

        if (enableStoryIntro && currentQuestionIndex == 0)
            StartStoryIntro();
        else
            ShowCurrentQuestion();
    }

    IEnumerator LoadQuestionsFromAssignmentEndpoint(string baseApi)
    {
        if (assignmentId <= 0)
            yield break;

        string url = baseApi + "/assignment/" + assignmentId;
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("QuizManager: assignment endpoint failed: " + request.error);
                yield break;
            }

            List<QuizApiQuestion> mapped = ParseQuestionsFromAssignmentEndpoint(request.downloadHandler.text);
            Debug.Log($"QuizManager: {url} -> parsed {mapped.Count} question(s)");
            if (mapped.Count > 0)
            {
                questions.Clear();
                questions.AddRange(mapped);
            }
        }
    }

    // ── Scale animation on press/release ───────────────────────────────
    void AddScaleAnimation(GameObject go)
    {
        var trigger = go.GetComponent<EventTrigger>() ?? go.AddComponent<EventTrigger>();

        var pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        pointerDown.callback.AddListener(_ => go.transform.localScale = Vector3.one * 1.15f);
        trigger.triggers.Add(pointerDown);

        var pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        pointerUp.callback.AddListener(_ => go.transform.localScale = Vector3.one);
        trigger.triggers.Add(pointerUp);

        var pointerExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        pointerExit.callback.AddListener(_ => go.transform.localScale = Vector3.one);
        trigger.triggers.Add(pointerExit);
    }

    public void GoToMap()
    {
        QuizSessionTimer.EndSession(timerSessionKey);
        SceneManager.LoadScene(mapSceneName);
    }

    void OnAnswerSelected(int index)
    {
        if (isStoryIntroActive)
            return;

        selectedIndex = index;
        selectedLetter = letters[index];

        for (int i = 0; i < 4; i++)
        {
            if (i == index)
            {
                allButtons[i].transform.localScale = Vector3.one * 1.1f;
                Color c = allImages[i].color;
                c.a = 1f;
                allImages[i].color = c;
            }
            else
            {
                allButtons[i].transform.localScale = Vector3.one * 0.9f;
                Color c = allImages[i].color;
                c.a = 0.7f;
                allImages[i].color = c;
            }
        }

        if (confirmPanel != null)
        {
            confirmPanel.SetActive(true);
            if (confirmLabel != null)
                confirmLabel.text = "Confirm answer " + selectedLetter + "?";
        }
    }

    void OnConfirmNo()
    {
        for (int i = 0; i < 4; i++)
        {
            allButtons[i].transform.localScale = Vector3.one;
            allImages[i].color = originalColors[i];
        }
        if (confirmPanel != null) confirmPanel.SetActive(false);
        selectedIndex = -1;
    }

    void OnConfirmYes()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);

        if (resultPanel != null) resultPanel.SetActive(true);

        bool isCorrect = (selectedIndex == correctIndex);
        QuizApiQuestion currentQuestion = (currentQuestionIndex >= 0 && currentQuestionIndex < questions.Count)
            ? questions[currentQuestionIndex]
            : null;

        string selectedAnswerText = GetSelectedAnswerText(currentQuestion, selectedIndex);
        string correctAnswerText = GetCorrectAnswerText(currentQuestion);
        RecordAnswerHistory(currentQuestion, selectedAnswerText, correctAnswerText, isCorrect);

        if (isCorrect)
        {
            correctAnswerCount++;
            // ★ CORRECT ANSWER CELEBRATION ★
            if (correctPanel != null) correctPanel.SetActive(true);
            if (wrongPanel != null)   wrongPanel.SetActive(false);

            // Play confetti particles
            if (confettiParticles != null)
                confettiParticles.Play();

            // Play bounce animation
            if (correctTextAnimator != null)
                correctTextAnimator.Play("CelebrateBounce", 0, 0f);
        }
        else
        {
            // ✗ WRONG ANSWER FEEDBACK ✗
            if (wrongPanel != null)   wrongPanel.SetActive(true);
            if (correctPanel != null) correctPanel.SetActive(false);

            // Play sad sparkles
            if (sadSparkles != null)
                sadSparkles.Play();

            // Play shake animation
            if (wrongTextAnimator != null)
                wrongTextAnimator.Play("SadShake", 0, 0f);

            // Load mini game after delay
            StartCoroutine(LoadMiniGameAfterDelay(2f));
        }
    }

    IEnumerator LoadMiniGameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        string[] candidates = BuildMiniGameCandidates();
        if (candidates.Length == 0)
        {
            Debug.LogError("No minigame scenes configured on QuizManager.");
            yield break;
        }

        // Save return context so minigame ending can send player back to this quiz.
        // For the final question, this intentionally stores questions.Count so resume can finish.
        int nextQuestionIndex = currentQuestionIndex + 1;
        PlayerPrefs.SetString(PacmanManager.ReturnSceneKey, SceneManager.GetActiveScene().name);
        PlayerPrefs.SetInt(PacmanManager.NextQuestionIndexKey, nextQuestionIndex);
        PlayerPrefs.Save();

        string selectedMiniGameScene = PickRandomMiniGame(candidates);
        PlayerPrefs.SetString(LastMiniGameSceneKey, selectedMiniGameScene);
        PlayerPrefs.Save();

#if UNITY_EDITOR
        // In editor play mode, allow loading by scene asset path even if Build Profiles are not configured yet.
        if (!Application.CanStreamedLevelBeLoaded(selectedMiniGameScene))
        {
            string editorScenePath = "Assets/Scenes/" + selectedMiniGameScene + ".unity";
            if (System.IO.File.Exists(editorScenePath))
            {
                EditorSceneManager.LoadSceneInPlayMode(editorScenePath, new LoadSceneParameters(LoadSceneMode.Single));
                yield break;
            }
        }
#endif

        SceneManager.LoadScene(selectedMiniGameScene);
    }

    private string[] BuildMiniGameCandidates()
    {
        List<string> candidates = new List<string>();

        if (miniGameSceneNames != null)
        {
            for (int i = 0; i < miniGameSceneNames.Length; i++)
            {
                string sceneName = (miniGameSceneNames[i] ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(sceneName))
                    continue;

                if (!candidates.Contains(sceneName))
                    candidates.Add(sceneName);
            }
        }

        // Ensure at least two options for randomization in case inspector only has one scene.
        string[] defaults = { "MemoryButtonsMinigame", "PacmanMinigame" };
        for (int i = 0; i < defaults.Length && candidates.Count < 2; i++)
        {
            if (!candidates.Contains(defaults[i]))
                candidates.Add(defaults[i]);
        }

        return candidates.ToArray();
    }

    private string PickRandomMiniGame(string[] candidates)
    {
        if (candidates == null || candidates.Length == 0)
            return string.Empty;

        if (candidates.Length == 1)
            return candidates[0];

        string lastScene = PlayerPrefs.GetString(LastMiniGameSceneKey, string.Empty);
        int guard = 0;
        string pick = candidates[UnityEngine.Random.Range(0, candidates.Length)];

        while (string.Equals(pick, lastScene, StringComparison.OrdinalIgnoreCase) && guard < 8)
        {
            pick = candidates[UnityEngine.Random.Range(0, candidates.Length)];
            guard++;
        }

        return pick;
    }

    public void OnNextQuestion()
    {
        if (resultPanel != null)  resultPanel.SetActive(false);
        if (correctPanel != null) correctPanel.SetActive(false);
        if (wrongPanel != null)   wrongPanel.SetActive(false);

        for (int i = 0; i < 4; i++)
        {
            allButtons[i].transform.localScale = Vector3.one;
            allImages[i].color = originalColors[i];
        }
        selectedIndex = -1;

        if (questions.Count == 0)
            return;

        currentQuestionIndex++;
        if (currentQuestionIndex >= questions.Count)
        {
            questionText.text = "Activity complete!";
            if (questionNum != null)
                questionNum.text = $"Question {questions.Count}/{questions.Count}";
            SetAnswerButtonState(false);
            StartCoroutine(SubmitCompletionAndLoadResult());
            return;
        }

        ShowCurrentQuestion();
    }

    public void SetCorrectAnswer(int index) => correctIndex = index;

    void ShowCurrentQuestion()
    {
        if (questions.Count == 0 || currentQuestionIndex < 0)
        {
            SetNoQuestionsState("No teacher questions found for this activity.");
            return;
        }

        if (currentQuestionIndex >= questions.Count)
        {
            // Completed via minigame return path.
            questionText.text = "Activity complete!";
            if (questionNum != null)
                questionNum.text = $"Question {questions.Count}/{questions.Count}";
            SetAnswerButtonState(false);
            StartCoroutine(SubmitCompletionAndLoadResult());
            return;
        }

        QuizApiQuestion question = questions[currentQuestionIndex];
        if (questionText != null)
            questionText.text = string.IsNullOrWhiteSpace(question.question_description)
                ? "Question unavailable"
                : question.question_description;

        if (questionNum != null)
            questionNum.text = $"Question {currentQuestionIndex + 1}/{questions.Count}";

        currentTutorialLink = question.tutorial_link ?? "";
        if (helpButton != null)
            helpButton.gameObject.SetActive(!string.IsNullOrWhiteSpace(currentTutorialLink));

        SetAnswerButtonState(true);
        BindAnswers(question);

        if (timerManager != null)
            timerManager.BindSession(timerSessionKey);
    }

    private string BuildTimerSessionKey()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        return $"{sceneName}_student{studentId}_assignment{assignmentId}";
    }

    void BindAnswers(QuizApiQuestion question)
    {
        List<QuizApiAnswer> answers = question != null ? question.answers : null;
        if (answers == null)
            answers = new List<QuizApiAnswer>();

        correctIndex = 0;
        for (int i = 0; i < allButtons.Length; i++)
        {
            Button button = allButtons[i];
            if (button == null)
                continue;

            bool hasAnswer = i < answers.Count;
            button.interactable = hasAnswer;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = hasAnswer ? (answers[i].answer_description ?? "") : "";
            }

            if (hasAnswer && answers[i] != null && answers[i].correct_answer == 1)
                correctIndex = i;

            button.transform.localScale = Vector3.one;
            if (i < allImages.Length && allImages[i] != null)
                allImages[i].color = originalColors[i];
        }

        selectedIndex = -1;
        selectedLetter = "";
    }

    void SetNoQuestionsState(string message)
    {
        if (questionText != null)
            questionText.text = message;
        if (questionNum != null)
            questionNum.text = "Question 0/0";

        SetAnswerButtonState(false);
    }

    void SetAnswerButtonState(bool enabled)
    {
        for (int i = 0; i < allButtons.Length; i++)
        {
            if (allButtons[i] != null)
                allButtons[i].interactable = enabled;
        }
    }

    private List<QuizApiQuestion> ParseQuestions(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<QuizApiQuestion>();

        string raw = json.Trim();
        try
        {
            if (raw.StartsWith("["))
                return JsonUtilityWrapper.FromJsonList<QuizApiQuestion>(raw) ?? new List<QuizApiQuestion>();

            QuizApiQuestionEnvelope envelope = JsonUtility.FromJson<QuizApiQuestionEnvelope>(raw);
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
            Debug.LogWarning("QuizManager: failed to parse questions: " + ex.Message);
        }

        return new List<QuizApiQuestion>();
    }

    private List<QuizApiQuestion> LoadQuestionsFromSelectedActivityCache()
    {
        string raw = PlayerPrefs.GetString("SelectedActivityQuestionsJson", string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
            return new List<QuizApiQuestion>();

        try
        {
            SelectedActivityQuestionWrapper wrapper = JsonUtility.FromJson<SelectedActivityQuestionWrapper>(raw);
            if (wrapper == null || wrapper.items == null || wrapper.items.Count == 0)
                return new List<QuizApiQuestion>();
            return MapSelectedActivityQuestions(wrapper.items);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("QuizManager: failed to parse SelectedActivityQuestionsJson: " + ex.Message);
            return new List<QuizApiQuestion>();
        }
    }

    private List<QuizApiQuestion> ParseQuestionsFromAssignmentEndpoint(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<QuizApiQuestion>();

        try
        {
            AssignmentEndpointResponse payload = JsonUtility.FromJson<AssignmentEndpointResponse>(json);
            if (payload == null || payload.questions == null || payload.questions.Count == 0)
                return new List<QuizApiQuestion>();

            return MapSelectedActivityQuestions(payload.questions);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("QuizManager: failed to parse assignment endpoint payload: " + ex.Message);
            return new List<QuizApiQuestion>();
        }
    }

    private List<QuizApiQuestion> MapSelectedActivityQuestions(List<SelectedActivityQuestion> sourceQuestions)
    {
        List<QuizApiQuestion> mapped = new List<QuizApiQuestion>();
        if (sourceQuestions == null)
            return mapped;

        for (int i = 0; i < sourceQuestions.Count; i++)
        {
            SelectedActivityQuestion source = sourceQuestions[i];
            if (source == null)
                continue;

            List<QuizApiAnswer> answers = new List<QuizApiAnswer>();
            if (source.options != null && source.options.Count > 0)
            {
                for (int optionIndex = 0; optionIndex < source.options.Count; optionIndex++)
                {
                    string optionText = source.options[optionIndex] ?? string.Empty;
                    answers.Add(new QuizApiAnswer
                    {
                        answer_description = optionText,
                        correct_answer = optionIndex == source.correct_answer_index ? 1 : 0
                    });
                }
            }

            if (answers.Count == 0 && source.correct_answers != null)
            {
                for (int answerIndex = 0; answerIndex < source.correct_answers.Count; answerIndex++)
                {
                    string answerText = source.correct_answers[answerIndex] ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(answerText))
                        continue;

                    answers.Add(new QuizApiAnswer
                    {
                        answer_description = answerText,
                        correct_answer = 1
                    });
                }
            }

            if (answers.Count == 0)
                continue;

            mapped.Add(new QuizApiQuestion
            {
                id = source.id,
                assignment_id = assignmentId,
                question_description = string.IsNullOrWhiteSpace(source.question_text) ? "Question unavailable" : source.question_text,
                tutorial_link = source.help_video_url ?? string.Empty,
                answers = answers
            });
        }

        return mapped;
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

        string selectedActivity = PlayerPrefs.GetString("SelectedActivity", string.Empty);
        if (int.TryParse(selectedActivity, out id) && id > 0)
            return id;

        return 0;
    }

    private string ResolveApiBaseUrl()
    {
        string configured = PlayerPrefs.GetString("ApiBaseUrl", string.Empty);
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.Trim().TrimEnd('/');
        return "https://homequest-c3k7.onrender.com";
    }

    private string GetSelectedAnswerText(QuizApiQuestion question, int answerIndex)
    {
        if (question == null || question.answers == null || answerIndex < 0 || answerIndex >= question.answers.Count)
            return string.Empty;

        QuizApiAnswer answer = question.answers[answerIndex];
        return answer != null ? (answer.answer_description ?? string.Empty) : string.Empty;
    }

    private string GetCorrectAnswerText(QuizApiQuestion question)
    {
        if (question == null || question.answers == null)
            return string.Empty;

        for (int i = 0; i < question.answers.Count; i++)
        {
            QuizApiAnswer answer = question.answers[i];
            if (answer != null && answer.correct_answer == 1)
                return answer.answer_description ?? string.Empty;
        }

        return string.Empty;
    }

    private void RecordAnswerHistory(QuizApiQuestion question, string selectedAnswer, string correctAnswer, bool isCorrect)
    {
        if (question == null || studentId <= 0)
            return;

        string questionTextValue = string.IsNullOrWhiteSpace(question.question_description)
            ? "Question"
            : question.question_description;

        HistoryLocalStore.AddEntry(studentId, questionTextValue, selectedAnswer ?? string.Empty, correctAnswer ?? string.Empty, isCorrect);
        StartCoroutine(SaveAnswerToHistory(question.id, questionTextValue, selectedAnswer ?? string.Empty, correctAnswer ?? string.Empty, isCorrect ? 1 : 0));
    }

    private IEnumerator SaveAnswerToHistory(int questionId, string questionTextValue, string studentAnswer, string correctAnswer, int isCorrect)
    {
        if (studentId <= 0 || assignmentId <= 0)
            yield break;

        WWWForm form = new WWWForm();
        form.AddField("student_id", studentId);
        form.AddField("assignment_id", assignmentId);
        form.AddField("question_id", Mathf.Max(0, questionId));
        form.AddField("question_text", questionTextValue ?? string.Empty);
        form.AddField("student_answer", studentAnswer ?? string.Empty);
        form.AddField("correct_answer", correctAnswer ?? string.Empty);
        form.AddField("is_correct", isCorrect);

        using (UnityWebRequest request = UnityWebRequest.Post(ResolveApiBaseUrl() + "/save_history", form))
        {
            yield return request.SendWebRequest();
        }
    }

    private IEnumerator SubmitCompletionAndLoadResult()
    {
        if (completionSubmitted)
            yield break;

        completionSubmitted = true;

        int totalQuestions = Mathf.Max(questions.Count, 1);
        int wrongCount = Mathf.Max(0, totalQuestions - correctAnswerCount);
        int elapsedSeconds = QuizSessionTimer.GetElapsedSeconds(timerSessionKey);
        int percentageScore = Mathf.Clamp(Mathf.RoundToInt((correctAnswerCount / (float)totalQuestions) * 100f), 0, 100);

        PlayerPrefs.SetInt("PlayerScore", percentageScore);
        PlayerPrefs.SetInt("TotalQuestions", totalQuestions);
        PlayerPrefs.Save();

        string studentName = ResolveStudentDisplayName();
        LeaderboardStore.UpsertCompletion(studentId.ToString(), studentName, percentageScore, elapsedSeconds);

        HistoryLocalStore.AddEntry(
            studentId,
            "Quiz completion",
            $"{correctAnswerCount}/{totalQuestions} ({percentageScore}%)",
            "Completed",
            true);

        if (studentId > 0 && assignmentId > 0)
        {
            WWWForm scoreForm = new WWWForm();
            scoreForm.AddField("student_id", studentId);
            scoreForm.AddField("assignment_id", assignmentId);
            scoreForm.AddField("score", correctAnswerCount);
            scoreForm.AddField("total_points", totalQuestions);

            bool submitted = false;
            using (UnityWebRequest request = UnityWebRequest.Post(ResolveApiBaseUrl() + "/submit_score2", scoreForm))
            {
                yield return request.SendWebRequest();
                submitted = LegacyApiResponseValidator.WasSuccessful(request, "QuizManager.submit_score2");
            }

            if (!submitted)
            {
                using (UnityWebRequest fallback = UnityWebRequest.Post(ResolveApiBaseUrl() + "/submit_score", scoreForm))
                {
                    yield return fallback.SendWebRequest();
                    LegacyApiResponseValidator.WasSuccessful(fallback, "QuizManager.submit_score");
                }
            }

            WWWForm summaryHistory = new WWWForm();
            summaryHistory.AddField("student_id", studentId);
            summaryHistory.AddField("assignment_id", assignmentId);
            summaryHistory.AddField("question_id", 0);
            summaryHistory.AddField("question_text", "Quiz completion");
            summaryHistory.AddField("student_answer", $"{correctAnswerCount}/{totalQuestions} ({percentageScore}%)");
            summaryHistory.AddField("correct_answer", "Completed");
            summaryHistory.AddField("is_correct", 1);

            using (UnityWebRequest request = UnityWebRequest.Post(ResolveApiBaseUrl() + "/save_history", summaryHistory))
            {
                yield return request.SendWebRequest();
            }
        }

        QuizSessionTimer.EndSession(timerSessionKey);
        GameResultState.SetResult(correctAnswerCount, totalQuestions, wrongCount, elapsedSeconds, string.Empty, mapSceneName);
        SceneManager.LoadScene("GameResult");
    }

    private string ResolveStudentDisplayName()
    {
        string studentName = SessionManager.Instance != null ? SessionManager.Instance.Username : string.Empty;
        if (string.IsNullOrWhiteSpace(studentName))
            studentName = PlayerPrefs.GetString("SESSION_USERNAME", string.Empty);

        if (string.IsNullOrWhiteSpace(studentName))
            return studentId > 0 ? "Student " + studentId : "Student";

        return studentName.Trim();
    }

    private void OpenCurrentTutorial()
    {
        if (!string.IsNullOrWhiteSpace(currentTutorialLink))
            Application.OpenURL(currentTutorialLink);
    }

    private void StartStoryIntro()
    {
        EnsureStoryIntroUi();
        storyIntroLines.Clear();
        storyIntroLines.AddRange(BuildStoryIntroLines());
        if (storyIntroLines.Count == 0)
        {
            ShowCurrentQuestion();
            return;
        }

        isStoryIntroActive = true;
        storyIntroIndex = 0;
        storyLineFullyShown = false;

        SetGameplayInteractable(false);
        if (helpButton != null)
            helpButton.gameObject.SetActive(false);
        if (confirmPanel != null)
            confirmPanel.SetActive(false);
        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (storyIntroTitleText != null)
            storyIntroTitleText.text = "Story Time";

        StartCoroutine(AnimateStoryIntroPanel(true));
        StartCoroutine(ShowStoryLine());
    }

    private List<string> BuildStoryIntroLines()
    {
        string subject = PlayerPrefs.GetString("SelectedSubject", string.Empty).Trim();
        string studentName = ResolveStudentDisplayName();
        if (string.IsNullOrWhiteSpace(studentName))
            studentName = "Explorer";

        string lessonLine = string.IsNullOrWhiteSpace(subject)
            ? "Today, we are going on a question adventure."
            : $"Today, our adventure is about {subject}.";

        return new List<string>
        {
            SanitizeStoryText($"Hi {studentName}! Welcome to Rainbow Quiz Park!"),
            lessonLine,
            "Each good answer helps your lantern glow brighter.",
            "Take your time, read carefully, and enjoy learning!"
        };
    }

    private IEnumerator ShowStoryLine()
    {
        if (storyIntroIndex < 0 || storyIntroIndex >= storyIntroLines.Count)
            yield break;

        if (storyTypingCoroutine != null)
            StopCoroutine(storyTypingCoroutine);

        storyLineFullyShown = false;
        UpdateStoryFooter(false);

        if (storyIntroBodyText != null)
        {
            storyIntroBodyText.text = string.Empty;
            storyTypingCoroutine = StartCoroutine(TypeStoryLine(storyIntroLines[storyIntroIndex]));
        }

        if (storyLineCounterText != null)
            storyLineCounterText.text = $"{storyIntroIndex + 1} / {storyIntroLines.Count}";

        yield return null;
    }

    private IEnumerator TypeStoryLine(string line)
    {
        if (storyIntroBodyText == null)
            yield break;

        storyIntroBodyText.text = string.Empty;
        float delay = Mathf.Max(0.008f, storyTypeSpeed);
        for (int i = 0; i < line.Length; i++)
        {
            storyIntroBodyText.text += line[i];
            yield return new WaitForSeconds(delay);
        }

        storyTypingCoroutine = null;
        storyLineFullyShown = true;
        UpdateStoryFooter(true);
    }

    private void CompleteStoryLineInstant()
    {
        if (storyIntroIndex < 0 || storyIntroIndex >= storyIntroLines.Count)
            return;

        if (storyTypingCoroutine != null)
        {
            StopCoroutine(storyTypingCoroutine);
            storyTypingCoroutine = null;
        }

        if (storyIntroBodyText != null)
            storyIntroBodyText.text = SanitizeStoryText(storyIntroLines[storyIntroIndex]);

        storyLineFullyShown = true;
        UpdateStoryFooter(true);
    }

    private void OnStoryAdvanceRequested()
    {
        if (!isStoryIntroActive)
            return;

        if (!storyLineFullyShown)
        {
            CompleteStoryLineInstant();
            return;
        }

        if (storyIntroIndex >= storyIntroLines.Count - 1)
            return;

        storyIntroIndex++;
        StartCoroutine(ShowStoryLine());
    }

    private void OnStoryStartRequested()
    {
        if (!isStoryIntroActive)
            return;

        if (!storyLineFullyShown)
        {
            CompleteStoryLineInstant();
            return;
        }

        StartCoroutine(CloseStoryIntroAndBegin());
    }

    private IEnumerator CloseStoryIntroAndBegin()
    {
        isStoryIntroActive = false;
        yield return StartCoroutine(AnimateStoryIntroPanel(false));
        SetGameplayInteractable(true);
        ShowCurrentQuestion();
    }

    private void UpdateStoryFooter(bool lineReady)
    {
        bool lastLine = storyIntroIndex >= storyIntroLines.Count - 1;

        if (storyIntroHintText != null)
        {
            storyIntroHintText.gameObject.SetActive(lineReady && !lastLine);
            storyIntroHintText.text = "Tap to Continue";
        }

        if (storyNextButton != null)
            storyNextButton.gameObject.SetActive(lineReady && !lastLine);

        if (storyStartButton != null)
            storyStartButton.gameObject.SetActive(lineReady && lastLine);

        if (lineReady && !lastLine)
            StartPromptBlink();
        else
            StopPromptBlink();
    }

    private void StartPromptBlink()
    {
        if (storyPromptBlinkCoroutine != null)
            StopCoroutine(storyPromptBlinkCoroutine);

        if (storyIntroHintText != null && storyIntroHintText.gameObject.activeSelf)
            storyPromptBlinkCoroutine = StartCoroutine(BlinkPromptLoop());
    }

    private void StopPromptBlink()
    {
        if (storyPromptBlinkCoroutine != null)
        {
            StopCoroutine(storyPromptBlinkCoroutine);
            storyPromptBlinkCoroutine = null;
        }

        if (storyIntroHintText != null)
        {
            Color color = storyIntroHintText.color;
            color.a = 1f;
            storyIntroHintText.color = color;
        }
    }

    private IEnumerator BlinkPromptLoop()
    {
        while (storyIntroHintText != null && storyIntroHintText.gameObject.activeSelf)
        {
            Color color = storyIntroHintText.color;
            color.a = 0.45f + (Mathf.Sin(Time.unscaledTime * 3.2f) + 1f) * 0.275f;
            storyIntroHintText.color = color;
            yield return null;
        }

        storyPromptBlinkCoroutine = null;
    }

    private IEnumerator AnimateStoryIntroPanel(bool show)
    {
        if (storyIntroCanvasGroup == null)
            yield break;

        if (show)
        {
            storyIntroCanvasGroup.alpha = 0f;
            storyIntroCanvasGroup.blocksRaycasts = true;
            storyIntroCanvasGroup.interactable = true;
            storyIntroPanel.SetActive(true);
        }

        float duration = Mathf.Max(0.01f, storyFadeDuration);
        float elapsed = 0f;
        float start = storyIntroCanvasGroup.alpha;
        float end = show ? 1f : 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            storyIntroCanvasGroup.alpha = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }

        storyIntroCanvasGroup.alpha = end;
        if (!show)
        {
            storyIntroCanvasGroup.blocksRaycasts = false;
            storyIntroCanvasGroup.interactable = false;
            storyIntroPanel.SetActive(false);
            StopPromptBlink();
        }
    }

    private void EnsureStoryIntroUi()
    {
        if (storyIntroPanel != null && storyIntroCanvasGroup != null)
            return;

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
            return;

        storyFontAsset = questionText != null ? questionText.font : null;
        storyFontMaterial = questionText != null ? questionText.fontSharedMaterial : null;

        storyIntroPanel = new GameObject("QuizStoryIntroPanel");
        storyIntroPanel.transform.SetParent(canvas.transform, false);
        storyIntroPanel.transform.SetAsLastSibling();

        RectTransform panelRect = storyIntroPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelBg = storyIntroPanel.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 180f / 255f);

        storyIntroCanvasGroup = storyIntroPanel.AddComponent<CanvasGroup>();
        storyIntroCanvasGroup.alpha = 0f;
        storyIntroCanvasGroup.blocksRaycasts = false;
        storyIntroCanvasGroup.interactable = false;

        GameObject popupObj = new GameObject("PopupCard");
        popupObj.transform.SetParent(storyIntroPanel.transform, false);
        RectTransform popupRect = popupObj.AddComponent<RectTransform>();
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.sizeDelta = new Vector2(960f, 620f);
        popupRect.anchoredPosition = Vector2.zero;

        Image popupImage = popupObj.AddComponent<Image>();
        popupImage.color = new Color(0.96f, 0.89f, 0.71f, 1f);
        Outline popupOutline = popupObj.AddComponent<Outline>();
        popupOutline.effectColor = new Color(0.53f, 0.34f, 0.18f, 0.95f);
        popupOutline.effectDistance = new Vector2(6f, -6f);
        Shadow popupShadow = popupObj.AddComponent<Shadow>();
        popupShadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
        popupShadow.effectDistance = new Vector2(10f, -10f);

        storyIntroTitleText = CreateStoryText("Title", popupObj.transform, new Vector2(0.5f, 0.84f), new Vector2(720f, 84f), 50, FontStyles.Bold, new Color(0.35f, 0.2f, 0.08f, 1f), TextAlignmentOptions.Center);
        storyIntroTitleText.text = "Story Time";

        storyIntroBodyText = CreateStoryText("Body", popupObj.transform, new Vector2(0.5f, 0.54f), new Vector2(760f, 270f), 40, FontStyles.Normal, new Color(0.28f, 0.18f, 0.1f, 1f), TextAlignmentOptions.Center);
        storyIntroHintText = CreateStoryText("Hint", popupObj.transform, new Vector2(0.39f, 0.12f), new Vector2(280f, 48f), 26, FontStyles.Bold, new Color(0.34f, 0.22f, 0.12f, 1f), TextAlignmentOptions.Center);
        storyLineCounterText = CreateStoryText("LineCounter", popupObj.transform, new Vector2(0.17f, 0.12f), new Vector2(130f, 48f), 26, FontStyles.Bold, new Color(0.42f, 0.28f, 0.16f, 0.8f), TextAlignmentOptions.Center);

        storyNextButton = CreateStoryButton("StoryNextButton", popupObj.transform, new Vector2(0.83f, 0.12f), new Vector2(170f, 66f), new Color(0.82f, 0.56f, 0.2f, 1f), "Next", OnStoryAdvanceRequested);
        storyStartButton = CreateStoryButton("StartActivityButton", popupObj.transform, new Vector2(0.5f, 0.12f), new Vector2(320f, 74f), new Color(0.2f, 0.72f, 0.24f, 1f), "Start Activity", OnStoryStartRequested);
        storyStartButton.gameObject.SetActive(false);

        storyIntroPanel.SetActive(false);
    }

    private TextMeshProUGUI CreateStoryText(string name, Transform parent, Vector2 anchor, Vector2 size, float fontSize, FontStyles fontStyle, Color color, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.text = string.Empty;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        if (storyFontAsset != null)
            text.font = storyFontAsset;
        if (storyFontMaterial != null)
            text.fontSharedMaterial = storyFontMaterial;
        return text;
    }

    private Button CreateStoryButton(string name, Transform parent, Vector2 anchor, Vector2 size, Color color, string label, UnityEngine.Events.UnityAction action)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        Image image = obj.AddComponent<Image>();
        image.color = color;
        Shadow shadow = obj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.2f);
        shadow.effectDistance = new Vector2(4f, -4f);

        Button button = obj.AddComponent<Button>();
        button.onClick.AddListener(action);

        TextMeshProUGUI text = CreateStoryText("Text", obj.transform, new Vector2(0.5f, 0.5f), size, 30, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        text.text = SanitizeStoryText(label);
        return button;
    }

    private string SanitizeStoryText(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        StringBuilder builder = new StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c == '\r')
                continue;

            if (c == '\n' || (c >= 32 && c <= 126))
                builder.Append(c);
        }

        return builder.ToString().Trim();
    }

    private void SetGameplayInteractable(bool interactable)
    {
        for (int i = 0; i < allButtons.Length; i++)
        {
            if (allButtons[i] != null)
                allButtons[i].interactable = interactable;
        }

        if (helpButton != null)
            helpButton.interactable = interactable;
        if (backButton != null)
            backButton.interactable = interactable;
    }
}
