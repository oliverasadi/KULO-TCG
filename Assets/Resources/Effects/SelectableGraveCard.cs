using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectableGraveCard : MonoBehaviour, IPointerClickHandler
{
    private GameObject realCard;

    public void Setup(GameObject originalCard)
    {
        realCard = originalCard;

        // ✅ Add a yellow glow for visual feedback
        var outline = gameObject.AddComponent<Outline>();
        outline.effectColor = Color.yellow;
        outline.effectDistance = new Vector2(2f, 2f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (realCard != null && GraveyardSelectionManager.Instance != null)
        {
            Debug.Log($"✅ Graveyard selection active. Passing {realCard.name} to selector.");
            GraveyardSelectionManager.Instance.SelectCard(realCard);
        }
    }
}
