using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CardWorkshopUI : MonoBehaviour
{
    private static CardWorkshopUI instance;

    [SerializeField] private GameObject root;
    [SerializeField] private Transform cardsRoot;
    [SerializeField] private TextMeshProUGUI selectedCardText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button costButton;
    [SerializeField] private Button damageButton;
    [SerializeField] private Button reforgeButton;
    [SerializeField] private Button closeButton;

    private CardData selectedCard;
    private Action onClosed;
    private bool upgradedThisVisit;

    public static CardWorkshopUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<CardWorkshopUI>();
            }

            if (instance == null)
            {
                GameObject go = new GameObject("CardWorkshopUI", typeof(CardWorkshopUI));
                instance = go.GetComponent<CardWorkshopUI>();
            }

            return instance;
        }
    }

    public void Open(Action closedCallback = null)
    {
        EnsureUi();
        onClosed = closedCallback;
        selectedCard = null;
        upgradedThisVisit = false;
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
        IReadOnlyList<CardData> runCards = GameManager.Instance != null
            ? GameManager.Instance.GetRunBonusCards()
            : null;

        RebuildCardList(runCards);

        if (selectedCardText != null)
        {
            selectedCardText.text = selectedCard == null
                ? "Карта не выбрана"
                : $"Выбрана: {selectedCard.CardName} | Цена {selectedCard.Cost}, Урон {selectedCard.Damage}, HP {selectedCard.MaxHealth}";
        }

        if (statusText != null && !upgradedThisVisit)
        {
            statusText.text = "Выберите карту и примените модификацию.";
        }

        bool canModify = selectedCard != null && !upgradedThisVisit;
        if (costButton != null) costButton.interactable = canModify;
        if (damageButton != null) damageButton.interactable = canModify;
        if (reforgeButton != null) reforgeButton.interactable = canModify;
    }

    private void RebuildCardList(IReadOnlyList<CardData> cards)
    {
        if (cardsRoot == null)
        {
            return;
        }

        for (int i = cardsRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(cardsRoot.GetChild(i).gameObject);
        }

        if (cards == null || cards.Count == 0)
        {
            CreateInfoRow("В бонусной колоде нет карт для модификации.");
            return;
        }

        for (int i = 0; i < cards.Count; i++)
        {
            CardData card = cards[i];
            if (card == null)
            {
                continue;
            }

            CreateCardRow(card);
        }
    }

    private void CreateCardRow(CardData cardData)
    {
        GameObject rowGO = new GameObject($"WorkshopCard_{cardData.CardName}", typeof(RectTransform), typeof(Image), typeof(Button));
        rowGO.transform.SetParent(cardsRoot, false);

        RectTransform rect = rowGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 46f);

        Image bg = rowGO.GetComponent<Image>();
        bool isSelected = selectedCard == cardData;
        bg.color = isSelected
            ? new Color(0.2f, 0.47f, 0.63f, 0.96f)
            : new Color(0.16f, 0.16f, 0.16f, 0.96f);

        Button button = rowGO.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            selectedCard = cardData;
            RefreshView();
        });

        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(rowGO.transform, false);

        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 0f);
        labelRect.offsetMax = new Vector2(-12f, 0f);

        TextMeshProUGUI label = labelGO.GetComponent<TextMeshProUGUI>();
        label.text = $"{cardData.CardName}  |  Цена {cardData.Cost}, Урон {cardData.Damage}, HP {cardData.MaxHealth}";
        label.fontSize = 20f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    private void ApplyModification(WorkshopModificationType modification)
    {
        if (selectedCard == null || upgradedThisVisit || GameManager.Instance == null)
        {
            return;
        }

        CardData modified = CardDataRuntimeFactory.CreateWorkshopVariant(selectedCard, modification);
        if (modified == null)
        {
            statusText.text = "Не удалось создать модификацию.";
            return;
        }

        bool replaced = GameManager.Instance.ReplaceRunBonusCard(selectedCard, modified);
        if (!replaced)
        {
            statusText.text = "Не удалось обновить карту в колоде.";
            return;
        }

        selectedCard = modified;
        upgradedThisVisit = true;
        statusText.text = $"Модификация применена: {modified.CardName}";
        RefreshView();
    }

    private void CreateInfoRow(string text)
    {
        GameObject rowGO = new GameObject("InfoRow", typeof(RectTransform), typeof(TextMeshProUGUI));
        rowGO.transform.SetParent(cardsRoot, false);

        RectTransform rect = rowGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 46f);

        TextMeshProUGUI label = rowGO.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 19f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
    }

    private void EnsureUi()
    {
        if (root != null && cardsRoot != null && costButton != null && damageButton != null && reforgeButton != null)
        {
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("WorkshopCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        if (root == null)
        {
            root = new GameObject("CardWorkshopRoot", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvas.transform, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1020f, 720f);

            Image bg = root.GetComponent<Image>();
            bg.color = new Color(0.03f, 0.03f, 0.03f, 0.94f);
        }

        CreateText(root.transform, "Title", "Мастерская карт", 42f, new Vector2(0f, 304f), new Vector2(930f, 56f), TextAlignmentOptions.Center);
        CreateText(root.transform, "Description", "Выберите карту и примените один из апгрейдов.", 24f, new Vector2(0f, 254f), new Vector2(930f, 52f), TextAlignmentOptions.Center);

        if (selectedCardText == null)
        {
            selectedCardText = CreateText(root.transform, "SelectedCardText", string.Empty, 22f, new Vector2(0f, 206f), new Vector2(930f, 38f), TextAlignmentOptions.Center);
        }

        if (statusText == null)
        {
            statusText = CreateText(root.transform, "StatusText", string.Empty, 20f, new Vector2(0f, 168f), new Vector2(930f, 36f), TextAlignmentOptions.Center);
        }

        if (cardsRoot == null)
        {
            GameObject listGO = new GameObject("CardsRoot", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listGO.transform.SetParent(root.transform, false);

            RectTransform listRect = listGO.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0.5f, 0.5f);
            listRect.anchorMax = new Vector2(0.5f, 0.5f);
            listRect.pivot = new Vector2(0.5f, 0.5f);
            listRect.anchoredPosition = new Vector2(0f, -20f);
            listRect.sizeDelta = new Vector2(930f, 352f);

            VerticalLayoutGroup layout = listGO.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            cardsRoot = listGO.transform;
        }

        if (costButton == null)
        {
            costButton = CreateButton(root.transform, "CostButton", "Удешевить (+1 урон)", new Vector2(-296f, -292f), new Vector2(300f, 50f), new Color(0.16f, 0.44f, 0.32f, 1f));
            costButton.onClick.RemoveAllListeners();
            costButton.onClick.AddListener(() => ApplyModification(WorkshopModificationType.CostReduction));
        }

        if (damageButton == null)
        {
            damageButton = CreateButton(root.transform, "DamageButton", "Усилить урон", new Vector2(0f, -292f), new Vector2(300f, 50f), new Color(0.45f, 0.22f, 0.16f, 1f));
            damageButton.onClick.RemoveAllListeners();
            damageButton.onClick.AddListener(() => ApplyModification(WorkshopModificationType.DamageBoost));
        }

        if (reforgeButton == null)
        {
            reforgeButton = CreateButton(root.transform, "ReforgeButton", "Перековать стихию", new Vector2(296f, -292f), new Vector2(300f, 50f), new Color(0.22f, 0.28f, 0.52f, 1f));
            reforgeButton.onClick.RemoveAllListeners();
            reforgeButton.onClick.AddListener(() => ApplyModification(WorkshopModificationType.ElementReforge));
        }

        if (closeButton == null)
        {
            closeButton = CreateButton(root.transform, "CloseButton", "Закрыть", new Vector2(0f, -350f), new Vector2(220f, 42f), new Color(0.35f, 0.16f, 0.16f, 1f));
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string textValue, float fontSize, Vector2 pos, Vector2 size, TextAlignmentOptions align)
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
        text.text = textValue;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = align;
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
