using UnityEngine;
using Mirror;

public class PlayerNetwork : NetworkBehaviour
{
    public static PlayerNetwork instance;

    [SyncVar] public int playerID; // Syncs player ID across the network
    [SyncVar] public bool isTurn;  // Syncs turn info across the network

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public override void OnStartLocalPlayer()
    {
        CmdSetPlayerID();
    }

    [Command]
    void CmdSetPlayerID()
    {
        playerID = NetworkServer.connections.Count;
        Debug.Log("Player " + playerID + " has joined.");
    }

    public void TakeTurn()
    {
        if (!isTurn) return;

        Debug.Log("Player " + playerID + " is taking their turn.");
        isTurn = false;

        CmdEndTurn();
    }

    [Command]
    void CmdEndTurn()
    {
        RpcSwitchTurn();
    }

    [ClientRpc]
    void RpcSwitchTurn()
    {
        isTurn = !isTurn; // Swap turns
        Debug.Log("Turn switched. Player " + playerID + " is now playing.");
    }
}
