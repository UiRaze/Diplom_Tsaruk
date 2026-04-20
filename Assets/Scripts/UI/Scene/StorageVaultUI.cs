using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class StorageVaultUI : MonoBehaviour
{
    private static StorageVaultUI instance;

    [SerializeField] private GameObject root;
    [SerializeField] private Transform runCardsRoot;
    [SerializeField] private Transform vaultCardsRoot;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button storeButton;
    [SerializeField] private Button retrieveButton;
    [SerializeField] private Button closeButton;

    private CardData selectedRunCard;
    private CardData selectedVaultCard;
    private Action onClosed;

    public static StorageVaultUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<StorageVaultUI>();
            }

            if (instance == null)
            {
                GameObject go = new GameObject("StorageVaultUI", typeof(StorageVaultUI));
                instance = go.GetComponent<StorageVaultUI>();
            }

            return instance;
        }
    }

    public void Open(Action closedCallback = null)
    {
        EnsureUi();
        onClosed = closedCallback;
        selectedRunCard = null;
        selectedVaultCard = null;
        RefreshView();

        if (root != null)
        {
            root.SetActive(true);
        }

        Time.timeScale = 0f;
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
        HideImmediate();
    }

    private void HideImmediate()
    {
        if (root != null)
        {
            root.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    private void Close()
    {
        HideImmediate();
        Action callback = onClosed;
        onClosed = null;
        callback?.Invoke();
    }

    private void RefreshView()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        RebuildList(runCardsRoot, GameManager.Instance.GetRunBonusCards(), selectedRunCard, card => selectedRunCard = card, "Карты забега отсутствуют.");
        RebuildList(vaultCardsRoot, GameManager.Instance.GetStorageVaultCards(), selectedVaultCard, card => selectedVaultCard = card, "Хранилище пусто.");

        if (storeButton != null)
        {
            storeButton.interactable = selectedRunCard != null;
        }

        if (retrieveButton != null)
        {
            retrieveButton.interactable = selectedVaultCard != null;
        }

        if (statusText != null && string.IsNullOrWhiteSpace(statusText.text))
        {
            statusText.text = "Выберите карту слева или справа.";
        }
    }

    private void RebuildList(Transform rootTransform, IReadOnlyList<CardData> cards, CardData selected, Action<CardData> onSelect, string emptyText)
    {
        if (rootTransform == null)
        {
            return;
        }

        for (int i = rootTransform.childCount - 1; i >= 0; i--)
        {
            Destroy(rootTransform.GetChild(i).gameObject);
        }

        if (cards == null || cards.Count == 0)
        {
            CreateInfoRow(rootTransform, emptyText);
            return;
        }

        for (int i = 0; i < cards.Count; i++)
        {
            CardData card = cards[i];
            if (card == null)
            {
                continue;
            }

            CreateCardRow(rootTransform, card, selected == card, onSelect);
        }
    }

    private void CreateCardRow(Transform parent, CardData cardData, bool isSelected, Action<CardData> onSelect)
    {
        GameObject rowGO = new GameObject($"VaultCard_{cardData.CardName}", typeof(RectTransform), typeof(Image), typeof(Button));
        rowGO.transform.SetParent(parent, false);

        RectTransform rect = rowGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 42f);

        Image bg = rowGO.GetComponent<Image>();
        bg.color = isSelected
            ? new Color(0.22f, 0.48f, 0.62f, 0.95f)
            : new Color(0.15f, 0.15f, 0.15f, 0.95f);

        Button button = rowGO.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            onSelect?.Invoke(cardData);
            statusText.text = $"Выбрана карта: {cardData.CardName}";
            RefreshView();
        });

        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(rowGO.transform, false);

        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10f, 0f);
        labelRect.offsetMax = new Vector2(-10f, 0f);

        TextMeshProUGUI label = labelGO.GetComponent<TextMeshProUGUI>();
        label.text = $"{cardData.CardName} (Цена {cardData.Cost}, Урон {cardData.Damage})";
        label.fontSize = 18f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    private void CreateInfoRow(Transform parent, string message)
    {
        GameObject rowGO = new GameObject("InfoRow", typeof(RectTransform), typeof(TextMeshProUGUI));
        rowGO.transform.SetParent(parent, false);

        RectTransform rect = rowGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 42f);

        TextMeshProUGUI text = rowGO.GetComponent<TextMeshProUGUI>();
        text.text = message;
        text.fontSize = 18f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
    }

    private void StoreSelectedCard()
    {
        if (GameManager.Instance == null || selectedRunCard == null)
        {
            return;
        }

        bool success = GameManager.Instance.StoreSpecificCard(selectedRunCard);
        if (success)
        {
            statusText.text = $"Карта отправлена в хранилище: {selectedRunCard.CardName}";
            selectedRunCard = null;
            RefreshView();
            return;
        }

        statusText.text = "Не удалось отправить карту в хранилище.";
    }

    private void RetrieveSelectedCard()
    {
        if (GameManager.Instance == null || selectedVaultCard == null)
        {
            return;
        }

        bool success = GameManager.Instance.RetrieveSpecificVaultCard(selectedVaultCard);
        if (success)
        {
            statusText.text = $"Карта возвращена в колоду: {selectedVaultCard.CardName}";
            selectedVaultCard = null;
            RefreshView();
            return;
        }

        statusText.text = "Не удалось вернуть карту из хранилища.";
    }

    private void EnsureUi()
    {
        if (root != null && runCardsRoot != null && vaultCardsRoot != null && storeButton != null && retrieveButton != null)
        {
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("VaultCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        if (root == null)
        {
            root = new GameObject("StorageVaultRoot", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvas.transform, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1200f, 700f);

            Image bg = root.GetComponent<Image>();
            bg.color = new Color(0.03f, 0.03f, 0.03f, 0.94f);
        }

        CreateText(root.transform, "Title", "Хранилище", 42f, new Vector2(0f, 304f), new Vector2(1000f, 56f), TextAlignmentOptions.Center);
        CreateText(root.transform, "LeftLabel", "Колода забега", 28f, new Vector2(-300f, 254f), new Vector2(420f, 40f), TextAlignmentOptions.Center);
        CreateText(root.transform, "RightLabel", "Карты в хранилище", 28f, new Vector2(300f, 254f), new Vector2(420f, 40f), TextAlignmentOptions.Center);

        if (statusText == null)
        {
            statusText = CreateText(root.transform, "StatusText", string.Empty, 20f, new Vector2(0f, -228f), new Vector2(1080f, 36f), TextAlignmentOptions.Center);
        }

        if (runCardsRoot == null)
        {
            runCardsRoot = CreateListRoot(root.transform, "RunCardsRoot", new Vector2(-300f, 18f), new Vector2(520f, 420f));
        }

        if (vaultCardsRoot == null)
        {
            vaultCardsRoot = CreateListRoot(root.transform, "VaultCardsRoot", new Vector2(300f, 18f), new Vector2(520f, 420f));
        }

        if (storeButton == null)
        {
            storeButton = CreateButton(root.transform, "StoreButton", "Спрятать ->", new Vector2(-120f, -168f), new Vector2(220f, 48f), new Color(0.16f, 0.44f, 0.32f, 1f));
            storeButton.onClick.RemoveAllListeners();
            storeButton.onClick.AddListener(StoreSelectedCard);
        }

        if (retrieveButton == null)
        {
            retrieveButton = CreateButton(root.transform, "RetrieveButton", "<- Забрать", new Vector2(120f, -168f), new Vector2(220f, 48f), new Color(0.18f, 0.28f, 0.52f, 1f));
            retrieveButton.onClick.RemoveAllListeners();
            retrieveButton.onClick.AddListener(RetrieveSelectedCard);
        }

        if (closeButton == null)
        {
            closeButton = CreateButton(root.transform, "CloseButton", "Закрыть", new Vector2(0f, -290f), new Vector2(260f, 48f), new Color(0.35f, 0.16f, 0.16f, 1f));
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }
    }

    private static Transform CreateListRoot(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject listGO = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
        listGO.transform.SetParent(parent, false);

        RectTransform rect = listGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        VerticalLayoutGroup layout = listGO.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.padding = new RectOffset(4, 4, 4, 4);
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        return listGO.transform;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string value, float fontSize, Vector2 pos, Vector2 size, TextAlignmentOptions align)
    {
        GameObject textGO = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(parent, false);

        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textGO.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = align;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, string labelText, Vector2 pos, Vector2 size, Color color)
    {
        GameObject buttonGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(parent, false);

        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Image image = buttonGO.GetComponent<Image>();
        image.color = color;

        Button button = buttonGO.GetComponent<Button>();

        TextMeshProUGUI label = CreateText(buttonGO.transform, "Label", labelText, 22f, Vector2.zero, size, TextAlignmentOptions.Center);
        label.raycastTarget = false;

        return button;
    }
}
