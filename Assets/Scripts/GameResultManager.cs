using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class GameResultState
{
    public static bool HasPendingResult { get; private set; }
    public static int Score { get; private set; }
    public static int TotalQuestions { get; private set; }
    public static int WrongCount { get; private set; }
    public static float TimeTaken { get; private set; }
    public static string NextActivityScene { get; private set; }
    public static string MainMenuScene { get; private set; }

    public static void SetResult(int score, int totalQuestions, int wrongCount, float timeTaken, string nextActivityScene = "", string mainMenuScene = "NewMap")
    {
        Score = Mathf.Max(0, score);
        TotalQuestions = Mathf.Max(0, totalQuestions);
        WrongCount = Mathf.Max(0, wrongCount);
        TimeTaken = Mathf.Max(0f, timeTaken);
        NextActivityScene = string.IsNullOrWhiteSpace(nextActivityScene) ? string.Empty : nextActivityScene.Trim();
        MainMenuScene = string.IsNullOrWhiteSpace(mainMenuScene) ? "NewMap" : mainMenuScene.Trim();
        HasPendingResult = true;

        PlayerPrefs.SetInt(GameResultManager.ScoreKey, Score);
        PlayerPrefs.SetInt(GameResultManager.TotalQuestionsKey, TotalQuestions);
        PlayerPrefs.SetInt(GameResultManager.WrongCountKey, WrongCount);
        PlayerPrefs.SetFloat(GameResultManager.TimeTakenKey, TimeTaken);
        PlayerPrefs.SetString(GameResultManager.NextActivitySceneKey, NextActivityScene);
        PlayerPrefs.SetString(GameResultManager.MainMenuSceneKey, MainMenuScene);
        PlayerPrefs.Save();
    }

    public static void Clear()
    {
        HasPendingResult = false;
    }
}

public class GameResultManager : MonoBehaviour
{
    public const string ScoreKey = "GameResult_Score";
    public const string TotalQuestionsKey = "GameResult_TotalQuestions";
    public const string WrongCountKey = "GameResult_WrongCount";
    public const string TimeTakenKey = "GameResult_TimeTaken";
    public const string NextActivitySceneKey = "GameResult_NextScene";
    public const string MainMenuSceneKey = "GameResult_MainMenuScene";

    [Header("Banner")]
    [SerializeField] private Animator bannerAnimator;
    [SerializeField] private Animator subtitleAnimator;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Stats Panel")]
    [SerializeField] private Animator statsPanelAnimator;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI correctText;
    [SerializeField] private TextMeshProUGUI wrongText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI trophyScoreText;
    [SerializeField] private Image stopwatchIcon;

    [Header("Trophy")]
    [SerializeField] private RectTransform trophyAwardRoot;
    [SerializeField] private Image trophyImage;
    [SerializeField] private TextMeshProUGUI trophyLabelText;
    [SerializeField] private CanvasGroup trophyLabelCanvasGroup;
    [SerializeField] private RectTransform starsRow;

    // Trophy sprites from Assets/images. Assign these in Inspector.
    [SerializeField] private Sprite bronzeTrophy;
    [SerializeField] private Sprite silverTrophy;
    [SerializeField] private Sprite goldTrophy;

    [Header("Trophy Timing")]
    [SerializeField] private float secondsAllowedPerQuestion = 25f;
    [SerializeField] private float minimumAllowedTimeSeconds = 60f;
    [SerializeField] private float trophyPrePause = 0.12f;
    [SerializeField] private float trophyPostPause = 0.2f;
    [SerializeField] private float labelFadeDuration = 0.3f;

    [Header("Layout Overrides (Optional)")]
    [SerializeField] private bool applyRuntimeLayout;
    [SerializeField] private bool enforceRuntimeSceneLayout = true;
    [SerializeField] private Vector2 trophyAwardAnchoredPosition = new Vector2(0f, 196f);
    [SerializeField] private Vector2 trophyAwardSize = new Vector2(300f, 230f);
    [SerializeField] private Vector2 trophyImageAnchoredPosition = new Vector2(0f, -24f);
    [SerializeField] private Vector2 trophyImageSize = new Vector2(136f, 136f);
    [SerializeField] private Vector2 trophyLabelAnchoredPosition = new Vector2(0f, 28f);
    [SerializeField] private Vector2 trophyLabelSize = new Vector2(520f, 56f);
    [SerializeField] private float trophyLabelFontSize = 18f;

