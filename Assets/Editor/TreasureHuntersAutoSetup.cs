using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TreasureHuntersAutoSetup : EditorWindow
{
    [MenuItem("Tools/Setup Treasure Hunters Opening Scene")]
    public static void ShowWindow()
    {
        GetWindow<TreasureHuntersAutoSetup>("Treasure Hunters Setup");
    }

    void OnGUI()
    {
        if (GUILayout.Button("Auto-Connect Treasure Hunters Opening"))
        {
            AutoConnect();
        }
    }

    static void AutoConnect()
    {
        // Find SceneManager
        var sceneManager = GameObject.Find("SceneManager");
        if (sceneManager == null)
        {
            Debug.LogError("SceneManager GameObject not found!");
            return;
        }
        var script = sceneManager.GetComponent<TreasureHuntersOpening>();
        if (script == null)
        {
            Debug.LogError("TreasureHuntersOpening script not found on SceneManager!");
            return;
        }
        // Find DialogText
        var dialogText = GameObject.Find("DialogText");
        if (dialogText == null || dialogText.GetComponent<TextMeshProUGUI>() == null)
        {
            Debug.LogError("DialogText (TextMeshProUGUI) not found!");
            return;
        }
        // Find NextButton
        var nextButton = GameObject.Find("NextButton");
        if (nextButton == null || nextButton.GetComponent<Button>() == null)
        {
            Debug.LogError("NextButton (Button) not found!");
            return;
        }
        // Assign fields
        script.dialogText = dialogText.GetComponent<TextMeshProUGUI>();
        script.nextButton = nextButton;
        // Wire up button event
        var btn = nextButton.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(script.Next);
        Debug.Log("Treasure Hunters Opening auto-setup complete!");
    }
}
