using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Shooting
{

    public class ShooterComponent : MonoBehaviour
    {
        #region Properties

        public float MaxDistance { get; set; } = 1000f;
        // Invoked whenever the shooter successfully shoots
        public UnityEvent OnShoot => _onShoot;
        // Invoked when a target is hit, with said target as parameter
        [HideInInspector] public UnityEvent<TargetInfo> OnTargetHit { get; }
        public UnityEvent OnHit => _onHit;
        public bool EnableCallbacks { get; set; } = true;

        #endregion

        #region Fields
        IShooter shooter = new RaycastShooter();

        [SerializeField] private UnityEvent _onShoot;
        [SerializeField] private UnityEvent _onHit;
        #endregion

        #region Methods
        private void Start()
        {
            shooter.OnShoot.AddListener(OnShoot.Invoke);
            shooter.OnTargetHit.AddListener(OnTargetHit.Invoke);
            shooter.OnTargetHit.AddListener(_ => OnHit.Invoke());
        }

        public bool TryShoot()
        {
            Vector3 Pos = transform.position;
            Vector3 Dir = transform.forward;
            return shooter.TryShoot(Pos, Dir, out List<BulletInfo> tempData);
        }
        #endregion
    }

}