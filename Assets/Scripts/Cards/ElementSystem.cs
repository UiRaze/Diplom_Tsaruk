using UnityEngine;

public enum Element
{
    Water = 0,
    Fire = 1,
    Wind = 2,
    Earth = 3
}

public static class ElementSystem
{
    public static float GetDamageMultiplier(Element attacker, Element defender)
    {
        if (attacker == defender)
        {
            return 1f;
        }

        if (HasAdvantage(attacker, defender))
        {
            return 1.5f;
        }

        if (HasAdvantage(defender, attacker))
        {
            return 0.75f;
        }

        return 1f;
    }

    public static bool HasAdvantage(Element attacker, Element defender)
    {
        return (attacker == Element.Water && defender == Element.Fire)
            || (attacker == Element.Fire && defender == Element.Wind)
            || (attacker == Element.Wind && defender == Element.Earth)
            || (attacker == Element.Earth && defender == Element.Water);
    }

    public static Color GetElementColor(Element element)
    {
        switch (element)
        {
            case Element.Water:
                return new Color(0.25f, 0.55f, 0.95f, 1f);
            case Element.Fire:
                return new Color(0.9f, 0.25f, 0.2f, 1f);
            case Element.Wind:
                return new Color(0.3f, 0.85f, 0.35f, 1f);
            case Element.Earth:
                return new Color(0.58f, 0.38f, 0.2f, 1f);
            default:
                return Color.white;
        }
    }
}
