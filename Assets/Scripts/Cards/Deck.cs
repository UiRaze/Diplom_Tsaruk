using System.Collections.Generic;
using UnityEngine;

public class Deck : MonoBehaviour
{
    [SerializeField] private List<CardData> initialCards = new List<CardData>();
    [SerializeField] private bool useMvpDefaultDeck = true;

    private List<CardData> cards = new List<CardData>();

    private void Start()
    {
        LoadDeckFromSave();
        cards.RemoveAll(card => card == null);

        if (cards.Count == 0)
        {
            LoadInitialDeck();
        }

        InjectRunBonusCards();
        Shuffle();
    }

    private void LoadDeckFromSave()
    {
        cards = DeckPersistence.Load();
    }

    public CardData DrawCard()
    {
        if (cards.Count == 0)
        {
            Debug.LogWarning("[Deck] Deck is empty");
            return null;
        }

        CardData card = cards[0];
        cards.RemoveAt(0);
        return card;
    }

    public void Shuffle()
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
    }

    private void LoadInitialDeck()
    {
        cards.Clear();

        if (useMvpDefaultDeck)
        {
            cards = DefaultDeckProvider.CreateMvpDeck();
        }

        if (cards.Count == 0)
        {
            for (int i = 0; i < initialCards.Count; i++)
            {
                CardData cardData = initialCards[i];
                if (cardData != null)
                {
                    cards.Add(cardData);
                }
            }
        }

        if (cards.Count == 0)
        {
            Debug.LogWarning("[Deck] Initial deck is empty");
        }
    }

    private void InjectRunBonusCards()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        IReadOnlyList<CardData> bonusCards = GameManager.Instance.GetRunBonusCards();
        if (bonusCards == null)
        {
            return;
        }

        for (int i = 0; i < bonusCards.Count; i++)
        {
            CardData cardData = bonusCards[i];
            if (cardData != null)
            {
                cards.Add(cardData);
            }
        }
    }
}
