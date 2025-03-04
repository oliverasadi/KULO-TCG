using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public virtual void StartTurn()
    {
        
    }
    
    public virtual void EndTurn()
    {
        TurnManager.instance.EndTurn();
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }
}
