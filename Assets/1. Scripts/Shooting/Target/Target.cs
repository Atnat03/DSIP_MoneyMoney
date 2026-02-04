using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

namespace Shooting
{
    [RequireComponent(typeof(Collider))]
    public class Target : MonoBehaviour, ITarget
    {
        #region Properties
        public Action<BulletInfo> OnShot { get; set; }
        public Collider Collider { get; private set; }
        #endregion

        #region Fields
        [SerializeField] private UnityEvent _onShot;
        #endregion

        #region Methods

        private void Start()
        {
            Collider = GetComponent<Collider>();
            OnShot += (bullet) => _onShot.Invoke();
        }
        #endregion
    }

}