using TMPro;
using UnityEngine;

public class UIController : MonoBehaviour
{
    public TextMeshProUGUI uiPause;
    public void OnInteract()
    {
        uiPause.transform.parent.gameObject.SetActive(true);
    }

    public void OnStopInteract()
    {
        uiPause.transform.parent.gameObject.SetActive(false);
    }

    public void SetText(string newText)
    {
        uiPause.text = newText;
    }
}
