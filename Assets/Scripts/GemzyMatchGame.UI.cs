using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public partial class GemzyGame
{
    private static readonly Color HudPanelColor = new Color(0.08f, 0.1f, 0.16f, 0.88f);
    private static readonly Color HudPanelBorderColor = new Color(0.44f, 0.7f, 0.86f, 0.85f);
    private static readonly Color GoldTextColor = new Color(1f, 0.86f, 0.36f, 1f);
    private static readonly Color CyanTextColor = new Color(0.54f, 0.95f, 1f, 1f);
    private static readonly Color ButtonBaseColor = new Color(0.12f, 0.46f, 0.64f, 0.98f);
    private static readonly Color ButtonHoverColor = new Color(0.2f, 0.65f, 0.84f, 1f);
    private static readonly Color ButtonPressColor = new Color(0.06f, 0.26f, 0.39f, 1f);

    private Camera gameCamera;
    [SerializeField] private Font pixelFont;
    [SerializeField] private Transform boardRoot;
    [SerializeField] private Transform tileRoot;
    [SerializeField] private RectTransform safeAreaRoot;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text movesText;
    [SerializeField] private Text targetText;
    [SerializeField] private Text statusText;
    [SerializeField] private Image progressFill;
    [SerializeField] private Text progressText;
    [SerializeField] private RectTransform feedbackRoot;
    [SerializeField] private CanvasGroup resultPanelGroup;
    [SerializeField] private Text resultTitleText;
    [SerializeField] private Text resultScoreText;
    [SerializeField] private Text resultMovesText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button playAgainButton;
    private int lastScreenWidth;
    private int lastScreenHeight;
    private Rect lastSafeArea;

    private void ConfigureCamera()
    {
        gameCamera = Camera.main;
        if (gameCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            gameCamera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
        }

        gameCamera.orthographic = true;
        ApplyCameraSize();
        gameCamera.transform.position = new Vector3(0f, 0f, -10f);
        gameCamera.backgroundColor = new Color(0.07f, 0.09f, 0.13f);
        gameCamera.clearFlags = CameraClearFlags.SolidColor;
        gameCamera.allowHDR = false;
        gameCamera.allowMSAA = false;
        gameCamera.useOcclusionCulling = false;
    }

    private void ConfigureMobileRuntime()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = MobileTargetFrameRate;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

#if UNITY_ANDROID || UNITY_IOS
        Screen.orientation = ScreenOrientation.Portrait;
#endif
    }

    private void RefreshMobileLayout()
    {
        if (lastScreenWidth == Screen.width && lastScreenHeight == Screen.height && lastSafeArea == Screen.safeArea)
        {
            return;
        }

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        lastSafeArea = Screen.safeArea;
        ApplyCameraSize();
        ApplySafeArea();
    }

    private void ApplyCameraSize()
    {
        if (gameCamera == null)
        {
            return;
        }

        float aspect = Screen.width > 0 && Screen.height > 0
            ? (float)Screen.width / Screen.height
            : 9f / 16f;
        float boardHalfWidth = Width * CellSize * 0.5f;
        float boardHalfHeight = Height * CellSize * 0.5f;
        float sizeForWidth = (boardHalfWidth + BoardCameraPadding) / Mathf.Max(0.45f, aspect);
        float sizeForHeight = boardHalfHeight + BoardCameraPadding;
        gameCamera.orthographicSize = Mathf.Clamp(Mathf.Max(sizeForWidth, sizeForHeight), MinCameraSize, MaxCameraSize);
    }

    private void CreateScene()
    {
        boardRoot = new GameObject("Board").transform;
        boardRoot.SetParent(transform);
        tileRoot = new GameObject("Tiles").transform;
        tileRoot.SetParent(boardRoot);

        CreateBoardBackground();
        CreateHud();
    }

    private void EnsureScene()
    {
        if (boardRoot == null)
        {
            Transform existingBoard = transform.Find("Board");
            boardRoot = existingBoard != null ? existingBoard : GameObject.Find("Board")?.transform;
        }

        if (tileRoot == null && boardRoot != null)
        {
            tileRoot = boardRoot.Find("Tiles");
        }

        if (safeAreaRoot == null)
        {
            Transform existingHud = transform.Find("Gemzy HUD");
            Transform existingSafeArea = existingHud != null ? existingHud.Find("Safe Area") : null;
            safeAreaRoot = existingSafeArea != null ? existingSafeArea.GetComponent<RectTransform>() : null;
        }

        if (restartButton == null && safeAreaRoot != null)
        {
            restartButton = safeAreaRoot.GetComponentInChildren<Button>();
        }

        if (boardRoot == null || tileRoot == null || scoreText == null || movesText == null || targetText == null ||
            statusText == null || progressFill == null || resultPanelGroup == null || feedbackRoot == null)
        {
            ClearGeneratedObjects();
            CreateScene();
        }
        else
        {
            EnsureEventSystem();
            EnsureRestartButtonListener();
            EnsurePlayAgainButtonListener();
            ApplySafeArea();
        }
    }

    private void CreateBoardBackground()
    {
        GameObject backdrop = CreateSpriteObject("Board Backdrop", boardRoot, new Vector3(0f, BoardLift, 0.25f), squareSprite);
        backdrop.transform.localScale = new Vector3(8.15f, 8.15f, 1f);
        backdrop.GetComponent<SpriteRenderer>().color = new Color(0.1f, 0.13f, 0.18f, 0.95f);

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Vector3 position = TilePosition(x, y);
                GameObject slot = CreateSpriteObject("Slot", boardRoot, new Vector3(position.x, position.y, 0.2f), squareSprite);
                slot.transform.localScale = new Vector3(0.86f, 0.86f, 1f);
                slot.GetComponent<SpriteRenderer>().color = ((x + y) % 2 == 0)
                    ? new Color(0.19f, 0.23f, 0.29f, 0.95f)
                    : new Color(0.14f, 0.18f, 0.24f, 0.95f);
            }
        }
    }

    private void CreateHud()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("Gemzy HUD");
        canvasObject.transform.SetParent(transform);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1080f, 1920f);
        canvasScaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        safeAreaRoot = new GameObject("Safe Area").AddComponent<RectTransform>();
        safeAreaRoot.SetParent(canvasObject.transform, false);
        safeAreaRoot.anchorMin = Vector2.zero;
        safeAreaRoot.anchorMax = Vector2.one;
        safeAreaRoot.offsetMin = Vector2.zero;
        safeAreaRoot.offsetMax = Vector2.zero;
        ApplySafeArea();

        Text title = CreateText(safeAreaRoot, "GEMZY", 68, FontStyle.Bold, TextAnchor.MiddleCenter);
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(720f, 90f));
        title.color = GoldTextColor;
        AddTextShadow(title, new Color(0.08f, 0.04f, 0.02f, 0.85f), new Vector2(4f, -4f));

        RectTransform statsRoot = CreateUiObject("Top Stats", safeAreaRoot).GetComponent<RectTransform>();
        SetRect(statsRoot, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(980f, 120f));

        scoreText = CreateStatPanel(statsRoot, "Score", new Vector2(0f, 0.5f), new Vector2(165f, 0f), TextAnchor.MiddleLeft);
        targetText = CreateStatPanel(statsRoot, "Target", new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter);
        movesText = CreateStatPanel(statsRoot, "Moves", new Vector2(1f, 0.5f), new Vector2(-165f, 0f), TextAnchor.MiddleRight);

        CreateProgressBar();

        feedbackRoot = CreateUiObject("Score Feedback", safeAreaRoot).GetComponent<RectTransform>();
        SetRect(feedbackRoot, new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(720f, 260f));

        statusText = CreateText(safeAreaRoot, "", 34, FontStyle.Bold, TextAnchor.MiddleCenter);
        SetRect(statusText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 92f), new Vector2(860f, 80f));
        statusText.color = CyanTextColor;
        AddTextShadow(statusText, new Color(0f, 0f, 0f, 0.8f), new Vector2(3f, -3f));

        restartButton = CreateButton(safeAreaRoot, "Restart");
        SetRect(restartButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 182f), new Vector2(290f, 78f));
        EnsureRestartButtonListener();
        CreateResultPanel();
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.transform.SetParent(transform);
        EventSystem uiSystem = eventSystem.AddComponent<EventSystem>();
        uiSystem.sendNavigationEvents = true;
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }

    private void EnsureRestartButtonListener()
    {
        if (restartButton == null)
        {
            return;
        }

        restartButton.onClick.RemoveListener(RestartGame);
        restartButton.onClick.AddListener(RestartGame);
    }

    private void EnsurePlayAgainButtonListener()
    {
        if (playAgainButton == null)
        {
            return;
        }

        playAgainButton.onClick.RemoveListener(RestartGame);
        playAgainButton.onClick.AddListener(RestartGame);
    }

    private void ApplySafeArea()
    {
        if (safeAreaRoot == null || Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }

        Rect safeArea = Screen.safeArea;
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        safeAreaRoot.anchorMin = anchorMin;
        safeAreaRoot.anchorMax = anchorMax;
        safeAreaRoot.offsetMin = Vector2.zero;
        safeAreaRoot.offsetMax = Vector2.zero;
    }

    private void UpdateHud(string status)
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }

        if (movesText != null)
        {
            movesText.text = movesLeft.ToString();
        }

        if (targetText != null)
        {
            targetText.text = TargetScore.ToString();
        }

        if (statusText != null)
        {
            statusText.text = status;
        }

        float progress = Mathf.Clamp01((float)score / TargetScore);
        if (progressFill != null)
        {
            progressFill.fillAmount = progress;
        }

        if (progressText != null)
        {
            progressText.text = Mathf.RoundToInt(progress * 100f) + "%";
        }
    }

    private void ShowResultPanel(bool won)
    {
        if (resultPanelGroup == null)
        {
            return;
        }

        resultPanelGroup.alpha = 1f;
        resultPanelGroup.interactable = true;
        resultPanelGroup.blocksRaycasts = true;

        if (resultTitleText != null)
        {
            resultTitleText.text = won ? "YOU WIN!" : "GAME OVER";
            resultTitleText.color = won ? GoldTextColor : new Color(1f, 0.47f, 0.48f, 1f);
        }

        if (resultScoreText != null)
        {
            resultScoreText.text = "Score  " + score + " / " + TargetScore;
        }

        if (resultMovesText != null)
        {
            resultMovesText.text = "Moves Left  " + movesLeft;
        }
    }

    private void HideResultPanel()
    {
        if (resultPanelGroup == null)
        {
            return;
        }

        resultPanelGroup.alpha = 0f;
        resultPanelGroup.interactable = false;
        resultPanelGroup.blocksRaycasts = false;
    }

    private void ShowScoreFeedback(int amount, int chain)
    {
        if (feedbackRoot == null)
        {
            return;
        }

        string label = chain > 1 ? "COMBO x" + chain + "  +" + amount : "+" + amount;
        Text feedback = CreateText(feedbackRoot, label, chain > 1 ? 52 : 42, FontStyle.Bold, TextAnchor.MiddleCenter);
        feedback.color = chain > 1 ? GoldTextColor : CyanTextColor;
        AddTextShadow(feedback, new Color(0f, 0f, 0f, 0.9f), new Vector2(4f, -4f));
        SetRect(feedback.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(Random.Range(-120f, 120f), Random.Range(-10f, 80f)), new Vector2(560f, 90f));
        StartCoroutine(AnimateScoreFeedback(feedback));
    }

    private System.Collections.IEnumerator AnimateScoreFeedback(Text feedback)
    {
        RectTransform rect = feedback.rectTransform;
        CanvasGroup group = feedback.gameObject.AddComponent<CanvasGroup>();
        Vector2 start = rect.anchoredPosition;
        Vector2 end = start + new Vector2(0f, 96f);
        float duration = 0.82f;
        float timer = 0f;

        while (timer < duration && feedback != null)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            rect.anchoredPosition = Vector2.Lerp(start, end, eased);
            rect.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.12f, Mathf.Sin(t * Mathf.PI));
            group.alpha = 1f - Mathf.Clamp01((t - 0.62f) / 0.38f);
            yield return null;
        }

        if (feedback != null)
        {
            SafeDestroy(feedback.gameObject);
        }
    }

    private void SetSelected(Tile tile)
    {
        selected = tile;
        if (tile != null)
        {
            UpdateHud("Pick an adjacent jewel");
        }
    }

    private Text CreateText(Transform parent, string content, int size, FontStyle style, TextAnchor alignment)
    {
        GameObject obj = CreateUiObject("Text", parent);
        Text text = obj.AddComponent<Text>();
        text.text = content;
        text.font = pixelFont != null ? pixelFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 16;
        text.resizeTextMaxSize = size;
        return text;
    }

    private Text CreateStatPanel(Transform parent, string label, Vector2 anchor, Vector2 position, TextAnchor valueAlignment)
    {
        GameObject panel = CreatePixelPanel(label + " Panel", parent, HudPanelColor, HudPanelBorderColor);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        SetRect(panelRect, anchor, position, new Vector2(300f, 102f));

        Text labelText = CreateText(panel.transform, label.ToUpperInvariant(), 22, FontStyle.Bold, TextAnchor.MiddleCenter);
        SetRect(labelText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(240f, 32f));
        labelText.color = new Color(0.65f, 0.82f, 0.9f, 1f);

        Text valueText = CreateText(panel.transform, "", 42, FontStyle.Bold, valueAlignment);
        SetRect(valueText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(230f, 58f));
        valueText.color = Color.white;
        AddTextShadow(valueText, new Color(0f, 0f, 0f, 0.75f), new Vector2(3f, -3f));
        return valueText;
    }

    private void CreateProgressBar()
    {
        GameObject frame = CreatePixelPanel("Progress Bar", safeAreaRoot, new Color(0.05f, 0.07f, 0.11f, 0.92f), HudPanelBorderColor);
        RectTransform frameRect = frame.GetComponent<RectTransform>();
        SetRect(frameRect, new Vector2(0.5f, 1f), new Vector2(0f, -246f), new Vector2(860f, 54f));

        GameObject fillObject = CreateUiObject("Progress Fill", frame.transform);
        Image fillImage = fillObject.AddComponent<Image>();
        fillImage.sprite = squareSprite;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.color = new Color(0.2f, 0.78f, 0.53f, 1f);
        progressFill = fillImage;
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(8f, 8f);
        fillRect.offsetMax = new Vector2(-8f, -8f);

        progressText = CreateText(frame.transform, "", 24, FontStyle.Bold, TextAnchor.MiddleCenter);
        SetRect(progressText.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 42f));
        progressText.color = Color.white;
        AddTextShadow(progressText, new Color(0f, 0f, 0f, 0.85f), new Vector2(2f, -2f));
    }

    private Button CreateButton(Transform parent, string label)
    {
        GameObject obj = CreatePixelPanel(label + " Button", parent, ButtonBaseColor, new Color(0.62f, 0.9f, 1f, 0.95f));

        Image image = obj.GetComponent<Image>();
        image.sprite = squareSprite;
        image.color = ButtonBaseColor;

        Button button = obj.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = Color.white;
        button.colors = colors;

        Text text = CreateText(obj.transform, label, 30, FontStyle.Bold, TextAnchor.MiddleCenter);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        text.color = Color.white;
        AddTextShadow(text, new Color(0f, 0f, 0f, 0.75f), new Vector2(2f, -2f));

        GemzyPixelButtonAnimator animator = obj.AddComponent<GemzyPixelButtonAnimator>();
        animator.Configure(image, ButtonBaseColor, ButtonHoverColor, ButtonPressColor);
        return button;
    }

    private void CreateResultPanel()
    {
        GameObject dimmer = CreateUiObject("Result Panel", safeAreaRoot);
        Image dimmerImage = dimmer.AddComponent<Image>();
        dimmerImage.sprite = squareSprite;
        dimmerImage.color = new Color(0.02f, 0.03f, 0.05f, 0.68f);
        RectTransform dimmerRect = dimmer.GetComponent<RectTransform>();
        dimmerRect.anchorMin = Vector2.zero;
        dimmerRect.anchorMax = Vector2.one;
        dimmerRect.offsetMin = Vector2.zero;
        dimmerRect.offsetMax = Vector2.zero;

        resultPanelGroup = dimmer.AddComponent<CanvasGroup>();

        GameObject panel = CreatePixelPanel("Result Card", dimmer.transform, new Color(0.07f, 0.1f, 0.16f, 0.98f), GoldTextColor);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        SetRect(panelRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 520f));

        resultTitleText = CreateText(panel.transform, "", 62, FontStyle.Bold, TextAnchor.MiddleCenter);
        SetRect(resultTitleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(620f, 92f));
        AddTextShadow(resultTitleText, new Color(0f, 0f, 0f, 0.9f), new Vector2(4f, -4f));

        resultScoreText = CreateText(panel.transform, "", 34, FontStyle.Bold, TextAnchor.MiddleCenter);
        SetRect(resultScoreText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 52f), new Vector2(560f, 62f));
        resultScoreText.color = Color.white;

        resultMovesText = CreateText(panel.transform, "", 30, FontStyle.Bold, TextAnchor.MiddleCenter);
        SetRect(resultMovesText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -14f), new Vector2(560f, 58f));
        resultMovesText.color = CyanTextColor;

        playAgainButton = CreateButton(panel.transform, "Play Again");
        SetRect(playAgainButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(340f, 84f));
        EnsurePlayAgainButtonListener();
        HideResultPanel();
    }

    private GameObject CreatePixelPanel(string name, Transform parent, Color fillColor, Color borderColor)
    {
        GameObject obj = CreateUiObject(name, parent);
        Image image = obj.AddComponent<Image>();
        image.sprite = squareSprite;
        image.color = fillColor;

        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = borderColor;
        outline.effectDistance = new Vector2(4f, -4f);
        return obj;
    }

    private GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = obj.AddComponent<RectTransform>();
        }
        return obj;
    }

    private void AddTextShadow(Text text, Color color, Vector2 distance)
    {
        Shadow shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
    }

    private void SetRect(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
        if (rect == null)
        {
            Debug.LogWarning("SetRect called with a null RectTransform.");
            return;
        }

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private void ClearGeneratedObjects()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            SafeDestroy(transform.GetChild(i).gameObject);
        }

        ClearNamedGeneratedObject("Board");
        ClearNamedGeneratedObject("Gemzy HUD");
    }

    private void ClearNamedGeneratedObject(string objectName)
    {
        GameObject generatedObject = GameObject.Find(objectName);
        if (generatedObject != null && generatedObject.transform.parent == null)
        {
            SafeDestroy(generatedObject);
        }
    }

    private void SafeDestroy(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}

