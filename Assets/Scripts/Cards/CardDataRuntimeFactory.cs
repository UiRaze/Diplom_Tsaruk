using System.Reflection;
using UnityEngine;

public enum WorkshopModificationType
{
    CostReduction = 0,
    DamageBoost = 1,
    ElementReforge = 2
}

public static class CardDataRuntimeFactory
{
    private static readonly BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly FieldInfo CardNameField = typeof(CardData).GetField("cardName", FieldFlags);
    private static readonly FieldInfo CostField = typeof(CardData).GetField("cost", FieldFlags);
    private static readonly FieldInfo DamageField = typeof(CardData).GetField("damage", FieldFlags);
    private static readonly FieldInfo HealthField = typeof(CardData).GetField("health", FieldFlags);
    private static readonly FieldInfo MaxHealthField = typeof(CardData).GetField("maxHealth", FieldFlags);
    private static readonly FieldInfo CardArtField = typeof(CardData).GetField("cardArt", FieldFlags);
    private static readonly FieldInfo CardElementField = typeof(CardData).GetField("cardElement", FieldFlags);
    private static readonly FieldInfo ElementIconField = typeof(CardData).GetField("elementIcon", FieldFlags);

    public static CardData CreateCraftedCard(CardData first, CardData second)
    {
        if (first == null || second == null)
        {
            return null;
        }

        CardData template = first.Damage >= second.Damage ? first : second;
        CardData cloned = Object.Instantiate(template);

        int cost = Mathf.Clamp(Mathf.RoundToInt((first.Cost + second.Cost) / 2f) + 1, 1, 5);
        int damage = Mathf.Clamp(Mathf.Max(first.Damage, second.Damage) + 1, 1, 6);
        int health = Mathf.Clamp(Mathf.Max(first.MaxHealth, second.MaxHealth), 1, 8);
        Element element = SelectFusionElement(first.CardElement, second.CardElement);
        Sprite icon = first.CardElement == element ? first.ElementIcon : second.ElementIcon;

        ApplyValues(
            cloned,
            $"Сплав: {first.CardName} + {second.CardName}",
            cost,
            damage,
            health,
            health,
            template.CardArt,
            element,
            icon);

        return cloned;
    }

    public static CardData CreateWorkshopVariant(CardData source, WorkshopModificationType modification)
    {
        if (source == null)
        {
            return null;
        }

        CardData cloned = Object.Instantiate(source);

        int cost = source.Cost;
        int damage = source.Damage;
        int health = source.Health;
        int maxHealth = source.MaxHealth;
        Element element = source.CardElement;
        string suffix;

        switch (modification)
        {
            case WorkshopModificationType.CostReduction:
                cost = Mathf.Clamp(source.Cost - 1, 1, 5);
                damage = Mathf.Clamp(source.Damage + 1, 1, 6);
                suffix = "Лёгкая Сборка";
                break;
            case WorkshopModificationType.DamageBoost:
                cost = Mathf.Clamp(source.Cost + 1, 1, 5);
                damage = Mathf.Clamp(source.Damage + 2, 1, 6);
                suffix = "Ударная Версия";
                break;
            case WorkshopModificationType.ElementReforge:
                element = GetDifferentElement(source.CardElement);
                maxHealth = Mathf.Clamp(source.MaxHealth + 1, 1, 8);
                health = Mathf.Clamp(source.Health + 1, 1, maxHealth);
                suffix = "Перековка";
                break;
            default:
                suffix = "Мастерская";
                break;
        }

        ApplyValues(
            cloned,
            $"{source.CardName} [{suffix}]",
            cost,
            damage,
            health,
            maxHealth,
            source.CardArt,
            element,
            source.ElementIcon);

        return cloned;
    }

    private static void ApplyValues(CardData target, string cardName, int cost, int damage, int health, int maxHealth, Sprite art, Element element, Sprite icon)
    {
        if (target == null)
        {
            return;
        }

        CardNameField?.SetValue(target, cardName);
        CostField?.SetValue(target, Mathf.Clamp(cost, 1, 5));
        DamageField?.SetValue(target, Mathf.Clamp(damage, 1, 6));
        HealthField?.SetValue(target, Mathf.Clamp(health, 1, 8));
        MaxHealthField?.SetValue(target, Mathf.Clamp(Mathf.Max(maxHealth, health), 1, 8));
        CardArtField?.SetValue(target, art);
        CardElementField?.SetValue(target, element);
        ElementIconField?.SetValue(target, icon);
    }

    private static Element SelectFusionElement(Element first, Element second)
    {
        if (first == second)
        {
            return first;
        }

        return Random.value < 0.5f ? first : second;
    }

    private static Element GetDifferentElement(Element source)
    {
        Element result = source;
        int guard = 0;
        while (result == source && guard < 8)
        {
            result = (Element)Random.Range(0, 4);
            guard++;
        }

        return result;
    }
}
