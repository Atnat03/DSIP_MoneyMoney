

using System.Collections.Generic;
using TMPro;
using UnityEngine;

public abstract class UIElement : MonoBehaviour
{
    public abstract void UpdateUI();
    public abstract void Enable();
    public abstract void Disable();

    protected virtual void UpdateText(TMP_Text comp, string text, string replacement, string markup)
    {
        if (comp != null)
            comp.text = text.Replace(markup, replacement);
    }

    public abstract void BindEvents();

    protected virtual string Get(Data data) => DataFetcher.GetString(data);
}