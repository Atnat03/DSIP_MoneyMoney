using UnityEngine;
using UnityEngine.UI;

public class VariableManager : MonoBehaviour
{
    public Image circleCD;
    public UIController uiController;
    
    public static VariableManager instance;
    public Image healthBar;
    
    void Start()
    {
        instance = this;
    }
    
}
