using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Menu,
        DeckBuild,
        Map,
        Battle
    }

    public static GameManager Instance;

    private const float MetaCarryOverRatio = 0.5f;

    [Header("Run State")]
    [SerializeField] private GameState currentState = GameState.Menu;
    [SerializeField] private string lastNodeId;
    [SerializeField] private bool runActive = true;
    [SerializeField] private bool isDefeatHandled;
    [SerializeField] private int mapSeed;

    [Header("Player Stats")]
    [SerializeField] private int playerHP = 20;
    [SerializeField] private int maxHP = 20;
    [SerializeField] private int playerGold;
    [SerializeField] private int playerResources;
    [SerializeField] private int currentLevel = 1;

    [Header("Map Settings")]
    [SerializeField] private int mapLevels = GameConfig.MAP_LEVELS;
    [SerializeField] private int maxBranchesPerLevel = GameConfig.MAX_BRANCHES;

    [Header("Run Deck Additions")]
    [SerializeField] private List<CardData> runBonusCards = new List<CardData>();

    [Header("Run Stats")]
    [SerializeField] private int battlesWon;
    [SerializeField] private int battlesLost;
    [SerializeField] private int totalRoundsPlayed;
    [SerializeField] private string lastDefeatReason = string.Empty;

    [Header("Node Context")]
    [SerializeField] private bool hasPendingBattleContext;
    [SerializeField] private NodeType pendingBattleNodeType = NodeType.NormalBattle;
    [SerializeField] private int pendingBattleRoundLimit;
    [SerializeField] private bool hasPendingCardReward;
    [SerializeField] private bool hasPendingEliteRelicReward;

    [Header("Run Modifiers")]
    [SerializeField] private List<RunRelic> activeRelics = new List<RunRelic>();
    [SerializeField] private int crossroadsNodesRemaining;
    [SerializeField] private int crossroadsBonusPercent;
    [SerializeField] private int crossroadsPenaltyPercent;
    [SerializeField] private int seerRevealDepth;
    [SerializeField] private List<CardData> storageVaultCards = new List<CardData>();
    [SerializeField] private List<CardData> removedCardsMemory = new List<CardData>();
    [SerializeField] private bool hasCardRewardBias;
    [SerializeField] private Element cardRewardBias = Element.Water;

    public int PlayerHP => playerHP;
    public int MaxHP => maxHP;
    public int PlayerGold => playerGold;
    public int PlayerResources => playerResources;
    public int CurrentLevel => currentLevel;
    public int MapLevels => Mathf.Max(2, mapLevels);
    public int MaxBranchesPerLevel => Mathf.Max(2, maxBranchesPerLevel);
    public int BattlesWon => battlesWon;
    public int BattlesLost => battlesLost;
    public int TotalRoundsPlayed => totalRoundsPlayed;
    public string LastDefeatReason => lastDefeatReason;
    public int HeroCoins => playerGold;
    public int MapSeed => mapSeed;
    public GameState CurrentState => currentState;
    public bool HasPendingBattleContext => hasPendingBattleContext;
    public NodeType PendingBattleNodeType => pendingBattleNodeType;
    public int PendingBattleRoundLimit => pendingBattleRoundLimit;
    public int SeerRevealDepth => seerRevealDepth;
    public int StorageCount => storageVaultCards.Count;
    public int RelicCount => activeRelics.Count;

    public event Action<int, int> OnPlayerHealthChanged;
    public event Action<int, int> OnCurrencyChanged;
    public event Action<GameState, GameState> OnGameStateChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += HandleSceneLoaded;
            GlobalSettingsMenu.EnsureExists();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        maxHP = Mathf.Max(1, maxHP);
        playerHP = Mathf.Clamp(playerHP, 0, maxHP);
        mapLevels = Mathf.Max(2, mapLevels);
        maxBranchesPerLevel = Mathf.Max(2, maxBranchesPerLevel);
        currentLevel = Mathf.Clamp(currentLevel, 1, MapLevels);
        runActive = true;
        SyncStateWithScene(SceneManager.GetActiveScene().name);

        NotifyHealthChanged();
        NotifyCurrencyChanged();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
    }

    public void SetGameState(GameState newState)
    {
        if (currentState == newState)
        {
            return;
        }

        GameState previousState = currentState;
        currentState = newState;
        OnGameStateChanged?.Invoke(previousState, currentState);
    }

    public void SwitchToMenuState()
    {
        SetGameState(GameState.Menu);
    }

    public void SwitchToDeckBuildState()
    {
        SetGameState(GameState.DeckBuild);
    }

    public void SwitchToMapState()
    {
        SetGameState(GameState.Map);
    }

    public void SwitchToBattleState()
    {
        SetGameState(GameState.Battle);
    }

    public void StartNewRun()
    {
        playerHP = maxHP;
        playerGold = 0;
        playerResources = 0;
        currentLevel = 1;
        battlesWon = 0;
        battlesLost = 0;
        totalRoundsPlayed = 0;
        lastNodeId = null;
        lastDefeatReason = string.Empty;
        runActive = true;
        isDefeatHandled = false;
        runBonusCards.Clear();
        mapSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        hasPendingBattleContext = false;
        pendingBattleNodeType = NodeType.NormalBattle;
        pendingBattleRoundLimit = 0;
        hasPendingCardReward = false;
        hasPendingEliteRelicReward = false;
        activeRelics.Clear();
        crossroadsNodesRemaining = 0;
        crossroadsBonusPercent = 0;
        crossroadsPenaltyPercent = 0;
        seerRevealDepth = 0;
        storageVaultCards.Clear();
        removedCardsMemory.Clear();
        hasCardRewardBias = false;
        cardRewardBias = Element.Water;

        NotifyHealthChanged();
        NotifyCurrencyChanged();
    }

    public void StartNewRunFromMenu()
    {
        StartNewRun();
        SwitchToMapState();
        SceneLoader.LoadSceneAsync("WorldMap");
    }

    public void StartRun()
    {
        if (!runActive || SceneManager.GetActiveScene().name == "MainMenu")
        {
            StartNewRun();
        }

        WorldMapManager worldMapManager = FindObjectOfType<WorldMapManager>();
        if (worldMapManager != null)
        {
            lastNodeId = worldMapManager.GetCurrentNodeId();
        }

        SwitchToBattleState();
        SceneLoader.LoadSceneAsync("CardSystemTest");
    }

    public void StartRunFromDeckBuilder()
    {
        bool canContinueExistingRun = runActive && !string.IsNullOrEmpty(lastNodeId);
        if (!canContinueExistingRun)
        {
            StartNewRun();
        }

        StartRun();
    }

    public void StartRunFromNode(MapNode node)
    {
        if (node != null && !string.IsNullOrEmpty(node.NodeId))
        {
            SetLastNode(node.NodeId);
        }

        StartRun();
    }

    public void ReturnToWorldMap()
    {
        SwitchToMapState();
        SceneLoader.LoadSceneAsync("WorldMap");
    }

    public void ReturnToMainMenu()
    {
        SwitchToMenuState();
        SceneLoader.LoadSceneAsync("MainMenu");
    }

    public string GetLastNodeId()
    {
        return lastNodeId;
    }

    public int GetOrCreateMapSeed()
    {
        if (mapSeed == 0)
        {
            mapSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        }

        return mapSeed;
    }

    public void SetLastNode(string nodeId)
    {
        lastNodeId = nodeId;
    }

    public void SetCurrentLevelFromNodeLevel(int nodeLevel)
    {
        int targetLevel = Mathf.Clamp(nodeLevel + 1, 1, MapLevels);
        currentLevel = targetLevel;
    }

    public void SetPlayerHP(int newHp)
    {
        int clamped = Mathf.Clamp(newHp, 0, maxHP);
        if (clamped == playerHP)
        {
            return;
        }

        playerHP = clamped;
        NotifyHealthChanged();
    }

    public void HealPlayer(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        int modified = ApplyHealModifiers(amount);
        SetPlayerHP(playerHP + modified);
    }

    public void RestorePlayerToFull()
    {
        SetPlayerHP(maxHP);
    }

    public bool SpendGold(int amount)
    {
        int normalized = Mathf.Max(0, amount);
        if (normalized <= 0)
        {
            return true;
        }

        if (playerGold < normalized)
        {
            return false;
        }

        playerGold -= normalized;
        NotifyCurrencyChanged();
        return true;
    }

    public void AddGold(int amount)
    {
        if (amount == 0)
        {
            return;
        }

        int adjustedAmount = ApplyGoldModifiers(amount);
        playerGold = Mathf.Max(0, playerGold + adjustedAmount);
        NotifyCurrencyChanged();
    }

    public void AddResources(int amount)
    {
        if (amount == 0)
        {
            return;
        }

        int adjustedAmount = ApplyResourceModifiers(amount);
        playerResources = Mathf.Max(0, playerResources + adjustedAmount);
        NotifyCurrencyChanged();
    }

    public void AddCardToRunDeck(CardData cardData)
    {
        if (cardData == null)
        {
            return;
        }

        runBonusCards.Add(cardData);
    }

    public IReadOnlyList<CardData> GetRunBonusCards()
    {
        return runBonusCards;
    }

    public void PrepareBattleContext(NodeType nodeType, int roundLimit = 0)
    {
        hasPendingBattleContext = true;
        pendingBattleNodeType = nodeType;
        pendingBattleRoundLimit = Mathf.Max(0, roundLimit);
    }

    public void ResolveBattleNodeResult(bool playerWon)
    {
        if (!hasPendingBattleContext)
        {
            return;
        }

        if (playerWon)
        {
            switch (pendingBattleNodeType)
            {
                case NodeType.NormalBattle:
                case NodeType.Boss:
                    hasPendingCardReward = true;
                    break;
                case NodeType.Elite:
                    hasPendingCardReward = true;
                    hasPendingEliteRelicReward = true;
                    break;
                case NodeType.TrialGate:
                    AddGold(35);
                    AddResources(20);
                    break;
            }
        }

        hasPendingBattleContext = false;
        pendingBattleRoundLimit = 0;
    }

    public bool ConsumePendingCardReward()
    {
        bool hadReward = hasPendingCardReward;
        hasPendingCardReward = false;
        return hadReward;
    }

    public bool ConsumePendingEliteRelicReward()
    {
        bool hadReward = hasPendingEliteRelicReward;
        hasPendingEliteRelicReward = false;
        return hadReward;
    }

    public void AddRelic(RunRelic relic)
    {
        if (relic == null)
        {
            return;
        }

        activeRelics.Add(relic);
    }

    public void AddRelic(string relicName, int rewardBonusPercent)
    {
        RunRelic relic = new RunRelic
        {
            id = $"legacy_{Guid.NewGuid()}",
            name = relicName,
            description = "Старый формат реликвии",
            goldBonusPercent = rewardBonusPercent,
            resourceBonusPercent = rewardBonusPercent
        };

        AddRelic(relic);
    }

    public IReadOnlyList<RunRelic> GetRelics()
    {
        return activeRelics;
    }

    public void ApplyCrossroadsEffect(int bonusPercent, int penaltyPercent, int durationInNodes)
    {
        crossroadsBonusPercent = Mathf.Max(0, bonusPercent);
        crossroadsPenaltyPercent = Mathf.Max(0, penaltyPercent);
        crossroadsNodesRemaining = Mathf.Max(0, durationInNodes);
    }

    public void AdvanceNodeStep()
    {
        if (crossroadsNodesRemaining > 0)
        {
            crossroadsNodesRemaining--;
            if (crossroadsNodesRemaining <= 0)
            {
                crossroadsBonusPercent = 0;
                crossroadsPenaltyPercent = 0;
            }
        }
    }

    public void SetSeerRevealDepth(int revealDepth)
    {
        seerRevealDepth = Mathf.Max(0, revealDepth);
    }

    public void SetCardRewardBias(Element preferredElement)
    {
        hasCardRewardBias = true;
        cardRewardBias = preferredElement;
    }

    public int GetGoldRelicBonusPercent()
    {
        return SumRelicBonus(relic => relic.goldBonusPercent);
    }

    public int GetResourceRelicBonusPercent()
    {
        return SumRelicBonus(relic => relic.resourceBonusPercent);
    }

    public int GetHealingRelicBonusPercent()
    {
        return SumRelicBonus(relic => relic.healingBonusPercent);
    }

    public int GetPlayerDamageRelicBonusPercent()
    {
        return SumRelicBonus(relic => relic.playerDamageBonusPercent);
    }

    public int GetShopDiscountRelicBonusPercent()
    {
        return SumRelicBonus(relic => relic.shopDiscountPercent);
    }

    public int GetCardRewardRelicBonusPercent()
    {
        return SumRelicBonus(relic => relic.cardRewardBonusPercent);
    }

    public int ApplyHealModifiers(int baseHealing)
    {
        if (baseHealing <= 0)
        {
            return 0;
        }

        int percent = GetHealingRelicBonusPercent();
        return ApplyPercent(baseHealing, percent);
    }

    public int GetPlayerBattleDamageBonusPercent()
    {
        return GetPlayerDamageRelicBonusPercent();
    }

    public int GetModifiedShopPrice(int basePrice)
    {
        int safePrice = Mathf.Max(1, basePrice);
        int discountPercent = Mathf.Clamp(GetShopDiscountRelicBonusPercent(), 0, 80);
        return Mathf.Max(1, Mathf.RoundToInt(safePrice * (1f - discountPercent / 100f)));
    }

    public List<CardData> GetRandomCardChoices(int count, Func<CardData, bool> filter = null)
    {
        CardData[] allCards = Resources.LoadAll<CardData>("ScriptableObjects/Cards");
        List<CardData> pool = new List<CardData>();
        int rewardBias = GetCardRewardRelicBonusPercent();

        for (int i = 0; i < allCards.Length; i++)
        {
            CardData card = allCards[i];
            if (card == null)
            {
                continue;
            }

            if (filter != null && !filter(card))
            {
                continue;
            }

            pool.Add(card);
            if (hasCardRewardBias && card.CardElement == cardRewardBias)
            {
                pool.Add(card);
                pool.Add(card);
            }

            if (rewardBias > 0)
            {
                bool premiumCard = card.Cost >= 3 || card.Damage >= 4;
                if (premiumCard)
                {
                    int extraCopies = Mathf.Clamp(Mathf.CeilToInt(rewardBias / 10f), 1, 4);
                    for (int c = 0; c < extraCopies; c++)
                    {
                        pool.Add(card);
                    }
                }
            }
        }

        Shuffle(pool);
        int takeCount = Mathf.Clamp(count, 0, pool.Count);
        if (takeCount < pool.Count)
        {
            pool.RemoveRange(takeCount, pool.Count - takeCount);
        }

        return pool;
    }

    public void StoreCardInVault(CardData cardData)
    {
        if (cardData == null)
        {
            return;
        }

        storageVaultCards.Add(cardData);
    }

    public IReadOnlyList<CardData> GetStorageVaultCards()
    {
        return storageVaultCards;
    }

    public bool StoreSpecificCard(CardData cardData)
    {
        if (!RemoveSpecificRunBonusCard(cardData, rememberRemoval: false))
        {
            return false;
        }

        StoreCardInVault(cardData);
        return true;
    }

    public bool RetrieveSpecificVaultCard(CardData cardData)
    {
        if (cardData == null)
        {
            return false;
        }

        if (!storageVaultCards.Remove(cardData))
        {
            return false;
        }

        AddCardToRunDeck(cardData);
        return true;
    }

    public bool TryRetrieveCardFromVault(out CardData cardData)
    {
        cardData = null;
        if (storageVaultCards.Count == 0)
        {
            return false;
        }

        cardData = storageVaultCards[0];
        storageVaultCards.RemoveAt(0);
        return cardData != null;
    }

    public bool RemoveSpecificRunBonusCard(CardData cardData, bool rememberRemoval = true)
    {
        if (cardData == null)
        {
            return false;
        }

        if (!runBonusCards.Remove(cardData))
        {
            return false;
        }

        if (rememberRemoval)
        {
            removedCardsMemory.Add(cardData);
        }

        return true;
    }

    public bool ReplaceRunBonusCard(CardData oldCard, CardData newCard)
    {
        if (oldCard == null || newCard == null)
        {
            return false;
        }

        int index = runBonusCards.IndexOf(oldCard);
        if (index < 0)
        {
            return false;
        }

        runBonusCards[index] = newCard;
        return true;
    }

    public bool TryRemoveRunBonusCard(out CardData removedCard)
    {
        removedCard = null;
        if (runBonusCards.Count == 0)
        {
            return false;
        }

        int lastIndex = runBonusCards.Count - 1;
        removedCard = runBonusCards[lastIndex];
        return RemoveSpecificRunBonusCard(removedCard, rememberRemoval: true);
    }

    public bool TryRestoreRememberedCard()
    {
        if (removedCardsMemory.Count == 0)
        {
            return false;
        }

        int index = UnityEngine.Random.Range(0, removedCardsMemory.Count);
        CardData restored = removedCardsMemory[index];
        removedCardsMemory.RemoveAt(index);

        if (restored == null)
        {
            return false;
        }

        AddCardToRunDeck(restored);
        return true;
    }

    public bool TryGetRandomRunBonusCard(out CardData cardData)
    {
        cardData = null;
        if (runBonusCards.Count == 0)
        {
            return false;
        }

        int index = UnityEngine.Random.Range(0, runBonusCards.Count);
        cardData = runBonusCards[index];
        return cardData != null;
    }

    public void RegisterBattleResult(bool playerWon, int roundsPlayedInBattle)
    {
        if (playerWon)
        {
            battlesWon++;
        }
        else
        {
            battlesLost++;
        }

        totalRoundsPlayed += Mathf.Max(0, roundsPlayedInBattle);
    }

    public void RegisterDefeat(string reason)
    {
        if (isDefeatHandled)
        {
            return;
        }

        isDefeatHandled = true;
        runActive = false;
        lastDefeatReason = string.IsNullOrWhiteSpace(reason) ? "Герой пал в бою." : reason;

        SaveMetaProgress();
    }

    public void ClearDefeatReason()
    {
        lastDefeatReason = string.Empty;
    }

    public string BuildRunStatsSummary()
    {
        return $"Победы: {battlesWon} | Поражения: {battlesLost} | Раундов: {totalRoundsPlayed} | Монеты: {playerGold} | Ресурсы: {playerResources}";
    }

    public int GetMetaGold()
    {
        return PlayerPrefs.GetInt("MetaGold", 0);
    }

    public int GetMetaResources()
    {
        return PlayerPrefs.GetInt("MetaResources", 0);
    }

    private void SaveMetaProgress()
    {
        int carryGold = Mathf.FloorToInt(playerGold * MetaCarryOverRatio);
        int carryResources = Mathf.FloorToInt(playerResources * MetaCarryOverRatio);

        PlayerPrefs.SetInt("MetaGold", PlayerPrefs.GetInt("MetaGold", 0) + carryGold);
        PlayerPrefs.SetInt("MetaResources", PlayerPrefs.GetInt("MetaResources", 0) + carryResources);
        PlayerPrefs.Save();
    }

    private void NotifyHealthChanged()
    {
        OnPlayerHealthChanged?.Invoke(playerHP, maxHP);
    }

    private void NotifyCurrencyChanged()
    {
        OnCurrencyChanged?.Invoke(playerGold, playerResources);
    }

    private int ApplyGoldModifiers(int amount)
    {
        if (amount <= 0)
        {
            return Mathf.Min(0, amount);
        }

        int totalPercent = GetGoldRelicBonusPercent() + GetCrossroadsRewardModifierPercent();
        return ApplyPercent(amount, totalPercent);
    }

    private int ApplyResourceModifiers(int amount)
    {
        if (amount <= 0)
        {
            return Mathf.Min(0, amount);
        }

        int totalPercent = GetResourceRelicBonusPercent() + GetCrossroadsRewardModifierPercent();
        return ApplyPercent(amount, totalPercent);
    }

    private int GetCrossroadsRewardModifierPercent()
    {
        if (crossroadsNodesRemaining <= 0)
        {
            return 0;
        }

        return crossroadsBonusPercent - crossroadsPenaltyPercent;
    }

    private int SumRelicBonus(Func<RunRelic, int> selector)
    {
        int sum = 0;
        for (int i = 0; i < activeRelics.Count; i++)
        {
            RunRelic relic = activeRelics[i];
            if (relic == null)
            {
                continue;
            }

            sum += selector(relic);
        }

        return sum;
    }

    private int ApplyPercent(int amount, int percent)
    {
        if (amount == 0 || percent == 0)
        {
            return amount;
        }

        float multiplier = 1f + percent / 100f;
        return Mathf.Max(0, Mathf.RoundToInt(amount * multiplier));
    }

    private void Shuffle(List<CardData> cards)
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
    }

    public void OpenSettings()
    {
        GlobalSettingsMenu.ToggleFromCode();
    }

    public void OpenDeckBuilder()
    {
        SwitchToDeckBuildState();
        SceneLoader.LoadSceneAsync("DeckBuilder");
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SyncStateWithScene(scene.name);
    }

    private void SyncStateWithScene(string sceneName)
    {
        switch (sceneName)
        {
            case "MainMenu":
                SetGameState(GameState.Menu);
                break;
            case "DeckBuilder":
                SetGameState(GameState.DeckBuild);
                break;
            case "WorldMap":
                SetGameState(GameState.Map);
                break;
            case "CardSystemTest":
                SetGameState(GameState.Battle);
                break;
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
