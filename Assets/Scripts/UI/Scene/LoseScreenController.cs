using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoseScreenController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string retrySceneName = "WorldMap";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("UI")]
    [SerializeField] private Image blurOverlay;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI reasonText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI metaText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;

    private GameManager gameManager;

    private void Start()
    {
        gameManager = GameManager.Instance;

        ResolveReferences();
        EnsureUiExists();
        FillTextData();

        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(OnRetryClicked);
            retryButton.onClick.AddListener(OnRetryClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        Time.timeScale = 1f;
    }

    private void FillTextData()
    {
        if (titleText != null)
        {
            titleText.text = "Поражение";
        }

        if (reasonText != null)
        {
            string reason = gameManager != null ? gameManager.LastDefeatReason : "Герой пал в бою.";
            reasonText.text = $"Причина: {reason}";
        }

        if (statsText != null)
        {
            string stats = gameManager != null
                ? gameManager.BuildRunStatsSummary()
                : "Статистика забега недоступна";

            statsText.text = stats;
        }

        if (metaText != null)
        {
            int metaGold = gameManager != null ? gameManager.GetMetaGold() : 0;
            int metaResources = gameManager != null ? gameManager.GetMetaResources() : 0;
            metaText.text = $"Мета-прогресс: {metaGold} монет, {metaResources} ресурсов";
        }
    }

    private void OnRetryClicked()
    {
        if (gameManager != null)
        {
            gameManager.StartNewRun();
        }

        SceneLoader.LoadSceneAsync(retrySceneName);
    }

    private void OnMainMenuClicked()
    {
        if (gameManager != null)
        {
            gameManager.ClearDefeatReason();
        }

        SceneLoader.LoadSceneAsync(mainMenuSceneName);
    }

    private void ResolveReferences()
    {
        if (blurOverlay == null)
        {
            blurOverlay = GameObject.Find("LoseBlurOverlay")?.GetComponent<Image>();
        }

        if (titleText == null)
        {
            titleText = GameObject.Find("LoseTitleText")?.GetComponent<TextMeshProUGUI>();
        }

        if (reasonText == null)
        {
            reasonText = GameObject.Find("LoseReasonText")?.GetComponent<TextMeshProUGUI>();
        }

        if (statsText == null)
        {
            statsText = GameObject.Find("LoseStatsText")?.GetComponent<TextMeshProUGUI>();
        }

        if (metaText == null)
        {
            metaText = GameObject.Find("LoseMetaText")?.GetComponent<TextMeshProUGUI>();
        }

        if (retryButton == null)
        {
            retryButton = GameObject.Find("RetryButton")?.GetComponent<Button>();
        }

        if (mainMenuButton == null)
        {
            mainMenuButton = GameObject.Find("BackToMenuButton")?.GetComponent<Button>();
        }
    }

    private void EnsureUiExists()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("LoseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        Transform parent = canvas.transform;

        if (blurOverlay == null)
        {
            GameObject overlayGO = new GameObject("LoseBlurOverlay", typeof(RectTransform), typeof(Image));
            overlayGO.transform.SetParent(parent, false);

            RectTransform rect = overlayGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            blurOverlay = overlayGO.GetComponent<Image>();
            blurOverlay.color = new Color(0f, 0f, 0f, 0.72f);
            blurOverlay.raycastTarget = true;
        }

        if (titleText == null)
        {
            titleText = CreateText(parent, "LoseTitleText", new Vector2(0f, 210f), new Vector2(900f, 80f), 64f, TextAlignmentOptions.Center, "Поражение");
        }

        if (reasonText == null)
        {
            reasonText = CreateText(parent, "LoseReasonText", new Vector2(0f, 120f), new Vector2(1100f, 70f), 34f, TextAlignmentOptions.Center, "Причина: ...");
        }

        if (statsText == null)
        {
            statsText = CreateText(parent, "LoseStatsText", new Vector2(0f, 40f), new Vector2(1300f, 70f), 30f, TextAlignmentOptions.Center, "Статистика");
        }

        if (metaText == null)
        {
            metaText = CreateText(parent, "LoseMetaText", new Vector2(0f, -30f), new Vector2(1300f, 70f), 28f, TextAlignmentOptions.Center, "Мета-прогресс");
        }

        if (retryButton == null)
        {
            retryButton = CreateButton(parent, "RetryButton", new Vector2(-180f, -150f), new Vector2(280f, 64f), "Попробовать снова");
        }

        if (mainMenuButton == null)
        {
            mainMenuButton = CreateButton(parent, "BackToMenuButton", new Vector2(180f, -150f), new Vector2(280f, 64f), "В главное меню");
        }
    }

    private TextMeshProUGUI CreateText(Transform parent, string objectName, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment, string initialText)
    {
        GameObject textGO = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(parent, false);

        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textGO.GetComponent<TextMeshProUGUI>();
        text.text = initialText;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;

        return text;
    }

    private Button CreateButton(Transform parent, string objectName, Vector2 position, Vector2 size, string label)
    {
        GameObject buttonGO = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(parent, false);

        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = buttonGO.GetComponent<Image>();
        image.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

        Button button = buttonGO.GetComponent<Button>();

        GameObject textGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(buttonGO.transform, false);

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textGO.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 28f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        return button;
    }
}
