using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Treasure Hunters Outro - Simple ending scene after completing the treasure hunt
/// Shows results with adventure theme
/// </summary>
public class TreasureHuntersOutro : MonoBehaviour
{
    [Header("═══ UI ELEMENTS ═══")]
    [Tooltip("Main title text")]
    public TextMeshProUGUI titleText;
    
    [Tooltip("Score display text")]
    public TextMeshProUGUI scoreText;
    
    [Tooltip("Message text")]
    public TextMeshProUGUI messageText;
    
    [Header("═══ VISUAL ELEMENTS ═══")]
    [Tooltip("Trophy/treasure image")]
    public Image rewardImage;
    
    [Tooltip("Gold trophy sprite")]
    public Sprite goldTrophy;
    
    [Tooltip("Silver trophy sprite")]
    public Sprite silverTrophy;
    
    [Tooltip("Bronze trophy sprite")]
    public Sprite bronzeTrophy;
    
    [Tooltip("Character image")]
    public Image characterImage;
    
    [Tooltip("Male character sprite")]
    public Sprite maleCharacter;
    
    [Tooltip("Female character sprite")]
    public Sprite femaleCharacter;
    
    [Header("═══ BUTTONS ═══")]
    [Tooltip("Continue/Next button")]
    public Button continueButton;
    
    [Header("═══ ANIMATION SETTINGS ═══")]
    [Tooltip("Enable animations")]
    public bool enableAnimations = true;
    
    [Tooltip("Trophy reveal animation duration")]
    public float trophyRevealDuration = 1f;
    
    [Header("═══ SCENE SETTINGS ═══")]
    [Tooltip("Scene to load on continue (e.g., map or dashboard)")]
    public string nextSceneName = "map";
    
    // Outro messages
    private string[] outroMessages = {
        "Quest complete!",
        "You've found the treasures and proved yourself as a true explorer.",
        "Your points have been added to the leaderboard.",
        "Ready for the next adventure?"
    };
    
    private int currentMessageIndex = 0;
    private int finalScore;
    private float percentage;
    
    void Awake()
    {
        // Setup continue button
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
    }
    
    void Start()
    {
        // Load results
        LoadResults();
        
        // Load character gender
        LoadCharacter();
        
        if (enableAnimations)
        {
            StartCoroutine(PlayOutroSequence());
        }
        else
        {
            DisplayResults();
        }
    }
    
    void LoadResults()
    {
        // Get score from PlayerPrefs (set by the game manager before loading this scene)
        finalScore = PlayerPrefs.GetInt("TreasureHuntScore", 0);
        int totalQuestions = PlayerPrefs.GetInt("TotalQuestions", 10);
        percentage = totalQuestions > 0 ? (float)finalScore / (totalQuestions * 200) * 100f : 0f;
    }
    
    void LoadCharacter()
    {
        if (characterImage == null) return;
        
        string gender = PlayerPrefs.GetString("StudentGender", "male");
        
        if (gender.ToLower() == "female" && femaleCharacter != null)
        {
            characterImage.sprite = femaleCharacter;
        }
        else if (maleCharacter != null)
        {
            characterImage.sprite = maleCharacter;
        }
    }
    
    void DisplayResults()
    {
        // Set title
        if (titleText != null)
            titleText.text = "Quest Complete!";
        
        // Set score
        if (scoreText != null)
            scoreText.text = $"Score: {finalScore}";
        
        // Set trophy based on percentage
        if (rewardImage != null)
        {
            if (percentage >= 80 && goldTrophy != null)
            {
                rewardImage.sprite = goldTrophy;
                if (messageText != null)
                    messageText.text = "🏆 Outstanding! You're on fire.";
            }
            else if (percentage >= 60 && silverTrophy != null)
            {
                rewardImage.sprite = silverTrophy;
                if (messageText != null)
                    messageText.text = "⭐ Excellent work. Keep it up.";
            }
            else if (bronzeTrophy != null)
            {
                rewardImage.sprite = bronzeTrophy;
                if (messageText != null)
                    messageText.text = "📈 Good job. You're improving.";
            }
        }
    }
    
