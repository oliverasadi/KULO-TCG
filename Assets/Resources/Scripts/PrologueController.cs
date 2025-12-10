using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PrologueController : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public ProloguePlayer prologuePlayerPrefab;   // your existing prefab (with its own Canvas)

    [Header("Skip popup (optional)")]
    [Tooltip("Prefab with SkipProloguePopup component, anchored bottom-right in its Canvas.")]
    public GameObject skipPopupPrefab;

    private AudioSource _voiceTemp;
    private ProloguePlayer _currentPlayer;

    private void Start()
    {
        if (!PrologueContext.IsValid)
        {
            Debug.LogWarning("[Prologue] No context set. Loading gameplay directly.");
            SceneManager.LoadScene("KULO", LoadSceneMode.Single);
            return;
        }

        // Apply selection data early so it's ready for gameplay load
        PlayerManager.selectedCharacterDeck = PrologueContext.deck;
        PlayerProfile.selectedCharacterName = PrologueContext.characterName;

        // Spawn the prologue player and hook to finish
        _currentPlayer = Instantiate(prologuePlayerPrefab);
        _currentPlayer.OnFinished.AddListener(OnPrologueFinished);
        _currentPlayer.Play(PrologueContext.sequence);

        // If this prologue has been watched before, show the skip popup
        if (HasSeenCurrentPrologue())
        {
            ShowSkipPopup();
        }
    }

    // ----------------------------------------------------------------------
    // Prologue watched tracking
    // ----------------------------------------------------------------------

    private string GetSeenKey()
    {
        // Use character name as part of the PlayerPrefs key
        var name = PrologueContext.characterName;
        if (string.IsNullOrEmpty(name))
            name = "UnknownCharacter";

        return $"PrologueSeen_{name}";
    }

    private bool HasSeenCurrentPrologue()
    {
        if (!PrologueContext.IsValid) return false;
        return PlayerPrefs.GetInt(GetSeenKey(), 0) == 1;
    }

    private void MarkCurrentPrologueSeen()
    {
        if (!PrologueContext.IsValid) return;

        PlayerPrefs.SetInt(GetSeenKey(), 1);
        PlayerPrefs.Save();
    }

    // Called by the ProloguePlayer when it finishes normally
    private void OnPrologueFinished()
    {
        // First time they get here we mark as “seen”
        MarkCurrentPrologueSeen();
        StartCoroutine(AfterPrologueRoutine());
    }

    // ----------------------------------------------------------------------
    // Skip popup
    // ----------------------------------------------------------------------

    private void ShowSkipPopup()
    {
        if (skipPopupPrefab == null)
        {
            Debug.LogWarning("[Prologue] skipPopupPrefab is not assigned – skip UI will not appear.");
            return;
        }

        // Try to parent the popup to the same Canvas the prologue uses.
        Canvas parentCanvas = null;

        if (_currentPlayer != null && _currentPlayer.canvasGroup != null)
            parentCanvas = _currentPlayer.canvasGroup.GetComponentInParent<Canvas>();

        if (parentCanvas == null)
            parentCanvas = FindObjectOfType<Canvas>();

        if (parentCanvas == null)
        {
            Debug.LogWarning("[Prologue] No Canvas found for skip popup.");
            return;
        }

        var popupObj = Instantiate(skipPopupPrefab, parentCanvas.transform, false);
        var popup = popupObj.GetComponent<SkipProloguePopup>();

        if (popup != null)
        {
            popup.Initialize(
                onYes: SkipPrologue,
                onNo: () =>
                {
                    // Do nothing – just let the prologue continue.
                    Debug.Log("[Prologue] Player chose not to skip.");
                });
        }
    }

    private void SkipPrologue()
    {
        Debug.Log("[Prologue] Skip requested via popup.");

        // Stop listening to the normal finish event so we don't double-load KULO.
        if (_currentPlayer != null)
        {
            _currentPlayer.OnFinished.RemoveListener(OnPrologueFinished);
            Destroy(_currentPlayer.gameObject);
            _currentPlayer = null;
        }

        // We DON'T mark as seen here – the flag is only set when they’ve actually
        // watched the full prologue once (OnPrologueFinished).
        StartCoroutine(AfterPrologueRoutine());
    }

    // ----------------------------------------------------------------------
    // Existing flow after the prologue
    // ----------------------------------------------------------------------

    private IEnumerator AfterPrologueRoutine()
    {
        // Optional: short voice line *after* prologue, before gameplay
        if (PrologueContext.voiceLineAfter != null)
        {
            if (_voiceTemp == null)
            {
                _voiceTemp = gameObject.AddComponent<AudioSource>();
                _voiceTemp.playOnAwake = false;
                _voiceTemp.spatialBlend = 0f;
            }
            _voiceTemp.clip = PrologueContext.voiceLineAfter;
            _voiceTemp.Play();
            yield return new WaitForSeconds(_voiceTemp.clip.length);
        }

        // Load gameplay
        SceneManager.LoadScene("KULO", LoadSceneMode.Single);
        PrologueContext.Clear();
    }
}
