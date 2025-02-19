using UnityEngine;
using Mirror;

public class NetworkManagerKULO : NetworkManager
{
    public static NetworkManagerKULO instance;
    public override void Awake()
    {
        base.Awake();
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);
        PlayerNetwork player = conn.identity.GetComponent<PlayerNetwork>();
        player.playerID = numPlayers; // Assigns player 1 or 2
    }

    public override void OnClientDisconnect()
    {
        Debug.Log("Player Disconnected");
    }
}
