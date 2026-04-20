using System.Collections.Generic;
using UnityEngine;

public static class CardCatalog
{
    private const string CardsResourcePath = "ScriptableObjects/Cards";

    private static readonly Dictionary<string, CardData> cardsById = new Dictionary<string, CardData>();
    private static bool isLoaded;

    public static CardData GetById(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            return null;
        }

        EnsureLoaded();
        cardsById.TryGetValue(cardId, out CardData cardData);
        return cardData;
    }

    public static string GetStableId(CardData cardData)
    {
        if (cardData == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(cardData.CardId))
        {
            return cardData.CardId;
        }

        if (!string.IsNullOrWhiteSpace(cardData.CardName))
        {
            return cardData.CardName;
        }

        return cardData.name;
    }

    public static List<CardData> ResolveCards(IReadOnlyList<string> cardIds)
    {
        List<CardData> result = new List<CardData>();
        if (cardIds == null || cardIds.Count == 0)
        {
            return result;
        }

        EnsureLoaded();

        for (int i = 0; i < cardIds.Count; i++)
        {
            string cardId = cardIds[i];
            if (string.IsNullOrWhiteSpace(cardId))
            {
                continue;
            }

            if (cardsById.TryGetValue(cardId, out CardData cardData) && cardData != null)
            {
                result.Add(cardData);
                continue;
            }

            Debug.LogWarning($"[CardCatalog] Card id '{cardId}' not found in Resources/{CardsResourcePath}.");
        }

        return result;
    }

    public static void Reload()
    {
        isLoaded = false;
        cardsById.Clear();
        EnsureLoaded();
    }

    private static void EnsureLoaded()
    {
        if (isLoaded)
        {
            return;
        }

        cardsById.Clear();
        CardData[] allCards = Resources.LoadAll<CardData>(CardsResourcePath);

        for (int i = 0; i < allCards.Length; i++)
        {
            CardData cardData = allCards[i];
            if (cardData == null)
            {
                continue;
            }

            string cardId = GetStableId(cardData);
            if (string.IsNullOrWhiteSpace(cardId))
            {
                continue;
            }

            if (cardsById.ContainsKey(cardId))
            {
                Debug.LogWarning($"[CardCatalog] Duplicate card id '{cardId}'. First asset will be used.");
                continue;
            }

            cardsById.Add(cardId, cardData);
        }

        isLoaded = true;
    }
}
