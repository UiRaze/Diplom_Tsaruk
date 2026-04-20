using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class NodeInteractionPopup : MonoBehaviour
{
    public struct PopupOption
    {
        public string Label;
        public Action Action;
    }

    private static NodeInteractionPopup instance;

    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Transform optionsRoot;

    private readonly List<Button> spawnedButtons = new List<Button>();
    private Action onClosed;

    public static NodeInteractionPopup Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<NodeInteractionPopup>();
            }

            if (instance == null)
            {
                GameObject go = new GameObject("NodeInteractionPopup", typeof(NodeInteractionPopup));
                instance = go.GetComponent<NodeInteractionPopup>();
            }

            return instance;
        }
    }

    public void Show(string title, string description, IReadOnlyList<PopupOption> options, Action closedCallback = null)
    {
        EnsureUi();
        onClosed = closedCallback;

        if (titleText != null)
        {
            titleText.text = title ?? string.Empty;
        }

        if (descriptionText != null)
        {
            descriptionText.text = description ?? string.Empty;
        }

        BuildOptions(options);

        if (root != null)
        {
            root.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void Close()
    {
        if (root != null)
        {
            root.SetActive(false);
        }

        Time.timeScale = 1f;

        Action callback = onClosed;
        onClosed = null;
        callback?.Invoke();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Start()
    {
        EnsureUi();
        Close();
    }

    private void BuildOptions(IReadOnlyList<PopupOption> options)
    {
        if (optionsRoot == null)
        {
            return;
        }

        for (int i = optionsRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(optionsRoot.GetChild(i).gameObject);
        }

        spawnedButtons.Clear();
        if (options == null)
        {
            return;
        }

        for (int i = 0; i < options.Count; i++)
        {
            CreateOptionButton(options[i]);
        }
    }

    private void CreateOptionButton(PopupOption option)
    {
        GameObject buttonGO = new GameObject("OptionButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(optionsRoot, false);

        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 60f);

        Image image = buttonGO.GetComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        Button button = buttonGO.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            Close();
            option.Action?.Invoke();
        });

        GameObject textGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(buttonGO.transform, false);

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = textGO.GetComponent<TextMeshProUGUI>();
        label.text = string.IsNullOrWhiteSpace(option.Label) ? "Выбрать" : option.Label;
        label.fontSize = 24f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        spawnedButtons.Add(button);
    }

    private void EnsureUi()
    {
        if (root != null && optionsRoot != null && closeButton != null && titleText != null && descriptionText != null)
        {
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("NodePopupCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        if (root == null)
        {
            root = new GameObject("NodeInteractionPopupRoot", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = new Vector2(860f, 620f);

            Image rootImage = root.GetComponent<Image>();
            rootImage.color = new Color(0f, 0f, 0f, 0.92f);
        }

        if (titleText == null)
        {
            titleText = CreateText(root.transform, "Title", new Vector2(0f, 220f), new Vector2(780f, 70f), 44f, TextAlignmentOptions.Center, "Событие");
        }

        if (descriptionText == null)
        {
            descriptionText = CreateText(root.transform, "Description", new Vector2(0f, 110f), new Vector2(780f, 180f), 26f, TextAlignmentOptions.Top, string.Empty);
        }

        if (optionsRoot == null)
        {
            GameObject optionsGO = new GameObject("OptionsRoot", typeof(RectTransform), typeof(VerticalLayoutGroup));
            optionsGO.transform.SetParent(root.transform, false);

            RectTransform optionsRect = optionsGO.GetComponent<RectTransform>();
            optionsRect.anchorMin = new Vector2(0.5f, 0.5f);
            optionsRect.anchorMax = new Vector2(0.5f, 0.5f);
            optionsRect.pivot = new Vector2(0.5f, 0.5f);
            optionsRect.anchoredPosition = new Vector2(0f, -60f);
            optionsRect.sizeDelta = new Vector2(760f, 260f);

            VerticalLayoutGroup layout = optionsGO.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 10f;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            optionsRoot = optionsGO.transform;
        }

        if (closeButton == null)
        {
            closeButton = CreateButton(root.transform, "CloseButton", new Vector2(0f, -258f), new Vector2(260f, 52f), "Закрыть");
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }
    }

    private TextMeshProUGUI CreateText(Transform parent, string objectName, Vector2 anchoredPos, Vector2 size, float fontSize, TextAlignmentOptions alignment, string value)
    {
        GameObject textGO = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(parent, false);

        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textGO.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;

        return text;
    }

    private Button CreateButton(Transform parent, string objectName, Vector2 anchoredPos, Vector2 size, string labelText)
    {
        GameObject buttonGO = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(parent, false);

        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Image image = buttonGO.GetComponent<Image>();
        image.color = new Color(0.35f, 0.15f, 0.15f, 1f);

        Button button = buttonGO.GetComponent<Button>();

        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(buttonGO.transform, false);

        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelGO.GetComponent<TextMeshProUGUI>();
        label.text = labelText;
        label.fontSize = 24f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        return button;
    }
}
