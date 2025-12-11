using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Playables; // For Timeline
using UnityEngine.UI;        // For Button

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private PlayableDirector cutsceneTimeline;
    [SerializeField] private string sceneToLoad = "NEWMAP";
    [SerializeField] private Button skipButton; // Assign your UI button in Inspector

    private bool hasLoadedScene = false; // Prevent multiple scene loads

    private void Start()
    {
        // Force cutscene to play
        if (cutsceneTimeline != null)
        {
            cutsceneTimeline.Play();
            // Automatically load next scene when cutscene ends
            cutsceneTimeline.stopped += OnCutsceneFinished;
        }

        // Hook skip button
        if (skipButton != null)
        {
            Debug.Log("Skip button found and connected!");
            skipButton.onClick.AddListener(SkipCutscene);
        }
        else
        {
            Debug.LogError("Skip button is not assigned in Inspector!");
            // Try to find it by name as backup
            GameObject skipObj = GameObject.Find("skip");
            if (skipObj != null)
            {
                skipButton = skipObj.GetComponent<Button>();
                if (skipButton != null)
                {
                    Debug.Log("Found skip button by name!");
                    skipButton.onClick.AddListener(SkipCutscene);
                }
            }
        }
    }

    private void OnCutsceneFinished(PlayableDirector obj)
    {
        LoadNextScene();
    }

    public void SkipCutscene()
    {
        Debug.Log("SKIP BUTTON CLICKED - IT WORKS!");

        // Load the map scene
        SceneManager.LoadScene(sceneToLoad);
    }

    private void LoadNextScene()
    {
        Debug.Log("LoadNextScene called"); // Debug message

        // Prevent loading the scene multiple times
        if (hasLoadedScene)
        {
            Debug.Log("Scene already loaded, returning");
            return;
        }

        hasLoadedScene = true;
        Debug.Log($"Loading scene: {sceneToLoad}"); // Debug message
        SceneManager.LoadScene(sceneToLoad);
    }

    private void OnDestroy()
    {
        // Clean up event listener to prevent memory leaks
        if (cutsceneTimeline != null)
            cutsceneTimeline.stopped -= OnCutsceneFinished;

        if (skipButton != null)
            skipButton.onClick.RemoveListener(SkipCutscene);
    }
}