    [Header("Scene Layout Targets")]
    [SerializeField] private Vector2 bannerSize = new Vector2(360f, 120f);
    [SerializeField] private Vector2 bannerPosition = new Vector2(0f, 188f);
    [SerializeField] private Vector2 panelSize = new Vector2(700f, 330f);
    [SerializeField] private Vector2 panelPosition = new Vector2(0f, -12f);
    [SerializeField] private Vector2 headerPosition = new Vector2(0f, 112f);
    [SerializeField] private Vector2 scorePosition = new Vector2(0f, 74f);
    [SerializeField] private Vector2 statsGridSize = new Vector2(640f, 170f);
    [SerializeField] private Vector2 statsGridPosition = new Vector2(0f, -26f);
    [SerializeField] private Vector2 buttonsRowSize = new Vector2(260f, 70f);
    [SerializeField] private Vector2 buttonsRowPosition = new Vector2(0f, -220f);
    [SerializeField] private Vector2 mainButtonSize = new Vector2(180f, 52f);
    [SerializeField] private Vector2[] statCardPositions =
    {
        new Vector2(-210f, -16f),
        new Vector2(0f, -16f),
        new Vector2(210f, -16f)
    };
    [SerializeField] private Vector2 statCardSize = new Vector2(190f, 96f);

    [Header("Trophy Particle Tunings")]
    [SerializeField] private int goldBurstCount = 34;
    [SerializeField] private int silverBurstCount = 24;
    [SerializeField] private int bronzeBurstCount = 18;

    [Header("Buttons")]
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button nextActivityButton;
    [SerializeField] private Animator buttonsAnimator;
    [SerializeField] private Animator nextButtonPulseAnimator;

    [Header("Mascot")]
    [SerializeField] private Image mascotImage;
    [SerializeField] private Sprite femaleMascotSprite;
    [SerializeField] private Sprite maleMascotSprite;

    [Header("Animation Timing")]
    [SerializeField] private float bannerDelay = 0.1f;
    [SerializeField] private float subtitleDelay = 0.35f;
    [SerializeField] private float statsDelay = 0.28f;
    [SerializeField] private float buttonsDelay = 0.35f;

    [Header("Scene Targets")]
    [SerializeField] private string fallbackNextActivityScene = "classroom";
    [SerializeField] private string fallbackMainMenuScene = "NewMap";

    private int score;
    private int totalQuestions;
    private int wrongCount;
    private float timeTaken;
    private string nextActivityScene;
    private string mainMenuScene;

    // Trophy thresholds (easy to tune by teachers/developers):
    // Bronze: <= 60, Silver: 61-79, Gold: >= 80.
    private const float BronzeMaxThreshold = 60f;
    private const float SilverMaxThreshold = 79f;

    private enum TrophyTier
    {
        Bronze,
        Silver,
        Gold
    }

    private TrophyTier currentTrophyTier = TrophyTier.Bronze;
    private float trophyScore;
    private RectTransform trophyRect;
    private Vector2 trophyBaseAnchoredPos;
    private ParticleSystem trophyBurstParticles;
    private ParticleSystem trophyShimmerParticles;
    private Coroutine trophyFloatRoutine;

    private void Awake()
    {
        LoadResultData();
        ResolveTrophySpriteReferences();
        ApplyMascotSprite();
        BindButtons();
        ApplyStats();
        ApplyRuntimeSceneLayout();
        PrepareInitialVisualState();
    }

