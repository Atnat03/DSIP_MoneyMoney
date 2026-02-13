using UnityEngine;

public class BankManager : MonoBehaviour
{
    public static BankManager instance;
    
    public BankBehaviour[] banks;

    void Start()
    {
        instance = this;
    }
    public int BankVisited()
    {
        int count = 0;
        foreach (BankBehaviour bank in banks)
        {
            if (bank.visited) count++;
        }
        if (count == 0) return 1;
        return count;
    }

    public void TeleportNextBank(string coloBank, Transform player)
    {
        foreach (BankBehaviour bank in banks)
        {
            if (bank.colorBank == coloBank)
            {
                player.position = bank.spawnPlayer.position;
                break;
            }
        }
    }
}
