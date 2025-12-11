using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ResultAnimationController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultTitleText;
    public TextMeshProUGUI scoreText;
    public Image resultIcon;
    public Button restartButton;
    public Button exitButton;
    
    [Header("Icons")]
    public Sprite successIcon;
    public Sprite failureIcon;
    
    [Header("Animation Settings")]
    public float fadeDuration = 0.5f;
    public float scaleDuration = 0.5f;
    public float delayBeforeShow = 0.5f;
    
    [Header("Colors")]
    public Color successColor = Color.green;
    public Color failureColor = Color.red;
    
    private CanvasGroup canvasGroup;
    private RectTransform panelRect;

    void Awake()
    {
        canvasGroup = resultPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = resultPanel.AddComponent<CanvasGroup>();
        
        panelRect = resultPanel.GetComponent<RectTransform>();
        
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
        
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);
    }

    public void ShowResults(bool isSuccess, int score, int totalQuestions, System.Action onRestart = null, System.Action onExit = null)
    {
        StartCoroutine(ShowResultsRoutine(isSuccess, score, totalQuestions, onRestart, onExit));
    }

    IEnumerator ShowResultsRoutine(bool isSuccess, int score, int totalQuestions, System.Action onRestart, System.Action onExit)
    {
        // Wait before showing
        yield return new WaitForSeconds(delayBeforeShow);
        
        // Set initial state
        resultPanel.SetActive(true);
        canvasGroup.alpha = 0f;
        panelRect.localScale = Vector3.zero;
        
        // Set content
        if (resultTitleText != null)
        {
            resultTitleText.text = isSuccess ? "SUCCESS!" : "FAILED!";
            resultTitleText.color = isSuccess ? successColor : failureColor;
        }
        
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}/{totalQuestions}";
        }
        
        if (resultIcon != null)
        {
            resultIcon.sprite = isSuccess ? successIcon : failureIcon;
            resultIcon.color = isSuccess ? successColor : failureColor;
        }
        
        // Fade in and scale up
        float elapsed = 0f;
        while (elapsed < Mathf.Max(fadeDuration, scaleDuration))
        {
            elapsed += Time.deltaTime;
            
            // Fade
            if (elapsed < fadeDuration)
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            else
                canvasGroup.alpha = 1f;
            
            // Scale with bounce
            if (elapsed < scaleDuration)
            {
                float t = elapsed / scaleDuration;
                float bounceT = EaseOutBounce(t);
                panelRect.localScale = Vector3.one * bounceT;
            }
            else
            {
                panelRect.localScale = Vector3.one;
            }
            
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
        panelRect.localScale = Vector3.one;
    }

    public void HideResults()
    {
        StartCoroutine(HideResultsRoutine());
    }

    IEnumerator HideResultsRoutine()
    {
        float elapsed = 0f;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            panelRect.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, elapsed / fadeDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        panelRect.localScale = Vector3.zero;
        resultPanel.SetActive(false);
    }

    void OnRestartClicked()
    {
        // This will be set by the game manager
    }

    void OnExitClicked()
    {
        // This will be set by the game manager
    }

    // Easing function for bounce effect
    float EaseOutBounce(float t)
    {
        if (t < 1f / 2.75f)
        {
            return 7.5625f * t * t;
        }
        else if (t < 2f / 2.75f)
        {
            t -= 1.5f / 2.75f;
            return 7.5625f * t * t + 0.75f;
        }
        else if (t < 2.5f / 2.75f)
        {
            t -= 2.25f / 2.75f;
            return 7.5625f * t * t + 0.9375f;
        }
        else
        {
            t -= 2.625f / 2.75f;
            return 7.5625f * t * t + 0.984375f;
        }
    }
}
