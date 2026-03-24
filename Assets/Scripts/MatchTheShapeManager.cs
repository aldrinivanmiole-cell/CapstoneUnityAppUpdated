using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum MatchShapeType
{
    Circle,
    Square,
    Triangle,
    Hexagon
}

public class MatchTheShapeManager : MonoBehaviour
{
    [Header("Routing")]
    [SerializeField] private string returnSceneName = "NewMap";
    [SerializeField] private string resultSceneName = "GameResult";

    [Header("Gameplay")]
    [SerializeField] private int startingLevel = 1;

    private int nextQuestionIndex;

    private readonly Dictionary<MatchShapeType, Sprite> shapeSprites = new Dictionary<MatchShapeType, Sprite>();
    private readonly Dictionary<MatchShapeType, Color> shapeColors = new Dictionary<MatchShapeType, Color>();

    private readonly List<ShapeCardDraggable> shapeCards = new List<ShapeCardDraggable>();
    private readonly List<NameCardDropZone> nameCards = new List<NameCardDropZone>();
    private readonly Dictionary<int, NameCardDropZone> nameCardByRow = new Dictionary<int, NameCardDropZone>();

    private RectTransform canvasRect;
    private Canvas rootCanvas;
    private RectTransform playArea;
    private RectTransform shapeLane;
    private RectTransform nameLane;

    private Sprite cardSprite;
    private Sprite skyGradientSprite;
    private Sprite grassSprite;
    private Sprite circleSprite;

    private TextMeshProUGUI levelText;
    private TextMeshProUGUI timerText;
    private TextMeshProUGUI progressText;

    private RectTransform introOverlay;
    private TextMeshProUGUI introBody;
    private TextMeshProUGUI introHint;
    private Button introStartButton;

    private RectTransform completeOverlay;
    private TextMeshProUGUI completeTitle;
    private TextMeshProUGUI completeStars;
    private TextMeshProUGUI completeButtonLabel;

    private RectTransform mascotRoot;
    private Image mascotBody;

    private ParticleSystem sparkleParticles;
    private ParticleSystem confettiParticles;

    private int currentLevel;
    private int matchedThisLevel;
    private int totalMatched;
    private int totalWrongDrops;
    private int totalPairCount;

    private float totalElapsed;
    private float levelStartTime;

    private bool introOpen;
    private bool isPlaying;
    private bool inputLocked;

    private ShapeCardDraggable selectedShapeCard;

    private readonly string[] introLines =
    {
        "In a magical land, all the shapes had lost their name tags...",
        "The shapes were confused and needed a hero to help them find their names again...",
        "Can you match each shape to its correct name?"
    };

    private int introLineIndex;

    private static readonly MatchShapeType[][] LevelShapes =
    {
        new[] { MatchShapeType.Circle, MatchShapeType.Square },
        new[] { MatchShapeType.Circle, MatchShapeType.Square, MatchShapeType.Triangle },
        new[] { MatchShapeType.Circle, MatchShapeType.Square, MatchShapeType.Triangle, MatchShapeType.Hexagon }
    };

    private void Awake()
    {
        ResolveReturnContext();

        currentLevel = Mathf.Clamp(startingLevel, 1, 3);

        BuildShapeColorTable();
        BuildSpriteCache();
        EnsureCamera();
        EnsureCanvasAndEventSystem();

        BuildBackground();
        BuildTopBar();
        BuildPlayArea();
        BuildStoryOverlay();
        BuildLevelCompletePopup();
        BuildMascot();
        BuildParticles();
    }

    private void Start()
    {
        ShowStoryIntro();
    }

    private void ResolveReturnContext()
    {
        if (string.IsNullOrWhiteSpace(MinigameData.nextSceneName))
        {
            MinigameData.nextSceneName = PlayerPrefs.GetString(PacmanManager.ReturnSceneKey, returnSceneName);
        }

        if (PlayerPrefs.HasKey(PacmanManager.NextQuestionIndexKey))
        {
            MinigameData.nextQuestionIndex = PlayerPrefs.GetInt(PacmanManager.NextQuestionIndexKey, MinigameData.nextQuestionIndex);
        }

        returnSceneName = string.IsNullOrWhiteSpace(MinigameData.nextSceneName)
            ? returnSceneName
            : MinigameData.nextSceneName;
        nextQuestionIndex = Mathf.Max(0, MinigameData.nextQuestionIndex);
    }

    public void OnReturnPressed()
    {
        SceneManager.LoadScene(returnSceneName);
    }

    public void OnShapeCardTapped(ShapeCardDraggable card)
    {
        if (!CanInteract(card) || card.IsLocked)
            return;

        if (selectedShapeCard == card)
        {
            card.SetSelectedVisual(false);
            selectedShapeCard = null;
            return;
        }

        if (selectedShapeCard != null)
            selectedShapeCard.SetSelectedVisual(false);

        selectedShapeCard = card;
        selectedShapeCard.SetSelectedVisual(true);
    }

    public void OnNameCardTapped(NameCardDropZone tappedNameCard)
    {
        if (!isPlaying || introOpen || inputLocked || tappedNameCard == null || tappedNameCard.IsMatched)
            return;

        if (selectedShapeCard == null || selectedShapeCard.IsLocked)
            return;

        StartCoroutine(AttemptMatchFromTap(selectedShapeCard, tappedNameCard));
    }

