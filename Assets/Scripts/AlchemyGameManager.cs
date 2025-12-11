using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

/// <summary>
/// Complete Game Manager for Alchemy True/False Quiz Game
/// This single script handles all game logic, UI, animations, and interactions
/// </summary>
public class AlchemyGameManager : MonoBehaviour
{
    #region Data Classes
    
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
    
    #endregion

    #region Question Settings
    
    [Header("═══ QUESTION SETTINGS ═══")]
    [Tooltip("Add all your True/False questions here")]
    public List<Question> allQuestions = new List<Question>();
    
    #endregion

    #region UI References - Main Gameplay
    
    [Header("═══ MAIN GAMEPLAY UI ═══")]
    [Tooltip("The main text showing the current question")]
    public TextMeshProUGUI questionText;
    
    [Tooltip("Shows current stage and question number (e.g., 'Easy Stage - Question 1/3')")]
    public TextMeshProUGUI stageInfoText;
    
    [Tooltip("The TRUE potion button (left side)")]
    public Button truePotionButton;
    
    [Tooltip("The FALSE potion button (right side)")]
    public Button falsePotionButton;
    
    [Tooltip("Image component of TRUE potion for visual feedback")]
    public Image truePotionImage;
    
    [Tooltip("Image component of FALSE potion for visual feedback")]
    public Image falsePotionImage;
    
    #endregion

    #region UI References - Cauldron
    
    [Header("═══ CAULDRON ═══")]
    [Tooltip("The cauldron GameObject")]
    public GameObject cauldron;
    
    [Tooltip("The cauldron's image component")]
    public Image cauldronImage;
    
    [Tooltip("Sprite shown when student succeeds")]
    public Sprite cauldronSuccessSprite;
    
    [Tooltip("Sprite shown when student fails")]
    public Sprite cauldronFailureSprite;
    
    [Tooltip("Enable gentle bobbing animation for cauldron")]
    public bool enableCauldronIdle = true;
    
    #endregion

    #region UI References - Potion Collection
    
    [Header("═══ POTION COLLECTION ═══")]
    [Tooltip("Container where collected potions appear (top-right area)")]
    public Transform potionCollectionArea;
    
    [Tooltip("Prefab for collected potion visual")]
    public GameObject collectedPotionPrefab;
    
    [Tooltip("Good potion color (when stage has correct answers)")]
    public Color goodPotionColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    
    [Tooltip("Bad potion color (when stage has no correct answers)")]
    public Color badPotionColor = new Color(0.8f, 0.2f, 0.2f, 1f);
    
    #endregion

    #region UI References - Confirmation Dialogs
    
    [Header("═══ ANSWER CONFIRMATION DIALOG ═══")]
    [Tooltip("Panel that appears when student clicks a potion")]
    public GameObject answerConfirmationPanel;
    
    [Tooltip("Text showing 'Are you sure you want TRUE/FALSE?'")]
    public TextMeshProUGUI answerConfirmationText;
    
    [Tooltip("Button to confirm the answer selection")]
    public Button confirmAnswerButton;
    
    [Tooltip("Button to cancel and choose again")]
    public Button cancelAnswerButton;
    
    [Header("═══ STAGE SUBMISSION DIALOG ═══")]
    [Tooltip("Panel that appears when ready to submit stage")]
    public GameObject stageSubmissionPanel;
    
    [Tooltip("Text in stage submission panel")]
    public TextMeshProUGUI stageSubmissionText;
    
    [Tooltip("Button to confirm stage submission")]
    public Button confirmStageButton;
    
    [Tooltip("Button to review answers before submitting")]
    public Button reviewStageButton;
    
    [Tooltip("Button that appears after answering last question in stage")]
    public GameObject submitStageButton;
    
    #endregion

    #region UI References - Review System
    
    [Header("═══ REVIEW SYSTEM ═══")]
    [Tooltip("Panel showing all answers in current stage for review")]
    public GameObject reviewPanel;
    
    [Tooltip("Container where review items are spawned")]
    public Transform reviewContainer;
    
    [Tooltip("Prefab for each review question item")]
    public GameObject reviewItemPrefab;
    
    [Tooltip("Button to close review panel")]
    public Button closeReviewButton;
    
    #endregion

    #region UI References - Results
    
    [Header("═══ RESULTS SCREEN ═══")]
    [Tooltip("Panel showing final results")]
    public GameObject resultPanel;
    
    [Tooltip("Title text (SUCCESS! or FAILED!)")]
    public TextMeshProUGUI resultTitleText;
    
