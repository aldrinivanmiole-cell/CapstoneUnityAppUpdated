using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;

[System.Serializable]
public class HistoryItem
{
    public int id;
    public string question_description;
    public string player_answer;
    public string correct_answer;
    public int is_correct; // 0 = wrong, 1 = correct
}

public class HistoryManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text titleText;
    public TMP_Text scoreText;
    public Transform contentContainer; // Assign 'Content' from ScrollView
    public GameObject historyRowPrefab; // Assign 'HistoryRow' prefab

    private int studentId;
    private string baseUrl = "https://homequest-c3k7.onrender.com/";

    void Start()
    {
        if (SessionManager.Instance == null)
        {
            Debug.LogWarning("SessionManager not found! Using default student ID.");
            studentId = 1; // Default student ID for testing
            if (titleText != null)
                titleText.text = "Student's History";
        }
        else
        {
            studentId = SessionManager.Instance.StudentId;
            if (titleText != null)
                titleText.text = $"{SessionManager.Instance.Username}'s History";
        }

        // Hide score text
        if (scoreText != null)
            scoreText.gameObject.SetActive(false);

        StartCoroutine(LoadHistory());
    }

    IEnumerator LoadTotalScore()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(baseUrl + "get_score?student_id=" + studentId))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
                scoreText.text = www.downloadHandler.text;
            else
                scoreText.text = "Error loading score";
        }
    }

    IEnumerator LoadHistory()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(baseUrl + "get_history?student_id=" + studentId))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error loading history: " + www.error);
                yield break;
            }

            string json = www.downloadHandler.text.Trim();
            if (string.IsNullOrEmpty(json) || !json.StartsWith("["))
            {
                Debug.LogWarning("No history data found.");
                yield break;
            }

            List<HistoryItem> historyList = JsonUtilityWrapper.FromJsonList<HistoryItem>(json);

            int index = 1;
            foreach (var item in historyList)
            {
                GameObject row = Instantiate(historyRowPrefab, contentContainer);
                
                // Try to get HistoryRow component
                HistoryRow rowComponent = row.GetComponent<HistoryRow>();
                
                // If not found, try to add it dynamically
                if (rowComponent == null)
                {
                    rowComponent = row.AddComponent<HistoryRow>();
                    Debug.Log("HistoryRow component added dynamically to prefab instance.");
                }
                
                if (rowComponent != null)
                {
                    // Find child TMP_Text components automatically
                    TMP_Text[] textComponents = row.GetComponentsInChildren<TMP_Text>();
                    if (textComponents.Length >= 4)
                    {
                        rowComponent.numberText = textComponents[0];
                        rowComponent.questionText = textComponents[1];
                        rowComponent.yourAnswerText = textComponents[2];
                        rowComponent.rightAnswerText = textComponents[3];
                    }
                    
                    rowComponent.SetData(index, item.question_description, item.player_answer, item.correct_answer);
                    rowComponent.SetAnswerStatus(item.is_correct == 1);
                }

                index++;
            }
        }
    }
}