    private IEnumerator AttemptMatchFromTap(ShapeCardDraggable shapeCard, NameCardDropZone tappedNameCard)
    {
        inputLocked = true;

        shapeCard.SetSelectedVisual(false);
        selectedShapeCard = null;

        NameCardDropZone nameAtShapeRow = FindNameCardAtRow(shapeCard.RowIndex);
        if (nameAtShapeRow == null)
        {
            inputLocked = false;
            yield break;
        }

        int fromRow = tappedNameCard.RowIndex;
        int toRow = shapeCard.RowIndex;
        bool swappedRows = tappedNameCard != nameAtShapeRow;

        if (swappedRows)
        {
            Vector2 tappedStart = tappedNameCard.Rect.anchoredPosition;
            Vector2 rowTargetStart = nameAtShapeRow.Rect.anchoredPosition;

            yield return StartCoroutine(TweenAnchoredPosition(tappedNameCard.Rect, tappedStart, rowTargetStart, 0.22f));
            yield return StartCoroutine(TweenAnchoredPosition(nameAtShapeRow.Rect, rowTargetStart, tappedStart, 0.22f));

            tappedNameCard.SetRowIndex(toRow);
            nameAtShapeRow.SetRowIndex(fromRow);

            nameCardByRow[toRow] = tappedNameCard;
            nameCardByRow[fromRow] = nameAtShapeRow;

            nameAtShapeRow = tappedNameCard;
        }

        bool correct = nameAtShapeRow.ShapeType == shapeCard.ShapeType;

        if (correct)
        {
            yield return StartCoroutine(HandleCorrectMatch(shapeCard, nameAtShapeRow));
            inputLocked = false;
            yield break;
        }

        totalWrongDrops += 1;

        if (swappedRows)
        {
            NameCardDropZone movedName = nameAtShapeRow;
            NameCardDropZone otherName = FindNameCardAtRow(fromRow);
            if (otherName != null)
            {
                Vector2 movedPos = movedName.Rect.anchoredPosition;
                Vector2 otherPos = otherName.Rect.anchoredPosition;

                yield return StartCoroutine(TweenAnchoredPosition(movedName.Rect, movedPos, otherPos, 0.2f));
                yield return StartCoroutine(TweenAnchoredPosition(otherName.Rect, otherPos, movedPos, 0.2f));

                movedName.SetRowIndex(fromRow);
                otherName.SetRowIndex(toRow);

                nameCardByRow[fromRow] = movedName;
                nameCardByRow[toRow] = otherName;
            }
        }

        yield return StartCoroutine(HandleWrongMatch(shapeCard));
        inputLocked = false;
    }

    public void OnStoryOverlayTapped()
    {
        if (!introOpen)
            return;

        if (introLineIndex < introLines.Length - 1)
        {
            introLineIndex += 1;
            if (introBody != null)
                introBody.text = introLines[introLineIndex];

            if (introLineIndex == introLines.Length - 1 && introHint != null)
                introHint.text = "Tap once more, then press Start Activity!";
            return;
        }

        if (introStartButton != null)
            introStartButton.gameObject.SetActive(true);

        if (introHint != null)
            introHint.text = "Press Start Activity!";
    }

    public void OnStartActivityPressed()
    {
        StartCoroutine(FadeOutIntroAndBegin());
    }

    public void OnLevelCompleteButtonPressed()
    {
        if (inputLocked)
            return;

        completeOverlay.gameObject.SetActive(false);

        if (currentLevel < 3)
        {
            currentLevel += 1;
            SetupLevel(currentLevel);
            isPlaying = true;
            return;
        }

        FinishGame();
    }

    private bool CanInteract(ShapeCardDraggable card)
    {
        return isPlaying && !introOpen && !inputLocked && card != null && !card.IsLocked;
    }

    private void ShowStoryIntro()
    {
        introOpen = true;
        isPlaying = false;
        inputLocked = false;

        totalElapsed = 0f;
        totalMatched = 0;
        totalWrongDrops = 0;
        totalPairCount = 0;
        for (int i = 0; i < LevelShapes.Length; i++)
            totalPairCount += LevelShapes[i].Length;

        introLineIndex = 0;
        if (introBody != null)
            introBody.text = introLines[introLineIndex];

        if (introHint != null)
            introHint.text = "Tap to continue";

        if (introStartButton != null)
            introStartButton.gameObject.SetActive(false);

        if (introOverlay != null)
            introOverlay.gameObject.SetActive(true);
    }

    private IEnumerator FadeOutIntroAndBegin()
    {
        if (introOverlay == null)
            yield break;

        CanvasGroup group = introOverlay.GetComponent<CanvasGroup>();
        float from = group != null ? group.alpha : 1f;

        float elapsed = 0f;
        const float fade = 0.28f;
        while (elapsed < fade)
        {
            elapsed += Time.deltaTime;
            if (group != null)
                group.alpha = Mathf.Lerp(from, 0f, Mathf.Clamp01(elapsed / fade));
            yield return null;
        }

        if (group != null)
            group.alpha = 0f;

        introOverlay.gameObject.SetActive(false);
        introOpen = false;

        SetupLevel(currentLevel);
        isPlaying = true;
    }

    private void SetupLevel(int level)
    {
        ClearCurrentCards();
        selectedShapeCard = null;
        nameCardByRow.Clear();

        MatchShapeType[] shapes = LevelShapes[Mathf.Clamp(level - 1, 0, LevelShapes.Length - 1)];
        List<MatchShapeType> nameOrder = new List<MatchShapeType>(shapes);
        Shuffle(nameOrder);

        // Ensure the shuffled list is not an exact positional copy.
        bool identical = true;
        for (int i = 0; i < shapes.Length; i++)
        {
            if (shapes[i] != nameOrder[i])
            {
                identical = false;
                break;
            }
        }
        if (identical && nameOrder.Count > 1)
        {
            MatchShapeType first = nameOrder[0];
            nameOrder.RemoveAt(0);
            nameOrder.Add(first);
        }

        matchedThisLevel = 0;
        levelStartTime = totalElapsed;

        if (levelText != null)
            levelText.text = "Level " + level;

        UpdateProgressText(shapes.Length);

        float cardSize = shapes.Length <= 2 ? 235f : (shapes.Length == 3 ? 200f : 175f);

        for (int i = 0; i < shapes.Length; i++)
        {
            MatchShapeType shapeType = shapes[i];
            Vector2 shapePos = GetLanePosition(i, shapes.Length);
            Vector2 namePos = GetLanePosition(i, shapes.Length);

            CreateShapeCard(shapeType, cardSize, i, shapePos);
            CreateNameCard(nameOrder[i], cardSize, i, namePos);
        }
    }

