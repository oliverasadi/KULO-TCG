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

    private bool IsSelfDestructCard(CardSO card)
    {
        if (card == null || card.effects == null) return false;

        foreach (var effect in card.effects)
        {
            if (effect == null) continue;

            // Same pattern as GridManager.HasSelfDestructEffect
            if (effect is X1DamianoEffect)
                return true;
        }

        return false;
    }
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

        // Otherwise, make sure all sacrifice requirements are met by AI cards on
        // the field and/or in hand AND estimate how many will come from the field.
        CardSO[,] fieldGrid = GridManager.instance.GetGrid();
        int totalFieldSacNeeded = 0;

        foreach (var req in card.sacrificeRequirements)
        {
            int foundOnField = 0;
            int foundInHand = 0;

            // Count matches on the field (AI-owned only).
            if (req.allowFromField)
            {
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
                                foundOnField++;
                        }
                    }
                }
            }

            // Count matches in hand (if allowed).
            if (req.allowFromHand && pm != null && pm.cardHandlers != null)
            {
                foreach (var handCard in pm.cardHandlers)
                {
                    if (handCard == null || handCard.cardData == null)
                        continue;

                    bool match = req.matchByCreatureType
                        ? (handCard.cardData.creatureType == req.requiredCardName)
                        : (handCard.cardData.cardName == req.requiredCardName);

                    CardUI handUI = handCard.GetComponent<CardUI>();
                    if (match && handUI != null && !handUI.isOnField && !handUI.isInGraveyard)
                    {
                        foundInHand++;
                    }
                }
            }

            int totalFound = foundOnField + foundInHand;
            if (totalFound < req.count)
            {
                Debug.Log($"[AIController] {card.cardName} is not playable; sacrifice requirement for {req.requiredCardName} not met (need {req.count}, found {totalFound}).");
                return false;
            }

            // Estimate how many of THIS requirement will actually come from the field.
            // (GridManager always tries to use field copies first.)
            int useFromField = Mathf.Min(req.count, foundOnField);
            totalFieldSacNeeded += useFromField;
        }

        // Extra safety rule:
        // If this is an Evolution that would eat 2+ of our own field cards,
        // AND the player currently has a two-in-a-row threat, do NOT play it.
        if (card.baseOrEvo == CardSO.BaseOrEvo.Evolution && totalFieldSacNeeded >= 2)
        {
            if (HasImmediatePlayerThreat(fieldGrid))
            {
                Debug.Log($"[AIController] Skipping risky evolution {card.cardName}: would sacrifice {totalFieldSacNeeded} field cards while player has line threats.");
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
    // Helper for buff/debuff spells (PowerChangeEffect)
    // Helper for buff / debuff spells (PowerChangeEffect)
    // Improved heuristic: handles "defensive" OpponentNextTurn buffs such as Cat Toy,
    // and avoids treating them as useless just because we haven't played our Cats yet.
    private bool HasUsefulPowerChangeTargets(PowerChangeEffect effect)
    {
        if (pm == null) return false;

        PlayerManager ownerPM = pm;
        PlayerManager oppPM = (ownerPM == TurnManager.instance.playerManager1)
            ? TurnManager.instance.playerManager2
            : TurnManager.instance.playerManager1;

        // Defensive "your side" buffs that only apply during the opponent's next turn
        // (e.g. Cat Toy).
        bool isDefensiveOppNextTurnBuff =
            effect.amount > 0 &&
            effect.targetOwner == PowerChangeEffect.TargetOwner.Yours &&
            effect.duration == PowerChangeEffect.Duration.OpponentNextTurn;

        // 0) Obvious bad cases – don't cast spells that help the wrong side.
        if (effect.amount > 0 && effect.targetOwner == PowerChangeEffect.TargetOwner.Opponent)
        {
            Debug.Log($"[AIController] {effect.name} would BUFF the opponent → skipping.");
            return false;
        }

        if (effect.amount < 0 && effect.targetOwner == PowerChangeEffect.TargetOwner.Yours)
        {
            Debug.Log($"[AIController] {effect.name} would DEBUFF our own cards → skipping.");
            return false;
        }

        // 1) "Fewer cards in hand" condition.
        if (effect.requireFewerCardsInHand)
        {
            int ownerHand = CountRealHandCards(ownerPM);
            int oppHand = CountRealHandCards(oppPM);

            Debug.Log($"[AIController] Checking fewer-cards condition for {effect.name}: AI={ownerHand}, Opp={oppHand}");

            if (ownerHand >= oppHand)
                return false;
        }

        // 2) Interactive modes – just require that there is anything on the board.
        if (effect.mode != PowerChangeEffect.Mode.StaticFilter)
        {
            var grid = GridManager.instance.GetGrid();
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    if (grid[x, y] != null)
                        return true;
                }
            }

            Debug.Log($"[AIController] PowerChangeEffect {effect.name} is interactive but board is empty → skipping.");
            return false;
        }

        // 3) Static filter spells (Cat Toy, It’s Food Time, etc.).
        PlayerManager targetPM =
            (effect.targetOwner == PowerChangeEffect.TargetOwner.Yours) ? ownerPM : oppPM;

        CardUI[] allUIs = GameObject.FindObjectsOfType<CardUI>();

        int boardTargets = 0;
        int handTargets = 0;   // only counts real hand targets if includeHand == true
        int futureHandMatches = 0;   // matching *creatures* in our hand for defensive buffs
        int oppMaxPower = 0;

        // First pass: count targets and find opponent's strongest creature.
        foreach (var ui in allUIs)
        {
            if (ui.cardData == null) continue;

            var h = ui.GetComponent<CardHandler>();
            if (h == null) continue;

            // Track opponent's strongest creature on board using effective power.
            if (ui.isOnField && h.cardOwner == oppPM)
            {
                int oppPower = ui.CalculateEffectivePower();
                if (oppPower > oppMaxPower)
                    oppMaxPower = oppPower;
            }

            // Does this card belong to the targeted player?
            bool belongsToTarget = (h.cardOwner == targetPM);

            // Does it match the filter?
            bool matchesFilter = true;
            if (effect.filterMode == PowerChangeEffect.FilterMode.Type &&
                ui.cardData.creatureType != effect.filterValue)
            {
                matchesFilter = false;
            }
            else if (effect.filterMode == PowerChangeEffect.FilterMode.NameContains &&
                     !ui.cardData.cardName.Contains(effect.filterValue))
            {
                matchesFilter = false;
            }

            if (!matchesFilter)
                continue;

            // Board target?
            if (ui.isOnField && belongsToTarget)
            {
                boardTargets++;
            }
            // Hand target (for heuristics only).
            else if (!ui.isOnField && !ui.isInGraveyard && belongsToTarget)
            {
                // Only count as "real" hand target if the effect actually supports includeHand.
                if (effect.includeHand)
                    handTargets++;

                // For defensive OpponentNextTurn buffs (Cat Toy), we also care about
                // matching *creatures* in our hand, because if we cast the spell this turn
                // and then play those Cats, they WILL be buffed on the opponent's turn.
                if (isDefensiveOppNextTurnBuff &&
                    ui.cardData.category == CardSO.CardCategory.Creature)
                {
                    futureHandMatches++;
                }
            }
        }

        // If there are literally no relevant targets anywhere, this spell is pointless.
        // For defensive OppNextTurn buffs we also allow the "future hand" Cats.
        if (boardTargets == 0 && handTargets == 0 &&
            !(isDefensiveOppNextTurnBuff && futureHandMatches > 0))
        {
            Debug.Log($"[AIController] PowerChangeEffect {effect.name} has NO valid targets → not useful.");
            return false;
        }

        // 4) Extra heuristic for positive buffs on AI's own side.
        if (effect.amount > 0 && effect.targetOwner == PowerChangeEffect.TargetOwner.Yours)
        {
            if (isDefensiveOppNextTurnBuff)
            {
                // Cat Toy style: purely defensive – as long as we have either Cats already
                // on the field OR Cats in hand we can play this turn, treat it as useful.
                Debug.Log($"[AIController] {effect.name} treated as defensive OppNextTurn buff. " +
                          $"boardTargets={boardTargets}, futureHandMatches={futureHandMatches}");
            }
            else
            {
                // Offensive / this-turn buffs like It’s Food Time:
                // Only "useful" if they actually improve a matchup on the current board.
                if (oppMaxPower <= 0)
                {
                    Debug.Log($"[AIController] {effect.name} would overkill into an empty board → saving it.");
                    return false;
                }

                bool flipsMatchup = false;

                foreach (var ui in allUIs)
                {
                    if (!ui.isOnField) continue;

                    var h = ui.GetComponent<CardHandler>();
                    if (h == null || h.cardOwner != targetPM) continue;

                    bool matchesFilter = true;
                    if (effect.filterMode == PowerChangeEffect.FilterMode.Type &&
                        ui.cardData.creatureType != effect.filterValue)
                    {
                        matchesFilter = false;
                    }
                    else if (effect.filterMode == PowerChangeEffect.FilterMode.NameContains &&
                             !ui.cardData.cardName.Contains(effect.filterValue))
                    {
                        matchesFilter = false;
                    }

                    if (!matchesFilter)
                        continue;

                    int current = ui.CalculateEffectivePower();
                    int buffed = current + effect.amount;

                    // Only "useful" if the buff turns an <= matchup into a strictly winning one.
                    if (current <= oppMaxPower && buffed > oppMaxPower)
                    {
                        flipsMatchup = true;
                        break;
                    }
                }

                if (!flipsMatchup)
                {
                    Debug.Log($"[AIController] {effect.name} has targets but doesn’t improve any matchup vs opponent board → skipping.");
                    return false;
                }
            }
        }

        Debug.Log($"[AIController] PowerChangeEffect {effect.name} deemed USEFUL. " +
                  $"boardTargets={boardTargets}, handTargets={handTargets}, " +
                  $"futureHandMatches={futureHandMatches}, oppMax={oppMaxPower}");
        return true;
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

    // Hand-effects are currently NOT used in KULO.
    // The previous implementation was incorrectly applying real spell effects
    // (like It's Food Time / Cat Toy) to the AI's cards while they were still
    // in hand, effectively giving the AI free buffs during evaluation.
    //
    // Leaving the call in AIPlay() is harmless now because this method is a no-op.
    private void ApplyEffectsToAIHandIfNeeded()
    {
        // Intentionally empty.
        // If you ever add true "while in hand" auras, we can re-implement this
        // with a flag on CardEffect (e.g. bool applyWhileInHand) and only
        // process those specific effects here.
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
    // Chooses a move based on aggressive, blocking, or random strategies.
    // For most SPELLS, just pick a simple empty cell (spells don't count for wins).
    private Vector2Int ChooseMove(CardSO[,] grid, CardSO candidate)
    {
        bool isSelfDestruct = IsSelfDestructCard(candidate);

        // For normal spells the board position does not matter.
        // Self-destruct spells (like X1 Damiano, if you ever make him a spell)
        // are handled via the aggressive / replacement logic instead.
        if (candidate != null &&
            candidate.category == CardSO.CardCategory.Spell &&
            !isSelfDestruct)
        {
            return FindRandomMove(grid);
        }

        // For creatures and "removal-style" cards:
        Vector2Int move = FindBestAggressiveMove(grid, candidate);

        // Self-destruct cards are removal-only:
        // if we didn't find a replacement target, don't waste them on
        // blocking or random empty cells.
        if (!isSelfDestruct && move.x == -1)
            move = FindBlockingMove(grid);
        if (!isSelfDestruct && move.x == -1)
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

    // Returns true if the player currently has any "two-in-a-row with an empty cell"
    // patterns that the AI should worry about.
    private bool HasImmediatePlayerThreat(CardSO[,] grid)
    {
        if (grid == null) return false;

        List<Vector2Int> blocking = FindBlockingMoves(grid);
        return blocking != null && blocking.Count > 0;
    }

    // Returns the set of PLAYER-owned cells that are part of any "two-in-a-row"
    // line the player has (i.e. cells that, if destroyed, also break the line).
    private HashSet<Vector2Int> FindPlayerCriticalCells(CardSO[,] grid)
    {
        var result = new HashSet<Vector2Int>();
        if (grid == null) return result;

        // Start from the places we'd block – then find the two player cards
        // that form the rest of each line.
        List<Vector2Int> blockingMoves = FindBlockingMoves(grid);
        foreach (var empty in blockingMoves)
        {
            int x = empty.x;
            int y = empty.y;

            // --- Row ---
            List<Vector2Int> rowPlayerCells = new List<Vector2Int>();
            for (int cx = 0; cx < 3; cx++)
            {
                if (grid[cx, y] != null && GetOwnerTagFromCell(cx, y) == "Player")
                    rowPlayerCells.Add(new Vector2Int(cx, y));
            }
            if (rowPlayerCells.Count >= 2)
            {
                result.Add(rowPlayerCells[0]);
                result.Add(rowPlayerCells[1]);
            }

            // --- Column ---
            List<Vector2Int> colPlayerCells = new List<Vector2Int>();
            for (int cy = 0; cy < 3; cy++)
            {
                if (grid[x, cy] != null && GetOwnerTagFromCell(x, cy) == "Player")
                    colPlayerCells.Add(new Vector2Int(x, cy));
            }
            if (colPlayerCells.Count >= 2)
            {
                result.Add(colPlayerCells[0]);
                result.Add(colPlayerCells[1]);
            }

            // --- Main diagonal (0,0)-(1,1)-(2,2) ---
            if (x == y)
            {
                List<Vector2Int> diagPlayerCells = new List<Vector2Int>();
                for (int d = 0; d < 3; d++)
                {
                    if (grid[d, d] != null && GetOwnerTagFromCell(d, d) == "Player")
                        diagPlayerCells.Add(new Vector2Int(d, d));
                }
                if (diagPlayerCells.Count >= 2)
                {
                    result.Add(diagPlayerCells[0]);
                    result.Add(diagPlayerCells[1]);
                }
            }

            // --- Anti-diagonal (0,2)-(1,1)-(2,0) ---
            if (x + y == 2)
            {
                List<Vector2Int> antiPlayerCells = new List<Vector2Int>();
                int[,] coords = new int[,] { { 0, 2 }, { 1, 1 }, { 2, 0 } };
                for (int i = 0; i < 3; i++)
                {
                    int ax = coords[i, 0];
                    int ay = coords[i, 1];
                    if (grid[ax, ay] != null && GetOwnerTagFromCell(ax, ay) == "Player")
                        antiPlayerCells.Add(new Vector2Int(ax, ay));
                }
                if (antiPlayerCells.Count >= 2)
                {
                    result.Add(antiPlayerCells[0]);
                    result.Add(antiPlayerCells[1]);
                }
            }
        }

        return result;
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

        // NEW: track how "good" the chosen creature is
        int bestCreatureScore = int.MinValue;

        var handSnapshot = (pm != null && pm.cardHandlers != null)
            ? pm.cardHandlers.ToArray()
            : System.Array.Empty<CardHandler>();

        foreach (var ch in handSnapshot)
        {
            if (ch == null || ch.cardData == null) continue;

            CardSO data = ch.cardData;

            // ---------------- CREATURES ----------------
            if (data.category == CardSO.CardCategory.Creature &&
                !TurnManager.instance.creaturePlayed &&
                IsCardPlayable(data))
            {
                bool isSelfDestruct = IsSelfDestructCard(data);   // X1 Damiano, etc.
                int score = data.power;                           // base: higher power is better

                // Don't waste self-destruct cards when there is nothing to hit
                if (isSelfDestruct)
                {
                    bool anyEnemyOnBoard = false;
                    for (int x = 0; x < 3 && !anyEnemyOnBoard; x++)
                        for (int y = 0; y < 3 && !anyEnemyOnBoard; y++)
                            if (grid[x, y] != null && GetOwnerTagFromCell(x, y) == "Player")
                                anyEnemyOnBoard = true;

                    if (!anyEnemyOnBoard)
                        score -= 10000; // huge penalty if it'd just suicide for no reason
                }

                if (score > bestCreatureScore)
                {
                    bestCreatureScore = score;
                    playableCreature = ch;   // ✅ now this will be Large Happy Zui over Mole Cat Girlfriend
                }
            }

            // ---------------- SPELLS (unchanged) ----------------
            if (data.category == CardSO.CardCategory.Spell &&
                !TurnManager.instance.spellPlayed &&
                IsCardPlayable(data))
            {
                bool isDamiano = data.effects != null &&
                                 data.effects.Exists(e => e != null && e.GetType().Name == "X1DamianoEffect");

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
    // Returns the best aggressive move for the AI given the current grid and the candidate card.
    // Returns the best aggressive move for the AI given the current grid
    // and the candidate card.
    private Vector2Int FindBestAggressiveMove(CardSO[,] grid, CardSO candidate)
    {
        if (grid == null || candidate == null)
            return new Vector2Int(-1, -1);

        bool isSelfDestruct = IsSelfDestructCard(candidate);

        // 1. If this is NOT a self-destruct card, first see if we can win immediately.
        if (!isSelfDestruct)
        {
            Vector2Int winningMove = FindWinningMove(grid);
            if (winningMove.x != -1)
                return winningMove;
        }

        // Cells containing player cards that are part of "two-in-a-row" threats.
        HashSet<Vector2Int> criticalEnemyCells = FindPlayerCriticalCells(grid);

        List<Vector2Int> criticalReplaceable = new List<Vector2Int>();
        List<Vector2Int> replaceable = new List<Vector2Int>();
        List<Vector2Int> open = new List<Vector2Int>();

        // 2. Scan all cells.
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                CardSO cellCard = grid[x, y];

                if (cellCard != null)
                {
                    // Only consider replacing PLAYER-owned cards.
                    if (GetOwnerTagFromCell(x, y) != "Player")
                        continue;

                    // Can this card actually beat or match the target, according to our
                    // replacement rules?
                    if (!CheckForReplacement(candidate, cellCard))
                        continue;

                    Vector2Int pos = new Vector2Int(x, y);

                    // Massive priority: kill pieces that are part of your 2-in-a-row.
                    if (criticalEnemyCells.Contains(pos))
                    {
                        // Extra priority for the centre if it happens to be critical.
                        if (x == 1 && y == 1)
                            return pos;

                        criticalReplaceable.Add(pos);
                    }
                    else
                    {
                        // Still slightly prefer centre even if it's not in a line.
                        if (x == 1 && y == 1)
                            return pos;

                        replaceable.Add(pos);
                    }
                }
                else
                {
                    // Only non-self-destruct cards should ever go into empty cells.
                    if (!isSelfDestruct)
                        open.Add(new Vector2Int(x, y));
                }
            }
        }

        // 3. If we can destroy a creature that is part of a dangerous player line,
        // do that before anything else.
        if (criticalReplaceable.Count > 0)
            return criticalReplaceable[0];

        // 4. For normal cards, also consider pure "blocking" moves
        // (empty cells that stop two-in-a-row).
        if (!isSelfDestruct)
        {
            List<Vector2Int> blocking = FindBlockingMoves(grid);
            if (blocking.Count > 0)
                return blocking[0];
        }

        // 5. Otherwise, replace *any* player card we can.
        if (replaceable.Count > 0)
            return replaceable[0];

        // 6. Finally, for non-self-destruct cards, just take an open cell.
        if (!isSelfDestruct && open.Count > 0)
            return open[0];

        // 7. For self-destruct cards, if we got here, there was nothing useful
        // to replace, so don't play it.
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
        bool isSelfDestruct = IsSelfDestructCard(handler?.cardData);

        if (move.x != -1)
        {
            Debug.Log($"AI plays {(isSpell ? "spell" : "creature")} {handler.cardData.cardName} at {move.x}, {move.y}");
            placed = PlaceAICardOnGrid(move.x, move.y, handler);
        }

        // For self-destruct cards like X1 Damiano, NEVER fall back to a random empty cell.
        // If we couldn't find a good removal target, just keep it in hand.
        if (!placed && !isSelfDestruct)
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
