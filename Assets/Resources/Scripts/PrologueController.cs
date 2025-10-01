using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PrologueController : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public ProloguePlayer prologuePlayerPrefab;   // your existing prefab (with its own Canvas)

    private AudioSource _voiceTemp;

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
        var player = Instantiate(prologuePlayerPrefab);
        player.OnFinished.AddListener(() => StartCoroutine(AfterPrologueRoutine()));
        player.Play(PrologueContext.sequence);
    }

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
