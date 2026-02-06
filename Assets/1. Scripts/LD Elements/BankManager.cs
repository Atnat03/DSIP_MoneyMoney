using UnityEngine;

public class BankManager : MonoBehaviour
{
    public static BankManager instance;
    
    public BankBehaviour[] banks;

    void Start()
    {
        instance = this;
    }

    public void TeleportNextBank(Transform player)
    {
        foreach (BankBehaviour bank in banks)
        {
            if (!bank.visited)
            {
                player.position = bank.spawnPlayer.position;
                break;
            }
        }
    }
}
