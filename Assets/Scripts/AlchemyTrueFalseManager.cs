using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class AlchemyTrueFalseManager : MonoBehaviour
{
    [System.Serializable]
    public class Question
    {
        public string questionText;
        public bool correctAnswer; // true = True, false = False
        public string difficulty; // "easy", "medium", "hard"
    }

    [System.Serializable]
    public class DifficultyStage
    {
        public string stageName;
        public int startIndex;
        public int endIndex;
        public List<bool> selectedAnswers = new List<bool>();
        public bool isCompleted = false;
    }

    [Header("Question Settings")]
    public List<Question> allQuestions = new List<Question>();
    public int currentQuestionIndex = 0;
    
    [Header("Difficulty Stages")]
    public List<DifficultyStage> difficultyStages = new List<DifficultyStage>();
    private int currentStageIndex = 0;
    
    [Header("UI References")]
    public TextMeshProUGUI questionText;
    public Button truePotionButton;
    public Button falsePotionButton;
    public GameObject answerConfirmationPanel;
    public TextMeshProUGUI confirmationText;
    public Button confirmAnswerButton;
    public Button cancelAnswerButton;
    
    [Header("Stage UI")]
    public GameObject stageReviewPanel;
    public Button reviewButton;
    public Button submitStageButton;
    public TextMeshProUGUI stageInfoText;
    public GameObject reviewQuestionsPanel;
    public Transform reviewQuestionsContainer;
    public GameObject reviewQuestionPrefab;
    
    [Header("Final Submission UI")]
    public GameObject finalSubmissionPanel;
    public TextMeshProUGUI finalConfirmationText;
    public Button confirmFinalButton;
    public Button reviewFinalButton;
    
    [Header("Cauldron & Results")]
    public GameObject cauldron;
    public Image cauldronImage;
    public Sprite cauldronSuccessSprite;
    public Sprite cauldronFailureSprite;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI scoreText;
    public Animator cauldronAnimator;
    
    [Header("Potion Collection")]
    public Transform potionCollectionArea;
    public GameObject collectedPotionPrefab;
    private List<GameObject> collectedPotions = new List<GameObject>();
    
    [Header("Animation Settings")]
    public float potionPourDelay = 0.5f;
    public float resultDisplayDelay = 2f;
    public float stageTransitionDuration = 2f;
    
    private bool selectedAnswer;
    private int totalCorrectAnswers = 0;
    private bool isTransitioning = false;
    
    // Programmatically created animation elements
    private GameObject stageTransitionPanel;
    private TextMeshProUGUI stageTransitionText;
    private Image stageTransitionImage;
    private CanvasGroup mainUICanvasGroup;
    private bool isAnswerSelected = false;

    void Start()
    {
        InitializeAnimationSystem();
        InitializeGame();
        SetupButtons();
        DisplayCurrentQuestion();
    }

    void InitializeGame()
    {
        currentQuestionIndex = 0;
        currentStageIndex = 0;
        totalCorrectAnswers = 0;
        collectedPotions.Clear();
        
        // Auto-generate difficulty stages based on questions
        if (difficultyStages.Count == 0 && allQuestions.Count > 0)
        {
            GenerateDifficultyStages();
        }
        
        // Hide panels
        if (answerConfirmationPanel != null) answerConfirmationPanel.SetActive(false);
        if (stageReviewPanel != null) stageReviewPanel.SetActive(false);
        if (finalSubmissionPanel != null) finalSubmissionPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (reviewQuestionsPanel != null) reviewQuestionsPanel.SetActive(false);
    }

    void GenerateDifficultyStages()
    {
        var easyQuestions = allQuestions.Where(q => q.difficulty.ToLower() == "easy").ToList();
        var mediumQuestions = allQuestions.Where(q => q.difficulty.ToLower() == "medium").ToList();
        var hardQuestions = allQuestions.Where(q => q.difficulty.ToLower() == "hard").ToList();
        
        int currentIndex = 0;
        
        if (easyQuestions.Count > 0)
        {
            difficultyStages.Add(new DifficultyStage
            {
                stageName = "Easy Stage",
                startIndex = currentIndex,
                endIndex = currentIndex + easyQuestions.Count - 1
            });
            currentIndex += easyQuestions.Count;
        }
        
        if (mediumQuestions.Count > 0)
        {
            difficultyStages.Add(new DifficultyStage
            {
                stageName = "Medium Stage",
                startIndex = currentIndex,
                endIndex = currentIndex + mediumQuestions.Count - 1
            });
            currentIndex += mediumQuestions.Count;
        }
        
        if (hardQuestions.Count > 0)
        {
            difficultyStages.Add(new DifficultyStage
            {
                stageName = "Hard Stage",
                startIndex = currentIndex,
                endIndex = currentIndex + hardQuestions.Count - 1
            });
        }
    }

    void SetupButtons()
    {
        if (truePotionButton != null)
            truePotionButton.onClick.AddListener(() => OnPotionClicked(true));
        
        if (falsePotionButton != null)
            falsePotionButton.onClick.AddListener(() => OnPotionClicked(false));
        
        if (confirmAnswerButton != null)
            confirmAnswerButton.onClick.AddListener(ConfirmAnswerSelection);
        
        if (cancelAnswerButton != null)
            cancelAnswerButton.onClick.AddListener(CancelAnswerSelection);
        
        if (reviewButton != null)
            reviewButton.onClick.AddListener(ShowReviewPanel);
        
        if (submitStageButton != null)
            submitStageButton.onClick.AddListener(ShowStageSubmissionConfirmation);
        
        if (confirmFinalButton != null)
            confirmFinalButton.onClick.AddListener(SubmitStage);
        
        if (reviewFinalButton != null)
            reviewFinalButton.onClick.AddListener(GoToReviewFromFinal);
    }

    void DisplayCurrentQuestion()
    {
        if (currentQuestionIndex < allQuestions.Count)
        {
            Question currentQuestion = allQuestions[currentQuestionIndex];
            if (questionText != null)
                questionText.text = currentQuestion.questionText;
            
            UpdateStageInfo();
            isAnswerSelected = false;
            
            // Enable potion buttons
            if (truePotionButton != null) truePotionButton.interactable = true;
            if (falsePotionButton != null) falsePotionButton.interactable = true;
        }
    }

    void UpdateStageInfo()
    {
        DifficultyStage currentStage = difficultyStages[currentStageIndex];
        int stageQuestionNumber = currentQuestionIndex - currentStage.startIndex + 1;
        int totalStageQuestions = currentStage.endIndex - currentStage.startIndex + 1;
        
        if (stageInfoText != null)
        {
            stageInfoText.text = $"{currentStage.stageName}\nQuestion {stageQuestionNumber}/{totalStageQuestions}";
        }
        
        // Show submit button if at the end of stage
        bool isLastQuestionInStage = currentQuestionIndex == currentStage.endIndex;
        if (submitStageButton != null)
            submitStageButton.gameObject.SetActive(isLastQuestionInStage && isAnswerSelected);
    }

    public void OnPotionClicked(bool isTrue)
    {
        selectedAnswer = isTrue;
        
        // Show confirmation dialog
        if (answerConfirmationPanel != null)
        {
            answerConfirmationPanel.SetActive(true);
            if (confirmationText != null)
                confirmationText.text = $"Are you sure you want to pick\n'{(isTrue ? "TRUE" : "FALSE")}' as your answer?";
        }
    }

    void ConfirmAnswerSelection()
    {
        // Hide confirmation panel
        if (answerConfirmationPanel != null)
            answerConfirmationPanel.SetActive(false);
        
        // Store the answer in current stage
        DifficultyStage currentStage = difficultyStages[currentStageIndex];
        currentStage.selectedAnswers.Add(selectedAnswer);
        
        isAnswerSelected = true;
        
        // Disable potion buttons after selection
        if (truePotionButton != null) truePotionButton.interactable = false;
        if (falsePotionButton != null) falsePotionButton.interactable = false;
        
        // Check if this is the last question in the stage
        if (currentQuestionIndex == currentStage.endIndex)
        {
            // Show submit stage button
            if (submitStageButton != null)
                submitStageButton.gameObject.SetActive(true);
        }
        else
        {
            // Move to next question automatically after a short delay
            StartCoroutine(MoveToNextQuestionDelayed(0.5f));
        }
        
        UpdateStageInfo();
    }

    void CancelAnswerSelection()
    {
        if (answerConfirmationPanel != null)
            answerConfirmationPanel.SetActive(false);
    }

    IEnumerator MoveToNextQuestionDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        MoveToNextQuestion();
    }

    void MoveToNextQuestion()
    {
        currentQuestionIndex++;
        
        if (currentQuestionIndex < allQuestions.Count)
        {
            DisplayCurrentQuestion();
        }
    }

    void ShowStageSubmissionConfirmation()
    {
        if (finalSubmissionPanel != null)
        {
            DifficultyStage currentStage = difficultyStages[currentStageIndex];
            finalSubmissionPanel.SetActive(true);
            
            if (finalConfirmationText != null)
                finalConfirmationText.text = $"Submit your answers for {currentStage.stageName}?\n\nYou can review your answers before submitting.";
        }
    }

    void GoToReviewFromFinal()
    {
        if (finalSubmissionPanel != null)
            finalSubmissionPanel.SetActive(false);
        
        ShowReviewPanel();
    }

    void ShowReviewPanel()
    {
        if (reviewQuestionsPanel != null)
        {
            reviewQuestionsPanel.SetActive(true);
            PopulateReviewPanel();
        }
    }

    void PopulateReviewPanel()
    {
        // Clear existing review items
        if (reviewQuestionsContainer != null)
        {
            foreach (Transform child in reviewQuestionsContainer)
            {
                Destroy(child.gameObject);
            }
        }
        
        DifficultyStage currentStage = difficultyStages[currentStageIndex];
        
        for (int i = currentStage.startIndex; i <= currentStage.endIndex; i++)
        {
            if (i < allQuestions.Count && (i - currentStage.startIndex) < currentStage.selectedAnswers.Count)
            {
                CreateReviewItem(i, currentStage.startIndex);
            }
        }
    }

    void CreateReviewItem(int questionIndex, int stageStartIndex)
    {
        if (reviewQuestionPrefab != null && reviewQuestionsContainer != null)
        {
            GameObject reviewItem = Instantiate(reviewQuestionPrefab, reviewQuestionsContainer);
            
            // Set question text
            TextMeshProUGUI questionTxt = reviewItem.transform.Find("QuestionText")?.GetComponent<TextMeshProUGUI>();
            if (questionTxt != null)
                questionTxt.text = $"Q{questionIndex - stageStartIndex + 1}: {allQuestions[questionIndex].questionText}";
            
            // Set answer text
            TextMeshProUGUI answerTxt = reviewItem.transform.Find("AnswerText")?.GetComponent<TextMeshProUGUI>();
            if (answerTxt != null)
            {
                int answerIndex = questionIndex - stageStartIndex;
                bool selectedAns = difficultyStages[currentStageIndex].selectedAnswers[answerIndex];
                answerTxt.text = $"Your Answer: {(selectedAns ? "TRUE" : "FALSE")}";
            }
            
            // Add change button
            Button changeBtn = reviewItem.transform.Find("ChangeButton")?.GetComponent<Button>();
            if (changeBtn != null)
            {
                int capturedIndex = questionIndex;
                changeBtn.onClick.AddListener(() => ChangeAnswer(capturedIndex));
            }
        }
    }

    void ChangeAnswer(int questionIndex)
    {
        // Close review panel
        if (reviewQuestionsPanel != null)
            reviewQuestionsPanel.SetActive(false);
        
        // Go back to that question
        currentQuestionIndex = questionIndex;
        
        // Remove the stored answer
        DifficultyStage currentStage = difficultyStages[currentStageIndex];
        int answerIndex = questionIndex - currentStage.startIndex;
        if (answerIndex < currentStage.selectedAnswers.Count)
        {
            currentStage.selectedAnswers.RemoveAt(answerIndex);
        }
        
        // Hide submit button
        if (submitStageButton != null)
            submitStageButton.gameObject.SetActive(false);
        
        DisplayCurrentQuestion();
    }

    void SubmitStage()
    {
        // Hide final submission panel
        if (finalSubmissionPanel != null)
            finalSubmissionPanel.SetActive(false);
        
        // Mark stage as completed
        DifficultyStage currentStage = difficultyStages[currentStageIndex];
        currentStage.isCompleted = true;
        
        // Calculate correct answers for this stage
        int stageCorrect = 0;
        for (int i = currentStage.startIndex; i <= currentStage.endIndex; i++)
        {
            int answerIndex = i - currentStage.startIndex;
            if (answerIndex < currentStage.selectedAnswers.Count)
            {
                if (currentStage.selectedAnswers[answerIndex] == allQuestions[i].correctAnswer)
                {
                    stageCorrect++;
                    totalCorrectAnswers++;
                }
            }
        }
        
        // Add collected potion for this stage
        AddCollectedPotion(stageCorrect > 0);
        
        // Move to next stage or finish
        currentStageIndex++;
        
        if (currentStageIndex < difficultyStages.Count)
        {
            // Show stage transition animation
            StartCoroutine(ShowStageTransitionAnimation());
        }
        else
        {
            // All stages completed - pour potions and show results
            StartCoroutine(PourPotionsAndShowResults());
        }
    }

    void AddCollectedPotion(bool isCorrect)
    {
        if (collectedPotionPrefab != null && potionCollectionArea != null)
        {
            GameObject potion = Instantiate(collectedPotionPrefab, potionCollectionArea);
            
            // Color the potion based on performance
            Image potionImg = potion.GetComponent<Image>();
            if (potionImg != null)
            {
                potionImg.color = isCorrect ? new Color(0.2f, 0.8f, 0.2f, 1f) : new Color(0.8f, 0.2f, 0.2f, 1f);
            }
            
            collectedPotions.Add(potion);
        }
    }

    IEnumerator PourPotionsAndShowResults()
    {
        // Disable UI
        if (truePotionButton != null) truePotionButton.gameObject.SetActive(false);
        if (falsePotionButton != null) falsePotionButton.gameObject.SetActive(false);
        if (questionText != null) questionText.gameObject.SetActive(false);
        
        // Pour each potion with delay
        foreach (GameObject potion in collectedPotions)
        {
            // Simple scale animation
            StartCoroutine(AnimatePotionPour(potion));
            yield return new WaitForSeconds(potionPourDelay);
        }
        
        yield return new WaitForSeconds(resultDisplayDelay);
        
        // Determine success or failure
        bool isSuccess = totalCorrectAnswers > 3; // More than 3 correct = success
        
        // Update cauldron image
        if (cauldronImage != null)
        {
            cauldronImage.sprite = isSuccess ? cauldronSuccessSprite : cauldronFailureSprite;
        }
        
        // Play cauldron animation
        if (cauldronAnimator != null)
        {
            cauldronAnimator.SetTrigger(isSuccess ? "Success" : "Failure");
        }
        
        yield return new WaitForSeconds(1f);
        
        // Show result panel
        ShowResults(isSuccess);
    }

    IEnumerator AnimatePotionPour(GameObject potion)
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 startScale = potion.transform.localScale;
        Vector3 targetPos = cauldron.transform.position;
        Vector3 startPos = potion.transform.position;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Move towards cauldron
            potion.transform.position = Vector3.Lerp(startPos, targetPos, t);
            
            // Scale down
            potion.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            
            yield return null;
        }
        
        Destroy(potion);
    }

    void ShowResults(bool isSuccess)
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            
            if (resultText != null)
            {
                resultText.text = isSuccess ? "Potion Created Successfully!" : "Potion Creation Failed!";
                resultText.color = isSuccess ? Color.green : Color.red;
            }
            
            if (scoreText != null)
            {
                scoreText.text = $"Score: {totalCorrectAnswers}/{allQuestions.Count}";
            }
        }
    }

    public void RestartGame()
    {
        // Reset all variables
        currentQuestionIndex = 0;
        currentStageIndex = 0;
        totalCorrectAnswers = 0;
        
        // Clear collected potions
        foreach (GameObject potion in collectedPotions)
        {
            if (potion != null) Destroy(potion);
        }
        collectedPotions.Clear();
        
        // Reset stages
        foreach (var stage in difficultyStages)
        {
            stage.selectedAnswers.Clear();
            stage.isCompleted = false;
        }
        
        // Show UI elements
        if (truePotionButton != null) truePotionButton.gameObject.SetActive(true);
        if (falsePotionButton != null) falsePotionButton.gameObject.SetActive(true);
        if (questionText != null) questionText.gameObject.SetActive(true);
        
        // Hide panels
        if (resultPanel != null) resultPanel.SetActive(false);
        if (answerConfirmationPanel != null) answerConfirmationPanel.SetActive(false);
        if (stageReviewPanel != null) stageReviewPanel.SetActive(false);
        if (finalSubmissionPanel != null) finalSubmissionPanel.SetActive(false);
        
        // Reset cauldron
        if (cauldronImage != null && cauldronFailureSprite != null)
            cauldronImage.sprite = cauldronFailureSprite;
        
        DisplayCurrentQuestion();
    }

    // ========== ANIMATION SYSTEM (CODE-BASED) ==========

    void InitializeAnimationSystem()
    {
        // Add CanvasGroup to main UI for fade effects
        if (questionText != null)
        {
            Transform parent = questionText.transform.parent;
            if (parent != null)
            {
                mainUICanvasGroup = parent.GetComponent<CanvasGroup>();
                if (mainUICanvasGroup == null)
                {
                    mainUICanvasGroup = parent.gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        // Create stage transition overlay panel programmatically
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null && stageTransitionPanel == null)
        {
            // Create overlay panel
            stageTransitionPanel = new GameObject("StageTransitionPanel");
            stageTransitionPanel.transform.SetParent(canvas.transform, false);
            
            RectTransform rectTransform = stageTransitionPanel.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            
            // Add background image
            stageTransitionImage = stageTransitionPanel.AddComponent<Image>();
            stageTransitionImage.color = new Color(0.1f, 0.05f, 0.15f, 0); // Dark purple, start transparent
            
            // Add canvas group for fading
            CanvasGroup cg = stageTransitionPanel.AddComponent<CanvasGroup>();
            cg.alpha = 0;
            cg.blocksRaycasts = false;
            
            // Create text element
            GameObject textObj = new GameObject("StageTransitionText");
            textObj.transform.SetParent(stageTransitionPanel.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(900, 500);
            textRect.anchoredPosition = Vector2.zero;
            
            stageTransitionText = textObj.AddComponent<TextMeshProUGUI>();
            stageTransitionText.fontSize = 56;
            stageTransitionText.fontStyle = FontStyles.Bold;
            stageTransitionText.alignment = TextAlignmentOptions.Center;
            stageTransitionText.color = new Color(1f, 0.9f, 0.3f); // Gold color
            
            // Add outline
            var outline = textObj.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0.3f, 0.1f, 0.5f);
            outline.effectDistance = new Vector2(3, -3);
            
            // Set high sort order to appear on top
            stageTransitionPanel.transform.SetAsLastSibling();
            stageTransitionPanel.SetActive(false);
        }
    }

    IEnumerator ShowStageTransitionAnimation()
    {
        isTransitioning = true;

        if (stageTransitionPanel != null && stageTransitionText != null && stageTransitionImage != null)
        {
            // Get previous and current stage names
            string previousStage = difficultyStages[currentStageIndex - 1].stageName;
            string nextStage = difficultyStages[currentStageIndex].stageName;
            
            stageTransitionText.text = $"✨ {previousStage} Complete! ✨\n\n🧪 Preparing {nextStage}... 🧪";
            
            // Show panel
            stageTransitionPanel.SetActive(true);
            CanvasGroup panelCG = stageTransitionPanel.GetComponent<CanvasGroup>();
            panelCG.blocksRaycasts = true;
            
            // Fade out main UI first
            if (mainUICanvasGroup != null)
            {
                float elapsed = 0f;
                float duration = 0.4f;
                
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    mainUICanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                    yield return null;
                }
                mainUICanvasGroup.alpha = 0f;
            }
            
            // Fade in transition panel with scale animation
            float elapsed2 = 0f;
            float duration2 = 0.6f;
            Vector3 startScale = new Vector3(0.7f, 0.7f, 1f);
            Vector3 endScale = Vector3.one;
            
            stageTransitionText.transform.localScale = startScale;
            
            while (elapsed2 < duration2)
            {
                elapsed2 += Time.deltaTime;
                float t = elapsed2 / duration2;
                
                // Smooth fade in
                panelCG.alpha = Mathf.Lerp(0f, 1f, t);
                stageTransitionImage.color = new Color(0.1f, 0.05f, 0.15f, Mathf.Lerp(0f, 0.95f, t));
                
                // Bounce scale animation
                float bounceT = Mathf.Sin(t * Mathf.PI);
                stageTransitionText.transform.localScale = Vector3.Lerp(startScale, endScale, t) * (1f + bounceT * 0.1f);
                
                yield return null;
            }
            
            // Hold for a moment
            yield return new WaitForSeconds(stageTransitionDuration);
            
            // Fade out transition panel
            elapsed2 = 0f;
            duration2 = 0.5f;
            
            while (elapsed2 < duration2)
            {
                elapsed2 += Time.deltaTime;
                float t = elapsed2 / duration2;
                
                panelCG.alpha = Mathf.Lerp(1f, 0f, t);
                stageTransitionImage.color = new Color(0.1f, 0.05f, 0.15f, Mathf.Lerp(0.95f, 0f, t));
                
                yield return null;
            }
            
            panelCG.alpha = 0f;
            panelCG.blocksRaycasts = false;
            stageTransitionPanel.SetActive(false);
        }

        // Move to next stage
        currentQuestionIndex++;
        
        if (submitStageButton != null)
            submitStageButton.gameObject.SetActive(false);
        
        DisplayCurrentQuestion();
        
        // Fade in main UI
        if (mainUICanvasGroup != null)
        {
            float elapsed = 0f;
            float duration = 0.5f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                mainUICanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }
            mainUICanvasGroup.alpha = 1f;
        }

        isTransitioning = false;
    }
}
