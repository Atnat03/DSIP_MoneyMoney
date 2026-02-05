

using Shooting;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIShooterInfo : UIElement
{
    #region Fields
    [SerializeField] private string _markup = "XX";

    [Space]
    [SerializeField] private TMP_Text _ammoCount;
    [Tooltip("Write your text and put your markup where you want the actual value to be")]
    [SerializeField] private string _ammoCountText;

    [Space]
    [SerializeField] private TMP_Text _maxAmmoCount;
    [Tooltip("Write your text and put your markup where you want the actual value to be")]
    [SerializeField] private string _maxAmmoCountText;
    


    #endregion

    #region Methods

    public override void UpdateUI()
    {
        UpdateText(_ammoCount, _ammoCountText, Get(Data.AmmoCount), _markup);
        UpdateText(_maxAmmoCount, _maxAmmoCountText, Get(Data.MaxAmmoCount), _markup);
    }

    public override List<string> GetBoundEvents()
    {
        return new List<string>()
        {
            "AmmoCount_DirtyFlag"
        };
    }

    public override void Enable()
    {
        _ammoCount.enabled = true;
        _maxAmmoCount.enabled = true;
    }
    public override void Disable()
    {
        _ammoCount.enabled = false;
        _maxAmmoCount.enabled = false;
    }

    #endregion

    
}
