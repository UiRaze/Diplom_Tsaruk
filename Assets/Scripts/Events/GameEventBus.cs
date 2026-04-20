using System;

public static class GameEventBus
{
    public static event Action OnPlayerPass;
    public static event Action OnRoundEnd;
    public static event Action<MapNode> OnNodeSelected;

    public static void RaisePlayerPass()
    {
        OnPlayerPass?.Invoke();
    }

    public static void RaiseRoundEnd()
    {
        OnRoundEnd?.Invoke();
    }

    public static void RaiseNodeSelected(MapNode node)
    {
        OnNodeSelected?.Invoke(node);
    }
}
