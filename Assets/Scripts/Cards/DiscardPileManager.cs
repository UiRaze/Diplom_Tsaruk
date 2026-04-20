using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DiscardPileManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button discardButton;
    [SerializeField] private TextMeshProUGUI discardCounterText;
    [SerializeField] private GameObject discardWindow;
    [SerializeField] private Transform discardContent;
    [SerializeField] private Button closeButton;

    [Header("Preview")]
    [SerializeField] private Vector2 previewCardSize = new Vector2(100f, 140f);

    private readonly List<CardData> discardedCards = new List<CardData>();

    public int Count => discardedCards.Count;

    private void Start()
    {
        ResolveReferences();
        EnsureUiExists();

        if (discardButton != null)
        {
            discardButton.onClick.RemoveListener(ToggleWindow);
            discardButton.onClick.AddListener(ToggleWindow);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseWindow);
            closeButton.onClick.AddListener(CloseWindow);
        }

        if (discardWindow != null)
        {
            discardWindow.SetActive(false);
        }

        RefreshCounter();
    }

    public void ClearForNewBattle()
    {
        discardedCards.Clear();
        RefreshCounter();
        RebuildWindow();
    }

    public void AddCard(Card card)
    {
        if (card == null)
        {
            return;
        }

        AddCardData(card.CardData);
        Destroy(card.gameObject);
    }

    public void AddCardData(CardData cardData)
    {
        if (cardData == null)
        {
            return;
        }

        discardedCards.Add(cardData);
        RefreshCounter();

        if (discardWindow != null && discardWindow.activeSelf)
        {
            RebuildWindow();
        }
    }

    public void ToggleWindow()
    {
        if (discardWindow == null)
        {
            return;
        }

        bool nextState = !discardWindow.activeSelf;
        discardWindow.SetActive(nextState);

        if (nextState)
        {
            RebuildWindow();
        }
    }

    public void CloseWindow()
    {
        if (discardWindow != null)
        {
            discardWindow.SetActive(false);
        }
    }

    private void RefreshCounter()
    {
        if (discardCounterText != null)
        {
            discardCounterText.text = Count.ToString();
        }
    }

    private void RebuildWindow()
    {
        if (discardContent == null)
        {
            return;
        }

        for (int i = discardContent.childCount - 1; i >= 0; i--)
        {
            Destroy(discardContent.GetChild(i).gameObject);
        }

        for (int i = 0; i < discardedCards.Count; i++)
        {
            CreatePreview(discardedCards[i], i);
        }
    }

    private void CreatePreview(CardData cardData, int index)
    {
        if (cardData == null || discardContent == null)
        {
            return;
        }

        GameObject previewGO = new GameObject($"DiscardPreview_{index}", typeof(RectTransform), typeof(Image));
        previewGO.transform.SetParent(discardContent, false);

        RectTransform previewRect = previewGO.GetComponent<RectTransform>();
        previewRect.sizeDelta = previewCardSize;

        Image previewImage = previewGO.GetComponent<Image>();
        previewImage.sprite = cardData.CardArt;
        previewImage.color = Color.white;
        previewImage.raycastTarget = false;

        GameObject labelGO = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(previewGO.transform, false);

        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.anchoredPosition = new Vector2(0f, -18f);
        labelRect.sizeDelta = new Vector2(0f, 28f);

        TextMeshProUGUI label = labelGO.GetComponent<TextMeshProUGUI>();
        label.text = cardData.CardName;
        label.fontSize = 14f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;

        CanvasGroup canvasGroup = previewGO.AddComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void ResolveReferences()
    {
        if (discardButton == null)
        {
            discardButton = GameObject.Find("DiscardButton")?.GetComponent<Button>();
        }

        if (discardCounterText == null)
        {
            discardCounterText = GameObject.Find("DiscardCounterText")?.GetComponent<TextMeshProUGUI>();
        }

        if (discardWindow == null)
        {
            GameObject window = GameObject.Find("DiscardWindow");
            if (window != null)
            {
                discardWindow = window;
            }
        }

        if (discardContent == null)
        {
            discardContent = GameObject.Find("DiscardContent")?.transform;
        }

        if (closeButton == null)
        {
            closeButton = GameObject.Find("DiscardCloseButton")?.GetComponent<Button>();
        }
    }

    private void EnsureUiExists()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        if (discardButton == null)
        {
            discardButton = CreateDiscardButton(canvas.transform);
        }

        if (discardCounterText == null && discardButton != null)
        {
            discardCounterText = CreateCounterText(discardButton.transform);
        }

        if (discardWindow == null)
        {
            discardWindow = CreateDiscardWindow(canvas.transform, out discardContent, out closeButton);
        }
        else
        {
            if (discardContent == null)
            {
                discardContent = discardWindow.transform.Find("Content");
            }

            if (closeButton == null)
            {
                closeButton = discardWindow.transform.Find("CloseButton")?.GetComponent<Button>();
            }
        }
    }

    private Button CreateDiscardButton(Transform parent)
    {
        GameObject buttonGO = new GameObject("DiscardButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(parent, false);

        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-24f, 20f);
        rect.sizeDelta = new Vector2(140f, 48f);

        Image image = buttonGO.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.75f);

        Button button = buttonGO.GetComponent<Button>();

        GameObject textGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(buttonGO.transform, false);

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textGO.GetComponent<TextMeshProUGUI>();
        text.text = "Сброс";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 24f;
        text.color = Color.white;
        text.raycastTarget = false;

        return button;
    }

    private TextMeshProUGUI CreateCounterText(Transform parent)
    {
        GameObject counterGO = new GameObject("DiscardCounterText", typeof(RectTransform), typeof(TextMeshProUGUI));
        counterGO.transform.SetParent(parent, false);

        RectTransform rect = counterGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-8f, -4f);
        rect.sizeDelta = new Vector2(30f, 24f);

        TextMeshProUGUI text = counterGO.GetComponent<TextMeshProUGUI>();
        text.text = "0";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 18f;
        text.color = Color.yellow;
        text.raycastTarget = false;

        return text;
    }

    private GameObject CreateDiscardWindow(Transform parent, out Transform content, out Button generatedCloseButton)
    {
        GameObject windowGO = new GameObject("DiscardWindow", typeof(RectTransform), typeof(Image));
        windowGO.transform.SetParent(parent, false);

        RectTransform windowRect = windowGO.GetComponent<RectTransform>();
        windowRect.anchorMin = new Vector2(0.5f, 0.5f);
        windowRect.anchorMax = new Vector2(0.5f, 0.5f);
        windowRect.pivot = new Vector2(0.5f, 0.5f);
        windowRect.anchoredPosition = Vector2.zero;
        windowRect.sizeDelta = new Vector2(760f, 520f);

        Image windowImage = windowGO.GetComponent<Image>();
        windowImage.color = new Color(0f, 0f, 0f, 0.9f);

        GameObject closeButtonGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeButtonGO.transform.SetParent(windowGO.transform, false);

        RectTransform closeRect = closeButtonGO.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-12f, -12f);
        closeRect.sizeDelta = new Vector2(80f, 36f);

        Image closeImage = closeButtonGO.GetComponent<Image>();
        closeImage.color = new Color(0.35f, 0.1f, 0.1f, 0.95f);

        generatedCloseButton = closeButtonGO.GetComponent<Button>();

        GameObject closeTextGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        closeTextGO.transform.SetParent(closeButtonGO.transform, false);

        RectTransform closeTextRect = closeTextGO.GetComponent<RectTransform>();
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.offsetMin = Vector2.zero;
        closeTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI closeText = closeTextGO.GetComponent<TextMeshProUGUI>();
        closeText.text = "Закрыть";
        closeText.alignment = TextAlignmentOptions.Center;
        closeText.fontSize = 18f;
        closeText.color = Color.white;
        closeText.raycastTarget = false;

        GameObject contentGO = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup));
        contentGO.transform.SetParent(windowGO.transform, false);

        RectTransform contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 0f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.offsetMin = new Vector2(24f, 24f);
        contentRect.offsetMax = new Vector2(-24f, -64f);

        GridLayoutGroup grid = contentGO.GetComponent<GridLayoutGroup>();
        grid.cellSize = previewCardSize;
        grid.spacing = new Vector2(10f, 22f);
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 6;

        content = contentGO.transform;
        return windowGO;
    }
}
