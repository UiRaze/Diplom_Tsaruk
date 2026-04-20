using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
public class DeckBuilder : MonoBehaviour
{
    [SerializeField] private Transform availableCardsPanel;
    [SerializeField] private Transform currentDeckPanel;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Button doneButton;
    [SerializeField] private TextMeshProUGUI deckCountText;

    private List<CardData> allCards = new List<CardData>();
    private List<CardData> currentDeck = new List<CardData>();

    private const int MIN_CARDS = GameConfig.MIN_DECK_SIZE;
    private const int MAX_CARDS = GameConfig.MAX_DECK_SIZE;

    private void Start()
    {
        // Проверяем обязательные ссылки
        if (cardPrefab == null)
        {
            Debug.LogError("❌ Card Prefab НЕ НАЗНАЧЕН в Инспекторе!", this);
            return;
        }

        if (availableCardsPanel == null)
        {
            Debug.LogError("❌ availableCardsPanel НЕ НАЗНАЧЕН в Инспекторе!", this);
            return;
        }

        if (currentDeckPanel == null)
        {
            Debug.LogError("❌ currentDeckPanel НЕ НАЗНАЧЕН в Инспекторе!", this);
            return;
        }

        if (doneButton == null)
        {
            Debug.LogError("❌ Done Button НЕ НАЗНАЧЕН в Инспекторе!", this);
            return;
        }

        if (deckCountText == null)
        {
            Debug.LogError("❌ Deck Count Text НЕ НАЗНАЧЕН в Инспекторе!", this);
            return;
        }

        LoadAllCards();
        PopulateAvailableCards();
        LoadSavedDeck();
        UpdateDeckCount();
        // ❌ УДАЛЯЕМ ЭТУ СТРОКУ: SetupGridLayouts(); 
    }
    private void SetupGridLayouts()
    {
        // Проверяем, назначены ли панели
        if (availableCardsPanel == null)
            Debug.LogError("❌ availableCardsPanel НЕ НАЗНАЧЕН в Инспекторе!", this);
        else
            SetupPanelLayout(availableCardsPanel);

        if (currentDeckPanel == null)
            Debug.LogError("❌ currentDeckPanel НЕ НАЗНАЧЕН в Инспекторе!", this);
        else
            SetupPanelLayout(currentDeckPanel);
    }

    private void SetupPanelLayout(Transform panel)
    {
        if (panel == null || panel.gameObject == null)
        {
            Debug.LogError("❌ Передана пустая панель в SetupPanelLayout!", this);
            return;
        }

        // Удаляем старые компоненты
        DestroyExistingLayoutComponents(panel.gameObject);

        // Определяем, какой тип лейаута использовать
        if (panel == availableCardsPanel)
        {
            // Для AvailableCards — горизонтальный лейаут
            var layout = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 15f;
            layout.padding.left = 10;
            layout.padding.right = 10;
            layout.padding.top = 10;
            layout.padding.bottom = 10;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            // Добавляем подстройку высоты
            var sizeFitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        else if (panel == currentDeckPanel)
        {
            // Для CurrentDeck — вертикальный лейаут
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.spacing = 15f;
            layout.padding.left = 10;
            layout.padding.right = 10;
            layout.padding.top = 10;
            layout.padding.bottom = 10;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // Добавляем подстройку высоты
            var sizeFitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    // Вспомогательный метод: чистим старые компоненты
    private void DestroyExistingLayoutComponents(GameObject panelObject)
    {
        var existingLayout = panelObject.GetComponent<GridLayoutGroup>();
        if (existingLayout != null) Destroy(existingLayout);

        var existingFitter = panelObject.GetComponent<ContentSizeFitter>();
        if (existingFitter != null) Destroy(existingFitter);
    }

    private void LoadAllCards()
    {
        // Загружаем все CardData из папки Resources/ScriptableObjects/Cards/
        CardData[] loadedCards = Resources.LoadAll<CardData>("ScriptableObjects/Cards");

        if (loadedCards != null && loadedCards.Length > 0)
        {
            allCards.AddRange(loadedCards);
        }
        else
        {
            Debug.LogWarning("Не найдены CardData в папке Resources/ScriptableObjects/Cards/");
        }
    }
    private void PopulateAvailableCards()
    {
        foreach (var cardData in allCards)
        {
            CreateCard(cardData, availableCardsPanel);
        }
    }

    private void CreateCard(CardData cardData, Transform parent)
    {
        var cardGO = Instantiate(cardPrefab, parent);
        var card = cardGO.GetComponent<Card>();
        card.SetCardData(cardData);

        // ✅ УСТАНАВЛИВАЕМ РАЗМЕР КАРТЫ ЧЕРЕЗ RECTTRANSFORM
        var rectTransform = cardGO.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(180f, 250f); // Размер карты
            rectTransform.localScale = Vector3.one; // Масштаб
        }

        // Добавляем EventTrigger
        var eventTrigger = cardGO.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = cardGO.AddComponent<EventTrigger>();
        }

        // Создаём событие клика
        var clickEvent = new EventTrigger.Entry();
        clickEvent.eventID = EventTriggerType.PointerClick;
        clickEvent.callback.AddListener((data) => OnCardClicked(cardData, cardGO, parent));
        eventTrigger.triggers.Add(clickEvent);
    }

    private void OnCardClicked(CardData cardData, GameObject cardGO, Transform parent)
    {
        if (parent == availableCardsPanel && currentDeck.Count >= MAX_CARDS)
        {
            Debug.LogWarning("Достигнут лимит карт!");
            return;
        }

        // Мгновенное перемещение
        var targetPanel = (parent == availableCardsPanel)
            ? currentDeckPanel
            : availableCardsPanel;

        // Переносим сам объект вместо создания копии
        cardGO.transform.SetParent(targetPanel, false);

        // ✅ УСТАНАВЛИВАЕМ РАЗМЕР ПРИ ПЕРЕМЕЩЕНИИ
        var rectTransform = cardGO.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(180f, 250f);
            rectTransform.localScale = Vector3.one;
        }

        // Обновляем данные колоды
        if (parent == availableCardsPanel)
        {
            currentDeck.Add(cardData);
        }
        else
        {
            currentDeck.Remove(cardData);
        }

        // Обновляем обработчики
        var eventTrigger = cardGO.GetComponent<EventTrigger>();
        if (eventTrigger != null)
        {
            eventTrigger.triggers.Clear();
            var clickEvent = new EventTrigger.Entry();
            clickEvent.eventID = EventTriggerType.PointerClick;
            clickEvent.callback.AddListener((data) => OnCardClicked(cardData, cardGO, targetPanel));
            eventTrigger.triggers.Add(clickEvent);
        }

        UpdateDeckCount();
    }

