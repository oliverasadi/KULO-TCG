using UnityEngine;
using UnityEngine.Serialization;
using System;
using System.Linq;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    public enum PlayerTypes { Local, AI, Online };
    public PlayerTypes playerType;
    public int playerNumber;

    // 🔔 Event for any listeners that care specifically about draws (optional for now)
    public event Action OnCardDrawn;

    // Static field to persist selected deck across scene reloads
    public static DeckDataSO selectedCharacterDeck;

    [Header("Game Deck & UI")]
    public List<CardSO> currentDeck = new List<CardSO>();
    public List<CardHandler> cardHandlers = new List<CardHandler>();
    public GameObject cardPrefab;
    public Transform cardSpawnArea;

    [Header("AI Deck Selection")]
    public DeckDataSO aiSelectedDeck;
    public DeckDataSO playerSelectedDeck;

    [FormerlySerializedAs("Zones")]
    [Header("Deck Objects")]
    public PlayerZones zones;

    public PlayerController pc;

    [Header("Gameplay Restrictions")]
    public bool blockPlaysNextTurn = false;

    private const int HAND_SIZE = 5;
    private DeckManager dm;

    private void Start()
    {
        dm = DeckManager.instance;
        Debug.Log("PlayerManager initialized for playerType: " + playerType);

        if (playerType == PlayerTypes.Local && selectedCharacterDeck != null)
        {
            playerSelectedDeck = selectedCharacterDeck;
            Debug.Log($"[PlayerManager] Overriding local deck to '{playerSelectedDeck.deckName}' from Character Select.");
        }

        if (playerType == PlayerTypes.AI)
        {
            if (aiSelectedDeck != null)
            {
                currentDeck = dm.LoadDeck(aiSelectedDeck);
                Debug.Log($"✅ AI loaded '{aiSelectedDeck.deckName}' deck from Inspector.");
            }
            else
            {
                Debug.LogWarning("⚠️ No AI deck selected in Inspector. Generating random deck.");
                currentDeck = dm.GenerateRandomDeck();
            }
        }
        else if (playerType == PlayerTypes.Local)
        {
            if (playerSelectedDeck != null)
            {
                currentDeck = dm.LoadDeck(playerSelectedDeck);
                Debug.Log($"✅ Player loaded '{playerSelectedDeck.deckName}' deck.");
            }
            else if (dm.availableDecks != null && dm.availableDecks.Count > 0)
            {
                currentDeck = dm.LoadDeck(dm.availableDecks[0]);
                Debug.Log($"⚠️ No player deck selected. Loaded default '{dm.availableDecks[0].deckName}' deck.");
            }
            else
            {
                Debug.LogWarning("⚠️ No player deck selected or available. Generating random deck.");
                currentDeck = dm.GenerateRandomDeck();
            }
        }
        else
        {
            currentDeck = dm.GenerateRandomDeck();
        }

        if (currentDeck == null || currentDeck.Count == 0)
        {
            Debug.LogWarning("Loaded deck is empty. Generating random deck as fallback.");
            currentDeck = dm.GenerateRandomDeck();
        }

        ShuffleDeck();
        DrawStartingHand();
    }

    public void ShuffleDeck()
    {
        for (int i = 0; i < currentDeck.Count; i++)
        {
            CardSO temp = currentDeck[i];
            int randomIndex = UnityEngine.Random.Range(i, currentDeck.Count);
            currentDeck[i] = currentDeck[randomIndex];
            currentDeck[randomIndex] = temp;
        }
        Debug.Log("✅ Deck shuffled! Deck count: " + currentDeck.Count);
    }

    public void DrawStartingHand()
    {
        for (int i = 0; i < HAND_SIZE; i++)
        {
            DrawCard();
        }
    }

    public void DrawCard()
    {
        if (currentDeck.Count > 0)
        {
            CardSO drawnCard = currentDeck[0];
            currentDeck.RemoveAt(0);

            Debug.Log($"[Draw] → Drew '{drawnCard.cardName}' (base power {drawnCard.power})");

            SpawnCard(drawnCard);

            if (zones != null)
                zones.UpdateDeckCount(currentDeck.Count);

            // Notify listeners that a card entered hand (auras/synergies)
            TurnManager.instance?.FireOnCardPlayed(drawnCard);

            // (Optional) local draw hook
            OnCardDrawn?.Invoke();

            // Grab the CardUI we just spawned (last in cardHandlers)
            CardHandler h = (cardHandlers != null && cardHandlers.Count > 0) ? cardHandlers[cardHandlers.Count - 1] : null;
            CardUI ui = h ? h.GetComponent<CardUI>() : null;

            if (ui != null)
            {
                int effNow = ui.CalculateEffectivePower();
                Debug.Log($"[Draw]   '{ui.cardData.cardName}' in hand → effective NOW: {effNow} (tempBoost={ui.temporaryBoost})");

                // Check again next frame to catch aura updates
                StartCoroutine(LogDrawAfterFrame(ui));
            }
            else
            {
                Debug.LogWarning("[Draw] Could not find CardUI for the drawn card immediately after SpawnCard.");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ Deck is empty! No more cards to draw.");
        }
    }

    private System.Collections.IEnumerator LogDrawAfterFrame(CardUI ui)
    {
        yield return null; // wait one frame for any aura recalc
        if (ui != null)
        {
            int eff = ui.CalculateEffectivePower();
            Debug.Log($"[Draw+Aura] '{ui.cardData.cardName}' effective AFTER 1 frame: {eff} (tempBoost={ui.temporaryBoost})");
        }
    }


    public void SpawnCard(CardSO cardData)
    {
        Debug.Log($"📬 [PlayerManager] Spawning card to hand: {cardData.cardName}");

        if (cardSpawnArea == null)
        {
            Debug.LogError("❌ Card Spawn Area is not assigned!");
            return;
        }

        GameObject newCard = Instantiate(cardPrefab, cardSpawnArea);
        CardHandler handler = newCard.GetComponent<CardHandler>();

        if (handler != null)
        {
            handler.SetCard(cardData, playerNumber == 2, playerType == PlayerTypes.AI);
            handler.cardOwner = this;
            cardHandlers.Add(handler);
        }
        else
        {
            Debug.LogError("❌ CardHandler component is missing on CardPrefab!");
        }
    }

    public void StartHandDiscardSelection(CardUI effectCard, int discardCount)
    {
        if (HandDiscardManager.Instance != null)
        {
            HandDiscardManager.Instance.BeginDiscardMode(discardCount, effectCard);
        }
        else
        {
            Debug.LogError("HandDiscardManager instance not found!");
        }
    }

    public void DiscardCard(CardUI card)
    {
        CardHandler handler = card.GetComponent<CardHandler>();
        if (handler != null && cardHandlers.Contains(handler))
        {
            cardHandlers.Remove(handler);
            zones.AddCardToGrave(card.gameObject);
            Debug.Log("Discarded card: " + card.cardData.cardName);
        }
        else
        {
            Debug.LogWarning("Attempted to discard a card that is not in hand.");
        }
    }

    public void EnforceHandLimit()
    {
        int maxHandSize = 10;
        if (cardHandlers.Count > maxHandSize)
        {
            int excess = cardHandlers.Count - maxHandSize;
            Debug.Log($"[PlayerManager] Enforcing hand limit. Discarding {excess} card(s).");

            for (int i = 0; i < excess; i++)
            {
                int lastIndex = cardHandlers.Count - 1;
                CardHandler cardToDiscard = cardHandlers[lastIndex];

                cardHandlers.RemoveAt(lastIndex);

                if (zones != null)
                {
                    zones.AddCardToGrave(cardToDiscard.gameObject);
                    Debug.Log($"[PlayerManager] Discarded excess card: {cardToDiscard.cardData.cardName}");
                }
                else
                {
                    Debug.LogWarning("[PlayerManager] Zones is null! Cannot discard properly.");
                }
            }
        }
    }

    public void EnforceHandLimitWithPrompt()
    {
        int maxHandSize = 10;
        int inHandCount = 0;

        foreach (CardHandler ch in cardHandlers)
        {
            CardUI cardUI = ch.GetComponent<CardUI>();
            if (cardUI != null && !cardUI.isOnField)
            {
                inHandCount++;
            }
        }

        Debug.Log($"[PlayerManager] End-of-turn hand count: {inHandCount}");

        if (inHandCount > maxHandSize)
        {
            int excess = inHandCount - maxHandSize;
            Debug.Log($"[PlayerManager] You must discard {excess} card(s).");

            if (HandDiscardManager.Instance != null)
            {
                HandDiscardManager.Instance.BeginDiscardMode(excess, null);
            }
            else
            {
                Debug.LogError("HandDiscardManager instance not found!");
            }
        }
        else
        {
            Debug.Log("[PlayerManager] Hand count is within limit.");
        }
    }

    // -------------------------------------------------------------------------
    // Replacement consumption + debug helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Removes ONE copy of the given card from THIS player's deck list.
    /// Matches by cardName (safer than reference equality).
    /// </summary>
    public bool RemoveCardFromDeck(CardSO cardData)
    {
        if (cardData == null)
        {
            Debug.LogWarning("[PlayerManager] RemoveCardFromDeck called with null cardData.");
            return false;
        }

        int idx = currentDeck.FindIndex(c => c != null && c.cardName == cardData.cardName);
        if (idx >= 0)
        {
            currentDeck.RemoveAt(idx);

            if (zones != null)
                zones.UpdateDeckCount(currentDeck.Count);

            Debug.Log($"[PlayerManager] Removed '{cardData.cardName}' from Player {playerNumber} deck. Deck now: {currentDeck.Count}");
            return true;
        }

        Debug.LogWarning($"[PlayerManager] '{cardData.cardName}' not found in Player {playerNumber} deck.");
        return false;
    }

    /// <summary>
    /// Tries to consume ONE copy of cardData from the player's HAND first, then DECK.
    /// If taken from hand, returns the existing hand card GameObject in existingHandCardObj.
    /// If taken from deck, existingHandCardObj will be null (and caller can instantiate).
    /// </summary>
    public bool TryConsumeCardFromDeckOrHand(CardSO cardData, out GameObject existingHandCardObj, out string source)
    {
        existingHandCardObj = null;
        source = "NONE";

        if (cardData == null)
        {
            Debug.LogWarning("[PlayerManager] TryConsumeCardFromDeckOrHand called with null cardData.");
            return false;
        }

        // 1) HAND (prefer using an existing copy if it's already drawn)
        for (int i = 0; i < cardHandlers.Count; i++)
        {
            CardHandler h = cardHandlers[i];
            if (h == null) continue;

            CardUI ui = h.GetComponent<CardUI>();
            if (ui == null || ui.cardData == null) continue;
            if (ui.isOnField) continue;

            if (ui.cardData.cardName == cardData.cardName)
            {
                cardHandlers.RemoveAt(i);
                existingHandCardObj = h.gameObject;
                source = "HAND";
                Debug.Log($"[PlayerManager] Consumed '{cardData.cardName}' from HAND (Player {playerNumber}).");
                return true;
            }
        }

        // 2) DECK
        if (RemoveCardFromDeck(cardData))
        {
            source = "DECK";
            return true;
        }

        return false;
    }

    /// <summary>
    /// Logs how many copies of cardName exist in THIS player's deck + hand.
    /// (Hand excludes cards already on the field.)
    /// </summary>
    public void LogDeckHandCount(string cardName, string label = "Count")
    {
        if (string.IsNullOrEmpty(cardName))
            return;

        int deckCount = currentDeck.Count(c => c != null && c.cardName == cardName);

        int handCount = 0;
        foreach (var h in cardHandlers)
        {
            if (h == null) continue;
            CardUI ui = h.GetComponent<CardUI>();
            if (ui == null || ui.cardData == null) continue;
            if (ui.isOnField) continue;
            if (ui.cardData.cardName == cardName) handCount++;
        }

        Debug.Log($"[CountCheck] {label} | Player {playerNumber} | '{cardName}' deck={deckCount} hand={handCount} total={deckCount + handCount}");
    }

    public void ResetBlockPlaysFlag()
    {
        if (blockPlaysNextTurn)
        {
            blockPlaysNextTurn = false;
            Debug.Log($"[PlayerManager] Cleared blockPlaysNextTurn for player {playerNumber}.");
        }
    }
}
