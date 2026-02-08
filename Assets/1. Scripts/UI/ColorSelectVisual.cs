using System;
using UnityEngine;
using UnityEngine.UI;

public class ColorSelectVisual : MonoBehaviour
{
    public Button[] buttonList;

    private void Start()
    {
        foreach (Button go in buttonList)
        {
            go.GetComponent<Outline>().enabled = false;
        }
        
        buttonList[0].GetComponent<Outline>().enabled = true;
    }

    public void OnSelectButton(Button b)
    {
        foreach (Button go in buttonList)
        {
            go.GetComponent<Outline>().enabled = false;
        }

        b.GetComponent<Outline>().enabled = true;
    }
}
