using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReviewQuestionItem : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI answerText;
    public Button changeButton;
    public Image backgroundImage;
    
    [Header("Colors")]
    public Color defaultColor = new Color(1f, 1f, 1f, 0.2f);
    public Color highlightColor = new Color(0.2f, 0.5f, 1f, 0.3f);
    
    private int questionIndex;
    private AlchemyTrueFalseManager gameManager;

    public void Initialize(int index, string question, string answer, AlchemyTrueFalseManager manager)
    {
        questionIndex = index;
        gameManager = manager;
        
        if (questionText != null)
            questionText.text = question;
        
        if (answerText != null)
            answerText.text = answer;
        
        if (changeButton != null)
            changeButton.onClick.AddListener(OnChangeClicked);
        
        if (backgroundImage != null)
            backgroundImage.color = defaultColor;
    }

    void OnChangeClicked()
    {
        // Highlight this item
        if (backgroundImage != null)
            backgroundImage.color = highlightColor;
    }

    public void SetHighlight(bool highlighted)
    {
        if (backgroundImage != null)
            backgroundImage.color = highlighted ? highlightColor : defaultColor;
    }
}
