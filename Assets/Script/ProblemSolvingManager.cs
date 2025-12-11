using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

[System.Serializable]
public class PSAnswer
{
    public string answer_description;
    public int correct_answer;
}

[System.Serializable]
public class Problem
{
    public int id;
    public int assignment_id;
    public string question_description;
    public string tutorial_link; // ✅ Add tutorial link
    public List<PSAnswer> answers;
}

public class ProblemSolvingManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text questionText;
    public TMP_Text progressText;
    public TMP_InputField answerInput;
    public Button submitButton;
    public Button tutorialButton; // ✅ Button for tutorial
    public GameObject finishPanel;
    public TMP_Text scoreText;

    private List<Problem> problems = new List<Problem>();
    private int currentIndex = 0;
    private int studentId;
    private int assignmentId;
    private string currentTutorialLink = ""; // ✅ Store current tutorial link
    private Dictionary<int, string> studentAnswers = new Dictionary<int, string>(); // Store answers for submission

    void Start()
    {
        studentId = SessionManager.Instance.StudentId;
        finishPanel.SetActive(false);
        submitButton.onClick.AddListener(OnSubmitAnswer);
        tutorialButton.onClick.AddListener(OpenTutorialLink); // ✅ Add listener
        StartCoroutine(LoadProblems());
    }

    IEnumerator LoadProblems()
    {
        assignmentId = CurrentClassSession.SelectedCategoryId; // This is the assignment ID
        string url = $"https://homequest-c3k7.onrender.com/get_problems?student_id={studentId}&assignment_id={assignmentId}";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
                yield break;
            }

            string json = www.downloadHandler.text;
            problems = JsonUtilityWrapper.FromJsonList<Problem>(json);

            if (problems.Count > 0)
            {
                assignmentId = problems[0].assignment_id;
                Debug.Log("Assignment ID: " + assignmentId);
                ShowProblem();
            }
        }
    }

    void ShowProblem()
    {
        if (currentIndex >= problems.Count)
        {
            StartCoroutine(SaveScore());
            return;
        }

        var p = problems[currentIndex];
        questionText.text = p.question_description;
        progressText.text = $"Problem {currentIndex + 1} of {problems.Count}";
        answerInput.text = "";

        // ✅ Update tutorial link
        currentTutorialLink = p.tutorial_link;
        tutorialButton.gameObject.SetActive(!string.IsNullOrEmpty(currentTutorialLink));
    }

    void OpenTutorialLink()
    {
        if (!string.IsNullOrEmpty(currentTutorialLink))
            Application.OpenURL(currentTutorialLink);
    }

    void OnSubmitAnswer()
    {
        string userAnswer = answerInput.text.Trim();
        if (string.IsNullOrEmpty(userAnswer)) return;

        var p = problems[currentIndex];
        
        // Store answer for submission - teacher will verify and grade manually
        studentAnswers[p.id] = userAnswer;
        
        // Show finish panel temporarily with "Answer Submitted!" message
        finishPanel.SetActive(true);
        if (scoreText != null)
        {
            scoreText.text = "Answer Submitted!";
            scoreText.color = Color.white;
            scoreText.fontSize = 36;
            scoreText.gameObject.SetActive(true);
        }
        
        currentIndex++;
        
        // Wait a moment then show next problem
        StartCoroutine(ShowNextProblemDelayed());
    }

    IEnumerator ShowNextProblemDelayed()
    {
        yield return new WaitForSeconds(3f);
        
        // Hide feedback panel
        finishPanel.SetActive(false);
            
        ShowProblem();
    }

    IEnumerator SaveScore()
    {
        PlayerPrefs.SetInt("PlayerScore", 0);
        PlayerPrefs.Save();

        // Save answers without scoring - teacher will grade manually
        WWWForm form = new WWWForm();
        form.AddField("student_id", studentId);
        form.AddField("assignment_id", assignmentId);
        
        // Convert answers dictionary to JSON
        string answersJson = "{";
        int count = 0;
        foreach (var kvp in studentAnswers)
        {
            if (count > 0) answersJson += ",";
            answersJson += $"\"{kvp.Key}\":\"{kvp.Value}\"";
            count++;
        }
        answersJson += "}";
        
        form.AddField("answers_json", answersJson);
        form.AddField("score", 0); // No auto-scoring, teacher will grade
        form.AddField("total_points", problems.Count);

        using (UnityWebRequest www = UnityWebRequest.Post("https://homequest-c3k7.onrender.com/submit_score2", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
                Debug.LogError("❌ Error submitting answers: " + www.error);
            else
                Debug.Log("✅ Answers submitted for teacher grading!");
        }

        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene("classroom");
    }
}