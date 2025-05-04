using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    public enum PlayerTypes { Local, AI, Online };
    public PlayerTypes playerType;
    public int playerNumber;

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
                Debug.Log($"✅ Player loaded '{playerSelectedDeck.deckName}' deck from Inspector.");
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
            int randomIndex = Random.Range(i, currentDeck.Count);
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
            SpawnCard(drawnCard);

            if (zones != null)
                zones.UpdateDeckCount(currentDeck.Count);
        }
        else
        {
            Debug.LogWarning("⚠️ Deck is empty! No more cards to draw.");
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

    public bool RemoveCardFromDeck(CardSO cardData)
    {
        if (DeckManager.instance.currentDeck.Contains(cardData))
        {
            DeckManager.instance.currentDeck.Remove(cardData);
            DeckZone.instance.UpdateDeckCount(DeckManager.instance.currentDeck.Count);
            Debug.Log($"[PlayerManager] Removed {cardData.cardName} from the deck.");
            return true;
        }

        Debug.LogWarning($"[PlayerManager] Card {cardData.cardName} was not found in the deck.");
        return false;
    }

    /// <summary>
    /// Clears the "block plays next turn" flag. Call this at the start of the player's turn.
    /// </summary>
    public void ResetBlockPlaysFlag()
    {
        if (blockPlaysNextTurn)
        {
            blockPlaysNextTurn = false;
            Debug.Log($"[PlayerManager] Cleared blockPlaysNextTurn for player {playerNumber}.");
        }
    }
}
