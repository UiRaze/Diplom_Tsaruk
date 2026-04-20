using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SlotModifierUI : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI label;

    private void Awake()
    {
        EnsureUi();
    }

    public void SetModifier(SlotModifier modifier)
    {
        EnsureUi();

        if (background != null)
        {
            background.color = SlotModifierLogic.GetColor(modifier);
        }

        if (label != null)
        {
            label.text = SlotModifierLogic.GetShortLabel(modifier);
        }
    }

    private void EnsureUi()
    {
        if (background == null)
        {
            background = GetComponent<Image>();
        }

        if (background == null)
        {
            background = gameObject.AddComponent<Image>();
        }

        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(56f, 24f);
            rect.anchoredPosition = new Vector2(0f, 18f);
        }

        if (label == null)
        {
            Transform labelTransform = transform.Find("Label");
            if (labelTransform != null)
            {
                label = labelTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (label == null)
        {
            GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(transform, false);

            RectTransform labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            label = labelGO.GetComponent<TextMeshProUGUI>();
            label.fontSize = 14f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
        }
    }
}
