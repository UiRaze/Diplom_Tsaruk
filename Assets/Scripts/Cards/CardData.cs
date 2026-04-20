using UnityEngine;

[CreateAssetMenu(fileName = "Card_", menuName = "Cards/Card Data")]
public class CardData : ScriptableObject
{
    [SerializeField] private string cardId;
    [SerializeField] private string cardName;
    [SerializeField] private int cost = 1;
    [SerializeField] private int damage = 1;
    [SerializeField] private int health = 1;
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private Sprite cardArt;
    [SerializeField] private Element cardElement = Element.Water;
    [SerializeField] private Sprite elementIcon;

    public string CardId => string.IsNullOrWhiteSpace(cardId) ? name : cardId;
    public string CardName => cardName;
    public int Cost => Mathf.Clamp(cost, 1, 5);
    public int Damage => Mathf.Clamp(damage, 1, 6);
    public int Health => Mathf.Clamp(health, 1, 5);
    public int MaxHealth => Mathf.Clamp(Mathf.Max(maxHealth, health), 1, 8);
    public Sprite CardArt => cardArt;
    public Element CardElement => cardElement;
    public Sprite ElementIcon => elementIcon;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            cardId = name;
        }

        cost = Mathf.Clamp(cost, 1, 5);
        damage = Mathf.Clamp(damage, 1, 6);
        health = Mathf.Clamp(health, 1, 8);
        maxHealth = Mathf.Clamp(maxHealth, 1, 8);

        if (maxHealth < health)
        {
            maxHealth = health;
        }
    }
}
