using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public partial class GemzyGame
{
    private Camera gameCamera;
    [SerializeField] private Font pixelFont;
    [SerializeField] private Transform boardRoot;
    [SerializeField] private Transform tileRoot;
    [SerializeField] private RectTransform safeAreaRoot;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text movesText;
    [SerializeField] private Text targetText;
    [SerializeField] private Text statusText;
    [SerializeField] private Button restartButton;
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

        if (boardRoot == null || tileRoot == null || scoreText == null || movesText == null || targetText == null || statusText == null)
        {
            ClearGeneratedObjects();
            CreateScene();
        }
        else
        {
            EnsureEventSystem();
            EnsureRestartButtonListener();
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

        Text title = CreateText(safeAreaRoot, "JEWEL MATCH", 54, FontStyle.Bold, TextAnchor.MiddleCenter);
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(680f, 90f));
        title.color = new Color(1f, 0.92f, 0.62f);

        scoreText = CreateText(safeAreaRoot, "", 34, FontStyle.Bold, TextAnchor.MiddleLeft);
        SetRect(scoreText.rectTransform, new Vector2(0f, 1f), new Vector2(42f, -150f), new Vector2(340f, 70f));

        movesText = CreateText(safeAreaRoot, "", 34, FontStyle.Bold, TextAnchor.MiddleRight);
        SetRect(movesText.rectTransform, new Vector2(1f, 1f), new Vector2(-42f, -150f), new Vector2(340f, 70f));

        targetText = CreateText(safeAreaRoot, "", 28, FontStyle.Normal, TextAnchor.MiddleCenter);
        SetRect(targetText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 108f), new Vector2(680f, 70f));

        statusText = CreateText(safeAreaRoot, "", 34, FontStyle.Bold, TextAnchor.MiddleCenter);
        SetRect(statusText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 56f), new Vector2(850f, 90f));
        statusText.color = new Color(0.67f, 0.93f, 1f);

        restartButton = CreateButton(safeAreaRoot, "Restart");
        SetRect(restartButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 178f), new Vector2(260f, 72f));
        EnsureRestartButtonListener();
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
        scoreText.text = "Score " + score;
        movesText.text = "Moves " + movesLeft;
        targetText.text = "Target " + TargetScore;
        statusText.text = status;
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
        GameObject obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);
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

    private Button CreateButton(Transform parent, string label)
    {
        GameObject obj = new GameObject(label + " Button");
        obj.transform.SetParent(parent, false);

        Image image = obj.AddComponent<Image>();
        image.color = new Color(0.17f, 0.45f, 0.65f, 0.94f);

        Button button = obj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.24f, 0.6f, 0.82f, 1f);
        colors.pressedColor = new Color(0.11f, 0.31f, 0.48f, 1f);
        button.colors = colors;

        Text text = CreateText(obj.transform, label, 30, FontStyle.Bold, TextAnchor.MiddleCenter);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return button;
    }

    private void SetRect(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
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
        ClearNamedGeneratedObject("Jewel Match HUD");
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
