using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Game/Config/GameConfig", order = 0)]
public class GameConfigSO : ScriptableObject
{
    [Header("Battle")]
    [Min(1)]
    public int totalRounds = 5;

    [Header("Deck")]
    [Min(1)]
    public int deckLimit = 30;

    [Header("Energy")]
    [Min(0)]
    public int startEnergy = 3;
}
