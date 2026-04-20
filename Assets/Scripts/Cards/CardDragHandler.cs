using UnityEngine;
using UnityEngine.EventSystems;

public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Card card;

    private Transform originalParent;
    private Vector3 originalLocalPosition;
    private Vector3 originalLocalScale;
    private CardSlot highlightedEnemySlot;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        card = GetComponent<Card>();

        originalLocalScale = transform.localScale;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (card != null && !card.IsPlayerOwned)
        {
            return;
        }

        originalParent = transform.parent;
        originalLocalPosition = transform.localPosition;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }

        transform.SetParent(transform.root);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition += eventData.delta;
        }

        UpdateHoverIndicator(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        ClearHoverIndicator();

        if (eventData.pointerEnter != null)
        {
            CardSlot dropTarget = eventData.pointerEnter.GetComponentInParent<CardSlot>();
            bool alreadyPlacedByDropHandler = dropTarget != null && dropTarget.CurrentCard == card;
            if (dropTarget != null)
            {
                bool placedSuccessfully = alreadyPlacedByDropHandler;

                if (!alreadyPlacedByDropHandler)
                {
                    placedSuccessfully = dropTarget.PlaceCard(card);
                }

                if (placedSuccessfully)
                {
                    transform.localScale = originalLocalScale;
                    if (!alreadyPlacedByDropHandler)
                    {
                        transform.localPosition = Vector3.zero;
                    }

                    if (canvasGroup != null)
                    {
                        canvasGroup.blocksRaycasts = true;
                    }

                    ClearHoverIndicator();

                    enabled = false;
                    return;
                }
            }
        }

        ReturnToOriginalPosition();
    }

    private void UpdateHoverIndicator(PointerEventData eventData)
    {
        if (card == null || card.CardData == null || !card.IsPlayerOwned)
        {
            return;
        }

        CardSlot slotUnderPointer = eventData.pointerEnter != null
            ? eventData.pointerEnter.GetComponentInParent<CardSlot>()
            : null;

        if (highlightedEnemySlot != null && highlightedEnemySlot != slotUnderPointer)
        {
            highlightedEnemySlot.HideElementAdvantage();
            highlightedEnemySlot = null;
        }

        if (slotUnderPointer != null && !slotUnderPointer.isPlayerField && slotUnderPointer.HasCard)
        {
            highlightedEnemySlot = slotUnderPointer;
            highlightedEnemySlot.ShowElementAdvantage(card);
        }
    }

    private void ClearHoverIndicator()
    {
        if (highlightedEnemySlot != null)
        {
            highlightedEnemySlot.HideElementAdvantage();
            highlightedEnemySlot = null;
        }
    }

    private void ReturnToOriginalPosition()
    {
        transform.SetParent(originalParent);
        transform.localPosition = originalLocalPosition;
        transform.localScale = originalLocalScale;
    }
}