    private void CreateShapeCard(MatchShapeType type, float size, int rowIndex, Vector2 anchoredPos)
    {
        GameObject card = CreateCardBase("ShapeCard_" + type, shapeLane, new Vector2(size, size));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchoredPosition = anchoredPos;

        GameObject shapeGo = new GameObject("ShapeImage", typeof(RectTransform), typeof(Image));
        shapeGo.transform.SetParent(card.transform, false);

        RectTransform shapeRect = shapeGo.GetComponent<RectTransform>();
        shapeRect.anchorMin = new Vector2(0.5f, 0.5f);
        shapeRect.anchorMax = new Vector2(0.5f, 0.5f);
        shapeRect.pivot = new Vector2(0.5f, 0.5f);
        shapeRect.sizeDelta = new Vector2(size * 0.58f, size * 0.58f);
        if (type == MatchShapeType.Square)
            shapeRect.anchoredPosition = new Vector2(0f, -4f);

        Image shapeImage = shapeGo.GetComponent<Image>();
        shapeImage.sprite = shapeSprites[type];
        shapeImage.color = shapeColors[type];

        ShapeCardDraggable draggable = card.AddComponent<ShapeCardDraggable>();
        draggable.Initialize(this, type, rowIndex, cardRect, anchoredPos, shapeImage);
        shapeCards.Add(draggable);
    }

    private void CreateNameCard(MatchShapeType type, float size, int rowIndex, Vector2 anchoredPos)
    {
        GameObject card = CreateCardBase("NameCard_" + type, nameLane, new Vector2(size, size));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchoredPosition = anchoredPos;

        TextMeshProUGUI label = CreateCenteredLabel(cardRect, type.ToString().ToLowerInvariant(), size * 0.14f);
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(0.14f, 0.18f, 0.27f, 1f);

        NameCardDropZone zone = card.AddComponent<NameCardDropZone>();
        zone.Initialize(this, type, rowIndex, cardRect, card.GetComponent<Image>(), card.GetComponent<Shadow>());
        nameCards.Add(zone);
        nameCardByRow[rowIndex] = zone;
    }

    private IEnumerator HandleCorrectMatch(ShapeCardDraggable card, NameCardDropZone zone)
    {
        Vector2 burstPos = (card.Rect.position + zone.Rect.position) * 0.5f;
        PlayStarBurst(burstPos);

        yield return StartCoroutine(BounceScale(card.Rect, 1f, 1.12f, 0.18f));
        yield return StartCoroutine(BounceScale(zone.Rect, 1f, 1.08f, 0.18f));

        yield return StartCoroutine(FlashImage(card.CardBackground, new Color(0.71f, 0.95f, 0.71f, 1f), 0.2f));
        yield return StartCoroutine(FlashImage(zone.CardBackground, new Color(0.71f, 0.95f, 0.71f, 1f), 0.2f));

        card.LockMatched();
        zone.SetMatchedVisual();

        matchedThisLevel += 1;
        totalMatched += 1;
        UpdateProgressText(LevelShapes[currentLevel - 1].Length);

        if (matchedThisLevel >= LevelShapes[currentLevel - 1].Length)
            StartCoroutine(HandleLevelComplete());
    }

    private IEnumerator HandleWrongMatch(ShapeCardDraggable card)
    {
        yield return StartCoroutine(Shake(card.Rect, 0.25f, 16f));
        yield return StartCoroutine(FlashImage(card.CardBackground, new Color(1f, 0.70f, 0.70f, 1f), 0.2f));

        StartCoroutine(MascotEncourageAnimation());
    }

    private IEnumerator HandleLevelComplete()
    {
        isPlaying = false;
        inputLocked = true;

        if (confettiParticles != null)
            confettiParticles.Play();

        StartCoroutine(MascotCelebrateAnimation());

        float levelDuration = Mathf.Max(1f, totalElapsed - levelStartTime);
        int stars = CalculateStars(currentLevel, levelDuration);

        if (completeTitle != null)
            completeTitle.text = "Level Complete!";

        if (completeStars != null)
            completeStars.text = new string('*', stars);

        if (completeButtonLabel != null)
            completeButtonLabel.text = currentLevel >= 3 ? "Finish" : "Next Level";

        completeOverlay.gameObject.SetActive(true);
        completeOverlay.localScale = Vector3.one * 0.75f;

        float elapsed = 0f;
        const float pop = 0.24f;
        while (elapsed < pop)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutBack(Mathf.Clamp01(elapsed / pop));
            completeOverlay.localScale = Vector3.Lerp(Vector3.one * 0.75f, Vector3.one, t);
            yield return null;
        }

