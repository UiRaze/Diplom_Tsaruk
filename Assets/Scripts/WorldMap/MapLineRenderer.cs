using UnityEngine;
using UnityEngine.UI;

public class MapLineRenderer : MonoBehaviour
{
    [SerializeField] private Image lineImage;
    [SerializeField] private Sprite horizontalLineSprite;
    [SerializeField] private Sprite verticalLineSprite;
    [SerializeField] private Sprite diagonalLineSprite;
    [SerializeField] private Sprite diagonalLineSprite2; // ��� ������� �����������

    public void DrawLineBetween(MapNode from, MapNode to)
    {
        if (lineImage == null || from == null || to == null) return;

        // �������� ������� �����
        Vector2 fromPos = from.GetComponent<RectTransform>().anchoredPosition;
        Vector2 toPos = to.GetComponent<RectTransform>().anchoredPosition;

        // ��������� ����� � ���� �����
        Vector2 direction = toPos - fromPos;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // ��������� ����������� �����
        var rt = GetComponent<RectTransform>();
        rt.anchoredPosition = fromPos + direction / 2f; // ���������� ����� ������
        rt.sizeDelta = new Vector2(distance, rt.sizeDelta.y); // ����� �����
        rt.localEulerAngles = new Vector3(0, 0, angle); // �������

        // �������� ������ � ����������� �� ����
        if (Mathf.Abs(angle) < 10 || Mathf.Abs(angle - 180) < 10)
        {
            lineImage.sprite = horizontalLineSprite;
        }
        else if (Mathf.Abs(Mathf.Abs(angle) - 90) < 10)
        {
            lineImage.sprite = verticalLineSprite;
        }
        else if (angle > 0 && angle < 90)
        {
            lineImage.sprite = diagonalLineSprite;
        }
        else
        {
            lineImage.sprite = diagonalLineSprite2;
        }

        // ����������� ������ �� �����
        lineImage.type = Image.Type.Sliced;
    }
}