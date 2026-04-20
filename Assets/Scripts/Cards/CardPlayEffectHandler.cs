using UnityEngine;

public abstract class CardPlayEffectHandler : MonoBehaviour
{
    public abstract bool CanHandle(Card card);
    public abstract void Apply(Card card, CardManager cardManager);
}
