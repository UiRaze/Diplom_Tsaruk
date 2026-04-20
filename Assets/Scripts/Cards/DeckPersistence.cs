using System;
using System.Collections.Generic;
using UnityEngine;

public static class DeckPersistence
{
    public const string DeckSaveKey = "SavedDeck1";
    private const int CurrentSaveVersion = 2;

    [Serializable]
    private sealed class DeckSaveDataV2
    {
        public int version = CurrentSaveVersion;
        public List<string> cardIds = new List<string>();
    }

    [Serializable]
    private sealed class DeckSaveDataLegacy
    {
        public List<CardData> cards = new List<CardData>();
    }

    public static void Save(IReadOnlyList<CardData> cards)
    {
        DeckSaveDataV2 data = new DeckSaveDataV2();

        if (cards != null)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                string cardId = CardCatalog.GetStableId(cards[i]);
                if (!string.IsNullOrWhiteSpace(cardId))
                {
                    data.cardIds.Add(cardId);
                }
            }
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(DeckSaveKey, json);
        PlayerPrefs.Save();
    }

    public static List<CardData> Load()
    {
        string json = PlayerPrefs.GetString(DeckSaveKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<CardData>();
        }

        DeckSaveDataV2 current = JsonUtility.FromJson<DeckSaveDataV2>(json);
        if (current != null && current.version >= CurrentSaveVersion)
        {
            return CardCatalog.ResolveCards(current.cardIds);
        }

        DeckSaveDataLegacy legacy = JsonUtility.FromJson<DeckSaveDataLegacy>(json);
        if (legacy == null || legacy.cards == null)
        {
            return new List<CardData>();
        }

        List<CardData> migrated = new List<CardData>();
        for (int i = 0; i < legacy.cards.Count; i++)
        {
            CardData cardData = legacy.cards[i];
            if (cardData != null)
            {
                migrated.Add(cardData);
            }
        }

        Save(migrated);
        return migrated;
    }
}
