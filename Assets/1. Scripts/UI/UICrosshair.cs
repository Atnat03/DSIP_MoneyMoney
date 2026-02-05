using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    [RequireComponent(typeof(Image))]
    public class UICrosshair : MonoBehaviour
    {

        #region Fields
        [Header("Shooting state")]
        [SerializeField] private Sprite _shootingSprite;
        [SerializeField] private Color _shootingColor;
        [SerializeField] private Vector2 _shootingSize;
        [Space]
        [Header("Hit state")]
        [SerializeField] private Sprite _hitSprite;
        [SerializeField] private Color _hitColor;
        [SerializeField] private Vector2 _hitSize;
        [Space]
        [Header("Disabled state")]
        [SerializeField] private Sprite _disabledSprite;
        [SerializeField] private Color _disabledColor;
        [SerializeField] private Vector2 _disabledSize;
        [Space]


        // Private
        private Image _targetImage;
        private Sprite _defaultSprite;
        private Color _defaultColor;
        private Vector2 _defaultSize;
        #endregion


        #region Methods


        private void ResetEveryStateToDefault()
        {
            _shootingSprite = _targetImage.sprite;
            _hitSprite = _targetImage.sprite;
            _disabledSprite = _targetImage.sprite;

            _shootingColor = _targetImage.color;
            _hitColor = _targetImage.color;
            _disabledColor = _targetImage.color;

            _shootingSize = _targetImage.rectTransform.sizeDelta;
            _hitSize = _targetImage.rectTransform.sizeDelta;
            _disabledSize = _targetImage.rectTransform.sizeDelta;
        }

        private void Start()
        {
            _targetImage = GetComponent<Image>();
            _defaultSprite = _targetImage.sprite;
            _defaultColor = _targetImage.color;
            _defaultSize = _targetImage.rectTransform.sizeDelta;

            EventBus.Register("OnPlayerShoot", SetShooting);
            EventBus.Register("OnPlayerShoot", () => this.WaitAndDo(0.2f, SetDefault));
        }

        public void SetState(CrosshairState state)
        {
            switch (state)
            {

                case CrosshairState.Default:
                    SetDefault();
                    break;
                case CrosshairState.Disabled:
                    SetDisabled();
                    break;
                case CrosshairState.Hit:
                    SetHit();
                    break;
                case CrosshairState.Shooting:
                    SetShooting();
                    break;
            }
        }


        public void SetDefault()
        {
            _targetImage.sprite = _defaultSprite;
            _targetImage.color = _defaultColor;
            _targetImage.rectTransform.sizeDelta = _defaultSize;
        }
        public void SetShooting()
        {
            _targetImage.sprite = _shootingSprite;
            _targetImage.color = _shootingColor;
            _targetImage.rectTransform.sizeDelta = _shootingSize;
        }
        public void SetHit()
        {
            _targetImage.sprite = _hitSprite;
            _targetImage.color = _hitColor;
            _targetImage.rectTransform.sizeDelta = _hitSize;
        }
        public void SetDisabled()
        {
            _targetImage.sprite = _disabledSprite;
            _targetImage.color = _disabledColor;
            _targetImage.rectTransform.sizeDelta = _disabledSize;
        }

        

        #endregion
    }

    public enum CrosshairState
    {
        Default,
        Shooting,
        Hit,
        Disabled
    }
}