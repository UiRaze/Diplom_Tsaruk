using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private Transform itemsRoot;
    [SerializeField] private Button closeButton;
    [SerializeField] private List<ShopItem> baseItems = new List<ShopItem>();

    private readonly List<ShopItem> activeItems = new List<ShopItem>();
    private Action onClosed;

    private void Start()
    {
        ResolveReferences();
        EnsureUi();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseShop);
            closeButton.onClick.AddListener(CloseShop);
        }

        CloseShop();
    }

    public void OpenShop(Action closedCallback = null)
    {
        ResolveReferences();
        EnsureUi();

        onClosed = closedCallback;
        BuildActiveItems();
        RebuildItemsUi();
        UpdateCoinsLabel();

        if (root != null)
        {
            root.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void CloseShop()
    {
        if (root != null)
        {
            root.SetActive(false);
        }

        Time.timeScale = 1f;

        onClosed?.Invoke();
        onClosed = null;
    }

    private void BuildActiveItems()
    {
        activeItems.Clear();

        if (baseItems != null)
        {
            for (int i = 0; i < baseItems.Count; i++)
            {
                ShopItem item = baseItems[i];
                if (item != null && item.cardData != null)
                {
                    activeItems.Add(new ShopItem { cardData = item.cardData, price = Mathf.Max(1, item.price) });
                }
            }
        }

        if (activeItems.Count == 0)
        {
            List<CardData> fallbackCards = DefaultDeckProvider.CreateMvpDeck();
            for (int i = 0; i < fallbackCards.Count && activeItems.Count < 5; i++)
            {
                CardData card = fallbackCards[i];
                if (card == null)
                {
                    continue;
                }

                activeItems.Add(new ShopItem
                {
                    cardData = card,
                    price = 15 + card.Cost * 5
                });
            }
        }
    }

    private void RebuildItemsUi()
    {
        if (itemsRoot == null)
        {
            return;
        }

        for (int i = itemsRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(itemsRoot.GetChild(i).gameObject);
        }

        for (int i = 0; i < activeItems.Count; i++)
        {
            CreateItemRow(activeItems[i]);
        }
    }

    private void CreateItemRow(ShopItem item)
    {
        if (item == null || item.cardData == null || itemsRoot == null)
        {
            return;
        }

        int finalPrice = GameManager.Instance != null
            ? GameManager.Instance.GetModifiedShopPrice(item.price)
            : item.price;

        GameObject rowGO = new GameObject($"ShopItem_{item.cardData.CardName}", typeof(RectTransform), typeof(Image));
        rowGO.transform.SetParent(itemsRoot, false);

        RectTransform rowRect = rowGO.GetComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(0f, 62f);

        Image rowImage = rowGO.GetComponent<Image>();
        rowImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(rowGO.transform, false);

        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(12f, 8f);
        labelRect.offsetMax = new Vector2(-180f, -8f);

        TextMeshProUGUI label = labelGO.GetComponent<TextMeshProUGUI>();
        if (finalPrice != item.price)
        {
            label.text = $"{item.cardData.CardName}  |  Цена: {finalPrice} (было {item.price})";
        }
        else
        {
            label.text = $"{item.cardData.CardName}  |  Цена: {item.price}";
        }

        label.fontSize = 22f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = Color.white;

        GameObject buyButtonGO = new GameObject("BuyButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buyButtonGO.transform.SetParent(rowGO.transform, false);

        RectTransform buyRect = buyButtonGO.GetComponent<RectTransform>();
        buyRect.anchorMin = new Vector2(1f, 0.5f);
        buyRect.anchorMax = new Vector2(1f, 0.5f);
        buyRect.pivot = new Vector2(1f, 0.5f);
        buyRect.anchoredPosition = new Vector2(-10f, 0f);
        buyRect.sizeDelta = new Vector2(150f, 44f);

        Image buyImage = buyButtonGO.GetComponent<Image>();
        buyImage.color = new Color(0.2f, 0.45f, 0.2f, 1f);

        Button buyButton = buyButtonGO.GetComponent<Button>();
        buyButton.onClick.AddListener(() => TryBuyItem(item, finalPrice, rowGO));

        GameObject buyLabelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        buyLabelGO.transform.SetParent(buyButtonGO.transform, false);

        RectTransform buyLabelRect = buyLabelGO.GetComponent<RectTransform>();
        buyLabelRect.anchorMin = Vector2.zero;
        buyLabelRect.anchorMax = Vector2.one;
        buyLabelRect.offsetMin = Vector2.zero;
        buyLabelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI buyLabel = buyLabelGO.GetComponent<TextMeshProUGUI>();
        buyLabel.text = "Купить";
        buyLabel.fontSize = 22f;
        buyLabel.alignment = TextAlignmentOptions.Center;
        buyLabel.color = Color.white;
        buyLabel.raycastTarget = false;
    }

    private void TryBuyItem(ShopItem item, int finalPrice, GameObject rowGO)
    {
        if (item == null || item.cardData == null || GameManager.Instance == null)
        {
            return;
        }

        if (!GameManager.Instance.SpendGold(finalPrice))
        {
            return;
        }

        GameManager.Instance.AddCardToRunDeck(item.cardData);
        activeItems.Remove(item);

        if (rowGO != null)
        {
            Destroy(rowGO);
        }

        UpdateCoinsLabel();
    }

    private void UpdateCoinsLabel()
    {
        if (coinsText == null)
        {
            return;
        }

        int coins = GameManager.Instance != null ? GameManager.Instance.HeroCoins : 0;
        coinsText.text = $"Монеты: {coins}";
    }

    private void ResolveReferences()
    {
        if (root == null)
        {
            root = GameObject.Find("ShopPopupRoot");
        }

        if (coinsText == null)
        {
            coinsText = GameObject.Find("ShopCoinsText")?.GetComponent<TextMeshProUGUI>();
        }

        if (itemsRoot == null)
        {
            itemsRoot = GameObject.Find("ShopItemsRoot")?.transform;
        }

        if (closeButton == null)
        {
            closeButton = GameObject.Find("ShopCloseButton")?.GetComponent<Button>();
        }
    }

    private void EnsureUi()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        if (root == null)
        {
            root = CreateRoot(canvas.transform);
        }

        if (coinsText == null)
        {
            coinsText = CreateText(root.transform, "ShopCoinsText", new Vector2(-220f, 130f), new Vector2(340f, 42f), 30f, TextAlignmentOptions.Left, "Монеты: 0");
        }

        if (itemsRoot == null)
        {
            itemsRoot = CreateItemsRoot(root.transform);
        }

        if (closeButton == null)
        {
            closeButton = CreateCloseButton(root.transform);
        }
    }

    private GameObject CreateRoot(Transform parent)
    {
        GameObject rootGO = new GameObject("ShopPopupRoot", typeof(RectTransform), typeof(Image));
        rootGO.transform.SetParent(parent, false);

        RectTransform rect = rootGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(860f, 620f);

        Image image = rootGO.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.92f);

        return rootGO;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align, string value)
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

        return text;
    }

    private Transform CreateItemsRoot(Transform parent)
    {
        GameObject rootGO = new GameObject("ShopItemsRoot", typeof(RectTransform), typeof(VerticalLayoutGroup));
        rootGO.transform.SetParent(parent, false);

        RectTransform rect = rootGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -30f);
        rect.sizeDelta = new Vector2(760f, 420f);

        VerticalLayoutGroup layout = rootGO.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return rootGO.transform;
    }

    private Button CreateCloseButton(Transform parent)
    {
        GameObject buttonGO = new GameObject("ShopCloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(parent, false);

        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-14f, -14f);
        rect.sizeDelta = new Vector2(160f, 48f);

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
        label.text = "Закрыть";
        label.fontSize = 24f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;

        return button;
    }
}
