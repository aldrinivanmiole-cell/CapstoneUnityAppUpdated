using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TreasureHuntersOpening : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI dialogText;
    public GameObject nextButton;
    
    [Header("Scene Settings")]
    public string multipleChoiceSceneName = "IndianaJonesMultipleChoice"; // Scene to load after dialogue
    
    [Header("Start Button (Optional)")]
    public GameObject startButton; // Button that appears after dialogue ends (optional)
    
    private string[] dialog = {
        "WELCOME, BRAVE ADVENTURER, TO THE TEMPLE OF LOST KNOWLEDGE!",
        "LEGENDS SPEAK OF ANCIENT TREASURES HIDDEN WITHIN THESE SACRED HALLS.",
        "EACH TREASURE HOLDS A MYSTERY - SOME ARE GENUINE, OTHERS ARE MERE ILLUSIONS.",
        "TO CLAIM THE TRUE ARTIFACTS, YOU MUST ANSWER THE RIDDLES CARVED IN STONE.",
        "CHOOSE WISELY, FOR ONLY THE CORRECT TREASURES WILL GRANT YOU PASSAGE.",
        "THE JOURNEY BEGINS NOW. MAY YOUR WISDOM GUIDE YOU TO VICTORY!"
    };
    private int index = 0;
    private Coroutine typingCoroutine;
    public float typeSpeed = 0.04f; // seconds per character
    private bool isTyping = false;

    void Awake() {
        // Auto-connect button if not already connected
        if (nextButton != null) {
            Button btn = nextButton.GetComponent<Button>();
            if (btn != null) {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(Next);
            }
        }
        
        // Setup start button if provided
        if (startButton != null) {
            startButton.SetActive(false); // Hide initially
            Button startBtn = startButton.GetComponent<Button>();
            if (startBtn != null) {
                startBtn.onClick.RemoveAllListeners();
                startBtn.onClick.AddListener(StartGame);
            }
        }
    }

    void Start() {
        ShowDialog();
    }

    public void Next() {
        if (isTyping || typingCoroutine != null) {
            // If text is still animating, finish instantly
            if (typingCoroutine != null) {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }
            dialogText.text = dialog[index];
            isTyping = false;
            return;
        }
        index++;
        if (index < dialog.Length) {
            ShowDialog();
        } else {
            // Dialogue finished - show start button or auto-load scene
            OnDialogueComplete();
        }
    }
    
    void OnDialogueComplete() {
        // Hide dialog and next button
        if (dialogText != null) dialogText.gameObject.SetActive(false);
        if (nextButton != null) nextButton.SetActive(false);
        
        // Show start button if provided, otherwise auto-load scene
        if (startButton != null) {
            startButton.SetActive(true);
        } else {
            // Auto-load scene after a short delay
            StartCoroutine(LoadSceneAfterDelay(1f));
        }
    }
    
    public void StartGame() {
        Debug.Log($"🚀 Starting game! Loading scene: {multipleChoiceSceneName}");
        SceneManager.LoadScene(multipleChoiceSceneName);
    }
    
    System.Collections.IEnumerator LoadSceneAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        StartGame();
    }

    void ShowDialog() {
        if (typingCoroutine != null) {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        isTyping = true;
        typingCoroutine = StartCoroutine(TypeText(dialog[index]));
    }

    System.Collections.IEnumerator TypeText(string line) {
        dialogText.text = "";
        foreach (char c in line) {
            dialogText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
        isTyping = false;
        typingCoroutine = null;
    }
}
