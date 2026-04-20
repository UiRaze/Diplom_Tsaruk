using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Deck deck;
    [SerializeField] private Hand hand;
    [SerializeField] private Transform handPanel;
    [SerializeField] private Transform battlefield;
    [SerializeField] private Transform enemyField;
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private TurnSystem turnSystem;
    [SerializeField] private List<CardPlayEffectHandler> playEffectHandlers = new List<CardPlayEffectHandler>();

    public RoundManager RoundManager => roundManager;
    public TurnSystem TurnSystem => turnSystem;

    private void Start()
    {
        if (hand == null || deck == null || cardPrefab == null || handPanel == null || roundManager == null)
        {
            Debug.LogError("[CardManager] Not all required references are set in the inspector", this);
            return;
        }

        if (turnSystem == null)
        {
            turnSystem = FindObjectOfType<TurnSystem>();
        }

        ResolveEffectHandlers();
        hand.Initialize(cardPrefab, handPanel);
    }

    // Вызывается из CardDragHandler, когда карта "прилипла"
    public void OnCardPlayed(Card card)
    {
        if (card == null || card.CardData == null)
        {
            Debug.LogError("Card or CardData is null in OnCardPlayed!", this);
            return;
        }

        if (roundManager != null && !roundManager.ConsumeEnergyApproval(card))
        {
            RejectCardPlay(card);
            return;
        }

        roundManager.OnPlayerPlayedCard();
        turnSystem?.EndPlayerTurn();
        ExecuteCardEffects(card);
    }

    private void RejectCardPlay(Card card)
    {
        if (card == null)
        {
            return;
        }

        CardSlot slot = card.transform.parent != null ? card.transform.parent.GetComponent<CardSlot>() : null;
        if (slot != null && slot.CurrentCard == card)
        {
            slot.RemoveCard();
        }

        if (handPanel != null)
        {
            card.transform.SetParent(handPanel, false);
            card.transform.localPosition = Vector3.zero;
            card.transform.localScale = Vector3.one;
        }

        CardDragHandler dragHandler = card.GetComponent<CardDragHandler>();
        if (dragHandler != null)
        {
            dragHandler.enabled = true;
        }

        CanvasGroup canvasGroup = card.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        Debug.LogWarning("[CardManager] Card play rejected: not enough energy to pay card cost.");
    }

    private void ExecuteCardEffects(Card card)
    {
        if (card == null)
        {
            return;
        }

        for (int i = 0; i < playEffectHandlers.Count; i++)
        {
            CardPlayEffectHandler handler = playEffectHandlers[i];
            if (handler == null || !handler.isActiveAndEnabled || !handler.CanHandle(card))
            {
                continue;
            }

            handler.Apply(card, this);
        }
    }

    private void ResolveEffectHandlers()
    {
        playEffectHandlers.RemoveAll(handler => handler == null);
        if (playEffectHandlers.Count > 0)
        {
            return;
        }

        CardPlayEffectHandler[] handlersInScene = FindObjectsOfType<CardPlayEffectHandler>();
        for (int i = 0; i < handlersInScene.Length; i++)
        {
            CardPlayEffectHandler handler = handlersInScene[i];
            if (handler != null)
            {
                playEffectHandlers.Add(handler);
            }
        }
    }
}
