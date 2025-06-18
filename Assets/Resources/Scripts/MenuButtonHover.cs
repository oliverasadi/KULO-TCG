using UnityEngine;
using UnityEngine.EventSystems;

public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public IconManager iconManager;
    public IconManager.IconType iconType;

    public void OnPointerEnter(PointerEventData eventData)
    {
        iconManager.ShowIcon(iconType);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        iconManager.StartResetDelay();
    }
}
