using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    [SerializeField] private CardData cardData;
    [SerializeField] private Image cardImage;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Image elementIconImage;
    [SerializeField] private bool isPlayerOwned = true;
    [SerializeField] private UnityEvent<Card> onPlayed;

    [Header("Damage Sprites")]
    [SerializeField] private Sprite fullHealthSprite;
    [SerializeField] private Sprite health75Sprite;
    [SerializeField] private Sprite health50Sprite;
    [SerializeField] private Sprite health25Sprite;

    public CardData CardData => cardData;
    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;
    public bool IsPlayerOwned => isPlayerOwned;

    private void Awake()
    {
        EnsureHealthTextExists();
        EnsureElementIconExists();

        if (cardData != null && CurrentHealth <= 0)
        {
            CurrentHealth = cardData.MaxHealth;
            UpdateVisualState();
        }
    }

    public void SetCardData(CardData data)
    {
        cardData = data;
        if (cardData == null)
        {
            Debug.LogError("[Card] CardData is null in SetCardData", this);
            return;
        }

        CurrentHealth = cardData.MaxHealth;
        UpdateVisualState();
    }

    public void SetOwner(bool playerOwned)
    {
        isPlayerOwned = playerOwned;
    }

    public void SetCurrentHealth(int healthValue)
    {
        if (cardData == null)
        {
            return;
        }

        CurrentHealth = Mathf.Clamp(healthValue, 0, cardData.MaxHealth);
        UpdateVisualState();
    }

    public void TakeDamage(int amount)
    {
        if (cardData == null || amount <= 0)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        UpdateVisualState();
    }

    public void HealToFull()
    {
        if (cardData == null)
        {
            return;
        }

        CurrentHealth = cardData.MaxHealth;
        UpdateVisualState();
    }

    public void Play()
    {
        onPlayed?.Invoke(this);
    }

    private void UpdateVisualState()
    {
        if (cardData == null)
        {
            return;
        }

        UpdateDamageSprite();
        UpdateHealthText();
        UpdateElementVisual();
    }

    private void UpdateDamageSprite()
    {
        if (cardImage == null)
        {
            return;
        }

        float healthRatio = cardData.MaxHealth > 0 ? (float)CurrentHealth / cardData.MaxHealth : 0f;
        Sprite targetSprite = cardData.CardArt;

        if (healthRatio <= 0.25f && health25Sprite != null)
        {
            targetSprite = health25Sprite;
        }
        else if (healthRatio <= 0.5f && health50Sprite != null)
        {
            targetSprite = health50Sprite;
        }
        else if (healthRatio <= 0.75f && health75Sprite != null)
        {
            targetSprite = health75Sprite;
        }
        else if (fullHealthSprite != null)
        {
            targetSprite = fullHealthSprite;
        }

        if (targetSprite != null)
        {
            cardImage.sprite = targetSprite;
        }
    }

    private void UpdateHealthText()
    {
        if (healthText == null || cardData == null)
        {
            return;
        }

        healthText.text = $"{CurrentHealth}/{cardData.MaxHealth}";
        healthText.color = ElementSystem.GetElementColor(cardData.CardElement);
    }

    private void UpdateElementVisual()
    {
        if (cardData == null)
        {
            return;
        }

        Color elementColor = ElementSystem.GetElementColor(cardData.CardElement);

        if (cardImage != null)
        {
            cardImage.color = Color.Lerp(Color.white, elementColor, 0.12f);
        }

        if (elementIconImage != null)
        {
            elementIconImage.sprite = cardData.ElementIcon;
            elementIconImage.color = elementColor;
            elementIconImage.gameObject.SetActive(cardData.ElementIcon != null);
        }
    }

    private void EnsureHealthTextExists()
    {
        if (healthText != null)
        {
            return;
        }

        Transform existing = transform.Find("HealthText");
        if (existing != null)
        {
            healthText = existing.GetComponent<TextMeshProUGUI>();
            if (healthText != null)
            {
                return;
            }
        }

        GameObject textGO = new GameObject("HealthText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(transform, false);

        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 8f);
        rect.sizeDelta = new Vector2(120f, 28f);

        healthText = textGO.GetComponent<TextMeshProUGUI>();
        healthText.text = "1/1";
        healthText.fontSize = 20f;
        healthText.alignment = TextAlignmentOptions.Center;
        healthText.color = Color.white;
        healthText.raycastTarget = false;
    }

    private void EnsureElementIconExists()
    {
        if (elementIconImage != null)
        {
            return;
        }

        Transform existing = transform.Find("ElementIcon");
        if (existing != null)
        {
            elementIconImage = existing.GetComponent<Image>();
            if (elementIconImage != null)
            {
                return;
            }
        }

        GameObject iconGO = new GameObject("ElementIcon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(transform, false);

        RectTransform rect = iconGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(6f, -6f);
        rect.sizeDelta = new Vector2(24f, 24f);

        elementIconImage = iconGO.GetComponent<Image>();
        elementIconImage.raycastTarget = false;
    }
}
