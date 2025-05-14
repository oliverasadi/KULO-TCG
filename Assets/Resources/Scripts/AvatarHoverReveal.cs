using UnityEngine;
using UnityEngine.EventSystems;

public class AvatarHoverReveal : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject editIcon;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (editIcon != null) editIcon.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (editIcon != null) editIcon.SetActive(false);
    }
}
