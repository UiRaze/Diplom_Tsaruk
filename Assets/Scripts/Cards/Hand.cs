using UnityEngine;

public class Hand : MonoBehaviour
{
    [SerializeField] private Deck deck;
    [SerializeField] private int handSize = 4;
    [SerializeField] private Vector2 cardSize = new Vector2(150f, 200f);

    private GameObject cardPrefab;
    private Transform handPanel;

    public int HandSize => handSize;
    public int CurrentCount => CountCardsInPanel();
    public Transform HandPanel => handPanel;

    public void Initialize(GameObject prefab, Transform handPanelTransform)
    {
        cardPrefab = prefab;
        handPanel = handPanelTransform;

        if (deck == null)
        {
            Debug.LogError("[Hand] Deck is not assigned", this);
            return;
        }

        DrawToHandSize(handSize);
    }

    public void DrawToHandSize(int targetSize)
    {
        if (!CanDraw())
        {
            return;
        }

        while (CountCardsInPanel() < targetSize)
        {
            if (!DrawCard())
            {
                break;
            }
        }
    }

    public bool DrawCard()
    {
        if (!CanDraw())
        {
            return false;
        }

        CardData drawnCardData = deck.DrawCard();
        if (drawnCardData == null)
        {
            return false;
        }

        GameObject cardGO = Instantiate(cardPrefab, handPanel);
        Card card = cardGO.GetComponent<Card>();
        if (card == null)
        {
            Debug.LogError("[Hand] Card prefab does not contain Card component", cardGO);
            Destroy(cardGO);
            return false;
        }

        card.SetCardData(drawnCardData);
        card.SetOwner(true);

        RectTransform rectTransform = cardGO.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = cardSize;
            rectTransform.localScale = Vector3.one;
        }

        return true;
    }

    // Backward-compatible wrapper for existing calls.
    public void DrawCard(GameObject prefab, Transform handPanelTransform)
    {
        if (cardPrefab == null)
        {
            cardPrefab = prefab;
        }

        if (handPanel == null)
        {
            handPanel = handPanelTransform;
        }

        DrawCard();
    }

    private bool CanDraw()
    {
        if (deck == null || cardPrefab == null || handPanel == null)
        {
            Debug.LogError("[Hand] Missing references for draw", this);
            return false;
        }

        return true;
    }

    private int CountCardsInPanel()
    {
        if (handPanel == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < handPanel.childCount; i++)
        {
            if (handPanel.GetChild(i).GetComponent<Card>() != null)
            {
                count++;
            }
        }

        return count;
    }
}
