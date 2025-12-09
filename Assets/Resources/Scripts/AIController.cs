using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AIController : PlayerController
{
    TurnManager tm;
    public PlayerManager pm;

    // Public field for the AI thinking prefab (assign your catgirl prefab here)
    public GameObject aiThinkingPrefab;

    // Store the candidate card that the AI is considering playing.
    private CardSO currentCandidate;

    void Start()
    {
        tm = TurnManager.instance;
    }

    void Update()
    {
        // No per-frame logic needed.
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Playability checks
    // ──────────────────────────────────────────────────────────────────────────

    // Checks if the card's conditions are met on the grid / hand.
    private bool IsCardPlayable(CardSO card)
    {
        if (card == null) return false;

        // If this is a SPELL, make sure it actually has something useful to do.
        if (card.category == CardSO.CardCategory.Spell)
        {
            if (!IsSpellUsefulForAI(card))
            {
                Debug.Log($"[AIController] Spell {card.cardName} has no useful targets → not playable right now.");
                return false;
            }
        }

        // If the card is an Evolution card, ensure the AI has the required base on its board.
        if (card.baseOrEvo == CardSO.BaseOrEvo.Evolution)
        {
            if (!card.requiresSacrifice || card.sacrificeRequirements == null || card.sacrificeRequirements.Count == 0)
            {
                Debug.Log($"[AIController] Evolution card {card.cardName} is not playable: no sacrifice requirements set.");
                return false;
            }

            bool hasValidBase = false;
            CardSO[,] grid = GridManager.instance.GetGrid();

            for (int i = 0; i < grid.GetLength(0); i++)
            {
                for (int j = 0; j < grid.GetLength(1); j++)
                {
                    if (grid[i, j] == null)
                        continue;

                    string owner = GetOwnerTagFromCell(i, j);
                    if (owner != "AI")
                        continue;

                    foreach (var req in card.sacrificeRequirements)
                    {
                        bool match = req.matchByCreatureType
                            ? (grid[i, j].creatureType == req.requiredCardName)
                            : (grid[i, j].cardName == req.requiredCardName);
                        if (match)
                        {
                            hasValidBase = true;
                            break;
                        }
                    }
                    if (hasValidBase)
                        break;
                }
                if (hasValidBase)
                    break;
            }

            if (!hasValidBase)
            {
                Debug.Log($"[AIController] Evolution card {card.cardName} is not playable because a valid base is not found on AI's board.");
                return false;
            }
        }

        // If no sacrifice is required, at this point it is playable.
        if (!card.requiresSacrifice || card.sacrificeRequirements == null || card.sacrificeRequirements.Count == 0)
            return true;

        // Otherwise, make sure all sacrifice requirements are met by AI-owned cards on the field.
        CardSO[,] fieldGrid = GridManager.instance.GetGrid();
        foreach (var req in card.sacrificeRequirements)
        {
            int foundCount = 0;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (fieldGrid[i, j] != null)
                    {
                        if (GetOwnerTagFromCell(i, j) != "AI")
                            continue;

                        bool match = req.matchByCreatureType
                            ? (fieldGrid[i, j].creatureType == req.requiredCardName)
                            : (fieldGrid[i, j].cardName == req.requiredCardName);
                        if (match)
                            foundCount++;
                    }
                }
            }
            if (foundCount < req.count)
            {
                Debug.Log($"[AIController] {card.cardName} is not playable; sacrifice requirement for {req.requiredCardName} not met (need {req.count}, found {foundCount}).");
                return false;
            }
        }

        return true;
    }

    // Decide whether a SPELL is actually worth playing right now.
    private bool IsSpellUsefulForAI(CardSO card)
    {
        if (pm == null) return false;
        if (card.effects == null || card.effects.Count == 0)
            return true; // no metadata to reason about → assume OK

        bool anyUseful = false;

        foreach (var effect in card.effects)
        {
            if (effect == null) continue;

            // Movement spells like "Jump the Fence", "The Jump!", etc.
            if (effect is MoveCardEffect moveEffect)
            {
                if (IsMoveSpellUseful(moveEffect))
                {
                    anyUseful = true;
                    break;
                }
                else
                {
                    // Try other effects if there are any.
                    continue;
                }
            }

            // Buff/debuff spells like "Cat Toy", "Perfected Soil", etc.
            if (effect is PowerChangeEffect powerEffect)
            {
                if (HasUsefulPowerChangeTargets(powerEffect))
                {
                    anyUseful = true;
                    break;
                }
                else
                {
                    continue;
                }
            }

            // Other effect types we don't model yet → assume useful for now.
            anyUseful = true;
            break;
        }

        if (!anyUseful)
            Debug.Log($"[AIController] Spell {card.cardName} judged not useful (no valid targets).");

        return anyUseful;
    }

    // Helper for movement spells (MoveCardEffect)
    private bool IsMoveSpellUseful(MoveCardEffect effect)
    {
        CardSO[,] grid = GridManager.instance.GetGrid();
        GameObject[,] gridObjs = GridManager.instance.GetGridObjects();

        // First check if there is at least one empty cell to move INTO.
        bool hasEmpty = false;
        for (int x = 0; x < 3 && !hasEmpty; x++)
        {
            for (int y = 0; y < 3 && !hasEmpty; y++)
            {
                if (grid[x, y] == null)
                    hasEmpty = true;
            }
        }

        if (!hasEmpty)
        {
            Debug.Log($"[AIController] Move spell {effect.name} not useful: no empty cells on board.");
            return false;
        }

        // Interactive any-destination mode – e.g. "Jump the Fence"
        if (effect.interactiveAnyDestination)
        {
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    if (grid[x, y] == null) continue;

                    GameObject obj = gridObjs[x, y];
                    if (obj == null) continue;

                    CardHandler handler = obj.GetComponent<CardHandler>();
                    if (handler == null || !handler.isAI) continue;  // must be AI's creature

                    string name = grid[x, y].cardName;

                    bool matchesAllowed =
                        (!string.IsNullOrEmpty(effect.allowedName1) && name == effect.allowedName1) ||
                        (!string.IsNullOrEmpty(effect.allowedName2) && name == effect.allowedName2);

                    if (matchesAllowed)
                    {
                        Debug.Log($"[AIController] Move spell {effect.name} is useful (found source {name} at {x},{y}).");
                        return true;
                    }
                }
            }

            Debug.Log($"[AIController] Move spell {effect.name} is not useful: AI has no allowed source creatures on board.");
            return false;
        }

        // Interactive relative-to-opponent mode – e.g. "The Jump!"
        if (effect.interactiveRelativeToOpponent)
        {
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    GameObject obj = gridObjs[x, y];
                    if (obj == null) continue;

                    CardHandler srcHandler = obj.GetComponent<CardHandler>();
                    if (srcHandler == null) continue;

                    // If mustBeYours, the source must be AI-owned.
                    if (effect.mustBeYours && !srcHandler.isAI)
                        continue;

                    // Look north/south for an opponent card
                    for (int dy = -1; dy <= 1; dy += 2)
                    {
                        int ty = y + dy;
                        if (ty < 0 || ty >= 3) continue;

                        GameObject targetObj = gridObjs[x, ty];
                        if (targetObj == null) continue;

                        CardHandler targetHandler = targetObj.GetComponent<CardHandler>();
                        if (targetHandler == null) continue;

                        // Needs to be opponent
                        if (targetHandler.isAI == srcHandler.isAI)
                            continue;

                        Debug.Log($"[AIController] Move spell {effect.name} is useful (relative to opponent at {x},{ty}).");
                        return true;
                    }
                }
            }

            Debug.Log($"[AIController] Move spell {effect.name} is not useful: no valid relative-to-opponent pairs.");
            return false;
        }

        // Other move modes (offsets-based, etc.) – be lenient for now.
        return true;
    }

    // Helper for buff/debuff spells (PowerChangeEffect)
    private bool HasUsefulPowerChangeTargets(PowerChangeEffect effect)
    {
        if (pm == null) return false;

        PlayerManager ownerPM = pm;
        PlayerManager oppPM = (ownerPM == TurnManager.instance.playerManager1)
            ? TurnManager.instance.playerManager2
            : TurnManager.instance.playerManager1;

        // Fewer-cards condition
        if (effect.requireFewerCardsInHand)
        {
            int ownerHand = CountRealHandCards(ownerPM);
            int oppHand = CountRealHandCards(oppPM);

            Debug.Log($"[AIController] Checking fewer-cards condition for {effect.name}: AI={ownerHand}, Opp={oppHand}");

            if (ownerHand >= oppHand)
                return false;
        }

        // Only estimate static filter spells; interactive ones we assume AI wants.
        if (effect.mode != PowerChangeEffect.Mode.StaticFilter)
            return true;

        // Determine which side the effect is targeting.
        PlayerManager targetPM = (effect.targetOwner == PowerChangeEffect.TargetOwner.Yours) ? ownerPM : oppPM;

        // Board targets
        foreach (var ui in Object.FindObjectsOfType<CardUI>())
        {
            if (!ui.isOnField) continue;

            var h = ui.GetComponent<CardHandler>();
            if (h == null || h.cardOwner != targetPM) continue;

            if (effect.filterMode == PowerChangeEffect.FilterMode.Type && ui.cardData.creatureType != effect.filterValue) continue;
            if (effect.filterMode == PowerChangeEffect.FilterMode.NameContains && !ui.cardData.cardName.Contains(effect.filterValue)) continue;

            Debug.Log($"[AIController] PowerChangeEffect {effect.name} has valid board target: {ui.cardData.cardName}");
            return true;
        }

        // Hand targets (if enabled)
        if (effect.includeHand)
        {
            foreach (var handler in targetPM.cardHandlers)
            {
                if (handler == null || handler.cardData == null) continue;

                var ui = handler.GetComponent<CardUI>();
                if (ui == null) continue;
                if (ui.isOnField || ui.isInGraveyard) continue;

                if (effect.filterMode == PowerChangeEffect.FilterMode.Type && ui.cardData.creatureType != effect.filterValue) continue;
                if (effect.filterMode == PowerChangeEffect.FilterMode.NameContains && !ui.cardData.cardName.Contains(effect.filterValue)) continue;

                Debug.Log($"[AIController] PowerChangeEffect {effect.name} has valid hand target: {ui.cardData.cardName}");
                return true;
            }
        }

        Debug.Log($"[AIController] PowerChangeEffect {effect.name} has NO valid targets for AI.");
        return false;
    }

    private int CountRealHandCards(PlayerManager player)
    {
        if (player == null || player.cardHandlers == null) return 0;

        int count = 0;
        foreach (var h in player.cardHandlers)
        {
            if (h == null || h.cardData == null) continue;

            var ui = h.GetComponent<CardUI>();
            if (ui == null) continue;
            if (!ui.isOnField && !ui.isInGraveyard)
                count++;
        }
        return count;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Hand effects
    // ──────────────────────────────────────────────────────────────────────────

    private void ApplyEffectsToAIHandIfNeeded()
    {
        if (pm == null || pm.cardHandlers == null) return;

        // Take a snapshot so effects that modify the hand won't break enumeration
        var snapshot = pm.cardHandlers.ToArray();

        foreach (var ch in snapshot)
        {
            if (ch == null) continue;

            var cardUI = ch.GetComponent<CardUI>();
            if (cardUI == null || cardUI.effectsAppliedInHand) continue;

            var effects = ch.cardData != null ? ch.cardData.effects : null;
            if (effects != null)
            {
                // Also snapshot effects in case an effect list is modified during application
                foreach (var effect in effects.ToArray())
                {
                    try
                    {
                        effect?.ApplyEffect(cardUI);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[AIController] Effect apply failed on {ch.cardData?.cardName}: {ex}");
                    }
                }
            }

            cardUI.effectsAppliedInHand = true;
        }
    }

    // Helper method to determine who owns a grid cell's card.
    private string GetOwnerTagFromCell(int x, int y)
    {
        GameObject obj = GridManager.instance.GetGridObjects()[x, y];
        if (obj == null)
            return "Empty";
        CardHandler ch = obj.GetComponent<CardHandler>();
        if (ch == null)
            return "Empty";
        return ch.isAI ? "AI" : "Player";
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Move choice
    // ──────────────────────────────────────────────────────────────────────────

    // Chooses a move based on aggressive, blocking, or random strategies.
    // For SPELLS, just pick a simple empty cell (spells don't count for wins).
    private Vector2Int ChooseMove(CardSO[,] grid, CardSO candidate)
    {
        if (candidate != null && candidate.category == CardSO.CardCategory.Spell)
        {
            // For most spells the board position does not matter; just pick an empty cell.
            return FindRandomMove(grid);
        }

        Vector2Int move = FindBestAggressiveMove(grid, candidate);
        if (move.x == -1)
            move = FindBlockingMove(grid);
        if (move.x == -1)
            move = FindRandomMove(grid);
        return move;
    }

    // Returns the first blocking move.
    private Vector2Int FindBlockingMove(CardSO[,] grid)
    {
        List<Vector2Int> blockingZones = FindBlockingMoves(grid);
        if (blockingZones.Count > 0)
            return blockingZones[0];
        return new Vector2Int(-1, -1);
    }

    public override void StartTurn()
    {
        StartCoroutine(AIPlay());
    }

    // Coroutine that makes the prefab wiggle.
    private IEnumerator WigglePrefab(GameObject instance)
    {
        float wiggleSpeed = 4f;    // Speed of the wiggle.
        float wiggleAmount = 10f;  // Maximum angle (degrees) to rotate.
        while (instance != null)
        {
            float angle = Mathf.PingPong(Time.time * wiggleSpeed, wiggleAmount) - (wiggleAmount / 2f);
            instance.transform.localRotation = Quaternion.Euler(0, 0, angle);
            yield return null;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Turn logic
    // ──────────────────────────────────────────────────────────────────────────

    // AIPlay uses the candidate card to choose a move, shows the thinking image, and then attempts placement.
    private IEnumerator AIPlay()
    {
        // Instantiate the AI thinking prefab and parent it to the Canvas.
        GameObject aiThinkingInstance = null;
        Coroutine wiggleCoroutine = null;
        if (aiThinkingPrefab != null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                aiThinkingInstance = Instantiate(aiThinkingPrefab, canvas.transform);
                wiggleCoroutine = StartCoroutine(WigglePrefab(aiThinkingInstance));
            }
        }

        // Wait a short moment for the player to notice the image.
        yield return new WaitForSeconds(0.5f);

        // Effects may modify the hand; apply them before we take our snapshot.
        ApplyEffectsToAIHandIfNeeded();

        CardSO[,] grid = GridManager.instance.GetGrid();

        // ── Scan AI hand using a SNAPSHOT (prevents InvalidOperationException) ──
        CardHandler playableCreature = null;
        CardHandler playableSpell = null;

        var handSnapshot = (pm != null && pm.cardHandlers != null)
            ? pm.cardHandlers.ToArray()
            : System.Array.Empty<CardHandler>();

        foreach (var ch in handSnapshot)
        {
            if (ch == null || ch.cardData == null) continue;

            if (ch.cardData.category == CardSO.CardCategory.Creature &&
                !TurnManager.instance.creaturePlayed && IsCardPlayable(ch.cardData))
            {
                playableCreature = ch;
            }

            if (ch.cardData.category == CardSO.CardCategory.Spell &&
                !TurnManager.instance.spellPlayed && IsCardPlayable(ch.cardData))
            {
                bool isDamiano = ch.cardData.effects != null &&
                                 ch.cardData.effects.Exists(e => e != null && e.GetType().Name == "X1DamianoEffect");

                // Damiano: only useful if there is at least one card on the field.
                if (isDamiano)
                {
                    bool anyOnField = false;
                    for (int x = 0; x < grid.GetLength(0) && !anyOnField; x++)
                        for (int y = 0; y < grid.GetLength(1) && !anyOnField; y++)
                            if (grid[x, y] != null) anyOnField = true;

                    if (!anyOnField)
                        continue;
                }

                playableSpell = ch;
            }
        }

        bool playSpellFirst = Random.value < 0.5f;
        if (playSpellFirst)
        {
            if (playableSpell != null)
            {
                currentCandidate = playableSpell.cardData;
                Vector2Int spellMove = ChooseMove(grid, currentCandidate);
                bool placed = TryPlayCard(playableSpell, spellMove, grid, isSpell: true);
                if (placed) yield return new WaitForSeconds(Random.Range(2f, 3f));
            }
            if (playableCreature != null)
            {
                currentCandidate = playableCreature.cardData;
                Vector2Int creatureMove = ChooseMove(grid, currentCandidate);
                bool placed = TryPlayCard(playableCreature, creatureMove, grid);
                if (placed) yield return new WaitForSeconds(Random.Range(1f, 2f));
            }
        }
        else
        {
            if (playableCreature != null)
            {
                currentCandidate = playableCreature.cardData;
                Vector2Int creatureMove = ChooseMove(grid, currentCandidate);
                bool placed = TryPlayCard(playableCreature, creatureMove, grid);
                if (placed) yield return new WaitForSeconds(Random.Range(1f, 2f));
            }
            if (playableSpell != null)
            {
                currentCandidate = playableSpell.cardData;
                Vector2Int spellMove = ChooseMove(grid, currentCandidate);
                bool placed = TryPlayCard(playableSpell, spellMove, grid, isSpell: true);
                if (placed) yield return new WaitForSeconds(Random.Range(1f, 2f));
            }
        }

        yield return new WaitForSeconds(Random.Range(1f, 2f));

        // Clean up thinking prefab safely
        if (wiggleCoroutine != null) StopCoroutine(wiggleCoroutine);
        if (aiThinkingInstance != null) Destroy(aiThinkingInstance);

        EndTurn();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Board evaluation
    // ──────────────────────────────────────────────────────────────────────────

    // Returns the best aggressive move for the AI given the current grid and the candidate card.
    private Vector2Int FindBestAggressiveMove(CardSO[,] grid, CardSO candidate)
    {
        // Priority 1: Try to win by completing 3 in a row.
        Vector2Int winningMove = FindWinningMove(grid);
        if (winningMove.x != -1)
            return winningMove;

        List<Vector2Int> replaceableZones = new List<Vector2Int>();
        List<Vector2Int> openZones = new List<Vector2Int>();

        // Priority 2: Target opponent's cards.
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                if (grid[x, y] != null && GetOwnerTagFromCell(x, y) == "Player")
                {
                    CardSO playerCard = grid[x, y];
                    if (playerCard != null && CheckForReplacement(candidate, playerCard))
                    {
                        if (x == 1 && y == 1)
                            return new Vector2Int(x, y);
                        replaceableZones.Add(new Vector2Int(x, y));
                    }
                }
                else if (grid[x, y] == null)
                {
                    openZones.Add(new Vector2Int(x, y));
                }
            }
        }

        List<Vector2Int> blockingZones = FindBlockingMoves(grid);
        if (blockingZones.Count > 0)
            return blockingZones[0];
        if (replaceableZones.Count > 0)
            return replaceableZones[0];
        if (openZones.Count > 0)
            return openZones[0];

        return new Vector2Int(-1, -1);
    }

    // New version of CheckForReplacement that uses the candidate card.
    private bool CheckForReplacement(CardSO candidate, CardSO playerCard)
    {
        if (candidate == null || pm == null || pm.cardHandlers == null)
            return false;

        CardHandler handler = pm.cardHandlers.Find(h => h.cardData == candidate);
        if (handler == null) return false;

        CardUI aiCardUI = handler.GetComponent<CardUI>();
        if (aiCardUI == null) return false;

        int aiEffectivePower = aiCardUI.CalculateEffectivePower();

        // Get the target card's actual effective power
        CardUI targetCardUI = FindCardUIOnGrid(playerCard);
        int targetEffectivePower = targetCardUI != null ? targetCardUI.CalculateEffectivePower() : playerCard.power;

        Debug.Log($"[AI] Evaluating replacement: {candidate.cardName} ({aiEffectivePower}) vs {playerCard.cardName} ({targetEffectivePower})");

        return aiEffectivePower >= targetEffectivePower;
    }

    private CardUI FindCardUIOnGrid(CardSO cardSO)
    {
        GameObject[,] gridObjects = GridManager.instance.GetGridObjects();
        for (int x = 0; x < 3; x++)
            for (int y = 0; y < 3; y++)
                if (gridObjects[x, y] != null)
                {
                    CardHandler handler = gridObjects[x, y].GetComponent<CardHandler>();
                    if (handler != null && handler.cardData == cardSO)
                        return handler.GetComponent<CardUI>();
                }
        return null;
    }

    // Searches for a winning move by checking rows, columns, and diagonals.
    private Vector2Int FindWinningMove(CardSO[,] grid)
    {
        // Check rows.
        for (int i = 0; i < 3; i++)
        {
            if (grid[i, 0] != null && grid[i, 1] != null && grid[i, 2] == null &&
                GetOwnerTagFromCell(i, 0) == "AI" && GetOwnerTagFromCell(i, 1) == "AI")
                return new Vector2Int(i, 2);
            if (grid[i, 0] != null && grid[i, 2] != null && grid[i, 1] == null &&
                GetOwnerTagFromCell(i, 0) == "AI" && GetOwnerTagFromCell(i, 2) == "AI")
                return new Vector2Int(i, 1);
            if (grid[i, 1] != null && grid[i, 2] != null && grid[i, 0] == null &&
                GetOwnerTagFromCell(i, 1) == "AI" && GetOwnerTagFromCell(i, 2) == "AI")
                return new Vector2Int(i, 0);
        }
        // Check columns.
        for (int i = 0; i < 3; i++)
        {
            if (grid[0, i] != null && grid[1, i] != null && grid[2, i] == null &&
                GetOwnerTagFromCell(0, i) == "AI" && GetOwnerTagFromCell(1, i) == "AI")
                return new Vector2Int(2, i);
            if (grid[0, i] != null && grid[2, i] != null && grid[1, i] == null &&
                GetOwnerTagFromCell(0, i) == "AI" && GetOwnerTagFromCell(2, i) == "AI")
                return new Vector2Int(1, i);
            if (grid[1, i] != null && grid[2, i] != null && grid[0, i] == null &&
                GetOwnerTagFromCell(1, i) == "AI" && GetOwnerTagFromCell(2, i) == "AI")
                return new Vector2Int(0, i);
        }
        // Check diagonals.
        if (grid[0, 0] != null && grid[1, 1] != null && grid[2, 2] == null &&
            GetOwnerTagFromCell(0, 0) == "AI" && GetOwnerTagFromCell(1, 1) == "AI")
            return new Vector2Int(2, 2);
        if (grid[0, 0] != null && grid[2, 2] != null && grid[1, 1] == null &&
            GetOwnerTagFromCell(0, 0) == "AI" && GetOwnerTagFromCell(2, 2) == "AI")
            return new Vector2Int(1, 1);
        if (grid[1, 1] != null && grid[2, 2] != null && grid[0, 0] == null &&
            GetOwnerTagFromCell(1, 1) == "AI" && GetOwnerTagFromCell(2, 2) == "AI")
            return new Vector2Int(0, 0);
        if (grid[0, 2] != null && grid[1, 1] != null && grid[2, 0] == null &&
            GetOwnerTagFromCell(0, 2) == "AI" && GetOwnerTagFromCell(1, 1) == "AI")
            return new Vector2Int(2, 0);
        if (grid[0, 2] != null && grid[2, 0] != null && grid[1, 1] == null &&
            GetOwnerTagFromCell(0, 2) == "AI" && GetOwnerTagFromCell(2, 0) == "AI")
            return new Vector2Int(1, 1);
        if (grid[1, 1] != null && grid[2, 0] != null && grid[0, 2] == null &&
            GetOwnerTagFromCell(1, 1) == "AI" && GetOwnerTagFromCell(2, 0) == "AI")
            return new Vector2Int(0, 2);

        return new Vector2Int(-1, -1); // No winning move found.
    }

    // Finds potential blocking moves by checking rows, columns, and diagonals for two Player cards.
    private List<Vector2Int> FindBlockingMoves(CardSO[,] grid)
    {
        List<Vector2Int> blockingZones = new List<Vector2Int>();

        // Check rows.
        for (int i = 0; i < 3; i++)
        {
            if (grid[i, 0] != null && grid[i, 1] != null && grid[i, 2] == null &&
                GetOwnerTagFromCell(i, 0) == "Player" && GetOwnerTagFromCell(i, 1) == "Player")
                blockingZones.Add(new Vector2Int(i, 2));
            if (grid[i, 0] != null && grid[i, 2] != null && grid[i, 1] == null &&
                GetOwnerTagFromCell(i, 0) == "Player" && GetOwnerTagFromCell(i, 2) == "Player")
                blockingZones.Add(new Vector2Int(i, 1));
            if (grid[i, 1] != null && grid[i, 2] != null && grid[i, 0] == null &&
                GetOwnerTagFromCell(i, 1) == "Player" && GetOwnerTagFromCell(i, 2) == "Player")
                blockingZones.Add(new Vector2Int(i, 0));
        }

        // Check columns.
        for (int i = 0; i < 3; i++)
        {
            if (grid[0, i] != null && grid[1, i] != null && grid[2, i] == null &&
                GetOwnerTagFromCell(0, i) == "Player" && GetOwnerTagFromCell(1, i) == "Player")
                blockingZones.Add(new Vector2Int(2, i));
            if (grid[0, i] != null && grid[2, i] != null && grid[1, i] == null &&
                GetOwnerTagFromCell(0, i) == "Player" && GetOwnerTagFromCell(2, i) == "Player")
                blockingZones.Add(new Vector2Int(1, i));
            if (grid[1, i] != null && grid[2, i] != null && grid[0, i] == null &&
                GetOwnerTagFromCell(1, i) == "Player" && GetOwnerTagFromCell(2, i) == "Player")
                blockingZones.Add(new Vector2Int(0, i));
        }

        // Check diagonals.
        if (grid[0, 0] != null && grid[1, 1] != null && grid[2, 2] == null &&
            GetOwnerTagFromCell(0, 0) == "Player" && GetOwnerTagFromCell(1, 1) == "Player")
            blockingZones.Add(new Vector2Int(2, 2));
        if (grid[0, 0] != null && grid[2, 2] != null && grid[1, 1] == null &&
            GetOwnerTagFromCell(0, 0) == "Player" && GetOwnerTagFromCell(2, 2) == "Player")
            blockingZones.Add(new Vector2Int(1, 1));
        if (grid[1, 1] != null && grid[2, 2] != null && grid[0, 0] == null &&
            GetOwnerTagFromCell(1, 1) == "Player" && GetOwnerTagFromCell(2, 2) == "Player")
            blockingZones.Add(new Vector2Int(0, 0));

        if (grid[0, 2] != null && grid[1, 1] != null && grid[2, 0] == null &&
            GetOwnerTagFromCell(0, 2) == "Player" && GetOwnerTagFromCell(1, 1) == "Player")
            blockingZones.Add(new Vector2Int(2, 0));
        if (grid[0, 2] != null && grid[2, 0] != null && grid[1, 1] == null &&
            GetOwnerTagFromCell(0, 2) == "Player" && GetOwnerTagFromCell(2, 0) == "Player")
            blockingZones.Add(new Vector2Int(1, 1));
        if (grid[1, 1] != null && grid[2, 0] != null && grid[0, 2] == null &&
            GetOwnerTagFromCell(1, 1) == "Player" && GetOwnerTagFromCell(2, 0) == "Player")
            blockingZones.Add(new Vector2Int(0, 2));

        return blockingZones;
    }

    private bool TryPlayCard(CardHandler handler, Vector2Int move, CardSO[,] grid, bool isSpell = false)
    {
        bool placed = false;

        if (move.x != -1)
        {
            Debug.Log($"AI plays {(isSpell ? "spell" : "creature")} {handler.cardData.cardName} at {move.x}, {move.y}");
            placed = PlaceAICardOnGrid(move.x, move.y, handler);
        }

        if (!placed)
        {
            Vector2Int fallback = FindRandomMove(grid);
            if (fallback.x != -1)
            {
                Debug.Log($"AI fallback: trying empty cell at {fallback.x}, {fallback.y} for {handler.cardData.cardName}");
                placed = PlaceAICardOnGrid(fallback.x, fallback.y, handler);
            }
        }

        if (placed)
            pm.cardHandlers.Remove(handler);

        return placed;
    }

    // Returns a random available grid cell.
    private Vector2Int FindRandomMove(CardSO[,] grid)
    {
        List<Vector2Int> availableMoves = new List<Vector2Int>();
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (grid[x, y] == null)
                    availableMoves.Add(new Vector2Int(x, y));
            }
        }
        if (availableMoves.Count > 0)
            return availableMoves[Random.Range(0, availableMoves.Count)];
        return new Vector2Int(-1, -1);
    }

    // Chooses the best playable card from the AI's hand.
    private CardHandler GetBestPlayableCardFromHand()
    {
        CardHandler bestCandidate = null;
        float highestPower = 0;
        foreach (CardHandler ch in pm.cardHandlers)
        {
            CardSO card = ch.cardData;
            if (card.category == CardSO.CardCategory.Creature && !tm.creaturePlayed && card.power > highestPower && IsCardPlayable(card))
            {
                bestCandidate = ch;
                highestPower = card.power;
            }
        }
        if (bestCandidate == null)
        {
            foreach (CardHandler ch in pm.cardHandlers)
            {
                CardSO card = ch.cardData;
                if (card.category == CardSO.CardCategory.Spell && !tm.spellPlayed && IsCardPlayable(card))
                {
                    bestCandidate = ch;
                    break;
                }
            }
        }
        return bestCandidate;
    }

    // Places the AI's card on the grid.
    private bool PlaceAICardOnGrid(int x, int y, CardHandler cardHandler)
    {
        // If the card is an Evolution card, adjust x and y
        // to be the same as the sacrificed (base) card on the field.
        if (cardHandler.cardData.baseOrEvo == CardSO.BaseOrEvo.Evolution)
        {
            int baseX = -1;
            int baseY = -1;
            CardSO[,] grid = GridManager.instance.GetGrid();

            // Look for a matching base card on the field.
            for (int i = 0; i < grid.GetLength(0); i++)
            {
                for (int j = 0; j < grid.GetLength(1); j++)
                {
                    if (grid[i, j] != null)
                    {
                        // Check against all sacrifice requirements.
                        foreach (var req in cardHandler.cardData.sacrificeRequirements)
                        {
                            bool match = req.matchByCreatureType
                                ? (grid[i, j].creatureType == req.requiredCardName)
                                : (grid[i, j].cardName == req.requiredCardName);

                            // Confirm ownership and match.
                            if (match && GetOwnerTagFromCell(i, j) == "AI")
                            {
                                baseX = i;
                                baseY = j;
                                break;
                            }
                        }
                    }
                    if (baseX != -1)
                        break;
                }
                if (baseX != -1)
                    break;
            }
            // If we found a valid base card on the field, use its coordinates.
            if (baseX != -1)
            {
                x = baseX;
                y = baseY;
            }
            // Else: no matching sacrifice on the field – fallback to the provided (x,y).
        }

        string cellName = $"GridCell_{x}_{y}";
        GameObject cellObj = GameObject.Find(cellName);
        if (cellObj != null)
        {
            Transform cellParent = cellObj.transform;
            bool placed = GridManager.instance.PlaceExistingCard(x, y, cardHandler.gameObject, cardHandler.cardData, cellParent);
            if (placed)
            {
                CardUI cardUI = cardHandler.GetComponent<CardUI>();
                if (cardUI != null)
                {
                    cardUI.isOnField = true;
                    cardUI.RevealCard();
                }
                CardPreviewManager.Instance.ShowCardPreview(cardHandler.cardData);

                // Set flags depending on the card category.
                if (cardHandler.cardData.category == CardSO.CardCategory.Creature)
                    tm.creaturePlayed = true;
                else if (cardHandler.cardData.category == CardSO.CardCategory.Spell)
                    tm.spellPlayed = true;
            }
            return placed;
        }
        else
        {
            Debug.LogError($"[AIController] Could not find a cell named '{cellName}' for the AI to place a card!");
            return false;
        }
    }

    private void EndTurn()
    {
        TurnManager.instance.EndTurn();
    }
}
