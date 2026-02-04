using UnityEngine;

public class UIController : MonoBehaviour
{
    public GameObject uiPause;
    public void OnInteract()
    {
        uiPause.SetActive(true);
    }

    public void OnStopInteract()
    {
        uiPause.SetActive(false);
    }
}
