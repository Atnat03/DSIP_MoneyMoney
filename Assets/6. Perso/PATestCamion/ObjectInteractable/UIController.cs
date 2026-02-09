using TMPro;
using UnityEngine;

public class UIController : MonoBehaviour
{
    public TextMeshProUGUI uiPause;
    public GameObject uitextParent;
    
    public void OnInteract()
    {
        uitextParent.gameObject.SetActive(true);
    }

    public void OnStopInteract()
    {
        uitextParent.gameObject.SetActive(false);
    }

    public void SetText(string newText)
    {
        uiPause.text = newText;
    }
}
