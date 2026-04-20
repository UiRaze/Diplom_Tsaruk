using System;
using System.Collections.Generic;
using UnityEngine;

public enum EventType
{
    OpenShop,
    OpenTextChoice
}

public static class NodeEventDispatcher
{
    public static event Action<EventType> OnEventRaised;

    private static readonly Dictionary<NodeType, Action<MapNode>> NodeRoutes = new Dictionary<NodeType, Action<MapNode>>
    {
        { NodeType.NormalBattle, LoadBattleScene },
        { NodeType.Elite, LoadBattleScene },
        { NodeType.Boss, LoadBattleScene },
        { NodeType.Rest, HandleRest },
        { NodeType.Shop, HandleShop },
        { NodeType.GamblingBazaar, HandleGamblingBazaar },
        { NodeType.RandomEvent, HandleTextChoice },
        { NodeType.TrialGate, HandleTrialGate },
        { NodeType.FortuneWheel, HandleFortuneWheel },
        { NodeType.Crossroads, HandleCrossroads },
        { NodeType.AlchemyPot, HandleAlchemyPot },
        { NodeType.CardWorkshop, HandleCardWorkshop },
        { NodeType.StorageVault, HandleStorageVault },
        { NodeType.Chronicler, HandleChronicler },
        { NodeType.SeerEye, HandleSeerEye },
        { NodeType.MemoryCandle, HandleMemoryCandle },
        { NodeType.MysticalAltar, HandleMysticalAltar }
    };

    public static void ExecuteEvent(MapNode node)
    {
        if (node == null || node.NodeData == null)
        {
            Debug.LogWarning("[NodeEventDispatcher] Node or NodeData is null.");
            return;
        }

        GameManager.Instance?.AdvanceNodeStep();

        NodeType nodeType = NormalizeNodeType(node.EffectiveNodeType);
        if (NodeRoutes.TryGetValue(nodeType, out Action<MapNode> handler))
        {
            handler?.Invoke(node);
            return;
        }

        Debug.Log($"[NodeEventDispatcher] No route for node type: {nodeType}");
    }

