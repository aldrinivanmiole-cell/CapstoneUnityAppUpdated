using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.SceneManagement;

[System.Serializable]
public class IJMultipleChoiceQuestion
{
    public int question_id;
    public string question_text;
    public List<IJAnswer> answers;
}

[System.Serializable]
public class IJAnswer
{
    public int answer_id;
    public string answer_text;
    public int correct_answer;
}

[System.Serializable]
public class IJQuestionResponse
{
    public List<IJMultipleChoiceQuestion> questions;
}

public class IndianaJonesMultipleChoice : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI questionNumberText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    
    [Header("Pedestal Buttons")]
    public Button pedestal1Button;
    public Button pedestal2Button;
    public Button pedestal3Button;
    public Button pedestal4Button;
    
    [Header("Pedestal Images")]
    public Image pedestal1Image;
    public Image pedestal2Image;
    public Image pedestal3Image;
    public Image pedestal4Image;
    
    [Header("Pedestal Answer Texts")]
    public TextMeshProUGUI answer1Text;
    public TextMeshProUGUI answer2Text;
    public TextMeshProUGUI answer3Text;
    public TextMeshProUGUI answer4Text;
    
    [Header("Pedestal Sprites")]
    public Sprite pedestalNormal;
    public Sprite pedestalGlow;
    public Sprite pedestalGray;
    
    [Header("Treasure Items on Pedestals")]
    public Image treasure1Image;
    public Image treasure2Image;
    public Image treasure3Image;
    public Image treasure4Image;
    public Sprite treasureRuby;
    public Sprite treasureCoin;
    public Sprite treasureCrystal;
    public Sprite treasurePearl;
    

    
    [Header("Character Display")]
    public Image characterImage;
    public Sprite maleCharacter;
    public Sprite femaleCharacter;
    
    [Header("Level Backgrounds")]
    public Image backgroundImage;              // The background Image component
    public List<Sprite> levelBackgrounds;      // List of background sprites to randomly choose from
    
    [Header("Effects")]
    public GameObject sparkleParticlePrefab;
    public GameObject wrongIconPrefab;
    public AudioSource correctSound;
    public AudioSource wrongSound;
    
    [Header("Confirmation Dialog")]
    [SerializeField] private GameObject confirmationdialog;
    [SerializeField] private TMP_Text ConfirmationText;
    [SerializeField] private Button ConfirmYesButton;
    [SerializeField] private Button ConfirmNoButton;
    
    [Header("Navigation Buttons")]
    public Button NextButton;
    public Button PreviousButton;
    
    private List<IJMultipleChoiceQuestion> questions = new List<IJMultipleChoiceQuestion>();
    private int currentQuestionIndex = 0;
    private int score = 0;
    private float timeRemaining;
    private float maxTime;
    private bool isAnswering = false;
    
    private string apiUrl = "https://homequest-c3k7.onrender.com";
    private int assignmentId;
    private int studentId;
    
    private IJAnswer selectedAnswer;
    private List<Sprite> availableTreasures = new List<Sprite>();
    
    // Track selected answer index for each question
    private Dictionary<int, int> selectedAnswerIndices = new Dictionary<int, int>();

    void Start()
    {
        // Get assignment and student info
        assignmentId = CurrentClassSession.SelectedCategoryId;
        studentId = SessionManager.Instance != null ? SessionManager.Instance.StudentId : PlayerPrefs.GetInt("StudentId", 0);
        
        // Set random character
        if (characterImage != null)
        {
            characterImage.sprite = Random.value > 0.5f ? maleCharacter : femaleCharacter;
        }
        
        // Initialize treasure list
        availableTreasures.Add(treasureRuby);
        availableTreasures.Add(treasureCoin);
        availableTreasures.Add(treasureCrystal);
        availableTreasures.Add(treasurePearl);
        
        // Assign random treasures to pedestals
        AssignRandomTreasures();
        
        // Ensure all pedestals are the same size
        NormalizePedestalSizes();
        
        // Start floating animation for treasures
        StartCoroutine(FloatTreasures());
        
        // Setup button listeners
        pedestal1Button.onClick.AddListener(() => SelectAnswer(0));
        pedestal2Button.onClick.AddListener(() => SelectAnswer(1));
        pedestal3Button.onClick.AddListener(() => SelectAnswer(2));
        pedestal4Button.onClick.AddListener(() => SelectAnswer(3));
        
        // Setup navigation button listeners
        if (NextButton != null)
            NextButton.onClick.AddListener(OnNextClicked);
        if (PreviousButton != null)
            PreviousButton.onClick.AddListener(OnPreviousClicked);
        
        // Initialize UI
        UpdateScoreDisplay();
        
        // Load questions
        StartCoroutine(LoadQuestions());
        
        SetNavigationButtonsVisibility();
    }
    
    void Update()
    {
        if (isAnswering && timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerDisplay();
            
            if (timeRemaining <= 0)
            {
                TimeUp();
            }
        }
    }
    
    IEnumerator LoadQuestions()
    {
        string url = $"{apiUrl}/get_multiple_choice?assignment_id={assignmentId}";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                IJQuestionResponse response = JsonUtility.FromJson<IJQuestionResponse>(jsonResponse);
                questions = response.questions;
                
                if (questions.Count > 0)
                {
                    DisplayQuestion();
                }
                else
                {
                    Debug.LogError("No questions loaded!");
                }
            }
            else
            {
                Debug.LogError("Failed to load questions: " + request.error);
            }
        }
    }
    
    void DisplayQuestion()
    {
        if (currentQuestionIndex >= questions.Count)
        {
            FinishQuiz();
            return;
        }
        
        // Change background for new question/level
        ChangeBackgroundRandomly();
        
        IJMultipleChoiceQuestion question = questions[currentQuestionIndex];
        
        // Display question
        questionText.text = question.question_text;
        questionNumberText.text = $"Question {currentQuestionIndex + 1}/{questions.Count}";
        
        // Shuffle answers
        List<IJAnswer> shuffledAnswers = new List<IJAnswer>(question.answers);
        for (int i = 0; i < shuffledAnswers.Count; i++)
        {
            IJAnswer temp = shuffledAnswers[i];
            int randomIndex = Random.Range(i, shuffledAnswers.Count);
            shuffledAnswers[i] = shuffledAnswers[randomIndex];
            shuffledAnswers[randomIndex] = temp;
        }
        
        // Display answers on pedestals
        answer1Text.text = shuffledAnswers.Count > 0 ? shuffledAnswers[0].answer_text : "";
        answer2Text.text = shuffledAnswers.Count > 1 ? shuffledAnswers[1].answer_text : "";
        answer3Text.text = shuffledAnswers.Count > 2 ? shuffledAnswers[2].answer_text : "";
        answer4Text.text = shuffledAnswers.Count > 3 ? shuffledAnswers[3].answer_text : "";
        
        // Reset pedestal sprites
        pedestal1Image.sprite = pedestalNormal;
        pedestal2Image.sprite = pedestalNormal;
        pedestal3Image.sprite = pedestalNormal;
        pedestal4Image.sprite = pedestalNormal;
        
        // Enable buttons
        pedestal1Button.interactable = shuffledAnswers.Count > 0;
        pedestal2Button.interactable = shuffledAnswers.Count > 1;
        pedestal3Button.interactable = shuffledAnswers.Count > 2;
        pedestal4Button.interactable = shuffledAnswers.Count > 3;
        
        // Set progressive difficulty timer
        if (currentQuestionIndex < 3)
        {
            maxTime = 30f; // Easy: Questions 1-3
        }
        else if (currentQuestionIndex < 7)
        {
            maxTime = 20f; // Medium: Questions 4-7
        }
        else
        {
            maxTime = 15f; // Hard: Questions 8-10
        }
        
        timeRemaining = maxTime;
        isAnswering = true;
        
        // Store shuffled answers for answer checking
        question.answers = shuffledAnswers;

        // Restore pedestal states based on previous selection
        RestorePedestalStates();
        SetNavigationButtonsVisibility();
    }

    void RestorePedestalStates()
    {
        // Reset all pedestals to normal
        pedestal1Image.sprite = pedestalNormal;
        pedestal2Image.sprite = pedestalNormal;
        pedestal3Image.sprite = pedestalNormal;
        pedestal4Image.sprite = pedestalNormal;

        // Gray out all pedestals if an answer was previously selected
        if (selectedAnswerIndices.TryGetValue(currentQuestionIndex, out int selectedIdx))
        {
            // Glow the selected pedestal
            GetPedestalImage(selectedIdx).sprite = pedestalGlow;
            // Gray out others
            for (int i = 0; i < 4; i++)
            {
                if (i != selectedIdx)
                {
                    GetPedestalImage(i).sprite = pedestalGray;
                }
            }
        }
    }
    
    void SelectAnswer(int answerIndex)
    {
        if (!isAnswering) return;
        isAnswering = false;

        IJMultipleChoiceQuestion question = questions[currentQuestionIndex];
        IJAnswer selectedAnswer = question.answers[answerIndex];

        // Save selected answer index for this question
        selectedAnswerIndices[currentQuestionIndex] = answerIndex;

        // Check answer
        bool isCorrect = selectedAnswer.correct_answer == 1;
        
        // Find the correct answer index
        int correctAnswerIndex = -1;
        for (int i = 0; i < question.answers.Count; i++)
        {
            if (question.answers[i].correct_answer == 1)
            {
                correctAnswerIndex = i;
                break;
            }
        }

        // Visual feedback - show correct answer glowing, gray out incorrect ones
        for (int i = 0; i < 4; i++)
        {
            if (i == correctAnswerIndex)
            {
                // Correct answer always glows
                GetPedestalImage(i).sprite = pedestalGlow;
            }
            else
            {
                // Wrong answers turn gray
                GetPedestalImage(i).sprite = pedestalGray;
            }
        }
        
        StartCoroutine(ShowAnswerFeedback(answerIndex, isCorrect, selectedAnswer));
    }
    
    IEnumerator ShowAnswerFeedback(int answerIndex, bool isCorrect, IJAnswer selectedAnswer)
    {
        Image selectedPedestal = GetPedestalImage(answerIndex);
        
        if (isCorrect)
        {
            // Correct answer feedback
            if (correctSound != null) correctSound.Play();
            
            // Show sparkle effect
            if (sparkleParticlePrefab != null)
            {
                GameObject sparkle = Instantiate(sparkleParticlePrefab, selectedPedestal.transform.position, Quaternion.identity);
                Destroy(sparkle, 2f);
            }
            
            // Calculate score based on time remaining
            int timeBonus = Mathf.RoundToInt((timeRemaining / maxTime) * 100);
            score += 100 + timeBonus;
            UpdateScoreDisplay();
        }
        else
        {
            // Wrong answer feedback
            if (wrongSound != null) wrongSound.Play();
            
            // Show X icon
            if (wrongIconPrefab != null)
            {
                GameObject wrongIcon = Instantiate(wrongIconPrefab, selectedPedestal.transform.position, Quaternion.identity);
                Destroy(wrongIcon, 2f);
            }
            
            // Gray out wrong pedestal
            selectedPedestal.sprite = pedestalGray;
            
            // Show correct answer
            HighlightCorrectAnswer();
        }
        
        // Save answer to history
        yield return StartCoroutine(SaveAnswerToHistory(selectedAnswer, isCorrect));
        
        yield return new WaitForSeconds(2f);
        
        // Next question
        currentQuestionIndex++;
        DisplayQuestion();
    }
    
    void HighlightCorrectAnswer()
    {
        IJMultipleChoiceQuestion question = questions[currentQuestionIndex];
        
        for (int i = 0; i < question.answers.Count; i++)
        {
            if (question.answers[i].correct_answer == 1)
            {
                Image pedestal = GetPedestalImage(i);
                pedestal.sprite = pedestalGlow;
                break;
            }
        }
    }
    
    Image GetPedestalImage(int index)
    {
        switch (index)
        {
            case 0: return pedestal1Image;
            case 1: return pedestal2Image;
            case 2: return pedestal3Image;
            case 3: return pedestal4Image;
            default: return pedestal1Image;
        }
    }
    
    void TimeUp()
    {
        if (!isAnswering) return;
        
        isAnswering = false;
        
        if (wrongSound != null) wrongSound.Play();
        
        // Show correct answer
        HighlightCorrectAnswer();
        
        // Disable all buttons
        pedestal1Button.interactable = false;
        pedestal2Button.interactable = false;
        pedestal3Button.interactable = false;
        pedestal4Button.interactable = false;
        
        IJMultipleChoiceQuestion question = questions[currentQuestionIndex];
        IJAnswer correctAnswer = null;
        foreach (var ans in question.answers)
        {
            if (ans.correct_answer == 1)
            {
                correctAnswer = ans;
                break;
            }
        }
        
        // Save timeout as wrong answer
        StartCoroutine(SaveTimeoutToHistory(correctAnswer));
        
        StartCoroutine(MoveToNextAfterTimeout());
    }
    
    IEnumerator MoveToNextAfterTimeout()
    {
        yield return new WaitForSeconds(2f);
        
        currentQuestionIndex++;
        DisplayQuestion();
    }
    
    void UpdateTimerDisplay()
    {
        int seconds = Mathf.CeilToInt(timeRemaining);
        timerText.text = seconds.ToString();
        
        // Color based on time remaining
        float timePercent = timeRemaining / maxTime;
        if (timePercent > 0.5f)
        {
            timerText.color = Color.green;
        }
        else if (timePercent > 0.25f)
        {
            timerText.color = Color.yellow;
        }
        else
        {
            timerText.color = Color.red;
        }
    }
    
    void UpdateScoreDisplay()
    {
        scoreText.text = $"Score: {score}";
    }
    

    
    IEnumerator SaveAnswerToHistory(IJAnswer selectedAnswer, bool isCorrect)
    {
        IJMultipleChoiceQuestion question = questions[currentQuestionIndex];
        
        WWWForm form = new WWWForm();
        form.AddField("student_id", studentId);
        form.AddField("assignment_id", assignmentId);
        form.AddField("question_id", question.question_id);
        form.AddField("student_answer", selectedAnswer.answer_text);
        form.AddField("correct_answer", isCorrect ? "1" : "0");
        
        using (UnityWebRequest request = UnityWebRequest.Post($"{apiUrl}/save_history", form))
        {
            yield return request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to save answer: " + request.error);
            }
        }
    }
    
    IEnumerator SaveTimeoutToHistory(IJAnswer correctAnswer)
    {
        IJMultipleChoiceQuestion question = questions[currentQuestionIndex];
        
        WWWForm form = new WWWForm();
        form.AddField("student_id", studentId);
        form.AddField("assignment_id", assignmentId);
        form.AddField("question_id", question.question_id);
        form.AddField("student_answer", "Time's up!");
        form.AddField("correct_answer", "0");
        
        using (UnityWebRequest request = UnityWebRequest.Post($"{apiUrl}/save_history", form))
        {
            yield return request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to save timeout: " + request.error);
            }
        }
    }
    
    void ChangeBackgroundRandomly()
    {
        if (backgroundImage != null && levelBackgrounds != null && levelBackgrounds.Count > 0)
        {
            // Pick a random background from the list
            int randomIndex = Random.Range(0, levelBackgrounds.Count);
            backgroundImage.sprite = levelBackgrounds[randomIndex];
            
            Debug.Log($"🎨 Changed to background #{randomIndex + 1} for question {currentQuestionIndex + 1}");
        }
    }

    void FinishQuiz()
    {
        StartCoroutine(SubmitFinalScore());
    }
    
    IEnumerator SubmitFinalScore()
    {
        // Calculate percentage
        int totalQuestions = questions.Count;
        float percentage = (float)score / (totalQuestions * 200) * 100; // Max 200 per question (100 base + 100 time bonus)
        
        WWWForm form = new WWWForm();
        form.AddField("student_id", studentId);
        form.AddField("assignment_id", assignmentId);
        form.AddField("score", score);
        
        using (UnityWebRequest request = UnityWebRequest.Post($"{apiUrl}/submit_score", form))
        {
            yield return request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to submit score: " + request.error);
            }
        }
        
        // Pass score data to ending scene
        TreasureHuntersEnding.FinalScore = score;
        TreasureHuntersEnding.TotalQuestions = questions.Count;
        
        // Load ending scene
        SceneManager.LoadScene("MC-Ending");
    }
    
    void NormalizePedestalSizes()
    {
        // Get the first pedestal's scale as reference
        Vector3 referenceScale = pedestal1Image != null ? pedestal1Image.rectTransform.localScale : Vector3.one;
        
        // Apply the same scale to all pedestals
        if (pedestal1Image != null)
            pedestal1Image.rectTransform.localScale = referenceScale;
        if (pedestal2Image != null)
            pedestal2Image.rectTransform.localScale = referenceScale;
        if (pedestal3Image != null)
            pedestal3Image.rectTransform.localScale = referenceScale;
        if (pedestal4Image != null)
            pedestal4Image.rectTransform.localScale = referenceScale;
        
        // Also normalize the sizes (width/height)
        if (pedestal1Image != null)
        {
            Vector2 referenceSize = pedestal1Image.rectTransform.sizeDelta;
            
            if (pedestal2Image != null)
                pedestal2Image.rectTransform.sizeDelta = referenceSize;
            if (pedestal3Image != null)
                pedestal3Image.rectTransform.sizeDelta = referenceSize;
            if (pedestal4Image != null)
                pedestal4Image.rectTransform.sizeDelta = referenceSize;
        }
    }
    
    void AssignRandomTreasures()
    {
        // Shuffle treasures
        List<Sprite> shuffledTreasures = new List<Sprite>(availableTreasures);
        for (int i = 0; i < shuffledTreasures.Count; i++)
        {
            Sprite temp = shuffledTreasures[i];
            int randomIndex = Random.Range(i, shuffledTreasures.Count);
            shuffledTreasures[i] = shuffledTreasures[randomIndex];
            shuffledTreasures[randomIndex] = temp;
        }
        
        // Assign to treasure images
        if (treasure1Image != null && shuffledTreasures.Count > 0)
            treasure1Image.sprite = shuffledTreasures[0];
        if (treasure2Image != null && shuffledTreasures.Count > 1)
            treasure2Image.sprite = shuffledTreasures[1];
        if (treasure3Image != null && shuffledTreasures.Count > 2)
            treasure3Image.sprite = shuffledTreasures[2];
        if (treasure4Image != null && shuffledTreasures.Count > 3)
            treasure4Image.sprite = shuffledTreasures[3];
    }
    
    IEnumerator FloatTreasures()
    {
        float floatSpeed = 1f;
        float floatAmount = 15f;
        float time = 0f;
        
        Vector3 treasure1StartPos = treasure1Image != null ? treasure1Image.rectTransform.anchoredPosition : Vector3.zero;
        Vector3 treasure2StartPos = treasure2Image != null ? treasure2Image.rectTransform.anchoredPosition : Vector3.zero;
        Vector3 treasure3StartPos = treasure3Image != null ? treasure3Image.rectTransform.anchoredPosition : Vector3.zero;
        Vector3 treasure4StartPos = treasure4Image != null ? treasure4Image.rectTransform.anchoredPosition : Vector3.zero;
        
        while (true)
        {
            time += Time.deltaTime * floatSpeed;
            float offset = Mathf.Sin(time) * floatAmount;
            
            if (treasure1Image != null)
                treasure1Image.rectTransform.anchoredPosition = treasure1StartPos + new Vector3(0, offset, 0);
            if (treasure2Image != null)
                treasure2Image.rectTransform.anchoredPosition = treasure2StartPos + new Vector3(0, offset, 0);
            if (treasure3Image != null)
                treasure3Image.rectTransform.anchoredPosition = treasure3StartPos + new Vector3(0, offset, 0);
            if (treasure4Image != null)
                treasure4Image.rectTransform.anchoredPosition = treasure4StartPos + new Vector3(0, offset, 0);
            
            yield return null;
        }
    }
    
    void SetNavigationButtonsVisibility()
    {
        if (NextButton != null)
            NextButton.gameObject.SetActive(true);
        if (PreviousButton != null)
            PreviousButton.gameObject.SetActive(currentQuestionIndex > 0);
    }

    public void OnNextClicked()
    {
        if (currentQuestionIndex < questions.Count - 1)
        {
            currentQuestionIndex++;
            DisplayQuestion();
        }
    }

    public void OnPreviousClicked()
    {
        if (currentQuestionIndex > 0)
        {
            currentQuestionIndex--;
            DisplayQuestion();
        }
    }
}
