using System;
using UnityEngine;

public static class Interact
{
    public static event Action<GameObject, GameObject> OnInteract;
    
    public static void RayInteract(GameObject obj,  GameObject player, string UINameInteraction)
    {
        OnInteract?.Invoke(obj, player);
        VariableManager.instance.uiController.SetText(UINameInteraction);
    }
}
