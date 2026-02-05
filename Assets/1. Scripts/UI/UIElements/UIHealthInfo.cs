using TMPro;
using UnityEngine;

public class UIHealthInfo : UIElement
{
    #region Fields
    [SerializeField] private string _markup = "XX";

    [Space]
    [SerializeField] private TMP_Text _currentHealth;
    [Tooltip("Write your text and put your markup where you want the actual value to be")]
    [SerializeField] private string _currentHealthText;

    [Space]
    [SerializeField] private TMP_Text _maxHealth;
    [Tooltip("Write your text and put your markup where you want the actual value to be")]
    [SerializeField] private string _maxHealthText;



    #endregion

    #region Methods

    public override void UpdateUI()
    {
        UpdateText(_currentHealth, _currentHealthText, Get(Data.CurrentHealth), _markup);
        UpdateText(_maxHealth, _maxHealthText, Get(Data.MaxHealth), _markup);
    }

    public override void Enable()
    {
        _currentHealth.enabled = true;
        _maxHealth.enabled = true;
    }
    public override void Disable()
    {
        _currentHealth.enabled = false;
        _maxHealth.enabled = false;
    }

    public override void BindEvents()
    {
        EventBus.Register("Health_DirtyFlag", (packet) => UpdateText(_currentHealth, _currentHealthText, packet.floatValue.ToString(), _markup));
    }

    #endregion


}
