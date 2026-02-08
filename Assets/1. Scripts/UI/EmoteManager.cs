using UnityEngine;
using UnityEngine.UI;

public class EmoteManager : MonoBehaviour
{
    public Sprite[] emotes;

    public Sprite GetEmoteByIndex(int index)
    {
        if(index >= 0 && index < emotes.Length)
            return emotes[index];
        return null;
    }
}
