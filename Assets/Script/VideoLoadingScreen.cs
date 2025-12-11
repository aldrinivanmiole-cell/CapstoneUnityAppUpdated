using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.UI;

public class VideoLoadingScreen : MonoBehaviour
{
    [Header("Video Player")]
    [SerializeField] private VideoPlayer videoPlayer;
    
    [Header("Video Clips by Gender")]
    [SerializeField] private VideoClip maleIntroVideo;
    [SerializeField] private VideoClip femaleIntroVideo;
    
    [Header("Scene to Load")]
    [SerializeField] private string sceneToLoad = "NEWMAP";
    
    [Header("Skip Button (Optional)")]
    [SerializeField] private Button skipButton;
    
    private bool hasLoadedScene = false;

    private void Start()
    {
        // Setup video player
        if (videoPlayer != null)
        {
            // Configure video player for Android compatibility
            ConfigureVideoPlayer();
            
            // Select video based on gender from SessionManager
            SelectVideoByGender();
            
            // Subscribe to video end event
            videoPlayer.loopPointReached += OnVideoFinished;
            
            // Prepare and play the video
            videoPlayer.Prepare();
            videoPlayer.prepareCompleted += OnVideoPrepared;
            
            Debug.Log("✅ Video loading screen started, preparing video...");
        }
        else
        {
            Debug.LogError("❌ VideoPlayer not assigned! Assigning default...");
            // Try to find VideoPlayer component
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer != null)
            {
                ConfigureVideoPlayer();
                SelectVideoByGender();
                videoPlayer.loopPointReached += OnVideoFinished;
                videoPlayer.Prepare();
                videoPlayer.prepareCompleted += OnVideoPrepared;
            }
            else
            {
                Debug.LogError("❌ No VideoPlayer found! Loading next scene immediately...");
                LoadNextScene();
            }
        }

        // Setup skip button
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(SkipVideo);
            Debug.Log("✅ Skip button connected");
        }
        else
        {
            // Try to find skip button by common names
            TryFindSkipButton();
        }
    }

    private void TryFindSkipButton()
    {
        string[] buttonNames = { "skip", "Skip", "SkipButton", "skipButton", "Skip Button" };
        
        foreach (string buttonName in buttonNames)
        {
            GameObject skipObj = GameObject.Find(buttonName);
            if (skipObj != null)
            {
                skipButton = skipObj.GetComponent<Button>();
                if (skipButton != null)
                {
                    skipButton.onClick.AddListener(SkipVideo);
                    Debug.Log($"✅ Found skip button by name: {buttonName}");
                    return;
                }
            }
        }
        
        Debug.Log("ℹ️ No skip button found (optional)");
    }

    private void ConfigureVideoPlayer()
    {
        if (videoPlayer == null) return;

        // Force Camera Far Plane render mode for Android compatibility
        videoPlayer.renderMode = VideoRenderMode.CameraFarPlane;
        
        // Set target camera (Main Camera)
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            videoPlayer.targetCamera = mainCamera;
            Debug.Log("✅ Video Player: Camera Far Plane mode with Main Camera");
        }
        else
        {
            Debug.LogError("❌ Main Camera not found! Video may not display.");
        }

        // Set aspect ratio mode
        videoPlayer.aspectRatio = VideoAspectRatio.FitHorizontally;
        
        // Audio settings
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        
        // Playback settings
        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = true;
        
        Debug.Log($"🎬 Video Player configured: RenderMode={videoPlayer.renderMode}, AspectRatio={videoPlayer.aspectRatio}");
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        Debug.Log("✅ Video prepared and ready to play!");
        videoPlayer.Play();
    }

    private void SelectVideoByGender()
    {
        if (videoPlayer == null) return;

        // Get gender from SessionManager
        string gender = "";
        if (SessionManager.Instance != null)
        {
            gender = SessionManager.Instance.gender;
            Debug.Log($"📹 Gender from SessionManager: '{gender}'");
        }
        else
        {
            Debug.LogWarning("⚠️ SessionManager not found, defaulting to male video");
        }

        // Select appropriate video
        if (!string.IsNullOrEmpty(gender) && gender.Trim().ToLower() == "female")
        {
            if (femaleIntroVideo != null)
            {
                videoPlayer.clip = femaleIntroVideo;
                Debug.Log("✅ Loaded FEMALE intro video");
            }
            else
            {
                Debug.LogError("❌ Female intro video not assigned! Using male video as fallback.");
                videoPlayer.clip = maleIntroVideo;
            }
        }
        else
        {
            if (maleIntroVideo != null)
            {
                videoPlayer.clip = maleIntroVideo;
                Debug.Log("✅ Loaded MALE intro video");
            }
            else
            {
                Debug.LogError("❌ Male intro video not assigned!");
            }
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        Debug.Log("✅ Video finished playing");
        LoadNextScene();
    }

    private void SkipVideo()
    {
        Debug.Log("⏭️ Video skipped by user");
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (hasLoadedScene) return;
        
        hasLoadedScene = true;
        
        Debug.Log($"🔄 Loading scene: {sceneToLoad}");
        SceneManager.LoadScene(sceneToLoad);
    }

    private void OnDestroy()
    {
        // Cleanup
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.prepareCompleted -= OnVideoPrepared;
        }
        
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(SkipVideo);
        }
    }
}
