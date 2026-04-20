using System.Collections.Generic;
using UnityEngine;

public static class DefaultDeckProvider
{
    private const string CardsResourcePath = "ScriptableObjects/Cards";

    public static List<CardData> CreateMvpDeck()
    {
        CardData[] loadedCards = Resources.LoadAll<CardData>(CardsResourcePath);
        Dictionary<string, CardData> cardsByName = new Dictionary<string, CardData>();

        foreach (CardData cardData in loadedCards)
        {
            if (cardData == null || string.IsNullOrEmpty(cardData.CardName))
            {
                continue;
            }

            cardsByName[cardData.CardName] = cardData;
        }

        List<CardData> result = new List<CardData>(15);
        AddCopies(cardsByName, result, "MVP_2_3", 3);
        AddCopies(cardsByName, result, "MVP_2_4", 3);
        AddCopies(cardsByName, result, "MVP_2_5", 3);
        AddCopies(cardsByName, result, "MVP_3_3", 3);
        AddCopies(cardsByName, result, "MVP_3_4", 2);
        AddCopies(cardsByName, result, "MVP_3_5", 1);

        if (result.Count == 0)
        {
            Debug.LogWarning("[DefaultDeckProvider] MVP cards not found, fallback to all card data assets");
            result.AddRange(loadedCards);
        }

        return result;
    }

    private static void AddCopies(Dictionary<string, CardData> cardsByName, List<CardData> target, string cardName, int count)
    {
        if (!cardsByName.TryGetValue(cardName, out CardData cardData) || cardData == null)
        {
            Debug.LogWarning($"[DefaultDeckProvider] Missing card asset: {cardName}");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            target.Add(cardData);
        }
    }
}