    public static void ProcessPendingRewards()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            return;
        }

        bool hasEliteReward = gm.ConsumePendingEliteRelicReward();
        bool hasCardReward = gm.ConsumePendingCardReward();

        Action showCardRewardAction = () => ShowCardRewardSelection(gm);

        if (hasEliteReward)
        {
            RunRelic relic = RelicLibrary.CreateRandomEliteRelic();
            gm.AddRelic(relic);

            string bonusText = RelicLibrary.BuildShortBonusText(relic);
            string description = $"{relic.description}\nБонусы: {bonusText}";
            ShowSingleActionPopup(
                $"Реликвия: {relic.name}",
                description,
                "Принять",
                hasCardReward ? showCardRewardAction : null);
            return;
        }

        if (hasCardReward)
        {
            showCardRewardAction();
        }
    }

    public static void RaiseEvent(EventType eventType)
    {
        OnEventRaised?.Invoke(eventType);

        switch (eventType)
        {
            case EventType.OpenShop:
                OpenShopWindow();
                break;
            case EventType.OpenTextChoice:
                OpenTextChoiceWindow();
                break;
        }
    }

    private static void LoadBattleScene(MapNode node)
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        NodeType nodeType = NormalizeNodeType(node.EffectiveNodeType);
        int roundLimit = nodeType == NodeType.TrialGate ? 5 : 0;
        GameManager.Instance.PrepareBattleContext(nodeType, roundLimit);
        GameManager.Instance.StartRunFromNode(node);
    }

    private static void HandleRest(MapNode node)
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        List<NodeInteractionPopup.PopupOption> options = new List<NodeInteractionPopup.PopupOption>
        {
            new NodeInteractionPopup.PopupOption
            {
                Label = "Отдохнуть (восстановить 35% HP)",
                Action = () =>
                {
                    int before = GameManager.Instance.PlayerHP;
                    int baseHeal = Mathf.CeilToInt(GameManager.Instance.MaxHP * 0.35f);
                    GameManager.Instance.HealPlayer(baseHeal);
                    int actual = Mathf.Max(0, GameManager.Instance.PlayerHP - before);
                    ShowSingleActionPopup("Привал", $"Вы восстановили {actual} HP.", "Продолжить");
                }
            },
            new NodeInteractionPopup.PopupOption
            {
                Label = "Улучшить колоду (+1 случайная карта)",
                Action = () =>
                {
                    List<CardData> choices = GameManager.Instance.GetRandomCardChoices(1, card => card.Cost >= 2);
                    if (choices.Count == 0 || choices[0] == null)
                    {
                        ShowSingleActionPopup("Привал", "Подходящие карты не найдены.", "Продолжить");
                        return;
                    }

                    GameManager.Instance.AddCardToRunDeck(choices[0]);
                    ShowSingleActionPopup("Привал", $"В колоду добавлена карта: {choices[0].CardName}", "Продолжить");
                }
            },
            new NodeInteractionPopup.PopupOption
            {
                Label = "Удалить 1 бонусную карту",
                Action = () =>
                {
                    if (GameManager.Instance.TryRemoveRunBonusCard(out CardData removed) && removed != null)
                    {
                        ShowSingleActionPopup("Привал", $"Удалена карта: {removed.CardName}", "Продолжить");
                        return;
                    }

                    ShowSingleActionPopup("Привал", "Бонусных карт для удаления нет.", "Продолжить");
                }
            }
        };

        NodeInteractionPopup.Instance.Show("Привал", "Выберите действие:", options, UpdateWorldMapUi);
    }

    private static void HandleShop(MapNode node)
    {
        RaiseEvent(EventType.OpenShop);
    }

    private static void HandleTextChoice(MapNode node)
    {
        RaiseEvent(EventType.OpenTextChoice);
    }

    private static void HandleAlchemyPot(MapNode node)
    {
        AlchemyPotUI.Instance.Open(UpdateWorldMapUi);
    }

    private static void HandleTrialGate(MapNode node)
    {
        List<NodeInteractionPopup.PopupOption> options = new List<NodeInteractionPopup.PopupOption>
        {
            new NodeInteractionPopup.PopupOption
            {
                Label = "Войти (условие: победа за 5 раундов)",
                Action = () =>
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.PrepareBattleContext(NodeType.TrialGate, 5);
                        GameManager.Instance.StartRunFromNode(node);
                    }
                }
            },
            new NodeInteractionPopup.PopupOption
            {
                Label = "Отступить",
                Action = () => { }
            }
        };

        NodeInteractionPopup.Instance.Show(
            "Врата Испытаний",
            "Особый бой с повышенной наградой. Нужна победа за 5 раундов.",
            options,
            UpdateWorldMapUi);
    }

    private static void HandleGamblingBazaar(MapNode node)
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        List<NodeInteractionPopup.PopupOption> options = new List<NodeInteractionPopup.PopupOption>
        {
            new NodeInteractionPopup.PopupOption
            {
                Label = "Заплатить 4 HP за карту",
                Action = () =>
                {
                    if (GameManager.Instance.PlayerHP <= 4)
                    {
                        ShowSingleActionPopup("Азартный базар", "Недостаточно HP для сделки.", "Продолжить");
                        return;
                    }

                    GameManager.Instance.SetPlayerHP(GameManager.Instance.PlayerHP - 4);
                    List<CardData> choices = GameManager.Instance.GetRandomCardChoices(1);
                    if (choices.Count > 0 && choices[0] != null)
                    {
                        GameManager.Instance.AddCardToRunDeck(choices[0]);
                        ShowSingleActionPopup("Азартный базар", $"Вы получили карту: {choices[0].CardName}", "Продолжить");
                    }
                }
            },
            new NodeInteractionPopup.PopupOption
            {
                Label = "Продать бонусную карту за 35 золота",
                Action = () =>
                {
                    if (GameManager.Instance.TryRemoveRunBonusCard(out CardData removed) && removed != null)
                    {
                        GameManager.Instance.AddGold(35);
                        ShowSingleActionPopup("Азартный базар", $"Продана карта {removed.CardName}.", "Продолжить");
                        return;
                    }

                    ShowSingleActionPopup("Азартный базар", "Бонусных карт для продажи нет.", "Продолжить");
                }
            },
            new NodeInteractionPopup.PopupOption
            {
                Label = "Уйти",
                Action = () => { }
            }
        };

        NodeInteractionPopup.Instance.Show("Азартный базар", "Здесь можно платить не только золотом.", options, UpdateWorldMapUi);
    }

    private static void HandleFortuneWheel(MapNode node)
    {
        List<NodeInteractionPopup.PopupOption> options = new List<NodeInteractionPopup.PopupOption>
        {
            new NodeInteractionPopup.PopupOption
            {
                Label = "Крутить колесо",
                Action = SpinFortuneWheel
            },
            new NodeInteractionPopup.PopupOption
            {
                Label = "Пропустить",
                Action = () => { }
            }
        };

        NodeInteractionPopup.Instance.Show("Колесо Фортуны", "Возможен джекпот, возможны потери.", options, UpdateWorldMapUi);
    }

    private static void HandleCrossroads(MapNode node)
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        List<NodeInteractionPopup.PopupOption> options = new List<NodeInteractionPopup.PopupOption>
        {
            new NodeInteractionPopup.PopupOption
            {
                Label = "Путь риска: +25% наград на 3 узла, -3 HP",
                Action = () =>
                {
                    GameManager.Instance.ApplyCrossroadsEffect(25, 0, 3);
                    GameManager.Instance.SetPlayerHP(Mathf.Max(1, GameManager.Instance.PlayerHP - 3));
                    ShowSingleActionPopup("Перекрёсток", "Получен временный бафф наград на 3 узла.", "Продолжить");
                }
            },
            new NodeInteractionPopup.PopupOption
            {
                Label = "Путь силы: +35% наград и -15% штрафа на 3 узла",
                Action = () =>
                {
                    GameManager.Instance.ApplyCrossroadsEffect(35, 15, 3);
                    ShowSingleActionPopup("Перекрёсток", "Применён смешанный бафф/дебафф на 3 узла.", "Продолжить");
                }
            }
        };

        NodeInteractionPopup.Instance.Show("Перекрёсток", "Выберите временный эффект.", options, UpdateWorldMapUi);
    }

    private static void HandleCardWorkshop(MapNode node)
    {
        CardWorkshopUI.Instance.Open(UpdateWorldMapUi);
    }

    private static void HandleStorageVault(MapNode node)
    {
        StorageVaultUI.Instance.Open(UpdateWorldMapUi);
    }

    private static void HandleChronicler(MapNode node)
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        List<NodeInteractionPopup.PopupOption> options = new List<NodeInteractionPopup.PopupOption>
        {
            new NodeInteractionPopup.PopupOption
            {
                Label = "Запись о Воде (больше водных наград)",
                Action = () =>
                {
                    GameManager.Instance.SetCardRewardBias(Element.Water);
                    ShowSingleActionPopup("Летописец", "Будущие карточные награды смещены в сторону Воды.", "Продолжить");
                }
            },
            new NodeInteractionPopup.PopupOption
            {
                Label = "Запись об Огне (больше огненных наград)",
                Action = () =>
                {
                    GameManager.Instance.SetCardRewardBias(Element.Fire);
                    ShowSingleActionPopup("Летописец", "Будущие карточные награды смещены в сторону Огня.", "Продолжить");
                }
            },
            new NodeInteractionPopup.PopupOption
            {
                Label = "Оставить как есть",
                Action = () => { }
            }
        };

        NodeInteractionPopup.Instance.Show("Летописец", "Выбор влияет на последующие награды.", options, UpdateWorldMapUi);
    }

    private static void HandleSeerEye(MapNode node)
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetSeerRevealDepth(2);
        ShowSingleActionPopup(
            "Глаз Прорицателя",
            "Туман войны ослаблен: видимость увеличена на 2 уровня вперед.",
            "Принять",
            UpdateWorldMapUi);
    }

    private static void HandleMemoryCandle(MapNode node)
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        bool success = UnityEngine.Random.value <= 0.65f && GameManager.Instance.TryRestoreRememberedCard();
        if (success)
        {
            ShowSingleActionPopup("Свеча Воспоминаний", "Ранее удалённая карта возвращена в колоду.", "Продолжить", UpdateWorldMapUi);
        }
        else
        {
            int before = GameManager.Instance.PlayerHP;
            GameManager.Instance.HealPlayer(3);
            int actual = Mathf.Max(0, GameManager.Instance.PlayerHP - before);
            ShowSingleActionPopup("Свеча Воспоминаний", $"Воспоминание не сработало. Вы восстановили {actual} HP.", "Продолжить", UpdateWorldMapUi);
        }
    }

    private static void HandleMysticalAltar(MapNode node)
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        EvaluateDeckCoherence(out bool coherent, out Element dominantElement);

        if (coherent)
        {
            RunRelic altarRelic = RelicLibrary.CreateAltarRelic(dominantElement);
            GameManager.Instance.AddRelic(altarRelic);
            GameManager.Instance.AddResources(20);

            string bonusText = RelicLibrary.BuildShortBonusText(altarRelic);
            ShowSingleActionPopup(
                "Мистический алтарь",
                $"Алтарь принял ваш билд.\nПолучена реликвия: {altarRelic.name}\nБонусы: {bonusText}",
                "Продолжить",
                UpdateWorldMapUi);
        }
        else
        {
            GameManager.Instance.SetPlayerHP(Mathf.Max(1, GameManager.Instance.PlayerHP - 4));
            ShowSingleActionPopup(
                "Мистический алтарь",
                "Алтарь отверг колоду. Вы потеряли 4 HP.",
                "Продолжить",
                UpdateWorldMapUi);
        }
    }

    private static void OpenShopWindow()
    {
        ShopUI shopUI = UnityEngine.Object.FindObjectOfType<ShopUI>();
        if (shopUI != null)
        {
            shopUI.OpenShop(UpdateWorldMapUi);
            return;
        }

        Debug.LogWarning("[NodeEventDispatcher] ShopUI not found. OpenShop skipped.");
    }

    private static void OpenTextChoiceWindow()
    {
        EventPopup eventPopup = UnityEngine.Object.FindObjectOfType<EventPopup>();
        if (eventPopup != null)
        {
            eventPopup.ShowRandomEvent(UpdateWorldMapUi);
            return;
        }

        Debug.LogWarning("[NodeEventDispatcher] EventPopup not found. OpenTextChoice skipped.");
    }

    private static void UpdateWorldMapUi()
    {
        WorldMapUI worldMapUi = UnityEngine.Object.FindObjectOfType<WorldMapUI>();
        worldMapUi?.UpdateStats();
        WorldMapManager worldMapManager = UnityEngine.Object.FindObjectOfType<WorldMapManager>();
        worldMapManager?.RefreshMapVisibility();
    }

    private static NodeType NormalizeNodeType(NodeType nodeType)
    {
        switch (nodeType)
        {
            case NodeType.Battle:
                return NodeType.NormalBattle;
            case NodeType.EliteBattle:
                return NodeType.Elite;
            case NodeType.BossBattle:
                return NodeType.Boss;
            case NodeType.Merchant:
                return NodeType.Shop;
            case NodeType.Event:
                return NodeType.RandomEvent;
            default:
                return nodeType;
        }
    }

    private static void SpinFortuneWheel()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        float roll = UnityEngine.Random.value;

        if (roll < 0.2f)
        {
            GameManager.Instance.AddGold(90);
            GameManager.Instance.AddResources(45);
            ShowSingleActionPopup("Колесо Фортуны", "Джекпот: +90 золота и +45 ресурсов.", "Продолжить", UpdateWorldMapUi);
            return;
        }

        if (roll < 0.65f)
        {
            List<CardData> choices = GameManager.Instance.GetRandomCardChoices(1);
            if (choices.Count > 0 && choices[0] != null)
            {
                GameManager.Instance.AddCardToRunDeck(choices[0]);
                ShowSingleActionPopup("Колесо Фортуны", $"Удача улыбнулась: {choices[0].CardName}", "Продолжить", UpdateWorldMapUi);
                return;
            }
        }

        if (GameManager.Instance.TryRemoveRunBonusCard(out CardData removed) && removed != null)
        {
            ShowSingleActionPopup("Колесо Фортуны", $"Неудача: потеряна карта {removed.CardName}.", "Продолжить", UpdateWorldMapUi);
        }
        else
        {
            GameManager.Instance.SetPlayerHP(Mathf.Max(1, GameManager.Instance.PlayerHP - 2));
            ShowSingleActionPopup("Колесо Фортуны", "Неудача: потеряно 2 HP.", "Продолжить", UpdateWorldMapUi);
        }
    }

    private static void ShowSingleActionPopup(string title, string description, string buttonLabel, Action closedCallback = null)
    {
        List<NodeInteractionPopup.PopupOption> options = new List<NodeInteractionPopup.PopupOption>
        {
            new NodeInteractionPopup.PopupOption
            {
                Label = buttonLabel,
                Action = () => { }
            }
        };

        NodeInteractionPopup.Instance.Show(title, description, options, closedCallback);
    }

    private static void ShowCardRewardSelection(GameManager gm)
    {
        if (gm == null)
        {
            return;
        }

        List<CardData> choices = gm.GetRandomCardChoices(3);
        if (choices.Count == 0)
        {
            return;
        }

        List<NodeInteractionPopup.PopupOption> options = new List<NodeInteractionPopup.PopupOption>(choices.Count);
        for (int i = 0; i < choices.Count; i++)
        {
            CardData choice = choices[i];
            if (choice == null)
            {
                continue;
            }

            options.Add(new NodeInteractionPopup.PopupOption
            {
                Label = $"{choice.CardName} (Цена {choice.Cost}, Урон {choice.Damage})",
                Action = () =>
                {
                    gm.AddCardToRunDeck(choice);
                    ShowSingleActionPopup("Новая карта", $"Карта добавлена: {choice.CardName}", "Продолжить");
                }
            });
        }

        if (options.Count > 0)
        {
            NodeInteractionPopup.Instance.Show(
                "Награда за бой",
                "Выберите 1 карту из 3:",
                options);
        }
    }

    private static void EvaluateDeckCoherence(out bool coherent, out Element dominantElement)
    {
        coherent = false;
        dominantElement = Element.Water;

        if (GameManager.Instance == null)
        {
            return;
        }

        IReadOnlyList<CardData> runCards = GameManager.Instance.GetRunBonusCards();
        if (runCards == null || runCards.Count == 0)
        {
            return;
        }

        Dictionary<Element, int> counts = new Dictionary<Element, int>();
        int validCards = 0;

        for (int i = 0; i < runCards.Count; i++)
        {
            CardData card = runCards[i];
            if (card == null)
            {
                continue;
            }

            validCards++;
            if (!counts.ContainsKey(card.CardElement))
            {
                counts[card.CardElement] = 0;
            }

            counts[card.CardElement]++;
        }

        if (validCards == 0)
        {
            return;
        }

        int maxCount = 0;
        foreach (KeyValuePair<Element, int> pair in counts)
        {
            if (pair.Value > maxCount)
            {
                maxCount = pair.Value;
                dominantElement = pair.Key;
            }
        }

        coherent = maxCount >= Mathf.CeilToInt(validCards * 0.6f);
    }
}
