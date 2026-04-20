using System;
using UnityEngine;

[Serializable]
public class RunRelic
{
    [SerializeField] public string id;
    [SerializeField] public string name;
    [SerializeField] public string description;
    [SerializeField] public int goldBonusPercent;
    [SerializeField] public int resourceBonusPercent;
    [SerializeField] public int healingBonusPercent;
    [SerializeField] public int playerDamageBonusPercent;
    [SerializeField] public int shopDiscountPercent;
    [SerializeField] public int cardRewardBonusPercent;
}

public static class RelicLibrary
{
    private static readonly RunRelic[] ElitePool =
    {
        new RunRelic
        {
            id = "elite_coin_lens",
            name = "Линза Алчности",
            description = "Усиливает денежную добычу после боёв и событий.",
            goldBonusPercent = 22
        },
        new RunRelic
        {
            id = "elite_resource_prism",
            name = "Призма Сборщика",
            description = "Увеличивает прирост ресурсов во всех источниках.",
            resourceBonusPercent = 24
        },
        new RunRelic
        {
            id = "elite_battle_sigil",
            name = "Печать Ветерана",
            description = "Повышает урон ваших карт в бою.",
            playerDamageBonusPercent = 14
        },
        new RunRelic
        {
            id = "elite_mender_heart",
            name = "Сердце Восстановления",
            description = "Любое лечение становится заметно сильнее.",
            healingBonusPercent = 30
        },
        new RunRelic
        {
            id = "elite_market_seal",
            name = "Купеческая Печать",
            description = "Снижает цены в магазинах и повышает ценность выбора.",
            shopDiscountPercent = 20,
            cardRewardBonusPercent = 14
        }
    };

    public static RunRelic CreateRandomEliteRelic()
    {
        int index = UnityEngine.Random.Range(0, ElitePool.Length);
        return Clone(ElitePool[index]);
    }

    public static RunRelic CreateAltarRelic(Element dominantElement)
    {
        switch (dominantElement)
        {
            case Element.Fire:
                return new RunRelic
                {
                    id = "altar_fire",
                    name = "Пепельный Знак",
                    description = "Алтарь пламени усиливает боевой натиск.",
                    playerDamageBonusPercent = 10,
                    goldBonusPercent = 10
                };
            case Element.Wind:
                return new RunRelic
                {
                    id = "altar_wind",
                    name = "Шепот Бури",
                    description = "Алтарь ветра помогает находить сильные награды.",
                    cardRewardBonusPercent = 16,
                    resourceBonusPercent = 8
                };
            case Element.Earth:
                return new RunRelic
                {
                    id = "altar_earth",
                    name = "Камень Опоры",
                    description = "Алтарь земли усиливает выживаемость и восстановление.",
                    healingBonusPercent = 18,
                    resourceBonusPercent = 12
                };
            case Element.Water:
            default:
                return new RunRelic
                {
                    id = "altar_water",
                    name = "Печать Течения",
                    description = "Алтарь воды укрепляет экономику забега.",
                    goldBonusPercent = 12,
                    resourceBonusPercent = 12
                };
        }
    }

    public static string BuildShortBonusText(RunRelic relic)
    {
        if (relic == null)
        {
            return string.Empty;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        AppendBonus(sb, relic.goldBonusPercent, "золото");
        AppendBonus(sb, relic.resourceBonusPercent, "ресурсы");
        AppendBonus(sb, relic.healingBonusPercent, "лечение");
        AppendBonus(sb, relic.playerDamageBonusPercent, "урон карт");
        AppendBonus(sb, relic.shopDiscountPercent, "скидка в магазине");
        AppendBonus(sb, relic.cardRewardBonusPercent, "качество наград");
        return sb.ToString();
    }

    private static RunRelic Clone(RunRelic source)
    {
        return new RunRelic
        {
            id = source.id,
            name = source.name,
            description = source.description,
            goldBonusPercent = source.goldBonusPercent,
            resourceBonusPercent = source.resourceBonusPercent,
            healingBonusPercent = source.healingBonusPercent,
            playerDamageBonusPercent = source.playerDamageBonusPercent,
            shopDiscountPercent = source.shopDiscountPercent,
            cardRewardBonusPercent = source.cardRewardBonusPercent
        };
    }

    private static void AppendBonus(System.Text.StringBuilder sb, int percent, string label)
    {
        if (percent == 0)
        {
            return;
        }

        if (sb.Length > 0)
        {
            sb.Append(", ");
        }

        sb.Append(percent > 0 ? "+" : string.Empty);
        sb.Append(percent);
        sb.Append("% ");
        sb.Append(label);
    }
}
