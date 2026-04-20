using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class AlchemyPotUI : MonoBehaviour
{
    private static AlchemyPotUI instance;

    [SerializeField] private GameObject root;
    [SerializeField] private Transform cardsRoot;
    [SerializeField] private TextMeshProUGUI selectedText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button craftButton;
    [SerializeField] private Button closeButton;

    private readonly List<CardData> selectedCards = new List<CardData>(2);
    private Action onClosed;
    private bool craftedThisVisit;

    public static AlchemyPotUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AlchemyPotUI>();
            }

            if (instance == null)
            {
                GameObject go = new GameObject("AlchemyPotUI", typeof(AlchemyPotUI));
                instance = go.GetComponent<AlchemyPotUI>();
            }

            return instance;
        }
    }

    public void Open(Action closedCallback = null)
    {
        EnsureUi();
        onClosed = closedCallback;
        craftedThisVisit = false;
        selectedCards.Clear();
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

        if (selectedText != null)
        {
            if (selectedCards.Count == 0)
            {
                selectedText.text = "Выбрано для жертвы: ничего";
            }
            else if (selectedCards.Count == 1)
            {
                selectedText.text = $"Выбрано для жертвы: {selectedCards[0].CardName}";
            }
            else
            {
                selectedText.text = $"Выбрано для жертвы: {selectedCards[0].CardName} + {selectedCards[1].CardName}";
            }
        }

        if (statusText != null && !craftedThisVisit)
        {
            statusText.text = "Выберите две карты и нажмите «Синтезировать».";
        }

        if (craftButton != null)
        {
            craftButton.interactable = !craftedThisVisit && selectedCards.Count == 2;
        }
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
            CreateInfoRow("В бонусной колоде нет карт для синтеза.");
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
        if (cardsRoot == null || cardData == null)
        {
            return;
        }

        GameObject rowGO = new GameObject($"Card_{cardData.CardName}", typeof(RectTransform), typeof(Image), typeof(Button));
        rowGO.transform.SetParent(cardsRoot, false);

        RectTransform rect = rowGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 46f);

        Image bg = rowGO.GetComponent<Image>();
        bg.color = selectedCards.Contains(cardData)
            ? new Color(0.24f, 0.48f, 0.25f, 0.95f)
            : new Color(0.16f, 0.16f, 0.16f, 0.95f);

        Button button = rowGO.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            ToggleSelection(cardData);
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
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
    }

    private void ToggleSelection(CardData cardData)
    {
        if (cardData == null || craftedThisVisit)
        {
            return;
        }

        if (selectedCards.Contains(cardData))
        {
            selectedCards.Remove(cardData);
            return;
        }

        if (selectedCards.Count >= 2)
        {
            if (statusText != null)
            {
                statusText.text = "Можно выбрать только две карты.";
            }

            return;
        }

        selectedCards.Add(cardData);
    }

    private void Craft()
    {
        if (craftedThisVisit || selectedCards.Count != 2 || GameManager.Instance == null)
        {
            return;
        }

        CardData first = selectedCards[0];
        CardData second = selectedCards[1];

        if (!GameManager.Instance.RemoveSpecificRunBonusCard(first, rememberRemoval: false))
        {
            statusText.text = "Не удалось изъять первую карту.";
            return;
        }

        if (!GameManager.Instance.RemoveSpecificRunBonusCard(second, rememberRemoval: false))
        {
            GameManager.Instance.AddCardToRunDeck(first);
            statusText.text = "Не удалось изъять вторую карту.";
            return;
        }

        CardData crafted = CardDataRuntimeFactory.CreateCraftedCard(first, second);
        if (crafted == null)
        {
            GameManager.Instance.AddCardToRunDeck(first);
            GameManager.Instance.AddCardToRunDeck(second);
            statusText.text = "Синтез сорвался.";
            return;
        }

        GameManager.Instance.AddCardToRunDeck(crafted);
        craftedThisVisit = true;
        selectedCards.Clear();

        if (statusText != null)
        {
            statusText.text = $"Синтез завершён: {crafted.CardName}";
        }

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
        if (root != null && cardsRoot != null && selectedText != null && craftButton != null && closeButton != null)
        {
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("AlchemyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        if (root == null)
        {
            root = new GameObject("AlchemyPotRoot", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvas.transform, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(980f, 700f);

            Image bg = root.GetComponent<Image>();
            bg.color = new Color(0.03f, 0.03f, 0.03f, 0.94f);
        }

        CreateText(root.transform, "Title", "Алхимический котёл", 42f, new Vector2(0f, 300f), new Vector2(900f, 56f), TextAlignmentOptions.Center);
        CreateText(root.transform, "Description", "Пожертвуйте 2 карты из бонусной колоды и получите новую усиленную карту.", 24f, new Vector2(0f, 248f), new Vector2(900f, 60f), TextAlignmentOptions.Center);

        if (selectedText == null)
        {
            selectedText = CreateText(root.transform, "SelectedText", string.Empty, 22f, new Vector2(0f, 198f), new Vector2(900f, 40f), TextAlignmentOptions.Center);
        }

        if (statusText == null)
        {
            statusText = CreateText(root.transform, "StatusText", string.Empty, 20f, new Vector2(0f, 154f), new Vector2(900f, 40f), TextAlignmentOptions.Center);
        }

        if (cardsRoot == null)
        {
            GameObject listGO = new GameObject("CardsRoot", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listGO.transform.SetParent(root.transform, false);

            RectTransform listRect = listGO.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0.5f, 0.5f);
            listRect.anchorMax = new Vector2(0.5f, 0.5f);
            listRect.pivot = new Vector2(0.5f, 0.5f);
            listRect.anchoredPosition = new Vector2(0f, -22f);
            listRect.sizeDelta = new Vector2(900f, 360f);

            VerticalLayoutGroup layout = listGO.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            cardsRoot = listGO.transform;
        }

        if (craftButton == null)
        {
            craftButton = CreateButton(root.transform, "CraftButton", "Синтезировать", new Vector2(-160f, -298f), new Vector2(260f, 50f), new Color(0.14f, 0.47f, 0.24f, 1f));
            craftButton.onClick.RemoveAllListeners();
            craftButton.onClick.AddListener(Craft);
        }

        if (closeButton == null)
        {
            closeButton = CreateButton(root.transform, "CloseButton", "Закрыть", new Vector2(160f, -298f), new Vector2(260f, 50f), new Color(0.35f, 0.16f, 0.16f, 1f));
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

        TextMeshProUGUI label = CreateText(buttonGO.transform, "Label", labelText, 24f, Vector2.zero, size, TextAlignmentOptions.Center);
        label.raycastTarget = false;

        return button;
    }
}
