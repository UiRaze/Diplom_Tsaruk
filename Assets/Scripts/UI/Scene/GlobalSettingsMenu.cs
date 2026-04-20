using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GlobalSettingsMenu : MonoBehaviour
{
    private const string MusicVolumeKey = "Settings.MusicVolume";
    private const string SfxVolumeKey = "Settings.SfxVolume";

    private static GlobalSettingsMenu instance;

    [SerializeField] private Canvas canvas;
    [SerializeField] private Button settingsButton;
    [SerializeField] private GameObject panel;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI musicValueText;
    [SerializeField] private TextMeshProUGUI sfxValueText;

    public static void EnsureExists()
    {
        if (instance != null)
        {
            return;
        }

        GlobalSettingsMenu existing = FindObjectOfType<GlobalSettingsMenu>();
        if (existing != null)
        {
            instance = existing;
            return;
        }

        GameObject go = new GameObject("GlobalSettingsMenu", typeof(GlobalSettingsMenu));
        instance = go.GetComponent<GlobalSettingsMenu>();
    }

    public static void ToggleFromCode()
    {
        EnsureExists();
        instance?.TogglePanel();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureExists();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void Start()
    {
        EnsureUi();
        ApplySavedVolumeValues();
        SetPanelVisible(false);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureUi();
        ApplySavedVolumeValues();
        SetPanelVisible(false);
    }

    private void TogglePanel()
    {
        if (panel == null)
        {
            EnsureUi();
        }

        if (panel != null)
        {
            SetPanelVisible(!panel.activeSelf);
        }
    }

    private void SetPanelVisible(bool isVisible)
    {
        if (panel != null)
        {
            panel.SetActive(isVisible);
        }
    }

    private void ApplySavedVolumeValues()
    {
        float music = PlayerPrefs.GetFloat(MusicVolumeKey, 0.5f);
        float sfx = PlayerPrefs.GetFloat(SfxVolumeKey, 0.7f);

        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(music);
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(sfx);
        }

        OnMusicChanged(music);
        OnSfxChanged(sfx);
    }

    private void OnMusicChanged(float value)
    {
        AudioManager.Instance?.SetMusicVolume(value);
        if (musicValueText != null)
        {
            musicValueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }

        PlayerPrefs.SetFloat(MusicVolumeKey, value);
    }

    private void OnSfxChanged(float value)
    {
        AudioManager.Instance?.SetSFXVolume(value);
        if (sfxValueText != null)
        {
            sfxValueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }

        PlayerPrefs.SetFloat(SfxVolumeKey, value);
    }

    private void EnsureUi()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("GlobalSettingsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);

            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 4000;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        if (settingsButton == null)
        {
            settingsButton = CreateSettingsButton(canvas.transform);
        }

        if (panel == null)
        {
            panel = CreateSettingsPanel(canvas.transform);
        }
    }

    private Button CreateSettingsButton(Transform parent)
    {
        GameObject buttonGO = new GameObject("GlobalSettingsButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(parent, false);

        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-16f, -16f);
        rect.sizeDelta = new Vector2(46f, 46f);

        Image image = buttonGO.GetComponent<Image>();
        image.color = new Color(0.15f, 0.62f, 0.98f, 1f);

        Button button = buttonGO.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(TogglePanel);

        GameObject textGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(buttonGO.transform, false);

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textGO.GetComponent<TextMeshProUGUI>();
        text.text = "≡";
        text.fontSize = 28f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        return button;
    }

    private GameObject CreateSettingsPanel(Transform parent)
    {
        GameObject panelGO = new GameObject("GlobalSettingsPanel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(parent, false);

        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-16f, -72f);
        panelRect.sizeDelta = new Vector2(360f, 250f);

        Image panelImage = panelGO.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.88f);

        CreateText(panelGO.transform, "Title", "Настройки", 32f, new Vector2(0f, -28f), new Vector2(320f, 40f), TextAlignmentOptions.Center);
        CreateText(panelGO.transform, "MusicLabel", "Музыка", 24f, new Vector2(-110f, -86f), new Vector2(130f, 30f), TextAlignmentOptions.MidlineLeft);
        CreateText(panelGO.transform, "SfxLabel", "Эффекты", 24f, new Vector2(-110f, -146f), new Vector2(130f, 30f), TextAlignmentOptions.MidlineLeft);

        musicSlider = CreateSlider(panelGO.transform, "MusicSlider", new Vector2(40f, -86f));
        sfxSlider = CreateSlider(panelGO.transform, "SfxSlider", new Vector2(40f, -146f));

        musicValueText = CreateText(panelGO.transform, "MusicValue", "0%", 20f, new Vector2(140f, -86f), new Vector2(80f, 30f), TextAlignmentOptions.Center);
        sfxValueText = CreateText(panelGO.transform, "SfxValue", "0%", 20f, new Vector2(140f, -146f), new Vector2(80f, 30f), TextAlignmentOptions.Center);

        Button close = CreateTextButton(panelGO.transform, "CloseButton", "Закрыть", new Vector2(0f, -206f), new Vector2(180f, 36f), new Color(0.3f, 0.2f, 0.2f, 1f));
        close.onClick.RemoveAllListeners();
        close.onClick.AddListener(() => SetPanelVisible(false));

        musicSlider.onValueChanged.RemoveAllListeners();
        musicSlider.onValueChanged.AddListener(OnMusicChanged);
        sfxSlider.onValueChanged.RemoveAllListeners();
        sfxSlider.onValueChanged.AddListener(OnSfxChanged);

        return panelGO;
    }

    private Slider CreateSlider(Transform parent, string name, Vector2 anchoredPosition)
    {
        GameObject sliderGO = new GameObject(name, typeof(RectTransform), typeof(Slider));
        sliderGO.transform.SetParent(parent, false);

        RectTransform rect = sliderGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(180f, 20f);

        GameObject bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(sliderGO.transform, false);
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bg = bgGO.GetComponent<Image>();
        bg.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        GameObject fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaGO.transform.SetParent(sliderGO.transform, false);
        RectTransform fillAreaRect = fillAreaGO.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(8f, 5f);
        fillAreaRect.offsetMax = new Vector2(-8f, -5f);

        GameObject fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fill = fillGO.GetComponent<Image>();
        fill.color = new Color(0.18f, 0.75f, 0.34f, 1f);

        GameObject handleAreaGO = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleAreaGO.transform.SetParent(sliderGO.transform, false);
        RectTransform handleAreaRect = handleAreaGO.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = Vector2.zero;
        handleAreaRect.offsetMax = Vector2.zero;

        GameObject handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleGO.transform.SetParent(handleAreaGO.transform, false);
        RectTransform handleRect = handleGO.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(16f, 22f);
        Image handle = handleGO.GetComponent<Image>();
        handle.color = new Color(0.95f, 0.95f, 0.95f, 1f);

        Slider slider = sliderGO.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.5f;
        slider.fillRect = fillRect;
        slider.targetGraphic = handle;
        slider.handleRect = handleRect;
        slider.direction = Slider.Direction.LeftToRight;

        return slider;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, string value, float fontSize, Vector2 anchoredPosition, Vector2 size, TextAlignmentOptions alignment)
    {
        GameObject textGO = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(parent, false);

        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textGO.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private Button CreateTextButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject buttonGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(parent, false);

        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonGO.GetComponent<Image>();
        image.color = color;

        Button button = buttonGO.GetComponent<Button>();

        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(buttonGO.transform, false);

        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = labelGO.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 22f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        return button;
    }
}
