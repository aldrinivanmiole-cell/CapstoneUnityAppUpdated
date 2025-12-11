using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TrophyDisplayManager : MonoBehaviour
{
    public Image trophyImage;
    public Sprite bronzeTrophy, silverTrophy, goldTrophy;
    public TMP_Text congratulationsText;

#if UNITY_EDITOR
    [MenuItem("GameObject/UI/Create Congratulations Text", false, 10)]
    static void CreateCongratulationsTextMenuItem()
    {
        // Check if it already exists
        if (GameObject.Find("CongratulationsText") != null)
        {
            Debug.LogWarning("CongratulationsText already exists!");
            return;
        }

        GameObject newTextObj = new GameObject("CongratulationsText");
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
            newTextObj.transform.SetParent(canvas.transform, false);
        else
        {
            Debug.LogError("No Canvas found in scene!");
            DestroyImmediate(newTextObj);
            return;
        }
        
        TextMeshProUGUI textComponent = newTextObj.AddComponent<TextMeshProUGUI>();
        RectTransform rectTransform = newTextObj.GetComponent<RectTransform>();
        
        // Position in bottom right corner (dark square area)
        rectTransform.anchorMin = new Vector2(0.55f, 0.02f);
        rectTransform.anchorMax = new Vector2(0.98f, 0.15f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        
        textComponent.text = "*** OUTSTANDING! ***\nYou're a SUPERSTAR!\nKeep up the AMAZING work!";
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.fontSize = 55;
        textComponent.fontStyle = FontStyles.Bold;
        textComponent.textWrappingMode = TextWrappingModes.Normal;
        textComponent.color = new Color(1f, 0.84f, 0f); // Gold color for preview

        Selection.activeGameObject = newTextObj;
        Undo.RegisterCreatedObjectUndo(newTextObj, "Create Congratulations Text");
        
        Debug.Log("✅ CongratulationsText created! You can now see and adjust it in the Scene view.");
    }
#endif

    void Start()
    {
        int score = PlayerPrefs.GetInt("PlayerScore", 0); // Default to 0 if none

        // Try to find the text if not assigned
        if (congratulationsText == null)
        {
            GameObject textObj = GameObject.Find("CongratulationsText");
            if (textObj != null)
                congratulationsText = textObj.GetComponent<TMP_Text>();
        }
        
        // If still not found, create it dynamically
        if (congratulationsText == null)
        {
            GameObject newTextObj = new GameObject("CongratulationsText");
            newTextObj.transform.SetParent(GameObject.Find("Canvas").transform, false);
            
            congratulationsText = newTextObj.AddComponent<TextMeshProUGUI>();
            RectTransform rectTransform = newTextObj.GetComponent<RectTransform>();
            
            // Position in bottom right corner (dark square area), away from the DONE button
            rectTransform.anchorMin = new Vector2(0.55f, 0.02f);
            rectTransform.anchorMax = new Vector2(0.98f, 0.15f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            
            congratulationsText.alignment = TextAlignmentOptions.Center;
            congratulationsText.fontSize = 52;
            congratulationsText.fontStyle = FontStyles.Bold;
            congratulationsText.textWrappingMode = TextWrappingModes.Normal;
        }

        string messageText = "";
        Color messageColor = Color.white;

        if (score >= 90)
        {
            trophyImage.sprite = goldTrophy;
            messageText = "*** OUTSTANDING! ***\nYou're a SUPERSTAR!\nKeep up the AMAZING work!";
            messageColor = new Color(1f, 0.84f, 0f); // Gold color
        }
        else if (score >= 70)
        {
            trophyImage.sprite = silverTrophy;
            messageText = "** GREAT JOB! **\nYou did AWESOME!\nYou're almost perfect!";
            messageColor = new Color(0.75f, 0.75f, 0.75f); // Silver color
        }
        else
        {
            trophyImage.sprite = bronzeTrophy;
            messageText = "* GOOD EFFORT! *\nYou're doing GREAT!\nKeep practicing & you'll shine!";
            messageColor = new Color(0.8f, 0.5f, 0.2f); // Bronze color
        }

        trophyImage.gameObject.SetActive(true);
        
        if (congratulationsText != null)
        {
            congratulationsText.text = messageText;
            congratulationsText.color = messageColor;
            congratulationsText.fontSize = 28;
            congratulationsText.alignment = TextAlignmentOptions.Center;
            congratulationsText.gameObject.SetActive(true);
            Debug.Log("✅ Congratulations message displayed: " + messageText);
        }
        else
        {
            Debug.LogWarning("⚠️ CongratulationsText not found! Please add a TMP_Text component to the scene.");
        }
    }
}
