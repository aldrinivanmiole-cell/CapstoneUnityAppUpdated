using UnityEngine;
using TMPro;

public class HistoryRow : MonoBehaviour
{
    [Header("Column Text References")]
    public TMP_Text numberText;
    public TMP_Text questionText;
    public TMP_Text yourAnswerText;
    public TMP_Text rightAnswerText;

    /// <summary>
    /// Set the data for this history row
    /// </summary>
    public void SetData(int number, string question, string yourAnswer, string rightAnswer)
    {
        if (numberText != null)
            numberText.text = number.ToString();
        else
            Debug.LogWarning("HistoryRow: numberText is not assigned!");

        if (questionText != null)
            questionText.text = question;
        else
            Debug.LogWarning("HistoryRow: questionText is not assigned!");

        if (yourAnswerText != null)
            yourAnswerText.text = yourAnswer;
        else
            Debug.LogWarning("HistoryRow: yourAnswerText (You Answered) is not assigned!");

        if (rightAnswerText != null)
            rightAnswerText.text = rightAnswer;
        else
            Debug.LogWarning("HistoryRow: rightAnswerText (Right Answer) is not assigned!");
    }

    /// <summary>
    /// Highlight the row if answer is correct or wrong
    /// </summary>
    public void SetAnswerStatus(bool isCorrect)
    {
        if (yourAnswerText != null && rightAnswerText != null)
        {
            if (isCorrect)
            {
                // Green color for correct answer
                yourAnswerText.color = new Color(0.2f, 0.8f, 0.2f); // Green
            }
            else
            {
                // Red color for wrong answer
                yourAnswerText.color = new Color(0.8f, 0.2f, 0.2f); // Red
            }
        }
    }
}