    [Tooltip("Score display (Score: X/Y)")]
    public TextMeshProUGUI resultScoreText;
    
    [Tooltip("Icon image for result")]
    public Image resultIcon;
    
    [Tooltip("Icon for success")]
    public Sprite successIcon;
    
    [Tooltip("Icon for failure")]
    public Sprite failureIcon;
    
    [Tooltip("Button to restart the game")]
    public Button restartButton;
    
    [Tooltip("Button to exit")]
    public Button exitButton;
    
    #endregion

    #region Animation Settings
    
    [Header("═══ ANIMATION SETTINGS ═══")]
    [Tooltip("How long potion hover animation takes")]
    public float potionHoverScale = 1.1f;
    
    [Tooltip("Speed of potion hover animation")]
    public float potionAnimSpeed = 5f;
    
    [Tooltip("Delay between pouring each potion")]
    public float potionPourDelay = 0.5f;
    
    [Tooltip("Duration of potion pouring animation")]
    public float potionPourDuration = 0.8f;
    
    [Tooltip("Cauldron idle bob speed")]
    public float cauldronBobSpeed = 1f;
    
    [Tooltip("Cauldron idle bob height")]
    public float cauldronBobAmount = 0.05f;
    
    [Tooltip("Duration of cauldron success animation")]
    public float cauldronSuccessDuration = 1f;
    
    [Tooltip("Duration of cauldron failure shake")]
    public float cauldronFailureDuration = 0.5f;
    
    [Tooltip("Wait time before showing results")]
    public float resultDisplayDelay = 1.5f;
    
    [Tooltip("Success threshold (correct answers needed)")]
    public int successThreshold = 4;
    
    #endregion

    #region Private Variables
    
    private List<DifficultyStage> difficultyStages = new List<DifficultyStage>();
    private int currentQuestionIndex = 0;
    private int currentStageIndex = 0;
    private int totalCorrectAnswers = 0;
    private bool selectedAnswer;
    private bool isAnswerSelected = false;
    
    private List<GameObject> collectedPotions = new List<GameObject>();
    private Vector3 cauldronOriginalPos;
    private float cauldronIdleTimer = 0f;
    
    private Vector3 truePotionOriginalScale;
    private Vector3 falsePotionOriginalScale;
    private bool isTruePotionHovered = false;
    private bool isFalsePotionHovered = false;
    
    #endregion

    #region Unity Lifecycle
    
    void Start()
    {
        InitializeGame();
        SetupButtonListeners();
        DisplayCurrentQuestion();
    }

    void Update()
    {
        UpdatePotionHoverAnimations();
        UpdateCauldronIdleAnimation();
    }
    
    #endregion

    #region Initialization
    
    void InitializeGame()
    {
        currentQuestionIndex = 0;
        currentStageIndex = 0;
        totalCorrectAnswers = 0;
        collectedPotions.Clear();
        isAnswerSelected = false;
        
        // Store original scales for potion animations
        if (truePotionButton != null)
            truePotionOriginalScale = truePotionButton.transform.localScale;
        if (falsePotionButton != null)
            falsePotionOriginalScale = falsePotionButton.transform.localScale;
        
        // Store original cauldron position
        if (cauldron != null)
            cauldronOriginalPos = cauldron.transform.localPosition;
        
        // Generate difficulty stages
        GenerateDifficultyStages();
        
        // Hide all panels
        HideAllPanels();
    }

    void HideAllPanels()
    {
        if (answerConfirmationPanel != null) answerConfirmationPanel.SetActive(false);
        if (stageSubmissionPanel != null) stageSubmissionPanel.SetActive(false);
        if (reviewPanel != null) reviewPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (submitStageButton != null) submitStageButton.SetActive(false);
    }

    void GenerateDifficultyStages()
    {
        difficultyStages.Clear();
        
        // Group questions by difficulty
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

    void SetupButtonListeners()
    {
        // Potion buttons
        if (truePotionButton != null)
            truePotionButton.onClick.AddListener(() => OnPotionClicked(true));
        
        if (falsePotionButton != null)
            falsePotionButton.onClick.AddListener(() => OnPotionClicked(false));
        
        // Answer confirmation buttons
        if (confirmAnswerButton != null)
            confirmAnswerButton.onClick.AddListener(ConfirmAnswerSelection);
        
        if (cancelAnswerButton != null)
            cancelAnswerButton.onClick.AddListener(CancelAnswerSelection);
        
        // Stage submission buttons
        if (confirmStageButton != null)
            confirmStageButton.onClick.AddListener(SubmitCurrentStage);
        
        if (reviewStageButton != null)
            reviewStageButton.onClick.AddListener(ShowReviewPanel);
        
        // Review close button
        if (closeReviewButton != null)
            closeReviewButton.onClick.AddListener(CloseReviewPanel);
        
        // Result buttons
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
        
        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);
        
        // Submit stage button (created in UI)
        if (submitStageButton != null)
        {
            Button submitBtn = submitStageButton.GetComponent<Button>();
            if (submitBtn != null)
                submitBtn.onClick.AddListener(ShowStageSubmissionDialog);
        }
    }
    
