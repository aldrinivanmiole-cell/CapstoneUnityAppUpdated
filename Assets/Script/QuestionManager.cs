using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

[System.Serializable]
public class QuestionData
{
    public int id;
    public string question_description;
    public List<string> choices;
    public string correct_answer;
}

[System.Serializable]
public class QuestionServerResponse
{
    public string status;
    public string message;
}

public class QuestionManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text questionText;
    public List<Button> choiceButtons; // assign 4 buttons in inspector
    public TMP_Text progressText;
    public GameObject finishPanel;
    public TMP_Text scoreText;

    private List<QuestionData> questions = new List<QuestionData>();
    private int currentIndex = 0;
    private int correctCount = 0;
    private int studentId;
    private int classesId = 1; // optional: assign from your classroom scene

    void Start()
    {
        if (SessionManager.Instance == null)
        {
            Debug.LogError("SessionManager not found!");
            return;
        }

        studentId = SessionManager.Instance.StudentId;
        if (studentId <= 0)
        {
            Debug.LogError("Invalid studentId in session!");
            return;
        }

        finishPanel.SetActive(false);
        StartCoroutine(LoadQuestions());
    }

    IEnumerator LoadQuestions()
    {
        using (UnityWebRequest www = UnityWebRequest.Get("https://homequest-c3k7.onrender.com/get_questions?student_id=" + studentId))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error loading questions: " + www.error);
            }
            else
            {
                string json = www.downloadHandler.text;
                Debug.Log("Questions JSON: " + json);

                questions = JsonUtilityWrapper.FromJsonList<QuestionData>(json);
                if (questions.Count > 0)
                    ShowQuestion();
                else
                    Debug.LogWarning("No questions found.");
            }
        }
    }

    void ShowQuestion()
    {
        if (currentIndex >= questions.Count)
        {
            StartCoroutine(SaveScore());
            return;
        }

        var q = questions[currentIndex];
        questionText.text = q.question_description;
        progressText.text = $"Question {currentIndex + 1} of {questions.Count}";

        // Shuffle and show choices
        for (int i = 0; i < choiceButtons.Count; i++)
        {
            if (i < q.choices.Count)
            {
                string choiceText = q.choices[i];
                choiceButtons[i].gameObject.SetActive(true);
                choiceButtons[i].GetComponentInChildren<TMP_Text>().text = choiceText;
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => OnAnswerSelected(choiceText));
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    void OnAnswerSelected(string selected)
    {
        var q = questions[currentIndex];
        if (selected == q.correct_answer)
            correctCount++;

        currentIndex++;
        ShowQuestion();
    }

    IEnumerator SaveScore()
    {
        // Calculate percentage score for trophy system
        int percentageScore = (questions.Count > 0) ? (correctCount * 100) / questions.Count : 0;
        PlayerPrefs.SetInt("PlayerScore", percentageScore);
        PlayerPrefs.Save();
        Debug.Log($"✅ Score saved to PlayerPrefs: {percentageScore}%");

        WWWForm form = new WWWForm();
        form.AddField("student_id", studentId);
        form.AddField("classes_id", classesId);
        form.AddField("score", correctCount);

        using (UnityWebRequest www = UnityWebRequest.Post("https://homequest-c3k7.onrender.com/submit_score", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error saving score: " + www.error);
            }
            else
            {
                var response = JsonUtility.FromJson<QuestionServerResponse>(www.downloadHandler.text);
                Debug.Log("Score saved: " + response.message);
            }
        }

        // Wait 5 seconds then navigate to gameresult scene
        yield return new WaitForSeconds(5f);
        SceneManager.LoadScene("gameresult");
    }
}
