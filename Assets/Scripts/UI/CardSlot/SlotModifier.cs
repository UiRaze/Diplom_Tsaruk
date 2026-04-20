using UnityEngine;

public enum SlotModifier
{
    DoubleStrike = 1,
    Inversion = 2,
    Recovery = 3,
    Stability = 4
}

public static class SlotModifierLogic
{
    public static SlotModifier RollD4()
    {
        return (SlotModifier)Random.Range(1, 5);
    }

    public static string GetShortLabel(SlotModifier modifier)
    {
        switch (modifier)
        {
            case SlotModifier.DoubleStrike:
                return "Двойнор урон";
            case SlotModifier.Inversion:
                return "Реверс";
            case SlotModifier.Recovery:
                return "Лечение";
            case SlotModifier.Stability:
            default:
                return "Стабильность";
        }
    }

    public static string GetDescription(SlotModifier modifier)
    {
        switch (modifier)
        {
            case SlotModifier.DoubleStrike:
                return "Двойной удар";
            case SlotModifier.Inversion:
                return "Инверсия";
            case SlotModifier.Recovery:
                return "Восстановление";
            case SlotModifier.Stability:
            default:
                return "Стабильность";
        }
    }

    public static Color GetColor(SlotModifier modifier)
    {
        switch (modifier)
        {
            case SlotModifier.DoubleStrike:
                return new Color(0.95f, 0.4f, 0.2f, 0.9f);
            case SlotModifier.Inversion:
                return new Color(0.45f, 0.25f, 0.9f, 0.9f);
            case SlotModifier.Recovery:
                return new Color(0.2f, 0.75f, 0.3f, 0.9f);
            case SlotModifier.Stability:
            default:
                return new Color(0.35f, 0.35f, 0.35f, 0.85f);
        }
    }
}
