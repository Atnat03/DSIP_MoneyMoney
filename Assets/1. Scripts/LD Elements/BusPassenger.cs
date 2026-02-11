using UnityEngine;
using UnityEngine.UI;

public class BusPassenger : MonoBehaviour
{
    public GameObject menuUI;
    public Image jaugeTime;
    public GameObject jaugeGlobale;


    
    public void OpenMenu()
    {
        menuUI.SetActive(true);
        GetComponent<FPSControllerMulti>().StartFreeze();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void SelectBank(string colorBank)
    {
        Debug.Log("aaa");
        menuUI.SetActive(false);
        GetComponent<FPSControllerMulti>().StopFreeze();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        BankManager.instance.TeleportNextBank(colorBank, transform);
    }

    public void CancelTP()
    {
        menuUI.SetActive(false);
        GetComponent<FPSControllerMulti>().StopFreeze();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
