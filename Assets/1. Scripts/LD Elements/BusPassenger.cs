using UnityEngine;

public class BusPassenger : MonoBehaviour
{
    public GameObject menuUI;
    
    public void OpenMenu()
    {
        menuUI.SetActive(true);
        GetComponent<FPSControllerMulti>().StartFreeze();
    }

    public void SelectBank(string colorBank)
    {
        BankManager.instance.TeleportNextBank(colorBank, transform);
        menuUI.SetActive(false);
        GetComponent<FPSControllerMulti>().StopFreeze();
    }

    public void CancelTP()
    {
        menuUI.SetActive(false);
        GetComponent<FPSControllerMulti>().StopFreeze();
    }
}
