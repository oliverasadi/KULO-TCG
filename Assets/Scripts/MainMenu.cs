using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public Button hostButton;
    public Button joinButton;
    public Button singlePlayerButton;

    void Start()
    {
        hostButton.onClick.AddListener(HostGame);
        joinButton.onClick.AddListener(JoinGame);
        singlePlayerButton.onClick.AddListener(StartSinglePlayer);
    }

    public void HostGame()
    {
        if (NetworkManager.singleton != null)
        {
            NetworkManager.singleton.StartHost();
        }
        else
        {
            Debug.LogError("NetworkManager is missing from the scene.");
        }
    }

    public void JoinGame()
    {
        if (NetworkManager.singleton != null)
        {
            NetworkManager.singleton.StartClient();
        }
        else
        {
            Debug.LogError("NetworkManager is missing from the scene.");
        }
    }

    public void StartSinglePlayer()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }
}
