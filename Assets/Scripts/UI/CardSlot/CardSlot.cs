using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardSlot : MonoBehaviour, IDropHandler
{
    [SerializeField] private Card currentCard;
    [SerializeField] private CardManager cardManager;
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private bool canAcceptCards = true;
    [SerializeField] public bool isPlayerField = true;
    [SerializeField] private Image hoverAdvantageIndicator;

    public Card CurrentCard => currentCard;
    public bool HasCard => currentCard != null;
    public bool CanAcceptCards => canAcceptCards;

    private void Awake()
    {
        if (roundManager == null)
        {
            roundManager = FindObjectOfType<RoundManager>();
        }

        EnsureHoverIndicator();
    }

    public void ResetSlot()
    {
        currentCard = null;
        HideElementAdvantage();
    }

    public void SetCanAcceptCards(bool canAccept)
    {
        canAcceptCards = canAccept;
    }

    public bool CanAcceptCard(Card card)
    {
        if (!canAcceptCards || card == null || currentCard != null)
        {
            return false;
        }

        if (!card.IsPlayerOwned)
        {
            return false;
        }

        if (!isPlayerField)
        {
            return false;
        }

        if (roundManager != null && !roundManager.CanPlayCard(card))
        {
            return false;
        }

        return true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
        {
            return;
        }

        Card card = eventData.pointerDrag.GetComponent<Card>();
        if (card == null)
        {
            return;
        }

        PlaceCard(card);
    }

    public bool PlaceCard(Card card)
    {
        if (card == null)
        {
            return false;
        }

        if (card.IsPlayerOwned)
        {
            if (!CanAcceptCard(card))
            {
                return false;
            }

            if (roundManager != null && !roundManager.TrySpendEnergyForCard(card))
            {
                return false;
            }
        }
        else if (currentCard != null)
        {
            return false;
        }

        card.transform.SetParent(transform);
        card.transform.localPosition = Vector3.zero;
        card.transform.localScale = Vector3.one;
        currentCard = card;

        CardDragHandler dragHandler = card.GetComponent<CardDragHandler>();
        if (dragHandler != null)
        {
            dragHandler.enabled = false;
        }

        HideElementAdvantage();

        if (cardManager != null && card.IsPlayerOwned)
        {
            cardManager.OnCardPlayed(card);
        }

        return true;
    }

    public Card RemoveCard()
    {
        Card removedCard = currentCard;
        currentCard = null;
        HideElementAdvantage();
        return removedCard;
    }

    public void ForceRemoveCard()
    {
        if (currentCard == null)
        {
            return;
        }

        Destroy(currentCard.gameObject);
        currentCard = null;
        HideElementAdvantage();
    }

    public void SetInteractable(bool interactable)
    {
        canAcceptCards = interactable;
    }

    public void ShowElementAdvantage(Card attackerCard)
    {
        if (hoverAdvantageIndicator == null)
        {
            return;
        }

        if (attackerCard == null || attackerCard.CardData == null || currentCard == null || currentCard.CardData == null)
        {
            hoverAdvantageIndicator.gameObject.SetActive(false);
            return;
        }

        float multiplier = ElementSystem.GetDamageMultiplier(attackerCard.CardData.CardElement, currentCard.CardData.CardElement);

        Color color;
        if (multiplier > 1f)
        {
            color = new Color(0.1f, 0.85f, 0.2f, 0.35f);
        }
        else if (multiplier < 1f)
        {
            color = new Color(0.9f, 0.2f, 0.2f, 0.35f);
        }
        else
        {
            color = new Color(0.95f, 0.85f, 0.2f, 0.3f);
        }

        hoverAdvantageIndicator.color = color;
        hoverAdvantageIndicator.gameObject.SetActive(true);
    }

    public void HideElementAdvantage()
    {
        if (hoverAdvantageIndicator != null)
        {
            hoverAdvantageIndicator.gameObject.SetActive(false);
        }
    }

    private void EnsureHoverIndicator()
    {
        if (hoverAdvantageIndicator != null)
        {
            hoverAdvantageIndicator.gameObject.SetActive(false);
            return;
        }

        Transform existing = transform.Find("ElementAdvantageIndicator");
        if (existing != null)
        {
            hoverAdvantageIndicator = existing.GetComponent<Image>();
            if (hoverAdvantageIndicator != null)
            {
                hoverAdvantageIndicator.gameObject.SetActive(false);
                return;
            }
        }

        GameObject indicatorGO = new GameObject("ElementAdvantageIndicator", typeof(RectTransform), typeof(Image));
        indicatorGO.transform.SetParent(transform, false);

        RectTransform rect = indicatorGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        hoverAdvantageIndicator = indicatorGO.GetComponent<Image>();
        hoverAdvantageIndicator.color = new Color(1f, 1f, 1f, 0.25f);
        hoverAdvantageIndicator.raycastTarget = false;
        hoverAdvantageIndicator.gameObject.SetActive(false);
    }
}
