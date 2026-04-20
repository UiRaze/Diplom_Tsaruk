using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EventPopup : MonoBehaviour
{
    [Serializable]
    private class EventScenario
    {
        public string title;
        public string description;
        public string optionA;
        public string optionB;
    }

    [SerializeField] private ResourceRewardPopup rewardPopup;
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button optionAButton;
    [SerializeField] private Button optionBButton;
    [SerializeField] private TextMeshProUGUI optionALabel;
    [SerializeField] private TextMeshProUGUI optionBLabel;

    [Header("Scenarios")]
    [SerializeField] private EventScenario[] scenarios =
    {
        new EventScenario
        {
            title = "Потерянный торговец",
            description = "Старый торговец просит помощи и обещает отблагодарить.",
            optionA = "Помочь",
            optionB = "Пройти мимо"
        },
        new EventScenario
        {
            title = "Загадочный сундук",
            description = "Перед вами сундук с непонятной печатью.",
            optionA = "Открыть",
            optionB = "Оставить"
        },
        new EventScenario
        {
            title = "Раненый союзник",
            description = "Вы встретили союзника, который просит припасы.",
            optionA = "Поддержать",
            optionB = "Игнорировать"
        }
    };

    private Action onClosed;

    private void Start()
    {
        ResolveReferences();
        EnsureUi();
        Hide();
    }

    public void ShowRandomEvent(Action closedCallback = null)
    {
        ResolveReferences();
        EnsureUi();

        onClosed = closedCallback;

        EventScenario scenario = scenarios != null && scenarios.Length > 0
            ? scenarios[UnityEngine.Random.Range(0, scenarios.Length)]
            : new EventScenario
            {
                title = "Случайное событие",
                description = "Выберите одно из двух действий.",
                optionA = "Вариант 1",
                optionB = "Вариант 2"
            };

        if (titleText != null)
        {
            titleText.text = scenario.title;
        }

        if (descriptionText != null)
        {
            descriptionText.text = scenario.description;
        }

        if (optionALabel != null)
        {
            optionALabel.text = scenario.optionA;
        }

        if (optionBLabel != null)
        {
            optionBLabel.text = scenario.optionB;
        }

        if (optionAButton != null)
        {
            optionAButton.onClick.RemoveAllListeners();
            optionAButton.onClick.AddListener(() => ApplyChoice(true));
        }

        if (optionBButton != null)
        {
            optionBButton.onClick.RemoveAllListeners();
            optionBButton.onClick.AddListener(() => ApplyChoice(false));
        }

        if (root != null)
        {
            root.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    private void ApplyChoice(bool firstOption)
    {
        int coins;
        int resources;

        if (firstOption)
        {
            coins = UnityEngine.Random.Range(10, 51);
            resources = UnityEngine.Random.Range(5, 21);
        }
        else
        {
            coins = UnityEngine.Random.Range(5, 26);
            resources = UnityEngine.Random.Range(2, 11);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddGold(coins);
            GameManager.Instance.AddResources(resources);
        }

        Hide();
        rewardPopup?.ShowReward(coins, resources, "Награда за событие");

        onClosed?.Invoke();
        onClosed = null;
    }

    private void ResolveReferences()
    {
        if (rewardPopup == null)
        {
            rewardPopup = FindObjectOfType<ResourceRewardPopup>();
        }

        if (root == null)
        {
            root = GameObject.Find("EventPopupRoot");
        }

        if (titleText == null)
        {
            titleText = GameObject.Find("EventPopupTitle")?.GetComponent<TextMeshProUGUI>();
        }

        if (descriptionText == null)
        {
            descriptionText = GameObject.Find("EventPopupDescription")?.GetComponent<TextMeshProUGUI>();
        }

        if (optionAButton == null)
        {
            optionAButton = GameObject.Find("EventOptionAButton")?.GetComponent<Button>();
        }

        if (optionBButton == null)
        {
            optionBButton = GameObject.Find("EventOptionBButton")?.GetComponent<Button>();
        }

        if (optionALabel == null)
        {
            optionALabel = GameObject.Find("EventOptionALabel")?.GetComponent<TextMeshProUGUI>();
        }

        if (optionBLabel == null)
        {
            optionBLabel = GameObject.Find("EventOptionBLabel")?.GetComponent<TextMeshProUGUI>();
        }
    }

    private void EnsureUi()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        if (root == null)
        {
            root = CreateRoot(canvas.transform);
        }

        if (titleText == null)
        {
            titleText = CreateText(root.transform, "EventPopupTitle", new Vector2(0f, 105f), new Vector2(560f, 56f), 34f, TextAlignmentOptions.Center, "Событие");
        }

        if (descriptionText == null)
        {
            descriptionText = CreateText(root.transform, "EventPopupDescription", new Vector2(0f, 35f), new Vector2(620f, 110f), 24f, TextAlignmentOptions.Center, string.Empty);
        }

        if (optionAButton == null)
        {
            optionAButton = CreateButton(root.transform, "EventOptionAButton", new Vector2(-170f, -88f), new Vector2(260f, 62f), out optionALabel);
        }

        if (optionBButton == null)
        {
            optionBButton = CreateButton(root.transform, "EventOptionBButton", new Vector2(170f, -88f), new Vector2(260f, 62f), out optionBLabel);
        }
    }

    private GameObject CreateRoot(Transform parent)
    {
        GameObject rootGO = new GameObject("EventPopupRoot", typeof(RectTransform), typeof(Image));
        rootGO.transform.SetParent(parent, false);

        RectTransform rect = rootGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(700f, 420f);

        Image image = rootGO.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.9f);

        return rootGO;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align, string value)
    {
        GameObject textGO = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(parent, false);

        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textGO.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = align;
        text.color = Color.white;

        return text;
    }

    private Button CreateButton(Transform parent, string name, Vector2 pos, Vector2 size, out TextMeshProUGUI label)
    {
        GameObject buttonGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(parent, false);

        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Image image = buttonGO.GetComponent<Image>();
        image.color = new Color(0.18f, 0.18f, 0.18f, 1f);

        Button button = buttonGO.GetComponent<Button>();

        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(buttonGO.transform, false);

        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        label = labelGO.GetComponent<TextMeshProUGUI>();
        label.text = string.Empty;
        label.fontSize = 24f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;

        return button;
    }
}
