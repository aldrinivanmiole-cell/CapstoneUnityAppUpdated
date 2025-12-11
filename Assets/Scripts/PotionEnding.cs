using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PotionEnding : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI endingText;
    public TextMeshProUGUI scoreText;
    public GameObject continueButton;
    
    [Header("Scene Settings")]
    public string nextSceneName = "gameresult"; // Scene to load after ending
    
    [Header("Potion Visuals (Optional)")]
    public Image potionImage; // Potion image to display
    public Sprite successPotionSprite; // Sprite for successful potion
    public Sprite failurePotionSprite; // Sprite for failed potion
    
    private int playerScore = 0;
    private int totalQuestions = 0;
    private int percentageScore = 0;
    
    void Start()
    {
        // Get score from PlayerPrefs
        playerScore = PlayerPrefs.GetInt("PlayerScore", 0);
        totalQuestions = PlayerPrefs.GetInt("TotalQuestions", 10);
        percentageScore = playerScore;
        
        // Calculate total questions from score if needed
        if (totalQuestions == 0 && playerScore > 0)
        {
            // Estimate based on percentage (assuming 100% = all correct)
            totalQuestions = 10; // Default fallback
        }
        
        // Setup continue button
        if (continueButton != null)
        {
            Button btn = continueButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(ContinueToResults);
            }
        }
        
        // Display ending based on score
        ShowEnding();
    }
    
    void ShowEnding()
    {
        // Update potion image based on performance
        if (potionImage != null)
        {
            if (percentageScore >= 80)
            {
                if (successPotionSprite != null)
                    potionImage.sprite = successPotionSprite;
                potionImage.color = new Color(0.3f, 0.8f, 0.3f); // Green glow
            }
            else if (percentageScore >= 60)
            {
                potionImage.color = new Color(1f, 0.6f, 0f); // Orange
            }
            else
            {
                if (failurePotionSprite != null)
                    potionImage.sprite = failurePotionSprite;
                potionImage.color = new Color(0.9f, 0.2f, 0.2f); // Red
            }
        }
        
        // Display score
        if (scoreText != null)
        {
            scoreText.text = $"You created {playerScore}% perfect potions!";
        }
        
        // Display ending message based on performance
        if (endingText != null)
        {
            string message = "";
            
            if (percentageScore >= 90)
            {
                message = "🌟 Master Alchemist! 🌟\n\nYour potion-making skills are extraordinary! You've successfully mixed chemicals and created perfect potions. Your knowledge of chemistry is truly impressive!";
            }
            else if (percentageScore >= 80)
            {
                message = "✨ Excellent Work! ✨\n\nYou're becoming a skilled alchemist! Most of your potions were successful. Keep practicing and you'll master the art of chemistry!";
            }
            else if (percentageScore >= 60)
            {
                message = "👍 Good Effort! 👍\n\nYou're on the right track! Some of your potions worked well. Study the chemical reactions more and you'll improve!";
            }
            else
            {
                message = "💪 Keep Learning! 💪\n\nChemistry is challenging, but don't give up! Review the chemical mixtures and try again. Every great alchemist started as a beginner!";
            }
            
            endingText.text = message;
        }
    }
    
    public void ContinueToResults()
    {
        Debug.Log("🧪 Continuing to results...");
        SceneManager.LoadScene(nextSceneName);
    }
}

