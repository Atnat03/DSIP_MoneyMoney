using UnityEngine;
using UnityEngine.EventSystems;

public class Emote : MonoBehaviour, IPointerEnterHandler
{
    public int emoteIndex;
    public PlayerCustom targetPlayer;

    public void OnPointerEnter(PointerEventData eventData)
    {
        // On ne touche pas le NetworkVariable ici
        if(targetPlayer != null && targetPlayer.IsOwner)
        {
            targetPlayer.localSelectedEmoteIndex = emoteIndex;
        }
    }
}
