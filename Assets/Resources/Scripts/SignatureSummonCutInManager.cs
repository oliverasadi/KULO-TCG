using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SignatureSummonCutInManager : MonoBehaviour
{
    public GameObject cutInOverlayPrefab; // Assign in Inspector

    private static readonly HashSet<string> signatureCardNames = new HashSet<string>
    {
        "Ultimate Red Seal",
        "Cat TriFecta",
        "Legendary White Tiger of the Pagoda"
    };

    void Start()
    {
        TurnManager.instance.OnCardPlayed += OnCardPlayedHandler;
    }

    void OnDestroy()
    {
        TurnManager.instance.OnCardPlayed -= OnCardPlayedHandler;
    }

    private void OnCardPlayedHandler(CardSO card)
    {
        if (!signatureCardNames.Contains(card.cardName)) return;

        // Confirm card is on field (not removed)
        bool isOnField = false;
        foreach (var placedCard in GridManager.instance.GetGrid())
        {
            if (placedCard == card)
            {
                isOnField = true;
                break;
            }
        }
        if (!isOnField) return;

        // Show Cut-In
        GameObject overlayCanvas = GameObject.Find("OverlayCanvas");
        if (overlayCanvas == null) return;

        GameObject cutIn = Instantiate(cutInOverlayPrefab, overlayCanvas.transform);
        cutIn.transform.SetAsLastSibling(); // ensure it's in front

        var ui = cutIn.GetComponent<CutInOverlayUI>();
        if (ui != null)
        {
            ui.Setup(card.cardName); // Customize the overlay content
        }
    }
}
