using System.Collections.Generic;
using UI;
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
        [HideInInspector] public UnityEvent<ITarget> OnTargetHit { get; } = new();
        public UnityEvent OnHit => _onHit;
        public bool EnableCallbacks { get => shooter.EnableCallbacks; set => shooter.EnableCallbacks = value; }

        #endregion

        #region Fields
        IShooter shooter = new RaycastShooter();

        // REFACTO : Shouldn't have a reference to the UI
        [SerializeField] private UICrosshair _crosshair;
        int framesSinceUIUpdate = int.MaxValue/2;

        [SerializeField] private UnityEvent _onShoot;
        [SerializeField] private UnityEvent _onHit;
        #endregion

        #region Methods
        private void Start()
        {
            shooter.OnShoot += OnShoot.Invoke;
            shooter.OnTargetHit += (target) => OnTargetHit.Invoke(target);
            shooter.OnTargetHit += _ => OnHit.Invoke();

            // REFACTO : Remove deppendencies to UI
            CrosshairFeedbacks();
        }

        private void CrosshairFeedbacks()
        {
            if (_crosshair == null)
                return;
            shooter.OnShoot += _crosshair.SetShooting;
            shooter.OnShoot += () => { framesSinceUIUpdate = 0; };
            shooter.OnTargetHit += _ => _crosshair.SetHit();
            shooter.OnTargetHit += _ => { framesSinceUIUpdate = 0; };
        }

        private void Update()
        {
            // REFACTO : Feedbacks shouldn't be handled this way
            if (_crosshair != null)
            {
                framesSinceUIUpdate++;
                if (framesSinceUIUpdate > 30)
                    _crosshair.SetDefault();
            }

            HandleInputs();
        }

        private void HandleInputs()
        {
            if (Input.GetMouseButtonDown(0))
                TryShoot();
        }

        public bool TryShoot()
        {
            Camera camera = Camera.main;
            Vector3 Pos = camera.transform.position;
            Vector3 Dir = camera.transform.forward;
            List<BulletInfo> bullets;
            bool canShoot = shooter.TryShoot(Pos, Dir, out bullets);
            if (canShoot)
            {
                Debug.Log("Player has shot");
                WarnShotTargets(bullets);
            }
            else
                Debug.Log("Player failed to shoot");

            return canShoot;
        }

        /// <summary>
        /// Invokes feedbacks on every target that is shot.
        /// </summary>
        /// <param name="shotBullets"></param>
        private void WarnShotTargets(List<BulletInfo> shotBullets)
        {
            List<ITarget> targets = new ();
            List<BulletInfo> correspondingBullets = new ();
            foreach (var bullet in shotBullets)
            {
                foreach (ITarget target in bullet.HitTargets)
                {
                    if (!targets.Contains(target))
                    {
                        targets.Add(target);
                        correspondingBullets.Add(bullet);
                    }
                }
            }

            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                var bullet = correspondingBullets[i];
                target.OnShot.Invoke(bullet);
            }
        }
        #endregion
    }

}