    IEnumerator PlayOutroSequence()
    {
        // Hide elements initially
        if (rewardImage != null) rewardImage.gameObject.SetActive(false);
        if (scoreText != null) scoreText.gameObject.SetActive(false);
        if (messageText != null) messageText.gameObject.SetActive(false);
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        
        // Title animation
        if (titleText != null)
        {
            titleText.text = "Quest Complete!";
            yield return StartCoroutine(FadeInText(titleText, 0.6f));
        }
        
        yield return new WaitForSeconds(0.5f);
        
        // Character celebration (optional scale animation)
        if (characterImage != null)
        {
            yield return StartCoroutine(BounceElement(characterImage.transform, 0.5f));
        }
        
        yield return new WaitForSeconds(0.3f);
        
        // Show trophy with dramatic reveal
        if (rewardImage != null)
        {
            rewardImage.gameObject.SetActive(true);
            
            // Set appropriate trophy
            if (percentage >= 80 && goldTrophy != null)
                rewardImage.sprite = goldTrophy;
            else if (percentage >= 60 && silverTrophy != null)
                rewardImage.sprite = silverTrophy;
            else if (bronzeTrophy != null)
                rewardImage.sprite = bronzeTrophy;
            
            yield return StartCoroutine(ScaleUpElement(rewardImage.transform, trophyRevealDuration));
        }
        
        yield return new WaitForSeconds(0.5f);
        
        // Show score
        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(true);
            scoreText.text = $"Score: {finalScore}";
            yield return StartCoroutine(FadeInText(scoreText, 0.4f));
        }
        
        yield return new WaitForSeconds(0.5f);
        
        // Show message
        if (messageText != null)
        {
            messageText.gameObject.SetActive(true);
            DisplayResults(); // Set the appropriate message
            yield return StartCoroutine(FadeInText(messageText, 0.5f));
        }
        
        yield return new WaitForSeconds(0.8f);
        
        // Show continue button
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            yield return StartCoroutine(ScaleUpElement(continueButton.transform, 0.4f));
        }
    }
    
    IEnumerator FadeInText(TextMeshProUGUI text, float duration)
    {
        if (text == null) yield break;
        
        Color originalColor = text.color;
        Color transparent = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        text.color = transparent;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            text.color = Color.Lerp(transparent, originalColor, elapsed / duration);
            yield return null;
        }
        text.color = originalColor;
    }
    
    IEnumerator ScaleUpElement(Transform element, float duration)
    {
        if (element == null) yield break;
        
        Vector3 originalScale = element.localScale;
        element.localScale = Vector3.zero;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Ease out elastic for bounce
            float scale = Mathf.Lerp(0f, 1.1f, t);
            if (t > 0.5f)
                scale = Mathf.Lerp(1.1f, 1f, (t - 0.5f) * 2f);
            element.localScale = originalScale * scale;
            yield return null;
        }
        element.localScale = originalScale;
    }
    
    IEnumerator BounceElement(Transform element, float duration)
    {
        if (element == null) yield break;
        
        Vector3 originalScale = element.localScale;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = 1f + Mathf.Sin(t * Mathf.PI * 2f) * 0.15f;
            element.localScale = originalScale * scale;
            yield return null;
        }
        element.localScale = originalScale;
    }
    
    void OnContinueClicked()
    {
        Debug.Log($"📍 Continuing to: {nextSceneName}");
        
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("⚠️ Next scene name is not set!");
        }
    }
    
    /// <summary>
    /// Call this static method from the game manager before loading the outro scene
    /// </summary>
    public static void SetResults(int score, int totalQuestions)
    {
        PlayerPrefs.SetInt("TreasureHuntScore", score);
        PlayerPrefs.SetInt("TotalQuestions", totalQuestions);
        PlayerPrefs.Save();
    }
}
