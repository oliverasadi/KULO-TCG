using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlayButtonHoverSound(0.3f);  // quieter hover
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlayButtonClickSound(0.7f);  // slightly lower click
    }
}
