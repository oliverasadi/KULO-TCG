using UnityEngine;

public class ClickCatcher : MonoBehaviour
{
    public SummonMenu menu;

    public void OnClick()
    {
        if (menu != null)
        {
            Debug.Log("[ClickCatcher] Background clicked — closing menu.");
            Destroy(menu.gameObject);
        }
    }
}
