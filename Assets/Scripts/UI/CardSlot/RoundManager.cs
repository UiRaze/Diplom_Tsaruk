
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoundManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI roundInfoText;
    [SerializeField] private TextMeshProUGUI roundResultText;
    [SerializeField] private TextMeshProUGUI energyText;
    [SerializeField] private TextMeshProUGUI playerHealthText;
    [SerializeField] private TextMeshProUGUI enemyHealthText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI resourcesText;
    [SerializeField] private Image playerHealthFill;
    [SerializeField] private Image enemyHealthFill;
    [SerializeField] private Image damageFlashOverlay;
    [SerializeField] private Button passButton;

    [Header("Fields")]
    [SerializeField] private Transform playerField;
    [SerializeField] private Transform enemyField;
    [SerializeField] private Transform playerHandPanel;
    [SerializeField] private Transform enemyHandPanel;

    [Header("Dependencies")]
    [SerializeField] private Hand playerHand;
    [SerializeField] private TurnSystem turnSystem;
    [SerializeField] private Enemy enemy;
    [SerializeField] private DiscardPileManager discardPileManager;

    [Header("Settings")]
    [SerializeField] private int totalRounds = 5;
    [SerializeField] private int winsNeeded = 3;
    [SerializeField] private int startingHealth = 20;
    [SerializeField] private int maxHandSize = 4;
    [SerializeField] private int maxBattleLanes = 3;

    [Header("Energy")]
    [SerializeField] private int roundStartEnergy = 2;
    [SerializeField] private int passEnergyBonus = 2;
    [SerializeField] private int cardEnergyCost = 1;
    [SerializeField] private int maxEnergy = 10;

    [Header("Scene Names")]
    [SerializeField] private string winSceneName = "WorldMap";
    [SerializeField] private string loseSceneName = "LoseScene";

    private readonly List<CardSlot> playerBattleSlots = new List<CardSlot>();
    private readonly List<CardSlot> enemyBattleSlots = new List<CardSlot>();
    private readonly Dictionary<CardSlot, SlotModifier> slotModifiers = new Dictionary<CardSlot, SlotModifier>();
    private readonly HashSet<int> pendingEnergyApprovedCards = new HashSet<int>();

    private int currentRound = 1;
    private int playerWins;
    private int enemyWins;
    private int playerHealth;
    private int enemyHealth;
    private int playerRoundScore;
    private int enemyRoundScore;
    private int playerEnergy;
    private int nextRoundEnergyBonus;

    private bool playerPassed;
    private bool enemyPassed;
    private bool isRoundEnding;
    private bool isBattleEnded;
    private bool endSceneStarted;

    public int CurrentRound => currentRound;
    public bool PlayerHasPassed => playerPassed;
    public bool EnemyHasPassed => enemyPassed;
    public bool IsRoundEnding => isRoundEnding;
    public IReadOnlyList<CardSlot> EnemyBattleSlots => enemyBattleSlots;

    private void Start()
    {
        ResolveReferences();
        CacheBattleSlots();
        int configuredRounds = GameConfig.TOTAL_ROUNDS;
        if (GameManager.Instance != null && GameManager.Instance.PendingBattleRoundLimit > 0)
        {
            configuredRounds = GameManager.Instance.PendingBattleRoundLimit;
        }

        totalRounds = Mathf.Max(1, configuredRounds);
        winsNeeded = Mathf.Max(1, Mathf.CeilToInt(totalRounds * 0.6f));

        playerHealth = GameManager.Instance != null ? GameManager.Instance.PlayerHP : startingHealth;
        enemyHealth = startingHealth;

        NodeType pendingBattleType = GameManager.Instance != null
            ? GameManager.Instance.PendingBattleNodeType
            : NodeType.NormalBattle;

        switch (pendingBattleType)
        {
            case NodeType.Elite:
                enemyHealth = Mathf.RoundToInt(startingHealth * 1.4f);
                break;
            case NodeType.Boss:
                enemyHealth = Mathf.RoundToInt(startingHealth * 1.8f);
                break;
            case NodeType.TrialGate:
                enemyHealth = Mathf.RoundToInt(startingHealth * 1.25f);
                break;
        }
        playerEnergy = Mathf.Clamp(roundStartEnergy, 0, maxEnergy);
        nextRoundEnergyBonus = 0;

        if (passButton != null)
        {
            passButton.onClick.RemoveListener(OnPassClicked);
            passButton.onClick.AddListener(OnPassClicked);
        }

        discardPileManager?.ClearForNewBattle();
        enemy?.ResetForNextRound();
        enemy?.DrawToHandSize(maxHandSize);
        RollSlotModifiers();
        RefreshUi();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;
        }

        EnablePlayerActions(true);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;
        }
    }

    public void EnablePlayerActions(bool enable)
    {
        bool canAct = enable && !playerPassed && !isRoundEnding && !isBattleEnded && (turnSystem == null || turnSystem.IsPlayerTurn());

        if (passButton != null)
        {
            passButton.interactable = canAct;
        }

        for (int i = 0; i < playerBattleSlots.Count; i++)
        {
            CardSlot slot = playerBattleSlots[i];
            if (slot != null)
            {
                slot.SetCanAcceptCards(canAct && !slot.HasCard);
            }
        }
    }

    public bool CanPlayCard(Card card)
    {
        if (card == null || !card.IsPlayerOwned || isRoundEnding || isBattleEnded || playerPassed)
        {
            return false;
        }

        if (turnSystem != null && !turnSystem.IsPlayerTurn())
        {
            return false;
        }

        int cost = GetCardCost(card);
        return playerEnergy >= cost;
    }

    public bool TrySpendEnergyForCard(Card card)
    {
        if (!CanPlayCard(card))
        {
            return false;
        }

        int cost = GetCardCost(card);
        playerEnergy = Mathf.Max(0, playerEnergy - cost);
        pendingEnergyApprovedCards.Add(card.GetInstanceID());
        UpdateEnergyUi();
        return true;
    }

    public bool ConsumeEnergyApproval(Card card)
    {
        if (card == null)
        {
            return false;
        }

        return pendingEnergyApprovedCards.Remove(card.GetInstanceID());
    }

    public void OnPlayerPlayedCard()
    {
        if (!isRoundEnding && !isBattleEnded)
        {
            EnablePlayerActions(true);
        }
    }

    public void OnEnemyPassed()
    {
        if (enemyPassed)
        {
            return;
        }

        enemyPassed = true;
        if (playerPassed && !isRoundEnding)
        {
            StartCoroutine(EndRoundCoroutine());
        }
    }

    private void OnPassClicked()
    {
        if (isRoundEnding || isBattleEnded || playerPassed || turnSystem == null || !turnSystem.IsPlayerTurn())
        {
            return;
        }

        playerPassed = true;
        GameEventBus.RaisePlayerPass();
        int maxBonus = Mathf.Max(0, maxEnergy - roundStartEnergy);
        nextRoundEnergyBonus = Mathf.Clamp(nextRoundEnergyBonus + passEnergyBonus, 0, maxBonus);
        playerEnergy = Mathf.Clamp(roundStartEnergy + nextRoundEnergyBonus, 0, maxEnergy);
        UpdateEnergyUi();

        enemy?.NotifyPlayerPassed();
        EnablePlayerActions(false);

        if (enemy != null)
        {
            turnSystem.EndPlayerTurn();
        }
        else
        {
            OnEnemyPassed();
        }
    }
    private IEnumerator EndRoundCoroutine()
    {
        isRoundEnding = true;
        EnablePlayerActions(false);

        yield return new WaitForSeconds(0.2f);

        ResolveCombat();
        RefreshUi();

        if (isBattleEnded)
        {
            yield break;
        }

        DetermineRoundWinner();
        GameEventBus.RaiseRoundEnd();
        UpdateRoundInfo();

        yield return new WaitForSeconds(1f);

        if (playerWins >= winsNeeded)
        {
            EndBattle(true, "Победа в битве!");
            yield break;
        }

        if (enemyWins >= winsNeeded)
        {
            EndBattle(false, "Поражение в битве!");
            yield break;
        }

        if (currentRound >= totalRounds)
        {
            bool playerWon = playerWins > enemyWins || (playerWins == enemyWins && playerHealth >= enemyHealth);
            EndBattle(playerWon, playerWon ? "Победа по итогам раундов." : "Поражение по итогам раундов.");
            yield break;
        }

        StartNextRound();
    }

    private void ResolveCombat()
    {
        playerRoundScore = 0;
        enemyRoundScore = 0;

        int laneCount = Mathf.Min(maxBattleLanes, Mathf.Min(playerBattleSlots.Count, enemyBattleSlots.Count));
        for (int lane = 0; lane < laneCount; lane++)
        {
            CardSlot playerSlot = playerBattleSlots[lane];
            CardSlot enemySlot = enemyBattleSlots[lane];

            Card playerCard = playerSlot != null ? playerSlot.RemoveCard() : null;
            Card enemyCard = enemySlot != null ? enemySlot.RemoveCard() : null;

            if (playerCard != null)
            {
                playerRoundScore += GetCardCost(playerCard);
            }

            if (enemyCard != null)
            {
                enemyRoundScore += GetCardCost(enemyCard);
            }

            SlotModifier playerModifier = GetSlotModifier(playerSlot);
            SlotModifier enemyModifier = GetSlotModifier(enemySlot);

            if (playerCard != null && enemyCard != null)
            {
                ResolveCardVsCard(playerCard, enemyCard, playerModifier, enemyModifier);

                ResolveCardAfterCombat(playerCard, true, playerModifier);
                ResolveCardAfterCombat(enemyCard, false, enemyModifier);
            }
            else if (playerCard != null)
            {
                ResolveCardVsHero(playerCard, playerModifier, enemyTarget: true);
                ResolveCardAfterCombat(playerCard, true, playerModifier);
            }
            else if (enemyCard != null)
            {
                ResolveCardVsHero(enemyCard, enemyModifier, enemyTarget: false);
                ResolveCardAfterCombat(enemyCard, false, enemyModifier);
            }

            if (isBattleEnded)
            {
                break;
            }
        }

        if (isBattleEnded)
        {
            bool playerWon = enemyHealth <= 0 && playerHealth > 0;
            EndBattle(playerWon, playerWon ? "Враг повержен по HP." : "Игрок побеждён по HP.");
        }
    }

    private void ResolveCardVsCard(Card playerCard, Card enemyCard, SlotModifier playerModifier, SlotModifier enemyModifier)
    {
        int playerHit = CalculateCardDamage(playerCard, enemyCard, playerModifier);
        int enemyHit = CalculateCardDamage(enemyCard, playerCard, enemyModifier);

        ApplyDamageToCard(enemyCard, playerHit, enemyModifier);
        ApplyDamageToCard(playerCard, enemyHit, playerModifier);

        if (!playerCard.IsDead && playerModifier == SlotModifier.DoubleStrike)
        {
            int second = CalculateCardDamage(playerCard, enemyCard.IsDead ? null : enemyCard, playerModifier);
            if (enemyCard.IsDead) ApplyDamageToEnemy(second); else ApplyDamageToCard(enemyCard, second, enemyModifier);
        }

        if (!enemyCard.IsDead && enemyModifier == SlotModifier.DoubleStrike)
        {
            int second = CalculateCardDamage(enemyCard, playerCard.IsDead ? null : playerCard, enemyModifier);
            if (playerCard.IsDead) ApplyDamageToPlayer(second); else ApplyDamageToCard(playerCard, second, playerModifier);
        }
    }

    private void ResolveCardVsHero(Card card, SlotModifier modifier, bool enemyTarget)
    {
        int first = CalculateCardDamage(card, null, modifier);

        if (enemyTarget)
        {
            ApplyDamageToEnemy(first);
        }
        else
        {
            ApplyDamageToPlayer(first);
        }

        if (!card.IsDead && modifier == SlotModifier.DoubleStrike)
        {
            int second = CalculateCardDamage(card, null, modifier);
            if (enemyTarget)
            {
                ApplyDamageToEnemy(second);
            }
            else
            {
                ApplyDamageToPlayer(second);
            }
        }
    }

    private void ApplyDamageToCard(Card card, int incomingDamage, SlotModifier modifier)
    {
        if (card == null || incomingDamage <= 0)
        {
            return;
        }

        if (modifier == SlotModifier.Inversion)
        {
            int inversionHealth = card.CardData != null ? Mathf.Max(1, card.CardData.Damage) : 1;
            int remaining = inversionHealth - incomingDamage;
            if (remaining <= 0) card.SetCurrentHealth(0);
            else card.SetCurrentHealth(Mathf.Clamp(remaining, 1, card.CardData.MaxHealth));
            return;
        }

        card.TakeDamage(incomingDamage);
    }

    private int CalculateCardDamage(Card attacker, Card defender, SlotModifier modifier)
    {
        if (attacker == null || attacker.CardData == null)
        {
            return 0;
        }

        int baseDamage = modifier == SlotModifier.Inversion ? Mathf.Max(1, attacker.CurrentHealth) : attacker.CardData.Damage;
        float multiplier = 1f;

        if (defender != null && defender.CardData != null)
        {
            multiplier = ElementSystem.GetDamageMultiplier(attacker.CardData.CardElement, defender.CardData.CardElement);
        }

        int damage = Mathf.Max(0, Mathf.RoundToInt(baseDamage * multiplier));

        if (attacker.IsPlayerOwned && GameManager.Instance != null)
        {
            int bonusPercent = GameManager.Instance.GetPlayerBattleDamageBonusPercent();
            if (bonusPercent != 0)
            {
                damage = Mathf.Max(0, Mathf.RoundToInt(damage * (1f + bonusPercent / 100f)));
            }
        }

        return damage;
    }
    private void DetermineRoundWinner()
    {
        string result;

        if (playerRoundScore > enemyRoundScore)
        {
            playerWins++;
            result = $"Раунд {currentRound}: Победа! ({playerRoundScore} > {enemyRoundScore})";
        }
        else if (enemyRoundScore > playerRoundScore)
        {
            enemyWins++;
            result = $"Раунд {currentRound}: Поражение! ({enemyRoundScore} > {playerRoundScore})";
        }
        else
        {
            result = $"Раунд {currentRound}: Ничья. ({playerRoundScore} = {enemyRoundScore})";
        }

        if (roundResultText != null)
        {
            roundResultText.text = result;
        }
    }

    private void StartNextRound()
    {
        currentRound++;
        playerPassed = false;
        enemyPassed = false;
        isRoundEnding = false;
        pendingEnergyApprovedCards.Clear();

        if (roundResultText != null)
        {
            roundResultText.text = string.Empty;
        }

        CleanupUnexpectedCards();

        playerHand?.DrawToHandSize(maxHandSize);
        enemy?.ResetForNextRound();
        enemy?.DrawToHandSize(maxHandSize);

        playerEnergy = Mathf.Clamp(roundStartEnergy + nextRoundEnergyBonus, 0, maxEnergy);
        RollSlotModifiers();

        RefreshUi();

        turnSystem?.ResetTurn();
        EnablePlayerActions(true);
    }

    private void ResolveCardAfterCombat(Card card, bool isPlayerCard, SlotModifier modifier)
    {
        if (card == null)
        {
            return;
        }

        if (card.IsDead)
        {
            if (discardPileManager != null) discardPileManager.AddCard(card);
            else Destroy(card.gameObject);
            return;
        }

        if (modifier == SlotModifier.Recovery)
        {
            card.HealToFull();
        }

        if (isPlayerCard)
        {
            ReturnPlayerCardToHand(card);
        }
        else if (enemy != null)
        {
            enemy.RegisterReturnedCard(card);
        }
        else
        {
            card.transform.SetParent(enemyHandPanel, false);
        }
    }

    private void ReturnPlayerCardToHand(Card card)
    {
        if (card == null || playerHandPanel == null)
        {
            return;
        }

        card.SetOwner(true);
        card.transform.SetParent(playerHandPanel, false);
        card.transform.localPosition = Vector3.zero;
        card.transform.localScale = Vector3.one;

        CardDragHandler dragHandler = card.GetComponent<CardDragHandler>();
        if (dragHandler != null)
        {
            dragHandler.enabled = true;
        }

        CanvasGroup canvasGroup = card.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }
    }

    private void ApplyDamageToPlayer(int damage)
    {
        if (damage <= 0 || isBattleEnded)
        {
            return;
        }

        playerHealth = Mathf.Max(0, playerHealth - damage);
        GameManager.Instance?.SetPlayerHP(playerHealth);
        UpdateHealthUi();

        if (playerHealth <= 0)
        {
            isBattleEnded = true;
        }
    }

    private void ApplyDamageToEnemy(int damage)
    {
        if (damage <= 0 || isBattleEnded)
        {
            return;
        }

        enemyHealth = Mathf.Max(0, enemyHealth - damage);
        UpdateHealthUi();

        if (enemyHealth <= 0)
        {
            isBattleEnded = true;
        }
    }

    private void EndBattle(bool playerWon, string resultMessage)
    {
        if (endSceneStarted)
        {
            return;
        }

        isBattleEnded = true;
        isRoundEnding = true;
        endSceneStarted = true;

        EnablePlayerActions(false);

        if (roundResultText != null)
        {
            roundResultText.text = resultMessage;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterBattleResult(playerWon, currentRound);
            GameManager.Instance.ResolveBattleNodeResult(playerWon);
            GameManager.Instance.SetPlayerHP(playerHealth);

            if (!playerWon)
            {
                GameManager.Instance.RegisterDefeat(resultMessage);
            }
        }

        string sceneToLoad = playerWon ? winSceneName : loseSceneName;
        StartCoroutine(LoadSceneWithDelay(sceneToLoad, playerWon));
    }

    private IEnumerator LoadSceneWithDelay(string sceneName, bool playerWon)
    {
        yield return new WaitForSeconds(1f);

        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneLoader.LoadSceneAsync(sceneName);
        }
        else
        {
            SceneLoader.LoadSceneAsync(playerWon ? "WorldMap" : "MainMenu");
        }
    }

    private void RefreshUi()
    {
        UpdateRoundInfo();
        UpdateHealthUi();
        UpdateEnergyUi();
        UpdateCurrencyUi();
    }

    private void UpdateRoundInfo()
    {
        if (roundInfoText != null)
        {
            roundInfoText.text = $"Раунд {currentRound}/{totalRounds} | Победы {playerWins}/{winsNeeded} | HP {playerHealth}:{enemyHealth}";
        }
    }

    private void UpdateHealthUi()
    {
        if (playerHealthText != null) playerHealthText.text = $"HP: {playerHealth}/{startingHealth}";
        if (enemyHealthText != null) enemyHealthText.text = $"HP: {enemyHealth}/{startingHealth}";

        if (playerHealthFill != null)
        {
            float ratio = Mathf.Clamp01(playerHealth / (float)Mathf.Max(1, startingHealth));
            playerHealthFill.fillAmount = ratio;
            playerHealthFill.color = GetHealthColor(ratio);
        }

        if (enemyHealthFill != null)
        {
            float ratio = Mathf.Clamp01(enemyHealth / (float)Mathf.Max(1, startingHealth));
            enemyHealthFill.fillAmount = ratio;
            enemyHealthFill.color = GetHealthColor(ratio);
        }
    }

    private void UpdateEnergyUi()
    {
        if (energyText != null)
        {
            energyText.text = $"Энергия: {playerEnergy}/{maxEnergy}";
        }
    }

    private void UpdateCurrencyUi()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (goldText != null) goldText.text = $"Монеты: {GameManager.Instance.PlayerGold}";
        if (resourcesText != null) resourcesText.text = $"Ресурсы: {GameManager.Instance.PlayerResources}";
    }

    private Color GetHealthColor(float ratio)
    {
        if (ratio > 0.5f) return new Color(0.2f, 0.8f, 0.2f, 1f);
        if (ratio > 0.25f) return new Color(0.95f, 0.75f, 0.15f, 1f);
        return new Color(0.9f, 0.2f, 0.2f, 1f);
    }
    private void CacheBattleSlots()
    {
        playerBattleSlots.Clear();
        enemyBattleSlots.Clear();

        List<CardSlot> playerSlots = GetSortedSlots(playerField);
        List<CardSlot> enemySlots = GetSortedSlots(enemyField);

        for (int i = 0; i < playerSlots.Count; i++)
        {
            CardSlot slot = playerSlots[i];
            slot.SetCanAcceptCards(false);
            if (slot.isPlayerField && playerBattleSlots.Count < maxBattleLanes)
            {
                playerBattleSlots.Add(slot);
                slot.gameObject.SetActive(true);
            }
            else
            {
                slot.gameObject.SetActive(false);
            }
        }

        for (int i = 0; i < enemySlots.Count; i++)
        {
            CardSlot slot = enemySlots[i];
            slot.SetCanAcceptCards(false);
            if (!slot.isPlayerField && enemyBattleSlots.Count < maxBattleLanes)
            {
                enemyBattleSlots.Add(slot);
                slot.gameObject.SetActive(true);
            }
            else
            {
                slot.gameObject.SetActive(false);
            }
        }
    }

    private void RollSlotModifiers()
    {
        slotModifiers.Clear();

        for (int i = 0; i < playerBattleSlots.Count; i++) AssignModifier(playerBattleSlots[i]);
        for (int i = 0; i < enemyBattleSlots.Count; i++) AssignModifier(enemyBattleSlots[i]);
    }

    private void AssignModifier(CardSlot slot)
    {
        if (slot == null) return;

        SlotModifier modifier = SlotModifierLogic.RollD4();
        slotModifiers[slot] = modifier;

        Transform badgeTransform = slot.transform.Find("SlotModifierBadge");
        SlotModifierUI badge = badgeTransform != null ? badgeTransform.GetComponent<SlotModifierUI>() : null;

        if (badge == null)
        {
            GameObject badgeGO = new GameObject("SlotModifierBadge", typeof(RectTransform), typeof(Image), typeof(SlotModifierUI));
            badgeGO.transform.SetParent(slot.transform, false);
            badge = badgeGO.GetComponent<SlotModifierUI>();
        }

        badge.SetModifier(modifier);
    }

    private SlotModifier GetSlotModifier(CardSlot slot)
    {
        if (slot != null && slotModifiers.TryGetValue(slot, out SlotModifier modifier)) return modifier;
        return SlotModifier.Stability;
    }

    private void CleanupUnexpectedCards()
    {
        for (int i = 0; i < playerBattleSlots.Count; i++)
        {
            CardSlot slot = playerBattleSlots[i];
            if (slot != null && slot.HasCard)
            {
                ResolveCardAfterCombat(slot.RemoveCard(), true, GetSlotModifier(slot));
            }
        }

        for (int i = 0; i < enemyBattleSlots.Count; i++)
        {
            CardSlot slot = enemyBattleSlots[i];
            if (slot != null && slot.HasCard)
            {
                ResolveCardAfterCombat(slot.RemoveCard(), false, GetSlotModifier(slot));
            }
        }
    }

    private int GetCardCost(Card card)
    {
        return card != null && card.CardData != null ? card.CardData.Cost : 0;
    }

    private void ResolveReferences()
    {
        if (playerHand == null) playerHand = FindObjectOfType<Hand>();
        if (turnSystem == null) turnSystem = FindObjectOfType<TurnSystem>();
        if (enemy == null) enemy = FindObjectOfType<Enemy>();
        if (discardPileManager == null) discardPileManager = FindObjectOfType<DiscardPileManager>();

        if (playerField == null) playerField = GameObject.Find("Battlefield")?.transform;
        if (enemyField == null) enemyField = GameObject.Find("EnemyField")?.transform;
        if (playerHandPanel == null) playerHandPanel = GameObject.Find("HandPanel")?.transform;
        if (enemyHandPanel == null) enemyHandPanel = GameObject.Find("EnemyHandPanel")?.transform;

        if (roundInfoText == null) roundInfoText = GameObject.Find("RoundInfoText")?.GetComponent<TextMeshProUGUI>();
        if (roundResultText == null) roundResultText = GameObject.Find("RoundResultText")?.GetComponent<TextMeshProUGUI>();
        if (energyText == null) energyText = GameObject.Find("EnergyText")?.GetComponent<TextMeshProUGUI>();
        if (playerHealthText == null) playerHealthText = GameObject.Find("PlayerHealthText")?.GetComponent<TextMeshProUGUI>();
        if (enemyHealthText == null) enemyHealthText = GameObject.Find("EnemyHealthText")?.GetComponent<TextMeshProUGUI>();
        if (goldText == null) goldText = GameObject.Find("GoldText")?.GetComponent<TextMeshProUGUI>();
        if (resourcesText == null) resourcesText = GameObject.Find("ResourcesText")?.GetComponent<TextMeshProUGUI>();
        if (playerHealthFill == null) playerHealthFill = GameObject.Find("PlayerHealthFill")?.GetComponent<Image>();
        if (enemyHealthFill == null) enemyHealthFill = GameObject.Find("EnemyHealthFill")?.GetComponent<Image>();
        if (damageFlashOverlay == null) damageFlashOverlay = GameObject.Find("DamageFlashOverlay")?.GetComponent<Image>();
        if (passButton == null) passButton = GameObject.Find("PassButton")?.GetComponent<Button>();
    }

    private List<CardSlot> GetSortedSlots(Transform fieldRoot)
    {
        List<CardSlot> result = new List<CardSlot>();
        if (fieldRoot == null) return result;

        CardSlot[] found = fieldRoot.GetComponentsInChildren<CardSlot>(true);
        result.AddRange(found);
        result.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
        return result;
    }

    private void HandleCurrencyChanged(int gold, int resources)
    {
        UpdateCurrencyUi();
    }
}