    private void RemoveFromCurrentDeck(CardData cardData, GameObject cardGO)
    {
        // Удаляем из списка текущей колоды
        currentDeck.Remove(cardData);

        // Удаляем объект из правой панели
        Destroy(cardGO);

        // Создаём новую копию в левой панели (AvailableCards)
        var newCardGO = Instantiate(cardPrefab, availableCardsPanel);
        var newCard = newCardGO.GetComponent<Card>();
        newCard.SetCardData(cardData);

        // ✅ УСТАНАВЛИВАЕМ РАЗМЕР
        var rectTransform = newCardGO.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(180f, 250f);
            rectTransform.localScale = Vector3.one;
        }

        // Назначаем обработчик клика
        var eventTrigger = newCardGO.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = newCardGO.AddComponent<EventTrigger>();
        }

        var clickEvent = new EventTrigger.Entry();
        clickEvent.eventID = EventTriggerType.PointerClick;
        clickEvent.callback.AddListener((data) => OnCardClicked(cardData, newCardGO, availableCardsPanel));
        eventTrigger.triggers.Add(clickEvent);

        UpdateDeckCount();
    }

    private void UpdateDeckCount()
    {
        deckCountText.text = $"Карт в колоде: {currentDeck.Count} / {MAX_CARDS}";
        doneButton.interactable = currentDeck.Count >= MIN_CARDS;
    }

    public void SaveAndStartRun()
    {
        SaveDeck();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartRunFromDeckBuilder();
        }
    }

    private void SaveDeck()
    {
        DeckPersistence.Save(currentDeck);
    }

    private void LoadSavedDeck()
    {
        currentDeck = DeckPersistence.Load();
        if (currentDeck.Count > 0)
        {
            PopulateCurrentDeck();
        }
    }

    private void PopulateCurrentDeck()
    {
        foreach (var cardData in currentDeck)
        {
            var cardGO = Instantiate(cardPrefab, currentDeckPanel);
            var card = cardGO.GetComponent<Card>();
            card.SetCardData(cardData);

            // ✅ УСТАНАВЛИВАЕМ РАЗМЕР
            var rectTransform = cardGO.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(180f, 250f);
                rectTransform.localScale = Vector3.one;
            }

            // Назначаем обработчик клика для удаления из колоды
            var eventTrigger = cardGO.GetComponent<EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = cardGO.AddComponent<EventTrigger>();
            }

            var clickEvent = new EventTrigger.Entry();
            clickEvent.eventID = EventTriggerType.PointerClick;
            clickEvent.callback.AddListener((data) => RemoveFromCurrentDeck(cardData, cardGO));
            eventTrigger.triggers.Add(clickEvent);
        }

        UpdateDeckCount();
    }
}
