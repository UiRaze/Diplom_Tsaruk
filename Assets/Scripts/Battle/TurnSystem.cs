using UnityEngine;

public class TurnSystem : MonoBehaviour
{
    [Header("Players")]
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private Enemy enemy;

    private bool isPlayerTurn = true;

    private void Start()
    {
        if (roundManager == null)
        {
            roundManager = FindObjectOfType<RoundManager>();
        }

        if (enemy == null)
        {
            enemy = FindObjectOfType<Enemy>();
        }

        StartPlayerTurn();
    }

    public void EndPlayerTurn()
    {
        if (!isPlayerTurn)
        {
            return;
        }

        isPlayerTurn = false;
        roundManager?.EnablePlayerActions(false);
        enemy?.StartEnemyTurn();
    }

    public void EndEnemyTurn()
    {
        if (roundManager != null && roundManager.IsRoundEnding)
        {
            return;
        }

        StartPlayerTurn();
    }

    public void ResetTurn()
    {
        StartPlayerTurn();
    }

    public bool IsPlayerTurn()
    {
        return isPlayerTurn;
    }

    private void StartPlayerTurn()
    {
        isPlayerTurn = true;
        roundManager?.EnablePlayerActions(true);
    }
}
