using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine; 

public class EndOfLevel : MonoBehaviour
{
    public bool truckInEndZone = false;
    public List<GameObject> treasures = new List<GameObject>();
    public int moneyTransferred = 0;
    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Truck"))
        {
            print("entering end zone");
            truckInEndZone = true;
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
            print("leaving end zone");
            truckInEndZone = false;
        }
        else if (other.CompareTag("Treasure"))
        {
            treasures.Remove(other.gameObject);
        }
    }
    
    public IEnumerator EndLevel()
    {
        print("end zone");
        
        for(float i=0; i < 5; i += Time.deltaTime)
        {
            if (truckInEndZone)
            { 
                yield return null; 
            }
            else
            {
                yield break;
            }
        }
        
        moneyTransferred = treasures.Count;
        
        print( "You successfully transferred " + moneyTransferred + "00 000 dollars !");
    }
}
