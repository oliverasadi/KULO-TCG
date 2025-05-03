using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GraveyardDisplayManager : MonoBehaviour
{
    public Transform contentParent;           // Assign the Content of ScrollView
    public GameObject cardDisplayPrefab;      // Assign a lightweight CardUI prefab
    public Button closeButton;                // Assign in Inspector

    private void Awake()
    {
        closeButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    public void ShowGraveyard(List<GameObject> graveyardCards)
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        foreach (GameObject cardObj in graveyardCards)
        {
            var handler = cardObj.GetComponent<CardHandler>();
            if (handler == null || handler.cardData == null) continue;

            GameObject cardInstance = Instantiate(cardDisplayPrefab, contentParent);
            var ui = cardInstance.GetComponent<CardUI>();
            if (ui != null)
            {
                ui.SetCardData(handler.cardData);
                ui.isInDeck = false;
                ui.isOnField = false;
            }
        }

        gameObject.SetActive(true);
    }
}
