

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class ExtensionMethods
{
    public static void WaitAndDo(this MonoBehaviour behaviour, float duration, Action callback)
    {
        behaviour.StartCoroutine(WaitAndDo(duration, callback));
    }
    private static IEnumerator WaitAndDo(float duration, Action callback)
    {
        Debug.Log("Waiting " + duration + "s before action");
        for (float d = 0; d < duration; d += Time.deltaTime)
            yield return null;
        callback.Invoke();
    }

}