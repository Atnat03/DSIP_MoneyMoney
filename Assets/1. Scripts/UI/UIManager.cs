

using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour, IManager
{
    #region Fields

    [SerializeField] private List<UIElement> _uiElements = new(); 

    #endregion

    #region Methods
    public void Register() => Reference.SetManager(this);
    public void Unregister() => Reference.RemoveManager(this);

    private void Start()
    {
        UpdateUI();
        SubscribeToAll();
    }

    /// <summary>
    /// Ensures every UI Element is updated when these events are called.
    /// </summary>
    private void SubscribeToAll()
    {
        foreach (UIElement uiElement in _uiElements)
        {
            uiElement.BindEvents();
        }
    }

    public void UpdateUI()
    {
        foreach (var uiElement in _uiElements)
        {
            uiElement.UpdateUI();
        }
    }
    #endregion
}