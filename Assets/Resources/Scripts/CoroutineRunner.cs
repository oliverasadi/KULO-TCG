using UnityEngine;
using System.Collections;

public class CoroutineRunner : MonoBehaviour
{
    public static CoroutineRunner Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Optionally, make this persistent
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RunCoroutine(IEnumerator routine)
    {
        StartCoroutine(routine);
    }
}
