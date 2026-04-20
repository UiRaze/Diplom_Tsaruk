using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldMapUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldMapManager mapManager;
    [SerializeField] private GameManager gameManager;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI resourcesText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Button startButton;

    private MapNode currentlySelectedNode;

    private void Start()
    {
        ResolveReferences();
        EnsureUiExists();

        if (mapManager == null)
        {
            mapManager = FindObjectOfType<WorldMapManager>();
        }

        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartClicked);
            startButton.onClick.AddListener(OnStartClicked);
            startButton.interactable = false;
        }

        if (gameManager != null)
        {
            gameManager.OnPlayerHealthChanged += HandleHealthChanged;
            gameManager.OnCurrencyChanged += HandleCurrencyChanged;
        }

        UpdateStats();
    }

    private void ResolveReferences()
    {
        if (hpText == null)
        {
            hpText = GameObject.Find("HPText")?.GetComponent<TextMeshProUGUI>();
        }

        if (goldText == null)
        {
            goldText = GameObject.Find("GoldText")?.GetComponent<TextMeshProUGUI>();
        }

        if (resourcesText == null)
        {
            resourcesText = GameObject.Find("ResourcesText")?.GetComponent<TextMeshProUGUI>();
        }

        if (levelText == null)
        {
            levelText = GameObject.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
        }

        if (startButton == null)
        {
            startButton = GameObject.Find("StartButton")?.GetComponent<Button>();
        }
    }

    private void EnsureUiExists()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        if (hpText == null)
        {
            hpText = CreateStatText(canvas.transform, "HPText", new Vector2(160f, -28f), TextAlignmentOptions.Left);
        }

        if (goldText == null)
        {
            goldText = CreateStatText(canvas.transform, "GoldText", new Vector2(160f, -58f), TextAlignmentOptions.Left);
        }

        if (resourcesText == null)
        {
            resourcesText = CreateStatText(canvas.transform, "ResourcesText", new Vector2(160f, -88f), TextAlignmentOptions.Left);
        }

        if (levelText == null)
        {
            levelText = CreateStatText(canvas.transform, "LevelText", new Vector2(160f, -118f), TextAlignmentOptions.Left);
        }
    }

    private TextMeshProUGUI CreateStatText(Transform parent, string name, Vector2 position, TextAlignmentOptions alignment)
    {
        GameObject textGO = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(parent, false);

        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(340f, 30f);

        TextMeshProUGUI text = textGO.GetComponent<TextMeshProUGUI>();
        text.fontSize = 26f;
        text.alignment = alignment;
        text.color = Color.white;
        text.text = string.Empty;

        return text;
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnPlayerHealthChanged -= HandleHealthChanged;
            gameManager.OnCurrencyChanged -= HandleCurrencyChanged;
        }
    }

    public void UpdateStats()
    {
        if (gameManager == null)
        {
            return;
        }

        if (hpText != null)
        {
            hpText.text = $"HP: {gameManager.PlayerHP}/{gameManager.MaxHP}";
        }

        if (goldText != null)
        {
            goldText.text = $"Монеты: {gameManager.PlayerGold}";
        }

        if (resourcesText != null)
        {
            resourcesText.text = $"Ресурсы: {gameManager.PlayerResources}";
        }

        if (levelText != null)
        {
            int totalLevels = gameManager != null ? gameManager.MapLevels : GameConfig.MAP_LEVELS;
            levelText.text = $"Этаж: {gameManager.CurrentLevel}/{totalLevels}";
        }
    }

    public void OnNodeSelected(MapNode node)
    {
        currentlySelectedNode = node;

        if (startButton != null)
        {
            startButton.interactable = true;
        }

        if (node != null)
        {
            Debug.Log($"[WorldMapUI] Узел выбран: {node.name}, Type: {node.EffectiveNodeType}");
        }
    }

    public void ClearSelection()
    {
        currentlySelectedNode = null;

        if (startButton != null)
        {
            startButton.interactable = false;
        }
    }

    private void OnStartClicked()
    {
        if (mapManager == null || currentlySelectedNode == null)
        {
            return;
        }

        mapManager.EnterNode();
    }

    private void OnEnable()
    {
        UpdateStats();
    }

    private void HandleHealthChanged(int hp, int maxHp)
    {
        UpdateStats();
    }

    private void HandleCurrencyChanged(int gold, int resources)
    {
        UpdateStats();
    }
}