    private void ResolveTrophySpriteReferences()
    {
        if (bronzeTrophy != null && silverTrophy != null && goldTrophy != null)
            return;

#if UNITY_EDITOR
        if (bronzeTrophy == null)
            bronzeTrophy = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/images/bronzetrophy.png");
        if (silverTrophy == null)
            silverTrophy = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/images/silvertrophy.png");
        if (goldTrophy == null)
            goldTrophy = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/images/goldtrophy.png");
#endif

        if (bronzeTrophy == null)
            bronzeTrophy = FindLoadedSpriteByName("bronzetrophy");
        if (silverTrophy == null)
            silverTrophy = FindLoadedSpriteByName("silvertrophy");
        if (goldTrophy == null)
            goldTrophy = FindLoadedSpriteByName("goldtrophy");
    }

    private Sprite FindLoadedSpriteByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        string key = name.Trim().ToLowerInvariant();
        Sprite[] loaded = Resources.FindObjectsOfTypeAll<Sprite>();
        for (int i = 0; i < loaded.Length; i++)
        {
            Sprite sprite = loaded[i];
            if (sprite == null || string.IsNullOrWhiteSpace(sprite.name))
                continue;

            if (sprite.name.Trim().ToLowerInvariant() == key)
                return sprite;
        }

        return null;
    }

    private void Start()
    {
        StartCoroutine(PlaySequence());
    }

    private void LoadResultData()
    {
        if (GameResultState.HasPendingResult)
        {
            score = GameResultState.Score;
            totalQuestions = GameResultState.TotalQuestions;
            wrongCount = GameResultState.WrongCount;
            timeTaken = GameResultState.TimeTaken;
            nextActivityScene = GameResultState.NextActivityScene;
            mainMenuScene = GameResultState.MainMenuScene;
            GameResultState.Clear();

            // Save to leaderboard when displaying results
            SaveToLeaderboard();
        }
        else
        {
            totalQuestions = Mathf.Max(0, PlayerPrefs.GetInt(TotalQuestionsKey, PlayerPrefs.GetInt("TotalQuestions", 0)));
            score = Mathf.Max(0, PlayerPrefs.GetInt(ScoreKey, 0));
            wrongCount = Mathf.Max(0, PlayerPrefs.GetInt(WrongCountKey, 0));
            timeTaken = Mathf.Max(0f, PlayerPrefs.GetFloat(TimeTakenKey, 0f));
            nextActivityScene = PlayerPrefs.GetString(NextActivitySceneKey, string.Empty);
            mainMenuScene = PlayerPrefs.GetString(MainMenuSceneKey, string.Empty);

            if (score <= 0 && totalQuestions > 0)
            {
                int percent = Mathf.Clamp(PlayerPrefs.GetInt("PlayerScore", 0), 0, 100);
                score = Mathf.RoundToInt(totalQuestions * (percent / 100f));
            }

            if (wrongCount <= 0 && totalQuestions > 0)
                wrongCount = Mathf.Max(0, totalQuestions - score);
        }

        if (totalQuestions < score)
            totalQuestions = score;

        if (string.IsNullOrWhiteSpace(nextActivityScene))
            nextActivityScene = fallbackNextActivityScene;
        if (string.IsNullOrWhiteSpace(mainMenuScene))
            mainMenuScene = fallbackMainMenuScene;

        float scorePercentage = totalQuestions > 0 ? (score / (float)totalQuestions) * 100f : 0f;
        float allowedTime = Mathf.Max(minimumAllowedTimeSeconds, totalQuestions * secondsAllowedPerQuestion);
        float timeBonus = timeTaken <= (allowedTime * 0.5f) ? 10f : (timeTaken <= allowedTime ? 5f : 0f);
        trophyScore = Mathf.Clamp(scorePercentage + timeBonus, 0f, 100f);

        if (trophyScore <= BronzeMaxThreshold)
            currentTrophyTier = TrophyTier.Bronze;
        else if (trophyScore <= SilverMaxThreshold)
            currentTrophyTier = TrophyTier.Silver;
        else
            currentTrophyTier = TrophyTier.Gold;
    }

    private void SaveToLeaderboard()
    {
        // Get student information
        int studentId = 0;
        string studentName = "Student";

        if (SessionManager.Instance != null && SessionManager.Instance.StudentId > 0)
        {
            studentId = SessionManager.Instance.StudentId;
            studentName = SessionManager.Instance.StudentName ?? "Student";
        }
        else
        {
            // Fallback to stored session data
            studentId = PlayerPrefs.GetInt("SESSION_STUDENT_ID", 0);
            studentName = PlayerPrefs.GetString("SESSION_STUDENT_NAME", "Student");
        }

        if (studentId > 0 && score > 0)
        {
            // Calculate percentage score
            int percentageScore = totalQuestions > 0 ? Mathf.RoundToInt((score / (float)totalQuestions) * 100f) : 0;

            // Save to leaderboard
            LeaderboardStore.UpsertCompletion(studentId.ToString(), studentName, percentageScore, Mathf.RoundToInt(timeTaken));
        }
    }

    private void ApplyMascotSprite()
    {
        if (mascotImage == null)
            return;

        string gender = PlayerPrefs.GetString("SESSION_GENDER", "male").Trim().ToLowerInvariant();
        bool useFemale = gender == "female" || gender == "f";
        Sprite sprite = useFemale && femaleMascotSprite != null ? femaleMascotSprite : maleMascotSprite;
        if (sprite != null)
            mascotImage.sprite = sprite;
    }

    private void BindButtons()
    {
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(GoToMainMenu);
            AddPressEffect(mainMenuButton.gameObject);
        }

        if (nextActivityButton != null)
        {
            nextActivityButton.onClick.RemoveAllListeners();
            nextActivityButton.onClick.AddListener(GoToNextActivity);
            AddPressEffect(nextActivityButton.gameObject);
        }
    }

    private void AddPressEffect(GameObject target)
    {
        if (target == null)
            return;

        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = target.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        EventTrigger.Entry down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        down.callback.AddListener(_ => target.transform.localScale = Vector3.one * 0.95f);
        trigger.triggers.Add(down);

        EventTrigger.Entry up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        up.callback.AddListener(_ => target.transform.localScale = Vector3.one);
        trigger.triggers.Add(up);

        EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => target.transform.localScale = Vector3.one);
        trigger.triggers.Add(exit);
    }

    private void ApplyStats()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score} / {Mathf.Max(totalQuestions, 1)}";

        if (correctText != null)
            correctText.text = $"Correct: {score}";

        if (wrongText != null)
            wrongText.text = $"Wrong: {Mathf.Max(wrongCount, Mathf.Max(0, totalQuestions - score))}";

        if (timeText != null)
            timeText.text = $"Time: {FormatTime(timeTaken)}";

        if (trophyScoreText != null)
            trophyScoreText.text = $"Trophy: {currentTrophyTier} ({Mathf.RoundToInt(trophyScore)})";

        EnsureTrophyUiReferences();
        ApplyTrophyVisuals();
    }

    private void PrepareInitialVisualState()
    {
        if (subtitleText != null)
            subtitleText.gameObject.SetActive(true);

        if (buttonsAnimator != null)
            buttonsAnimator.gameObject.SetActive(true);

        if (starsRow != null)
            starsRow.gameObject.SetActive(false);

        if (trophyLabelCanvasGroup != null)
            trophyLabelCanvasGroup.alpha = 0f;

        if (trophyRect != null)
        {
            trophyRect.localScale = Vector3.zero;
            trophyRect.anchoredPosition = trophyBaseAnchoredPos + new Vector2(0f, 45f);
        }
    }

    private IEnumerator PlaySequence()
    {
        yield return new WaitForSeconds(bannerDelay);
        TriggerAnimator(bannerAnimator, "Show");

        yield return new WaitForSeconds(subtitleDelay);
        TriggerAnimator(subtitleAnimator, "Show");

        yield return new WaitForSeconds(trophyPrePause);
        yield return StartCoroutine(PlayTrophyEntrance());
        yield return new WaitForSeconds(trophyPostPause);

        yield return new WaitForSeconds(statsDelay);
        TriggerAnimator(statsPanelAnimator, "Show");

        if (trophyLabelCanvasGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(trophyLabelCanvasGroup, 0f, 1f, labelFadeDuration));

        yield return new WaitForSeconds(buttonsDelay);
        TriggerAnimator(buttonsAnimator, "Show");
        TriggerAnimator(nextButtonPulseAnimator, "StartPulse");

        // Keep final placement stable after animators trigger.
        if (enforceRuntimeSceneLayout)
            yield return StartCoroutine(EnforceLayoutForSeconds(0.9f));
    }

    private IEnumerator EnforceLayoutForSeconds(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            ApplyRuntimeSceneLayout();
            elapsed += Time.deltaTime;
            yield return null;
        }
        ApplyRuntimeSceneLayout();
    }

    private void ApplyRuntimeSceneLayout()
    {
        if (!enforceRuntimeSceneLayout)
            return;

        RectTransform canvasRoot = transform.root != null ? transform.root.Find("GameResultCanvas") as RectTransform : null;
        if (canvasRoot == null)
            return;

        RectTransform bannerHolder = canvasRoot.Find("BannerHolder") as RectTransform;
        RectTransform panel = canvasRoot.Find("StatsPanel") as RectTransform;
        RectTransform statsHeaderRect = panel != null ? panel.Find("StatsHeader") as RectTransform : null;
        RectTransform scoreRect = panel != null ? panel.Find("ScoreText") as RectTransform : null;
        RectTransform statsGridRect = panel != null ? panel.Find("StatsGrid") as RectTransform : null;
        RectTransform buttonsRowRect = canvasRoot.Find("ButtonsRow") as RectTransform;
        RectTransform mainButtonRect = buttonsRowRect != null ? buttonsRowRect.Find("MainMenuButton") as RectTransform : null;

        SetRectCentered(bannerHolder, bannerSize, bannerPosition);
        SetRectCentered(panel, panelSize, panelPosition);
        SetRectCentered(statsHeaderRect, statsHeaderRect != null ? statsHeaderRect.sizeDelta : Vector2.zero, headerPosition);
        SetRectCentered(scoreRect, scoreRect != null ? scoreRect.sizeDelta : Vector2.zero, scorePosition);
        SetRectCentered(statsGridRect, statsGridSize, statsGridPosition);
        SetRectCentered(buttonsRowRect, buttonsRowSize, buttonsRowPosition);
        SetRectCentered(mainButtonRect, mainButtonSize, Vector2.zero);

        if (statsGridRect != null)
        {
            HorizontalLayoutGroup hlg = statsGridRect.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
                Destroy(hlg);

            GridLayoutGroup glg = statsGridRect.GetComponent<GridLayoutGroup>();
            if (glg != null)
                Destroy(glg);

            ContentSizeFitter csf = statsGridRect.GetComponent<ContentSizeFitter>();
            if (csf != null)
                Destroy(csf);

            int cardCount = Mathf.Min(statsGridRect.childCount, statCardPositions.Length);
            for (int i = 0; i < cardCount; i++)
            {
                RectTransform card = statsGridRect.GetChild(i) as RectTransform;
                SetRectCentered(card, statCardSize, statCardPositions[i]);
            }
        }
    }

    private void SetRectCentered(RectTransform rect, Vector2 size, Vector2 position)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        if (size != Vector2.zero)
            rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private void EnsureTrophyUiReferences()
    {
        if (starsRow == null)
        {
            Transform stars = transform.root.Find("GameResultCanvas/StarsRow");
            if (stars != null)
                starsRow = stars as RectTransform;
        }

        if (trophyScoreText == null)
        {
            Transform streakText = transform.root.Find("GameResultCanvas/StatsPanel/StatsGrid/StatCard/StreakText");
            if (streakText != null)
                trophyScoreText = streakText.GetComponent<TextMeshProUGUI>();
        }

        if (trophyImage == null)
        {
            Transform existingTrophy = transform.root.Find("GameResultCanvas/TrophyAward/TrophyImage");
            if (existingTrophy != null)
                trophyImage = existingTrophy.GetComponent<Image>();
        }

        Transform existingHolder = transform.root.Find("GameResultCanvas/TrophyAward");
        RectTransform existingHolderRect = existingHolder as RectTransform;
        if (trophyAwardRoot == null && existingHolderRect != null)
            trophyAwardRoot = existingHolderRect;

        if (trophyImage == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                GameObject holder = new GameObject("TrophyAward", typeof(RectTransform));
                holder.transform.SetParent(canvas.transform, false);
                RectTransform holderRect = holder.GetComponent<RectTransform>();
                holderRect.anchorMin = new Vector2(0.5f, 0.5f);
                holderRect.anchorMax = new Vector2(0.5f, 0.5f);
                holderRect.pivot = new Vector2(0.5f, 0.5f);
                holderRect.anchoredPosition = trophyAwardAnchoredPosition;
                holderRect.sizeDelta = trophyAwardSize;
                trophyAwardRoot = holderRect;

                GameObject imageObj = new GameObject("TrophyImage", typeof(RectTransform), typeof(Image));
                imageObj.transform.SetParent(holder.transform, false);
                RectTransform imageRect = imageObj.GetComponent<RectTransform>();
                imageRect.anchorMin = new Vector2(0.5f, 1f);
                imageRect.anchorMax = new Vector2(0.5f, 1f);
                imageRect.pivot = new Vector2(0.5f, 0.5f);
                imageRect.anchoredPosition = trophyImageAnchoredPosition;
                imageRect.sizeDelta = trophyImageSize;
                trophyImage = imageObj.GetComponent<Image>();
                trophyImage.preserveAspect = true;
                trophyImage.raycastTarget = false;

                GameObject labelObj = new GameObject("TrophyLabel", typeof(RectTransform), typeof(CanvasGroup), typeof(TextMeshProUGUI));
                labelObj.transform.SetParent(holder.transform, false);
                RectTransform labelRect = labelObj.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0.5f, 0f);
                labelRect.anchorMax = new Vector2(0.5f, 0f);
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.anchoredPosition = trophyLabelAnchoredPosition;
                labelRect.sizeDelta = trophyLabelSize;

                trophyLabelCanvasGroup = labelObj.GetComponent<CanvasGroup>();
                trophyLabelText = labelObj.GetComponent<TextMeshProUGUI>();
                trophyLabelText.alignment = TextAlignmentOptions.Center;
                trophyLabelText.fontSize = trophyLabelFontSize;
                trophyLabelText.fontStyle = FontStyles.Bold;
            }
        }

        if (applyRuntimeLayout && existingHolderRect != null)
        {
            existingHolderRect.anchorMin = new Vector2(0.5f, 0.5f);
            existingHolderRect.anchorMax = new Vector2(0.5f, 0.5f);
            existingHolderRect.pivot = new Vector2(0.5f, 0.5f);
            existingHolderRect.anchoredPosition = trophyAwardAnchoredPosition;
            existingHolderRect.sizeDelta = trophyAwardSize;
        }

        if (trophyLabelText == null && trophyImage != null)
        {
            Transform label = trophyImage.transform.parent.Find("TrophyLabel");
            if (label != null)
                trophyLabelText = label.GetComponent<TextMeshProUGUI>();
        }

        if (trophyLabelCanvasGroup == null && trophyLabelText != null)
            trophyLabelCanvasGroup = trophyLabelText.GetComponent<CanvasGroup>();

        if (trophyImage != null)
        {
            trophyImage.preserveAspect = true;
            trophyImage.raycastTarget = false;
            trophyRect = trophyImage.rectTransform;

            if (applyRuntimeLayout)
            {
                trophyRect.anchorMin = new Vector2(0.5f, 1f);
                trophyRect.anchorMax = new Vector2(0.5f, 1f);
                trophyRect.pivot = new Vector2(0.5f, 0.5f);
                trophyRect.anchoredPosition = trophyImageAnchoredPosition;
                trophyRect.sizeDelta = trophyImageSize;
            }

            trophyBaseAnchoredPos = trophyRect.anchoredPosition;
        }

        if (trophyLabelText != null)
        {
            RectTransform labelRect = trophyLabelText.rectTransform;

            if (applyRuntimeLayout)
            {
                labelRect.anchorMin = new Vector2(0.5f, 0f);
                labelRect.anchorMax = new Vector2(0.5f, 0f);
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.anchoredPosition = trophyLabelAnchoredPosition;
                labelRect.sizeDelta = trophyLabelSize;
                trophyLabelText.fontSize = trophyLabelFontSize;
            }

            trophyLabelText.textWrappingMode = TextWrappingModes.Normal;
        }
    }

    private void ApplyTrophyVisuals()
    {
        if (trophyImage == null)
            return;

        Sprite chosen = currentTrophyTier == TrophyTier.Gold ? goldTrophy : (currentTrophyTier == TrophyTier.Silver ? silverTrophy : bronzeTrophy);
        if (chosen != null)
            trophyImage.sprite = chosen;
        trophyImage.enabled = chosen != null;

        if (trophyLabelText != null)
        {
            switch (currentTrophyTier)
            {
                case TrophyTier.Gold:
                    trophyLabelText.text = "Amazing! You got a Gold Trophy!";
                    trophyLabelText.color = new Color32(255, 193, 7, 255);
                    break;
                case TrophyTier.Silver:
                    trophyLabelText.text = "Great Job! You got a Silver Trophy!";
                    trophyLabelText.color = new Color32(109, 129, 156, 255);
                    break;
                default:
                    trophyLabelText.text = "Good try! You got a Bronze Trophy!";
                    trophyLabelText.color = new Color32(173, 98, 52, 255);
                    break;
            }
        }

        ConfigureTrophyParticles();
    }

    private void ConfigureTrophyParticles()
    {
        if (trophyImage == null)
            return;

        if (trophyBurstParticles == null)
            trophyBurstParticles = CreateTrophyParticleSystem("TrophyBurst", false);
        if (trophyShimmerParticles == null)
            trophyShimmerParticles = CreateTrophyParticleSystem("TrophyShimmer", true);

        if (trophyBurstParticles == null || trophyShimmerParticles == null)
            return;

        // Stop and clear before changing module values to avoid runtime duration errors.
        trophyBurstParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        trophyShimmerParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        Color a;
        Color b;
        int burstCount;
        switch (currentTrophyTier)
        {
            case TrophyTier.Gold:
                a = new Color32(255, 242, 96, 220);
                b = new Color32(255, 255, 255, 220);
                burstCount = goldBurstCount;
                break;
            case TrophyTier.Silver:
                a = new Color32(214, 230, 255, 210);
                b = new Color32(242, 247, 255, 220);
                burstCount = silverBurstCount;
                break;
            default:
                a = new Color32(205, 133, 63, 210);
                b = new Color32(151, 89, 52, 210);
                burstCount = bronzeBurstCount;
                break;
        }

        var burstMain = trophyBurstParticles.main;
        burstMain.duration = 0.75f;
        burstMain.loop = false;
        burstMain.startColor = new ParticleSystem.MinMaxGradient(a, b);
        burstMain.startSpeed = new ParticleSystem.MinMaxCurve(110f, 170f);

        var emission = trophyBurstParticles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstCount) });

        var shimmerMain = trophyShimmerParticles.main;
        shimmerMain.duration = 1.8f;
        shimmerMain.loop = true;
        shimmerMain.startColor = new ParticleSystem.MinMaxGradient(a, b);
        shimmerMain.startSpeed = new ParticleSystem.MinMaxCurve(16f, 34f);

        var shimmerEmission = trophyShimmerParticles.emission;
        shimmerEmission.rateOverTime = currentTrophyTier == TrophyTier.Gold ? 14f : (currentTrophyTier == TrophyTier.Silver ? 10f : 7f);
    }

    private ParticleSystem CreateTrophyParticleSystem(string name, bool loop)
    {
        if (trophyImage == null)
            return null;

        Transform existing = trophyImage.transform.Find(name);
        if (existing != null)
            return existing.GetComponent<ParticleSystem>();

        GameObject particleObj = new GameObject(name, typeof(RectTransform), typeof(ParticleSystem), typeof(ParticleSystemRenderer));
        particleObj.transform.SetParent(trophyImage.transform, false);

        RectTransform rect = particleObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        ParticleSystem ps = particleObj.GetComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = loop;
        main.playOnAwake = false;
        main.startLifetime = loop ? new ParticleSystem.MinMaxCurve(0.45f, 0.9f) : new ParticleSystem.MinMaxCurve(0.6f, 1.05f);
        main.startSize = loop ? new ParticleSystem.MinMaxCurve(0.018f, 0.032f) : new ParticleSystem.MinMaxCurve(0.022f, 0.042f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = loop ? 120 : 100;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 75f;
        shape.arcMode = ParticleSystemShapeMultiModeValue.Random;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = loop ? 10f : 0f;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.orbitalY = loop ? 22f : 0f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(1f, 1f, 1f, 0.75f), 0.6f),
                new GradientColorKey(new Color(1f, 1f, 1f, 0.55f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.95f, 0.12f),
                new GradientAlphaKey(0.65f, 0.68f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystemRenderer renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 30;

        return ps;
    }

    private IEnumerator PlayTrophyEntrance()
    {
        if (trophyRect == null)
            yield break;

        float duration = 0.34f;
        float elapsed = 0f;
        Vector2 startPos = trophyBaseAnchoredPos + new Vector2(0f, 55f);
        Vector2 settlePos = trophyBaseAnchoredPos;

        trophyRect.anchoredPosition = startPos;
        trophyRect.localScale = Vector3.zero;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutBack(t);
            trophyRect.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one * 1.2f, eased);
            trophyRect.anchoredPosition = Vector2.LerpUnclamped(startPos, settlePos, eased);
            yield return null;
        }

        elapsed = 0f;
        float settleDuration = 0.16f;
        Vector3 overshootScale = Vector3.one * 1.2f;
        while (elapsed < settleDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / settleDuration);
            float eased = EaseOutCubic(t);
            trophyRect.localScale = Vector3.LerpUnclamped(overshootScale, Vector3.one, eased);
            yield return null;
        }

        trophyRect.localScale = Vector3.one;
        trophyRect.anchoredPosition = settlePos;

        if (trophyBurstParticles != null)
            trophyBurstParticles.Play(true);
        if (trophyShimmerParticles != null)
            trophyShimmerParticles.Play(true);

        if (trophyFloatRoutine != null)
            StopCoroutine(trophyFloatRoutine);
        trophyFloatRoutine = StartCoroutine(TrophyFloatLoop());
    }

    private IEnumerator TrophyFloatLoop()
    {
        while (trophyRect != null)
        {
            float bob = Mathf.Sin(Time.time * 1.5f) * 8f;
            trophyRect.anchoredPosition = trophyBaseAnchoredPos + new Vector2(0f, bob);
            yield return null;
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup target, float from, float to, float duration)
    {
        if (target == null)
            yield break;

        float elapsed = 0f;
        target.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            target.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        target.alpha = to;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }

    private static float EaseOutCubic(float t)
    {
        float x = 1f - t;
        return 1f - (x * x * x);
    }

    private void TriggerAnimator(Animator animator, string triggerName)
    {
        if (animator != null && !string.IsNullOrWhiteSpace(triggerName))
            animator.SetTrigger(triggerName);
    }

    private string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.RoundToInt(seconds));
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;
        return minutes > 0 ? $"{minutes}m {secs}s" : $"{secs}s";
    }

    private void GoToNextActivity()
    {
        if (!string.IsNullOrWhiteSpace(nextActivityScene) && Application.CanStreamedLevelBeLoaded(nextActivityScene))
        {
            SceneManager.LoadScene(nextActivityScene);
            return;
        }

        SceneManager.LoadScene(fallbackNextActivityScene);
    }

    private void GoToMainMenu()
    {
        if (!string.IsNullOrWhiteSpace(mainMenuScene) && Application.CanStreamedLevelBeLoaded(mainMenuScene))
        {
            SceneManager.LoadScene(mainMenuScene);
            return;
        }

        SceneManager.LoadScene(fallbackMainMenuScene);
    }
}
