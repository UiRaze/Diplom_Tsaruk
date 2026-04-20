using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    private class EnemyHandEntry
    {
        public CardData Data;
        public GameObject BackVisual;
        public int CurrentHealth;
    }

    [Header("Enemy Hand")]
    [SerializeField] private Transform enemyHandPanel;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private GameObject cardBackPrefab;
    [SerializeField] private int handSize = 4;

    [Header("Enemy Deck")]
    [SerializeField] private List<CardData> initialDeck = new List<CardData>();

    [Header("Settings")]
    [SerializeField] private float turnDelay = 1f;
    [SerializeField] private float cardBackHideDuration = 0.3f;

    private readonly List<CardData> deck = new List<CardData>();
    private readonly List<EnemyHandEntry> handEntries = new List<EnemyHandEntry>();

    private bool isEnemyTurn;
    private bool hasPassed;

    private TurnSystem turnSystem;
    private RoundManager roundManager;

    public bool HasPassed => hasPassed;
    public Transform EnemyHandPanel => enemyHandPanel;
    public int HandCount => handEntries.Count;

    private void Start()
    {
        turnSystem = FindObjectOfType<TurnSystem>();
        roundManager = FindObjectOfType<RoundManager>();

        if (enemyHandPanel == null)
        {
            enemyHandPanel = GameObject.Find("EnemyHandPanel")?.transform;
        }

        if (cardPrefab == null)
        {
            cardPrefab = Resources.Load<GameObject>("Prefabs/Cards/Card");
        }

        if (cardBackPrefab == null)
        {
            cardBackPrefab = Resources.Load<GameObject>("Prefabs/Cards/EnemyCardBack");
        }

        InitializeDeck();
        DrawToHandSize(handSize);
    }

    public void NotifyPlayerPassed()
    {
        hasPassed = false;
    }

    public void StartEnemyTurn()
    {
        if (isEnemyTurn)
        {
            return;
        }

        if (roundManager == null || roundManager.IsRoundEnding)
        {
            return;
        }

        if (hasPassed)
        {
            turnSystem?.EndEnemyTurn();
            return;
        }

        isEnemyTurn = true;
        StartCoroutine(EnemyTurnCoroutine());
    }

    public void DrawToHandSize(int targetSize)
    {
        handEntries.RemoveAll(entry => entry == null || entry.Data == null);

        while (handEntries.Count < targetSize)
        {
            if (!DrawCard())
            {
                break;
            }
        }
    }

    public void ResetForNextRound()
    {
        hasPassed = false;
        isEnemyTurn = false;
    }

    public void RegisterReturnedCard(Card card)
    {
        if (card == null || card.CardData == null)
        {
            return;
        }

        EnemyHandEntry entry = new EnemyHandEntry
        {
            Data = card.CardData,
            BackVisual = CreateCardBackVisual(),
            CurrentHealth = card.CurrentHealth
        };

        handEntries.Add(entry);
        Destroy(card.gameObject);
    }

    public void ForcePass()
    {
        PassRoundAndEndTurn();
    }

    private IEnumerator EnemyTurnCoroutine()
    {
        yield return new WaitForSeconds(turnDelay);

        if (roundManager == null || roundManager.IsRoundEnding)
        {
            isEnemyTurn = false;
            yield break;
        }

        CardSlot targetSlot = SelectFreeSlot();
        EnemyHandEntry entryToPlay = SelectCardForCurrentRound();

        if (targetSlot == null || entryToPlay == null)
        {
            PassRoundAndEndTurn();
            yield break;
        }

        handEntries.Remove(entryToPlay);

        Card revealedCard = CreateCardFromEntry(entryToPlay);
        if (revealedCard == null || !targetSlot.PlaceCard(revealedCard))
        {
            if (revealedCard != null)
            {
                Destroy(revealedCard.gameObject);
            }

            PassRoundAndEndTurn();
            yield break;
        }

        if (entryToPlay.BackVisual != null)
        {
            StartCoroutine(HideCardBack(entryToPlay.BackVisual));
        }

        yield return new WaitForSeconds(0.2f);

        if (roundManager != null && roundManager.PlayerHasPassed)
        {
            PassRoundAndEndTurn();
        }
        else
        {
            EndTurnAndPassControl();
        }
    }

    private void EndTurnAndPassControl()
    {
        isEnemyTurn = false;
        turnSystem?.EndEnemyTurn();
    }

    private void PassRoundAndEndTurn()
    {
        hasPassed = true;
        isEnemyTurn = false;

        roundManager?.OnEnemyPassed();
        turnSystem?.EndEnemyTurn();
    }

    private bool DrawCard()
    {
        if (deck.Count == 0 || enemyHandPanel == null)
        {
            return false;
        }

        CardData drawnData = deck[0];
        deck.RemoveAt(0);

        if (drawnData == null)
        {
            return false;
        }

        EnemyHandEntry entry = new EnemyHandEntry
        {
            Data = drawnData,
            BackVisual = CreateCardBackVisual(),
            CurrentHealth = drawnData.MaxHealth
        };

        handEntries.Add(entry);
        return true;
    }

    private EnemyHandEntry SelectCardForCurrentRound()
    {
        handEntries.RemoveAll(entry => entry == null || entry.Data == null);

        if (handEntries.Count == 0)
        {
            return null;
        }

        EnemyHandEntry cheapest = null;

        for (int i = 0; i < handEntries.Count; i++)
        {
            EnemyHandEntry candidate = handEntries[i];
            if (candidate == null || candidate.Data == null)
            {
                continue;
            }

            if (cheapest == null || candidate.Data.Cost < cheapest.Data.Cost)
            {
                cheapest = candidate;
            }
        }

        return cheapest;
    }

    private CardSlot SelectFreeSlot()
    {
        if (roundManager == null)
        {
            return null;
        }

        List<CardSlot> freeSlots = new List<CardSlot>();
        IReadOnlyList<CardSlot> combatSlots = roundManager.EnemyBattleSlots;

        for (int i = 0; i < combatSlots.Count; i++)
        {
            CardSlot slot = combatSlots[i];
            if (slot != null && !slot.HasCard)
            {
                freeSlots.Add(slot);
            }
        }

        if (freeSlots.Count == 0)
        {
            return null;
        }

        return freeSlots[Random.Range(0, freeSlots.Count)];
    }

    private Card CreateCardFromEntry(EnemyHandEntry entry)
    {
        if (entry == null || entry.Data == null || cardPrefab == null || enemyHandPanel == null)
        {
            return null;
        }

        GameObject cardGO = Instantiate(cardPrefab, enemyHandPanel);

        RectTransform rectTransform = cardGO.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(150f, 200f);
            rectTransform.localScale = Vector3.one;
        }

        Card card = cardGO.GetComponent<Card>();
        if (card == null)
        {
            Destroy(cardGO);
            return null;
        }

        card.SetCardData(entry.Data);
        card.SetOwner(false);
        card.SetCurrentHealth(entry.CurrentHealth);

        CardDragHandler dragHandler = card.GetComponent<CardDragHandler>();
        if (dragHandler != null)
        {
            dragHandler.enabled = false;
        }

        CanvasGroup canvasGroup = card.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        return card;
    }

    private GameObject CreateCardBackVisual()
    {
        if (enemyHandPanel == null)
        {
            return null;
        }

        GameObject backGO;

        if (cardBackPrefab != null)
        {
            backGO = Instantiate(cardBackPrefab, enemyHandPanel);
        }
        else
        {
            backGO = new GameObject("CardBack", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            backGO.transform.SetParent(enemyHandPanel, false);

            Image backImage = backGO.GetComponent<Image>();
            backImage.color = new Color(0.13f, 0.13f, 0.2f, 1f);
        }

        RectTransform rectTransform = backGO.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(150f, 200f);
            rectTransform.localScale = Vector3.one;
        }

        CanvasGroup canvasGroup = backGO.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = backGO.AddComponent<CanvasGroup>();
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        return backGO;
    }

    private IEnumerator HideCardBack(GameObject backVisual)
    {
        if (backVisual == null)
        {
            yield break;
        }

        RectTransform rectTransform = backVisual.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Destroy(backVisual);
            yield break;
        }

        Vector3 startScale = rectTransform.localScale;
        float elapsed = 0f;

        while (elapsed < cardBackHideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, cardBackHideDuration));
            rectTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }

        Destroy(backVisual);
    }

    private void InitializeDeck()
    {
        deck.Clear();

        for (int i = 0; i < initialDeck.Count; i++)
        {
            if (initialDeck[i] != null)
            {
                deck.Add(initialDeck[i]);
            }
        }

        if (deck.Count == 0)
        {
            deck.AddRange(DefaultDeckProvider.CreateMvpDeck());
        }

        Shuffle(deck);
    }

    private void Shuffle(List<CardData> targetDeck)
    {
        for (int i = targetDeck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (targetDeck[i], targetDeck[j]) = (targetDeck[j], targetDeck[i]);
        }
    }
}