    #endregion

    #region Question Display
    
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
            EnablePotionButtons(true);
        }
    }

    void UpdateStageInfo()
    {
        if (currentStageIndex >= difficultyStages.Count) return;
        
        DifficultyStage currentStage = difficultyStages[currentStageIndex];
        int stageQuestionNumber = currentQuestionIndex - currentStage.startIndex + 1;
        int totalStageQuestions = currentStage.endIndex - currentStage.startIndex + 1;
        
        if (stageInfoText != null)
        {
            stageInfoText.text = $"{currentStage.stageName}\nQuestion {stageQuestionNumber}/{totalStageQuestions}";
        }
        
        // Show submit button if at the end of stage and answer is selected
        bool isLastQuestionInStage = currentQuestionIndex == currentStage.endIndex;
        if (submitStageButton != null)
            submitStageButton.SetActive(isLastQuestionInStage && isAnswerSelected);
    }
    
    #endregion

    #region Potion Selection
    
    public void OnPotionClicked(bool isTrue)
    {
        selectedAnswer = isTrue;
        
        // Show confirmation dialog
        if (answerConfirmationPanel != null)
        {
            answerConfirmationPanel.SetActive(true);
            
            if (answerConfirmationText != null)
                answerConfirmationText.text = $"Are you sure you want to pick\n'{(isTrue ? "TRUE" : "FALSE")}' as your answer?";
        }
    }

    void ConfirmAnswerSelection()
    {
        // Hide confirmation panel
        if (answerConfirmationPanel != null)
            answerConfirmationPanel.SetActive(false);
        
        // Store the answer in current stage
        if (currentStageIndex < difficultyStages.Count)
        {
            DifficultyStage currentStage = difficultyStages[currentStageIndex];
            currentStage.selectedAnswers.Add(selectedAnswer);
        }
        
        isAnswerSelected = true;
        
        // Disable potion buttons after selection
        EnablePotionButtons(false);
        
        // Check if this is the last question in the stage
        DifficultyStage stage = difficultyStages[currentStageIndex];
        if (currentQuestionIndex == stage.endIndex)
        {
            // Show submit stage button
            if (submitStageButton != null)
                submitStageButton.SetActive(true);
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
        currentQuestionIndex++;
        
        if (currentQuestionIndex < allQuestions.Count)
        {
            DisplayCurrentQuestion();
        }
    }

    void EnablePotionButtons(bool enabled)
    {
        if (truePotionButton != null)
            truePotionButton.interactable = enabled;
        
        if (falsePotionButton != null)
            falsePotionButton.interactable = enabled;
    }
    
    #endregion

    #region Stage Submission
    
    void ShowStageSubmissionDialog()
    {
        if (stageSubmissionPanel != null)
        {
            DifficultyStage currentStage = difficultyStages[currentStageIndex];
            stageSubmissionPanel.SetActive(true);
            
            if (stageSubmissionText != null)
                stageSubmissionText.text = $"Submit your answers for {currentStage.stageName}?\n\nYou can review your answers before submitting.";
        }
    }

    void SubmitCurrentStage()
    {
        // Hide submission panel
        if (stageSubmissionPanel != null)
            stageSubmissionPanel.SetActive(false);
        
        if (currentStageIndex >= difficultyStages.Count) return;
        
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
        
        // Hide submit stage button
        if (submitStageButton != null)
            submitStageButton.SetActive(false);
        
        // Move to next stage or finish
        currentStageIndex++;
        
        if (currentStageIndex < difficultyStages.Count)
        {
            // Move to next stage
            currentQuestionIndex++;
            DisplayCurrentQuestion();
        }
        else
        {
            // All stages completed - pour potions and show results
            StartCoroutine(PourPotionsAndShowResults());
        }
    }
    
    #endregion

    #region Review System
    
    void ShowReviewPanel()
    {
        // Hide stage submission panel
        if (stageSubmissionPanel != null)
            stageSubmissionPanel.SetActive(false);
        
        // Show review panel
        if (reviewPanel != null)
        {
            reviewPanel.SetActive(true);
            PopulateReviewPanel();
        }
    }

    void CloseReviewPanel()
    {
        if (reviewPanel != null)
            reviewPanel.SetActive(false);
    }

    void PopulateReviewPanel()
    {
        if (reviewContainer == null) return;
        
        // Clear existing review items
        foreach (Transform child in reviewContainer)
        {
            Destroy(child.gameObject);
        }
        
        if (currentStageIndex >= difficultyStages.Count) return;
        
        DifficultyStage currentStage = difficultyStages[currentStageIndex];
        
        // Create review items for current stage
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
        if (reviewItemPrefab == null || reviewContainer == null) return;
        
        GameObject reviewItem = Instantiate(reviewItemPrefab, reviewContainer);
        
        int relativeQuestionNum = questionIndex - stageStartIndex + 1;
        int answerIndex = questionIndex - stageStartIndex;
        bool selectedAns = difficultyStages[currentStageIndex].selectedAnswers[answerIndex];
        
        // Set question text
        TextMeshProUGUI questionTxt = reviewItem.transform.Find("QuestionText")?.GetComponent<TextMeshProUGUI>();
        if (questionTxt != null)
            questionTxt.text = $"Q{relativeQuestionNum}: {allQuestions[questionIndex].questionText}";
        
        // Set answer text
        TextMeshProUGUI answerTxt = reviewItem.transform.Find("AnswerText")?.GetComponent<TextMeshProUGUI>();
        if (answerTxt != null)
            answerTxt.text = $"Your Answer: {(selectedAns ? "TRUE" : "FALSE")}";
        
        // Setup change button
        Button changeBtn = reviewItem.transform.Find("ChangeButton")?.GetComponent<Button>();
        if (changeBtn != null)
        {
            int capturedIndex = questionIndex;
            changeBtn.onClick.RemoveAllListeners();
            changeBtn.onClick.AddListener(() => ChangeAnswer(capturedIndex));
        }
    }

    void ChangeAnswer(int questionIndex)
    {
        // Close review panel
        if (reviewPanel != null)
            reviewPanel.SetActive(false);
        
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
            submitStageButton.SetActive(false);
        
        DisplayCurrentQuestion();
    }
    
    #endregion

    #region Potion Collection
    
    void AddCollectedPotion(bool hasCorrectAnswers)
    {
        if (collectedPotionPrefab == null || potionCollectionArea == null) return;
        
        GameObject potion = Instantiate(collectedPotionPrefab, potionCollectionArea);
        
        // Color the potion based on performance
        Image potionImg = potion.GetComponent<Image>();
        if (potionImg != null)
        {
            potionImg.color = hasCorrectAnswers ? goodPotionColor : badPotionColor;
        }
        
        collectedPotions.Add(potion);
    }
    
    #endregion

    #region Results & Animations
    
    IEnumerator PourPotionsAndShowResults()
    {
        // Hide gameplay UI
        if (truePotionButton != null) truePotionButton.gameObject.SetActive(false);
        if (falsePotionButton != null) falsePotionButton.gameObject.SetActive(false);
        if (questionText != null) questionText.gameObject.SetActive(false);
        if (stageInfoText != null) stageInfoText.gameObject.SetActive(false);
        
        // Pour each potion with animation
        foreach (GameObject potion in collectedPotions)
        {
            if (potion != null)
            {
                StartCoroutine(AnimatePotionPour(potion));
                yield return new WaitForSeconds(potionPourDelay);
            }
        }
        
        yield return new WaitForSeconds(resultDisplayDelay);
        
        // Determine success or failure
        bool isSuccess = totalCorrectAnswers >= successThreshold;
        
        // Update cauldron sprite
        if (cauldronImage != null)
        {
            cauldronImage.sprite = isSuccess ? cauldronSuccessSprite : cauldronFailureSprite;
        }
        
        // Play cauldron animation
        if (isSuccess)
            StartCoroutine(CauldronSuccessAnimation());
        else
            StartCoroutine(CauldronFailureAnimation());
        
        yield return new WaitForSeconds(1f);
        
        // Show results
        ShowResultPanel(isSuccess);
    }

    IEnumerator AnimatePotionPour(GameObject potion)
    {
        if (cauldron == null || potion == null) yield break;
        
        float elapsed = 0f;
        Vector3 startScale = potion.transform.localScale;
        Vector3 targetPos = cauldron.transform.position;
        Vector3 startPos = potion.transform.position;
        
        while (elapsed < potionPourDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / potionPourDuration;
            
            // Move towards cauldron
            potion.transform.position = Vector3.Lerp(startPos, targetPos, t);
            
            // Scale down
            potion.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            
            yield return null;
        }
        
        Destroy(potion);
    }

    IEnumerator CauldronSuccessAnimation()
    {
        if (cauldron == null) yield break;
        
        float elapsed = 0f;
        float halfDuration = cauldronSuccessDuration / 2f;
        Vector3 startScale = cauldron.transform.localScale;
        Vector3 targetScale = startScale * 1.2f;
        
        // Scale up
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            cauldron.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        
        // Scale back down
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            cauldron.transform.localScale = Vector3.Lerp(targetScale, startScale, t);
            yield return null;
        }
        
        cauldron.transform.localScale = startScale;
    }

    IEnumerator CauldronFailureAnimation()
    {
        if (cauldron == null) yield break;
        
        float elapsed = 0f;
        float shakeAmount = 10f;
        
        while (elapsed < cauldronFailureDuration)
        {
            elapsed += Time.deltaTime;
            
            float xOffset = Random.Range(-shakeAmount, shakeAmount);
            cauldron.transform.localPosition = cauldronOriginalPos + new Vector3(xOffset, 0, 0);
            
            yield return null;
        }
        
        cauldron.transform.localPosition = cauldronOriginalPos;
    }

    void ShowResultPanel(bool isSuccess)
    {
        if (resultPanel == null) return;
        
        resultPanel.SetActive(true);
        
        // Set title
        if (resultTitleText != null)
        {
            resultTitleText.text = isSuccess ? "SUCCESS!" : "FAILED!";
            resultTitleText.color = isSuccess ? Color.green : Color.red;
        }
        
        // Set score
        if (resultScoreText != null)
        {
            resultScoreText.text = $"Score: {totalCorrectAnswers}/{allQuestions.Count}";
        }
        
        // Set icon
        if (resultIcon != null)
        {
            resultIcon.sprite = isSuccess ? successIcon : failureIcon;
            resultIcon.color = isSuccess ? Color.green : Color.red;
        }
    }
    
    #endregion

    #region Idle Animations
    
    void UpdatePotionHoverAnimations()
    {
        // True potion hover
        if (truePotionButton != null)
        {
            Vector3 targetScale = isTruePotionHovered ? truePotionOriginalScale * potionHoverScale : truePotionOriginalScale;
            truePotionButton.transform.localScale = Vector3.Lerp(
                truePotionButton.transform.localScale, 
                targetScale, 
                Time.deltaTime * potionAnimSpeed
            );
        }
        
        // False potion hover
        if (falsePotionButton != null)
        {
            Vector3 targetScale = isFalsePotionHovered ? falsePotionOriginalScale * potionHoverScale : falsePotionOriginalScale;
            falsePotionButton.transform.localScale = Vector3.Lerp(
                falsePotionButton.transform.localScale, 
                targetScale, 
                Time.deltaTime * potionAnimSpeed
            );
        }
    }

    void UpdateCauldronIdleAnimation()
    {
        if (!enableCauldronIdle || cauldron == null) return;
        
        cauldronIdleTimer += Time.deltaTime * cauldronBobSpeed;
        float yOffset = Mathf.Sin(cauldronIdleTimer) * cauldronBobAmount;
        cauldron.transform.localPosition = cauldronOriginalPos + new Vector3(0, yOffset, 0);
    }

    // Call these from UI Event Triggers
    public void OnTruePotionHoverEnter() { isTruePotionHovered = true; }
    public void OnTruePotionHoverExit() { isTruePotionHovered = false; }
    public void OnFalsePotionHoverEnter() { isFalsePotionHovered = true; }
    public void OnFalsePotionHoverExit() { isFalsePotionHovered = false; }
    
    #endregion

    #region Game Control
    
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
        
        // Show gameplay UI
        if (truePotionButton != null) truePotionButton.gameObject.SetActive(true);
        if (falsePotionButton != null) falsePotionButton.gameObject.SetActive(true);
        if (questionText != null) questionText.gameObject.SetActive(true);
        if (stageInfoText != null) stageInfoText.gameObject.SetActive(true);
        
        // Hide all panels
        HideAllPanels();
        
        // Reset cauldron
        if (cauldronImage != null && cauldronFailureSprite != null)
            cauldronImage.sprite = cauldronFailureSprite;
        
        DisplayCurrentQuestion();
    }

    public void ExitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    #endregion
}
