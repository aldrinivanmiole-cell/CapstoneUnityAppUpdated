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
}

public class HistoryManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text titleText;
    public TMP_Text scoreText;
    public Transform contentContainer; // Assign 'Content' from ScrollView
    public GameObject historyRowPrefab; // Assign 'HistoryRow' prefab

    private int studentId;
    private string baseUrl = "https://homeworkquest.site/";

    void Start()
    {
        if (SessionManager.Instance == null)
        {
            Debug.LogError("SessionManager not found!");
            return;
        }

        studentId = SessionManager.Instance.StudentId;
        titleText.text = $"{SessionManager.Instance.Username}'s History";

        StartCoroutine(LoadTotalScore());
        StartCoroutine(LoadHistory());
    }

    IEnumerator LoadTotalScore()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(baseUrl + "get_score.php?student_id=" + studentId))
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
        using (UnityWebRequest www = UnityWebRequest.Get(baseUrl + "get_history.php?student_id=" + studentId))
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
                TMP_Text[] cols = row.GetComponentsInChildren<TMP_Text>();

                cols[0].text = index.ToString();
                cols[1].text = item.question_description;
                cols[2].text = item.player_answer;

                index++;
            }
        }
    }
}