public sealed class GemzyPixelButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private Image targetImage;
    private Color normalColor;
    private Color hoverColor;
    private Color pressColor;
    private Color desiredColor;
    private RectTransform rectTransform;
    private Vector3 desiredScale = Vector3.one;

    public void Configure(Image image, Color normal, Color hover, Color pressed)
    {
        targetImage = image;
        normalColor = normal;
        hoverColor = hover;
        pressColor = pressed;
        desiredColor = normalColor;
        rectTransform = GetComponent<RectTransform>();
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
            normalColor = targetImage != null ? targetImage.color : Color.white;
            hoverColor = normalColor;
            pressColor = normalColor;
            desiredColor = normalColor;
        }
    }

    private void Update()
    {
        if (targetImage != null)
        {
            targetImage.color = Color.Lerp(targetImage.color, desiredColor, Time.unscaledDeltaTime * 18f);
        }

        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, desiredScale, Time.unscaledDeltaTime * 18f);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        desiredColor = hoverColor;
        desiredScale = Vector3.one * 1.04f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        desiredColor = normalColor;
        desiredScale = Vector3.one;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        desiredColor = pressColor;
        desiredScale = Vector3.one * 0.94f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        desiredColor = hoverColor;
        desiredScale = Vector3.one * 1.04f;
    }
}
