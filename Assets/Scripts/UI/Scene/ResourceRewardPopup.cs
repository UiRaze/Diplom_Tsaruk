using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceRewardPopup : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button okButton;

    private void Start()
    {
        ResolveReferences();
        EnsureUi();

        if (okButton != null)
        {
            okButton.onClick.RemoveListener(Close);
            okButton.onClick.AddListener(Close);
        }

        Close();
    }

    public void ShowReward(int coins, int resources, string title = "Награда")
    {
        ResolveReferences();
        EnsureUi();

        if (titleText != null)
        {
            titleText.text = title;
        }

        if (descriptionText != null)
        {
            descriptionText.text = $"Монеты: +{coins}\nРесурсы: +{resources}";
        }

        if (root != null)
        {
            root.SetActive(true);
        }
    }

    public void Close()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
    }

    private void ResolveReferences()
    {
        if (root == null)
        {
            GameObject foundRoot = GameObject.Find("ResourceRewardPopupRoot");
            if (foundRoot != null)
            {
                root = foundRoot;
            }
        }

        if (titleText == null)
        {
            titleText = GameObject.Find("ResourceRewardTitle")?.GetComponent<TextMeshProUGUI>();
        }

        if (descriptionText == null)
        {
            descriptionText = GameObject.Find("ResourceRewardDescription")?.GetComponent<TextMeshProUGUI>();
        }

        if (okButton == null)
        {
            okButton = GameObject.Find("ResourceRewardOkButton")?.GetComponent<Button>();
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
            titleText = CreateText(root.transform, "ResourceRewardTitle", new Vector2(0f, 90f), new Vector2(420f, 50f), 36f, TextAlignmentOptions.Center, "Награда");
        }

        if (descriptionText == null)
        {
            descriptionText = CreateText(root.transform, "ResourceRewardDescription", new Vector2(0f, 20f), new Vector2(460f, 120f), 30f, TextAlignmentOptions.Center, string.Empty);
        }

        if (okButton == null)
        {
            okButton = CreateButton(root.transform, "ResourceRewardOkButton", new Vector2(0f, -90f), new Vector2(180f, 52f), "OK");
        }
    }

    private GameObject CreateRoot(Transform parent)
    {
        GameObject rootGO = new GameObject("ResourceRewardPopupRoot", typeof(RectTransform), typeof(Image));
        rootGO.transform.SetParent(parent, false);

        RectTransform rect = rootGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(520f, 320f);
        rect.anchoredPosition = Vector2.zero;

        Image image = rootGO.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.9f);

        return rootGO;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align, string textValue)
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
        text.text = textValue;
        text.fontSize = fontSize;
        text.alignment = align;
        text.color = Color.white;

        return text;
    }

    private Button CreateButton(Transform parent, string name, Vector2 pos, Vector2 size, string label)
    {
        GameObject btnGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(parent, false);

        RectTransform rect = btnGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Image image = btnGO.GetComponent<Image>();
        image.color = new Color(0.18f, 0.18f, 0.18f, 1f);

        Button button = btnGO.GetComponent<Button>();

        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(btnGO.transform, false);

        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI labelText = labelGO.GetComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 26f;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.white;
        labelText.raycastTarget = false;

        return button;
    }
}
