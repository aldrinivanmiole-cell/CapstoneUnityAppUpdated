using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConfirmationDialog : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI messageText;
    public Button confirmButton;
    public Button cancelButton;
    public GameObject panel;
    
    [Header("Animation")]
    public bool useAnimation = true;
    public float animationDuration = 0.3f;
    
    private System.Action onConfirmCallback;
    private System.Action onCancelCallback;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null && panel != null)
            canvasGroup = panel.GetComponent<CanvasGroup>();
        
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);
        
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);
    }

    public void ShowDialog(string title, string message, System.Action onConfirm, System.Action onCancel = null)
    {
        if (titleText != null)
            titleText.text = title;
        
        if (messageText != null)
            messageText.text = message;
        
        onConfirmCallback = onConfirm;
        onCancelCallback = onCancel;
        
        if (panel != null)
            panel.SetActive(true);
        
        if (useAnimation && canvasGroup != null)
        {
            StartCoroutine(FadeIn());
        }
        else if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }

    public void HideDialog()
    {
        if (useAnimation && canvasGroup != null)
        {
            StartCoroutine(FadeOut());
        }
        else
        {
            if (panel != null)
                panel.SetActive(false);
        }
    }

    void OnConfirmClicked()
    {
        onConfirmCallback?.Invoke();
        HideDialog();
    }

    void OnCancelClicked()
    {
        onCancelCallback?.Invoke();
        HideDialog();
    }

    System.Collections.IEnumerator FadeIn()
    {
        float elapsed = 0f;
        canvasGroup.alpha = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / animationDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }

    System.Collections.IEnumerator FadeOut()
    {
        float elapsed = 0f;
        canvasGroup.alpha = 1f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / animationDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        
        if (panel != null)
            panel.SetActive(false);
    }
}
