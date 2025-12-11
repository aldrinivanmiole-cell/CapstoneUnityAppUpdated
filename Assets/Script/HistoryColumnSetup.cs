using UnityEngine;
using TMPro;

public class HistoryColumnSetup : MonoBehaviour
{
    [Header("Column Header References")]
    public TMP_Text numberHeader;
    public TMP_Text questionHeader;
    public TMP_Text yourAnswerHeader;
    public TMP_Text rightAnswerHeader;

    void Start()
    {
        SetupHeaders();
    }

    void SetupHeaders()
    {
        if (numberHeader != null)
            numberHeader.text = "#";
        
        if (questionHeader != null)
            questionHeader.text = "Question";
        
        if (yourAnswerHeader != null)
            yourAnswerHeader.text = "You Answered";
        
        if (rightAnswerHeader != null)
            rightAnswerHeader.text = "Right Answer";
    }
}
