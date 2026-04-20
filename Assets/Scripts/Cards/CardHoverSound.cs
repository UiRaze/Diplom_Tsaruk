using UnityEngine;
using UnityEngine.EventSystems;

public class CardHoverSound : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private AudioClip hoverSound; // Можно назначить индивидуальный звук
    [SerializeField] private bool useGlobalHoverSound = true; // Использовать общий звук из AudioManager

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (useGlobalHoverSound)
        {
            // Воспроизводим звук через AudioManager
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCardHoverSound();
            }
        }
        else if (hoverSound != null)
        {
            // Воспроизводим индивидуальный звук
            AudioSource.PlayClipAtPoint(hoverSound, Camera.main.transform.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Можно добавить звук при уходе курсора, если нужно
    }
}