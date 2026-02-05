using TMPro;
using UnityEngine;

namespace UI
{
    public class Tooltip : MonoBehaviour
    {
        #region Properties

        #endregion

        #region Fields
        [Header("Tooltip references")]
        [Tooltip("Text component to update")]
        [SerializeField] private TMP_Text _textComponent;
        [Space]
        [Header("Tooltip parameters")]
        [Tooltip("Should the tooltip always face the camera ?")]
        [SerializeField] private bool _billboard;
        [Tooltip("Should the Z axis be flipped ?")]
        [SerializeField] private bool _reverseZ;

        private Transform _camera;
        #endregion

        #region Methods

        public void SetText(string text)
        {
            EnforceReferences();
            _textComponent.text = text;
        }

        private bool EnforceReferences()
        {
            if (_textComponent == null)
            {
                Debug.LogError("[Tooltip] (" + name + ") : Please reference a text component in the inspector");
                return false;
            }
            if (Camera.main != null)
            {
                _camera = Camera.main.transform;
            }
            else return false;
            return true;
        }

        private void Update()
        {
            if (_camera == null) EnforceReferences();
            if (_camera == null) return;
            if (_billboard)
            {
                transform.rotation = Quaternion.LookRotation(_camera.position - transform.position, Vector3.up);
                if (_reverseZ) transform.localRotation = transform.localRotation * Quaternion.Euler(new Vector3(0, 180, 0));
            }
        }
        #endregion
    }
}