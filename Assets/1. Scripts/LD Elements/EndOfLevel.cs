using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine; 

public class EndOfLevel : MonoBehaviour
{
    public bool truckInEndZone = false;
    public List<GameObject> treasures = new List<GameObject>();
    public int moneyTransferred = 0;
    
    public TMP_Text zoneText;
    public TMP_Text timerText;
    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Truck"))
        {
            zoneText.text = ("Entering end zone...");
            StopCoroutine(FadeText(zoneText));
            zoneText.alpha = 1;
            
            truckInEndZone = true;
            
            StartCoroutine(FadeText(zoneText));
            StartCoroutine(EndLevel());
        }
        else if (other.CompareTag("Treasure"))
        {
            treasures.Add(other.gameObject);
        }
    }
    
    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Truck"))
        {
            zoneText.text = ("Leaving end zone...");
            StopCoroutine(FadeText(zoneText));
            zoneText.alpha = 1;
            
            truckInEndZone = false;
            
            StartCoroutine(FadeText(zoneText));
            StartCoroutine(EndLevel());
        }
        else if (other.CompareTag("Treasure"))
        {
            treasures.Remove(other.gameObject);
        }
    }

    private IEnumerator FadeText(TMP_Text text)
    {
        while (text.alpha > 0)
        {
            text.alpha -= (Time.deltaTime * 0.5f);
            yield return null;
        }
    }

    private IEnumerator EndLevel()
    {
        StopCoroutine(FadeText(timerText));
        timerText.alpha = 1;
        
        for(float i=0; i < 5; i += Time.deltaTime)
        {
            if (truckInEndZone)
            {
                timerText.text = (5-(int)i).ToString();
                yield return null; 
            }
            else
            {
                timerText.text = ("Cancelled...");
                StartCoroutine(FadeText(timerText));
                yield break;
            }
        }
        
        moneyTransferred = treasures.Count;
        
        zoneText.alpha = 0;
        timerText.text =( "You successfully transferred " + moneyTransferred + "00 000 dollars !");
        Time.timeScale = 0;
    }
}
