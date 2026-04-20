using TMPro;
using UnityEngine;

public class BattleEnergyManager : MonoBehaviour
{
    [Header("Energy Rules")]
    [SerializeField] private int roundStartEnergy = 3;
    [SerializeField] private int energyPerTurn = 2;
    [SerializeField] private int maxEnergy = 10;
    [SerializeField] private bool useCardDataCost = false;
    [SerializeField] private int defaultCardCost = 1;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI energyText;
    [SerializeField] private string energyFormat = "Энергия: {0}/{1}";

    private int currentEnergy;

    public int CurrentEnergy => currentEnergy;
    public int MaxEnergy => maxEnergy;

    private void Start()
    {
        ResolveReferences();
        EnsureEnergyUiExists();
        BeginRound();
    }

    public void BeginRound()
    {
        currentEnergy = Mathf.Clamp(roundStartEnergy, 0, maxEnergy);
        RefreshUi();
    }

    public void GrantTurnEnergy()
    {
        AddEnergy(energyPerTurn);
    }

    public void AddEnergy(int amount)
    {
        if (amount == 0)
        {
            return;
        }

        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0, maxEnergy);
        RefreshUi();
    }

    public int GetCostForCard(Card card)
    {
        if (card == null)
        {
            return defaultCardCost;
        }

        if (useCardDataCost && card.CardData != null)
        {
            return Mathf.Max(1, card.CardData.Cost);
        }

        return Mathf.Max(1, defaultCardCost);
    }

    public bool CanSpendForCard(Card card)
    {
        int cost = GetCostForCard(card);
        return currentEnergy >= cost;
    }

    public bool TrySpendForCard(Card card)
    {
        int cost = GetCostForCard(card);
        if (currentEnergy < cost)
        {
            return false;
        }

        currentEnergy -= cost;
        RefreshUi();
        return true;
    }

    public void RefreshUi()
    {
        if (energyText != null)
        {
            energyText.text = string.Format(energyFormat, currentEnergy, maxEnergy);
        }
    }

    private void ResolveReferences()
    {
        if (energyText == null)
        {
            energyText = GameObject.Find("EnergyText")?.GetComponent<TextMeshProUGUI>();
        }
    }

    private void EnsureEnergyUiExists()
    {
        if (energyText != null)
        {
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        GameObject textGO = new GameObject("EnergyText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(canvas.transform, false);

        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -40f);
        rect.sizeDelta = new Vector2(280f, 38f);

        energyText = textGO.GetComponent<TextMeshProUGUI>();
        energyText.alignment = TextAlignmentOptions.Center;
        energyText.fontSize = 28f;
        energyText.color = Color.white;
        energyText.text = string.Format(energyFormat, currentEnergy, maxEnergy);
    }
}
