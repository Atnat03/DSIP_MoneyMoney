using UnityEngine;

public class Selfdestroy : MonoBehaviour
{

   public float duration;
   void Start()
   {
      //Script de Nathan
      Destroy(gameObject, duration);
   }
}