        completeOverlay.localScale = Vector3.one;
        inputLocked = false;
    }

    private void FinishGame()
    {
        ContinueToNextQuestion();
    }

    private void ContinueToNextQuestion()
    {
        PlayerPrefs.SetInt(PacmanManager.ResumeQuestionIndexKey, nextQuestionIndex);
        PlayerPrefs.DeleteKey(PacmanManager.NextQuestionIndexKey);
        PlayerPrefs.DeleteKey(PacmanManager.ReturnSceneKey);
        PlayerPrefs.Save();

        MinigameData.Clear();
        SceneManager.LoadScene(returnSceneName);
    }

    private void BuildBackground()
    {
        GameObject sky = CreateUiObject("Sky", canvasRect, Vector2.zero, stretch: true);
        Image skyImage = sky.AddComponent<Image>();
        skyImage.sprite = skyGradientSprite;

        for (int i = 0; i < 3; i++)
        {
            GameObject cloud = CreateUiObject("Cloud_" + (i + 1), canvasRect, new Vector2(290f, 120f));
            RectTransform rect = cloud.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.68f + i * 0.08f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(180f + i * 420f, 0f);

            Image cloudImage = cloud.AddComponent<Image>();
            cloudImage.sprite = circleSprite;
            cloudImage.color = new Color(1f, 1f, 1f, 0.85f);

            CloudDrift drift = cloud.AddComponent<CloudDrift>();
            drift.Initialize(18f + i * 6f, 2300f);
        }

        GameObject grass = CreateUiObject("Grass", canvasRect, new Vector2(0f, 220f), stretchX: true);
        RectTransform grassRect = grass.GetComponent<RectTransform>();
        grassRect.anchorMin = new Vector2(0f, 0f);
        grassRect.anchorMax = new Vector2(1f, 0f);
        grassRect.pivot = new Vector2(0.5f, 0f);

        Image grassImage = grass.AddComponent<Image>();
        grassImage.sprite = grassSprite;
        grassImage.type = Image.Type.Sliced;
    }

    private void BuildTopBar()
    {
        RectTransform topBar = CreateUiObject("TopBar", canvasRect, new Vector2(0f, 220f)).GetComponent<RectTransform>();
        topBar.anchorMin = new Vector2(0f, 1f);
        topBar.anchorMax = new Vector2(1f, 1f);
        topBar.pivot = new Vector2(0.5f, 1f);
        topBar.sizeDelta = new Vector2(0f, 220f);
        topBar.anchoredPosition = new Vector2(0f, -8f);

        Button returnButton = CreateRoundedButton(topBar, "Return", new Vector2(170f, 88f), new Color(0.90f, 0.28f, 0.26f, 1f));
        RectTransform returnRect = returnButton.GetComponent<RectTransform>();
        returnRect.anchorMin = new Vector2(0f, 1f);
        returnRect.anchorMax = new Vector2(0f, 1f);
        returnRect.pivot = new Vector2(0f, 1f);
        returnRect.anchoredPosition = new Vector2(26f, -42f);
        returnButton.onClick.AddListener(OnReturnPressed);
        CreateCenteredLabel(returnRect, "<", 58f).fontStyle = FontStyles.Bold;

        RectTransform levelBanner = CreateUiObject("LevelBanner", topBar, new Vector2(330f, 88f)).GetComponent<RectTransform>();
        levelBanner.anchorMin = new Vector2(0.5f, 1f);
        levelBanner.anchorMax = new Vector2(0.5f, 1f);
        levelBanner.pivot = new Vector2(0.5f, 1f);
        levelBanner.anchoredPosition = new Vector2(0f, -42f);

        Image bannerImage = levelBanner.gameObject.AddComponent<Image>();
        bannerImage.sprite = cardSprite;
        bannerImage.type = Image.Type.Sliced;
        bannerImage.color = new Color(1f, 0.90f, 0.70f, 1f);

        levelText = CreateCenteredLabel(levelBanner, "Level 1", 42f);
        levelText.fontStyle = FontStyles.Bold;
        levelText.color = new Color(0.34f, 0.18f, 0.10f, 1f);

        RectTransform timerBadge = CreateUiObject("TimerBadge", topBar, new Vector2(290f, 88f)).GetComponent<RectTransform>();
        timerBadge.anchorMin = new Vector2(1f, 1f);
        timerBadge.anchorMax = new Vector2(1f, 1f);
        timerBadge.pivot = new Vector2(1f, 1f);
        timerBadge.anchoredPosition = new Vector2(-26f, -42f);

        Image timerBg = timerBadge.gameObject.AddComponent<Image>();
        timerBg.sprite = cardSprite;
        timerBg.type = Image.Type.Sliced;
        timerBg.color = new Color(1f, 0.80f, 0.60f, 1f);

        timerText = CreateCenteredLabel(timerBadge, "Time: 0s", 34f);
        timerText.fontStyle = FontStyles.Bold;
        timerText.color = new Color(0.36f, 0.19f, 0.10f, 1f);

        RectTransform progressHolder = CreateUiObject("ProgressHolder", topBar, new Vector2(420f, 68f)).GetComponent<RectTransform>();
        progressHolder.anchorMin = new Vector2(0.5f, 1f);
        progressHolder.anchorMax = new Vector2(0.5f, 1f);
        progressHolder.pivot = new Vector2(0.5f, 1f);
        progressHolder.anchoredPosition = new Vector2(0f, -148f);

        progressText = CreateCenteredLabel(progressHolder, "Matched: 0 / 0", 32f);
        progressText.fontStyle = FontStyles.Bold;
        progressText.color = new Color(0.85f, 0.40f, 0.20f, 1f);
    }

    private void BuildPlayArea()
    {
        playArea = CreateUiObject("PlayArea", canvasRect, Vector2.zero, stretch: true).GetComponent<RectTransform>();

        RectTransform board = CreateUiObject("Board", playArea, Vector2.zero, stretch: true).GetComponent<RectTransform>();
        board.anchorMin = new Vector2(0.08f, 0.13f);
        board.anchorMax = new Vector2(0.92f, 0.80f);
        board.offsetMin = Vector2.zero;
        board.offsetMax = Vector2.zero;

        Image boardBg = board.gameObject.AddComponent<Image>();
        boardBg.sprite = cardSprite;
        boardBg.type = Image.Type.Sliced;
        boardBg.color = new Color(0.99f, 0.97f, 0.90f, 0.96f);

        Shadow boardShadow = board.gameObject.AddComponent<Shadow>();
        boardShadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
        boardShadow.effectDistance = new Vector2(8f, -8f);

        shapeLane = CreateUiObject("ShapeLane", board, new Vector2(480f, 600f)).GetComponent<RectTransform>();
        shapeLane.anchorMin = new Vector2(0.27f, 0.5f);
        shapeLane.anchorMax = shapeLane.anchorMin;

        nameLane = CreateUiObject("NameLane", board, new Vector2(480f, 600f)).GetComponent<RectTransform>();
        nameLane.anchorMin = new Vector2(0.73f, 0.5f);
        nameLane.anchorMax = nameLane.anchorMin;
    }

    private void BuildStoryOverlay()
    {
        introOverlay = CreateUiObject("StoryOverlay", canvasRect, Vector2.zero, stretch: true).GetComponent<RectTransform>();

        Image dim = introOverlay.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.62f);

        CanvasGroup group = introOverlay.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 1f;

        Button tapCatcher = introOverlay.gameObject.AddComponent<Button>();
        tapCatcher.transition = Selectable.Transition.None;
        tapCatcher.onClick.AddListener(OnStoryOverlayTapped);

        RectTransform panel = CreateUiObject("StoryPanel", introOverlay, new Vector2(920f, 650f)).GetComponent<RectTransform>();
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);

        Image panelBg = panel.gameObject.AddComponent<Image>();
        panelBg.sprite = cardSprite;
        panelBg.type = Image.Type.Sliced;
        panelBg.color = new Color(0.98f, 0.91f, 0.78f, 1f);

        CreateCenteredLabel(panel, "Story Time!", 56f).rectTransform.anchoredPosition = new Vector2(0f, 242f);

        introBody = CreateCenteredLabel(panel, string.Empty, 38f);
        introBody.rectTransform.sizeDelta = new Vector2(760f, 270f);
        introBody.textWrappingMode = TextWrappingModes.Normal;
        introBody.color = new Color(0.22f, 0.17f, 0.10f, 1f);

        introHint = CreateCenteredLabel(panel, "Tap to continue", 30f);
        introHint.rectTransform.anchoredPosition = new Vector2(0f, -200f);
        introHint.color = new Color(0.64f, 0.32f, 0.18f, 1f);

        RectTransform mascot = CreateUiObject("IntroMascot", panel, new Vector2(130f, 130f)).GetComponent<RectTransform>();
        mascot.anchorMin = new Vector2(0f, 1f);
        mascot.anchorMax = new Vector2(0f, 1f);
        mascot.pivot = new Vector2(0f, 1f);
        mascot.anchoredPosition = new Vector2(30f, -30f);

        Image mascotImage = mascot.gameObject.AddComponent<Image>();
        mascotImage.sprite = circleSprite;
        mascotImage.color = new Color(1f, 0.80f, 0.35f, 1f);

        Button startButton = CreateRoundedButton(panel, "Start", new Vector2(330f, 90f), new Color(0.97f, 0.52f, 0.21f, 1f));
        RectTransform startRect = startButton.GetComponent<RectTransform>();
        startRect.anchorMin = new Vector2(0.5f, 0f);
        startRect.anchorMax = new Vector2(0.5f, 0f);
        startRect.pivot = new Vector2(0.5f, 0f);
        startRect.anchoredPosition = new Vector2(0f, 24f);

        CreateCenteredLabel(startRect, "Start Activity!", 34f).color = Color.white;

        startButton.onClick.AddListener(OnStartActivityPressed);
        introStartButton = startButton;
    }

    private void BuildLevelCompletePopup()
    {
        completeOverlay = CreateUiObject("CompleteOverlay", canvasRect, Vector2.zero, stretch: true).GetComponent<RectTransform>();

        Image dim = completeOverlay.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.50f);

        RectTransform panel = CreateUiObject("CompletePanel", completeOverlay, new Vector2(760f, 470f)).GetComponent<RectTransform>();
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);

        Image panelBg = panel.gameObject.AddComponent<Image>();
        panelBg.sprite = cardSprite;
        panelBg.type = Image.Type.Sliced;
        panelBg.color = new Color(0.99f, 0.92f, 0.78f, 1f);

        completeTitle = CreateCenteredLabel(panel, "Level Complete!", 58f);
        completeTitle.rectTransform.anchoredPosition = new Vector2(0f, 138f);
        completeTitle.fontStyle = FontStyles.Bold;
        completeTitle.color = new Color(0.38f, 0.22f, 0.10f, 1f);

        completeStars = CreateCenteredLabel(panel, "***", 66f);
        completeStars.rectTransform.anchoredPosition = new Vector2(0f, 40f);
        completeStars.color = new Color(0.98f, 0.78f, 0.22f, 1f);

        Button actionButton = CreateRoundedButton(panel, "Action", new Vector2(280f, 90f), new Color(0.24f, 0.67f, 0.96f, 1f));
        RectTransform actionRect = actionButton.GetComponent<RectTransform>();
        actionRect.anchorMin = new Vector2(0.5f, 0f);
        actionRect.anchorMax = new Vector2(0.5f, 0f);
        actionRect.pivot = new Vector2(0.5f, 0f);
        actionRect.anchoredPosition = new Vector2(0f, 36f);
        actionButton.onClick.AddListener(OnLevelCompleteButtonPressed);

        completeButtonLabel = CreateCenteredLabel(actionRect, "Next Level", 36f);
        completeButtonLabel.color = Color.white;
        completeButtonLabel.fontStyle = FontStyles.Bold;

        completeOverlay.gameObject.SetActive(false);
    }

    private void BuildMascot()
    {
        mascotRoot = CreateUiObject("Mascot", canvasRect, new Vector2(170f, 170f)).GetComponent<RectTransform>();
        mascotRoot.anchorMin = new Vector2(0f, 0f);
        mascotRoot.anchorMax = new Vector2(0f, 0f);
        mascotRoot.pivot = new Vector2(0f, 0f);
        mascotRoot.anchoredPosition = new Vector2(28f, 18f);

        mascotBody = mascotRoot.gameObject.AddComponent<Image>();
        mascotBody.sprite = circleSprite;
        mascotBody.color = new Color(1f, 0.80f, 0.35f, 1f);

        CreateEye(new Vector2(-24f, 18f));
        CreateEye(new Vector2(24f, 18f));
    }

    private void CreateEye(Vector2 pos)
    {
        GameObject eye = new GameObject("Eye", typeof(RectTransform), typeof(Image));
        eye.transform.SetParent(mascotRoot, false);

        RectTransform rect = eye.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(18f, 18f);
        rect.anchoredPosition = pos;

        Image img = eye.GetComponent<Image>();
        img.sprite = circleSprite;
        img.color = Color.black;
    }

    private void BuildParticles()
    {
        GameObject sparkleGo = new GameObject("Sparkles", typeof(ParticleSystem));
        sparkleGo.transform.SetParent(transform, false);
        sparkleParticles = sparkleGo.GetComponent<ParticleSystem>();

        var main = sparkleParticles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.4f, 4.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startColor = new Color(1f, 1f, 1f, 0.7f);
        main.maxParticles = 140;

        var emission = sparkleParticles.emission;
        emission.rateOverTime = 18f;

        var shape = sparkleParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(16f, 9f, 0.1f);

        var velocity = sparkleParticles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.12f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        GameObject confettiGo = new GameObject("Confetti", typeof(ParticleSystem));
        confettiGo.transform.SetParent(transform, false);
        confettiGo.transform.position = new Vector3(0f, 4.4f, 0f);
        confettiParticles = confettiGo.GetComponent<ParticleSystem>();

        var cMain = confettiParticles.main;
        cMain.loop = false;
        cMain.playOnAwake = false;
        cMain.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.8f);
        cMain.startSpeed = new ParticleSystem.MinMaxCurve(2.6f, 4.2f);
        cMain.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.17f);
        cMain.maxParticles = 180;
        cMain.gravityModifier = 0.42f;

        var cEmission = confettiParticles.emission;
        cEmission.rateOverTime = 0f;
        cEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 100) });

        var cShape = confettiParticles.shape;
        cShape.shapeType = ParticleSystemShapeType.Box;
        cShape.scale = new Vector3(9f, 0.2f, 0.1f);
    }

    private void PlayStarBurst(Vector3 screenPosition)
    {
        GameObject burstGo = new GameObject("StarBurst", typeof(ParticleSystem));
        burstGo.transform.SetParent(transform, false);
        burstGo.transform.position = ScreenToWorld(screenPosition);

        ParticleSystem ps = burstGo.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.7f, 2.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.11f);
        main.startColor = new Color(1f, 0.95f, 0.55f, 1f);

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.11f;

        ps.Play();
        Destroy(burstGo, 1.1f);
    }

    private Vector3 ScreenToWorld(Vector3 screenPosition)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return Vector3.zero;

        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
        world.z = 0f;
        return world;
    }

    private void AnimateMascotIdle()
    {
        if (mascotRoot == null)
            return;

        float bob = Mathf.Sin(Time.time * 1.8f) * 6f;
        mascotRoot.anchoredPosition = new Vector2(28f, 18f + bob);
    }

    private IEnumerator MascotEncourageAnimation()
    {
        if (mascotRoot == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < 0.45f)
        {
            elapsed += Time.deltaTime;
            float z = Mathf.Sin(elapsed * 28f) * 10f;
            mascotRoot.localRotation = Quaternion.Euler(0f, 0f, z);
            mascotBody.color = Color.Lerp(new Color(1f, 0.80f, 0.35f, 1f), new Color(1f, 0.89f, 0.58f, 1f), Mathf.PingPong(elapsed * 4f, 1f));
            yield return null;
        }

        mascotRoot.localRotation = Quaternion.identity;
        mascotBody.color = new Color(1f, 0.80f, 0.35f, 1f);
    }

    private IEnumerator MascotCelebrateAnimation()
    {
        if (mascotRoot == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < 1.1f)
        {
            elapsed += Time.deltaTime;
            float z = Mathf.Sin(elapsed * 16f) * 14f;
            mascotRoot.localRotation = Quaternion.Euler(0f, 0f, z);
            mascotRoot.localScale = Vector3.one * (1f + Mathf.Sin(elapsed * 11f) * 0.08f);
            yield return null;
        }

        mascotRoot.localRotation = Quaternion.identity;
        mascotRoot.localScale = Vector3.one;
    }

    private Vector2 GetLanePosition(int index, int count)
    {
        float spacing = count == 2 ? 260f : (count == 3 ? 185f : 145f);
        float linearStartY = spacing * (count - 1) * 0.5f;
        return new Vector2(0f, linearStartY - index * spacing);
    }

    private NameCardDropZone FindNameCardAtRow(int rowIndex)
    {
        if (nameCardByRow.TryGetValue(rowIndex, out NameCardDropZone zone))
            return zone;
        return null;
    }

    private void UpdateProgressText(int total)
    {
        if (progressText != null)
            progressText.text = "Matched: " + matchedThisLevel + " / " + Mathf.Max(1, total);
    }

    private int CalculateStars(int level, float levelTime)
    {
        float fast = 12f + level * 4f;
        float medium = 20f + level * 5f;

        if (levelTime <= fast)
            return 3;
        if (levelTime <= medium)
            return 2;
        return 1;
    }

    private void ClearCurrentCards()
    {
        for (int i = 0; i < shapeCards.Count; i++)
        {
            if (shapeCards[i] != null)
                Destroy(shapeCards[i].gameObject);
        }
        shapeCards.Clear();

        for (int i = 0; i < nameCards.Count; i++)
        {
            if (nameCards[i] != null)
                Destroy(nameCards[i].gameObject);
        }
        nameCards.Clear();
    }

    private GameObject CreateCardBase(string name, Transform parent, Vector2 size)
    {
        GameObject go = CreateUiObject(name, parent, size);

        Image bg = go.AddComponent<Image>();
        bg.sprite = cardSprite;
        bg.type = Image.Type.Sliced;
        bg.color = Color.white;

        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
        shadow.effectDistance = new Vector2(4f, -4f);

        return go;
    }

    private Button CreateRoundedButton(Transform parent, string name, Vector2 size, Color color)
    {
        GameObject go = CreateUiObject(name, parent, size);

        Image image = go.AddComponent<Image>();
        image.sprite = cardSprite;
        image.type = Image.Type.Sliced;
        image.color = color;

        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
        shadow.effectDistance = new Vector2(4f, -4f);

        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    private GameObject CreateUiObject(string name, Transform parent, Vector2 size, bool stretch = false, bool stretchX = false)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        if (stretch)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go;
        }

        if (stretchX)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.offsetMin = new Vector2(0f, 0f);
            rect.offsetMax = new Vector2(0f, size.y);
            return go;
        }

        rect.sizeDelta = size;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        return go;
    }

    private TextMeshProUGUI CreateCenteredLabel(RectTransform parent, string text, float fontSize)
    {
        GameObject go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = parent.sizeDelta;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;

        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;

        return tmp;
    }

    private IEnumerator TweenAnchoredPosition(RectTransform rect, Vector2 from, Vector2 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / duration));
            rect.anchoredPosition = Vector2.Lerp(from, to, t);
            yield return null;
        }

        rect.anchoredPosition = to;
    }

    private IEnumerator BounceScale(RectTransform rect, float from, float to, float duration)
    {
        float half = duration * 0.5f;

        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            rect.localScale = Vector3.one * Mathf.Lerp(from, to, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            rect.localScale = Vector3.one * Mathf.Lerp(to, from, t);
            yield return null;
        }

        rect.localScale = Vector3.one * from;
    }

    private IEnumerator Shake(RectTransform rect, float duration, float strength)
    {
        Vector2 basePos = rect.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = Mathf.Sin(elapsed * 60f) * strength * (1f - elapsed / duration);
            rect.anchoredPosition = basePos + new Vector2(x, 0f);
            yield return null;
        }

        rect.anchoredPosition = basePos;
    }

    private IEnumerator FlashImage(Image image, Color flashColor, float duration)
    {
        if (image == null)
            yield break;

        Color baseColor = image.color;
        float half = duration * 0.5f;

        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            image.color = Color.Lerp(baseColor, flashColor, Mathf.Clamp01(elapsed / half));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            image.color = Color.Lerp(flashColor, baseColor, Mathf.Clamp01(elapsed / half));
            yield return null;
        }

        image.color = baseColor;
    }

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private static float EaseOutBack(float t)
    {
        t = Mathf.Clamp01(t);
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    private void BuildShapeColorTable()
    {
        shapeColors[MatchShapeType.Circle] = new Color(0.98f, 0.85f, 0.22f, 1f);
        shapeColors[MatchShapeType.Square] = new Color(0.94f, 0.30f, 0.30f, 1f);
        shapeColors[MatchShapeType.Triangle] = new Color(0.25f, 0.54f, 0.95f, 1f);
        shapeColors[MatchShapeType.Hexagon] = new Color(0.28f, 0.78f, 0.39f, 1f);
    }

    private void BuildSpriteCache()
    {
        cardSprite = BuildRoundedCardSprite(64, 64, 8, 4, Color.white, new Color(0.24f, 0.58f, 0.96f, 1f));
        skyGradientSprite = BuildVerticalGradientSprite(
            32,
            512,
            new Color(0.74f, 0.90f, 1f, 1f),
            new Color(0.49f, 0.79f, 0.98f, 1f),
            new Color(0.36f, 0.69f, 0.95f, 1f));

        grassSprite = BuildGrassSprite(64, 20, new Color(0.35f, 0.78f, 0.37f, 1f));
        circleSprite = BuildPolygonSprite(64, 64, 48);

        shapeSprites[MatchShapeType.Circle] = BuildPolygonSprite(256, 256, 48, 90f);
        shapeSprites[MatchShapeType.Square] = BuildPolygonSprite(256, 256, 4, 45f);
        shapeSprites[MatchShapeType.Triangle] = BuildPolygonSprite(256, 256, 3, 90f);
        shapeSprites[MatchShapeType.Hexagon] = BuildPolygonSprite(256, 256, 6, 30f);
    }

    private Sprite BuildRoundedCardSprite(int width, int height, int radius, int border, Color fill, Color borderColor)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool inOuter = IsInsideRoundedRect(x, y, width, height, radius);
                bool inInner = IsInsideRoundedRect(x, y, width, height, Mathf.Max(0, radius - border))
                               && x >= border
                               && x < width - border
                               && y >= border
                               && y < height - border;

                if (!inOuter)
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
                else
                    tex.SetPixel(x, y, inInner ? fill : borderColor);
            }
        }

        tex.Apply();
        return Sprite.Create(
            tex,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(12f, 12f, 12f, 12f));
    }

    private static bool IsInsideRoundedRect(int x, int y, int width, int height, int r)
    {
        int left = r;
        int right = width - r - 1;
        int bottom = r;
        int top = height - r - 1;

        if (x >= left && x <= right)
            return true;
        if (y >= bottom && y <= top)
            return true;

        int cx = x < left ? left : right;
        int cy = y < bottom ? bottom : top;

        int dx = x - cx;
        int dy = y - cy;

        return dx * dx + dy * dy <= r * r;
    }

    private Sprite BuildVerticalGradientSprite(int width, int height, Color top, Color middle, Color bottom)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < height; y++)
        {
            float t = y / (height - 1f);
            Color color = t < 0.5f
                ? Color.Lerp(bottom, middle, t * 2f)
                : Color.Lerp(middle, top, (t - 0.5f) * 2f);

            for (int x = 0; x < width; x++)
                tex.SetPixel(x, y, color);
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite BuildGrassSprite(int width, int height, Color color)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool topRow = y >= height - 4;
                bool cornerCut = topRow && ((x < 5 && y > height - 2) || (x >= width - 5 && y > height - 2));
                tex.SetPixel(x, y, cornerCut ? new Color(0f, 0f, 0f, 0f) : color);
            }
        }

        tex.Apply();
        return Sprite.Create(
            tex,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(10f, 3f, 10f, 8f));
    }

    private Sprite BuildPolygonSprite(int width, int height, int sides, float startAngleDeg = 90f)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
        float radius = Mathf.Min(width, height) * 0.42f;

        Vector2[] verts = new Vector2[Mathf.Max(3, sides)];
        for (int i = 0; i < verts.Length; i++)
        {
            float angle = Mathf.Deg2Rad * (startAngleDeg + i * (360f / verts.Length));
            verts[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool inside = IsPointInsidePolygon(new Vector2(x, y), verts);
                tex.SetPixel(x, y, inside ? Color.white : new Color(0f, 0f, 0f, 0f));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private static bool IsPointInsidePolygon(Vector2 point, Vector2[] vertices)
    {
        bool inside = false;
        for (int i = 0, j = vertices.Length - 1; i < vertices.Length; j = i++)
        {
            bool intersects = ((vertices[i].y > point.y) != (vertices[j].y > point.y)) &&
                              (point.x < (vertices[j].x - vertices[i].x) * (point.y - vertices[i].y)
                               / ((vertices[j].y - vertices[i].y) + 0.0001f) + vertices[i].x);
            if (intersects)
                inside = !inside;
        }
        return inside;
    }

    private void EnsureCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGo = new GameObject("Main Camera", typeof(Camera));
            cam = camGo.GetComponent<Camera>();
            cam.tag = "MainCamera";
        }

        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.60f, 0.85f, 1f, 1f);
        cam.transform.position = new Vector3(0f, 0f, -10f);
    }

    private void EnsureCanvasAndEventSystem()
    {
        rootCanvas = FindFirstObjectByType<Canvas>();
        if (rootCanvas == null)
        {
            GameObject canvasGo = new GameObject("MatchTheShapeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            rootCanvas = canvasGo.GetComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        canvasRect = rootCanvas.GetComponent<RectTransform>();

        EventSystem es = FindFirstObjectByType<EventSystem>();
        if (es == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}

public class ShapeCardDraggable : MonoBehaviour, IPointerClickHandler
{
    public MatchShapeType ShapeType { get; private set; }
    public RectTransform Rect { get; private set; }
    public Vector2 OriginalPosition { get; private set; }
    public int RowIndex { get; private set; }
    public bool IsLocked { get; private set; }

    public Image CardBackground { get; private set; }

    private MatchTheShapeManager manager;
    private Shadow shadow;
    private Outline outline;
    private Coroutine selectedPulse;
    private readonly Color normalOutline = new Color(0f, 0f, 0f, 0f);
    private readonly Color selectedOutline = new Color(1f, 0.70f, 0.18f, 1f);
    private readonly Color lockedOutline = new Color(0.18f, 0.72f, 0.26f, 1f);

    public void Initialize(
        MatchTheShapeManager owner,
        MatchShapeType shape,
        int rowIndex,
        RectTransform rect,
        Vector2 origin,
        Image shapeImage)
    {
        manager = owner;
        ShapeType = shape;
        RowIndex = rowIndex;
        Rect = rect;
        OriginalPosition = origin;
        CardBackground = GetComponent<Image>();
        shadow = GetComponent<Shadow>();
        outline = gameObject.AddComponent<Outline>();
        outline.effectDistance = new Vector2(7f, 7f);
        outline.effectColor = normalOutline;

        IsLocked = false;
    }

    public void SetSelectedVisual(bool selected)
    {
        if (IsLocked)
            return;

        if (selected)
        {
            if (selectedPulse != null)
                StopCoroutine(selectedPulse);

            selectedPulse = StartCoroutine(SelectedPulse());
            if (outline != null)
                outline.effectColor = selectedOutline;

            if (shadow != null)
                shadow.effectDistance = new Vector2(8f, -8f);
            return;
        }

        if (selectedPulse != null)
        {
            StopCoroutine(selectedPulse);
            selectedPulse = null;
        }

        transform.localScale = Vector3.one;
        if (outline != null)
            outline.effectColor = normalOutline;

        if (shadow != null)
            shadow.effectDistance = new Vector2(4f, -4f);
    }

    public void LockMatched()
    {
        IsLocked = true;
        if (selectedPulse != null)
        {
            StopCoroutine(selectedPulse);
            selectedPulse = null;
        }

        transform.localScale = Vector3.one;

        if (CardBackground != null)
            CardBackground.color = new Color(0.90f, 1f, 0.90f, 1f);

        if (outline != null)
            outline.effectColor = lockedOutline;

        if (shadow != null)
        {
            shadow.effectDistance = new Vector2(5f, -5f);
            shadow.effectColor = new Color(0.16f, 0.58f, 0.23f, 0.35f);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        manager?.OnShapeCardTapped(this);
    }

    private IEnumerator SelectedPulse()
    {
        while (true)
        {
            float s = 1.1f + Mathf.Sin(Time.time * 8f) * 0.02f;
            transform.localScale = Vector3.one * s;
            yield return null;
        }
    }
}

public class NameCardDropZone : MonoBehaviour, IPointerClickHandler
{
    public MatchShapeType ShapeType { get; private set; }
    public int RowIndex { get; private set; }
    public RectTransform Rect { get; private set; }
    public bool IsMatched { get; private set; }

    public Image CardBackground { get; private set; }

    private MatchTheShapeManager manager;
    private Shadow shadow;
    private Outline outline;

    public void Initialize(MatchTheShapeManager owner, MatchShapeType shapeType, int rowIndex, RectTransform rect, Image cardImage, Shadow cardShadow)
    {
        manager = owner;
        ShapeType = shapeType;
        RowIndex = rowIndex;
        Rect = rect;
        CardBackground = cardImage;
        shadow = cardShadow;
        outline = gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0f);
        outline.effectDistance = new Vector2(7f, 7f);

        IsMatched = false;
    }

    public void SetRowIndex(int rowIndex)
    {
        RowIndex = rowIndex;
    }

    public void SetMatchedVisual()
    {
        IsMatched = true;
        transform.localScale = Vector3.one;

        if (CardBackground != null)
            CardBackground.color = new Color(0.86f, 1f, 0.86f, 1f);

        if (outline != null)
            outline.effectColor = new Color(0.18f, 0.72f, 0.26f, 1f);

        if (shadow != null)
        {
            shadow.effectDistance = new Vector2(5f, -5f);
            shadow.effectColor = new Color(0.16f, 0.58f, 0.23f, 0.35f);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        manager?.OnNameCardTapped(this);
    }
}

public class CloudDrift : MonoBehaviour
{
    private float speed;
    private float wrapWidth;
    private RectTransform rect;

    public void Initialize(float driftSpeed, float wrap)
    {
        speed = driftSpeed;
        wrapWidth = wrap;
        rect = GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (rect == null)
            return;

        Vector2 pos = rect.anchoredPosition;
        pos.x += speed * Time.deltaTime;

        if (pos.x > wrapWidth * 0.5f)
            pos.x = -wrapWidth * 0.5f;

        rect.anchoredPosition = pos;
    }
}

public static class MatchTheShapeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureManager()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.name.Equals("MatchTheShape", StringComparison.OrdinalIgnoreCase))
            return;

        MatchTheShapeManager existing = UnityEngine.Object.FindFirstObjectByType<MatchTheShapeManager>();
        if (existing != null)
            return;

        GameObject root = new GameObject("MatchTheShapeManager");
        root.AddComponent<MatchTheShapeManager>();
    }
}
