using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button startRunButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button deckBuilderButton;
    [SerializeField] private Button quitButton;

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
        }

        if (startRunButton != null)
        {
            startRunButton.onClick.RemoveAllListeners();
            startRunButton.onClick.AddListener(GameManager.Instance.StartNewRunFromMenu);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(GameManager.Instance.OpenSettings);
        }

        if (deckBuilderButton != null)
        {
            deckBuilderButton.onClick.RemoveAllListeners();
            deckBuilderButton.onClick.AddListener(GameManager.Instance.OpenDeckBuilder);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(GameManager.Instance.QuitGame);
        }

        if (!string.IsNullOrWhiteSpace(GameManager.Instance.LastDefeatReason) &&
            FindObjectOfType<LoseScreenController>() == null)
        {
            gameObject.AddComponent<LoseScreenController>();
        }
    }
}
