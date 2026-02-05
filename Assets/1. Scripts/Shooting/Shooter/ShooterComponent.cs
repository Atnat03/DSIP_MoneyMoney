using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

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
        public bool EnableCallbacks { get => _enableCallbacks; set { _enableCallbacks = value; _shooter.EnableCallbacks = value; } }

        #endregion

        #region Fields
        IShooter _shooter = new RaycastShooter();

        [Header("Parameters")]
        [SerializeField] private int _maxAmmo = 10;

        [Header("References")]
        // REFACTO : Shouldn't have a reference to the UI
        [SerializeField] private UICrosshair _crosshair;
        [SerializeField] private LineRenderer _shotTrailPrefab;

        [Header("Events")]
        [SerializeField] private UnityEvent _onShoot;
        [SerializeField] private UnityEvent _onHit;

        // Private fields
        private List<LineRenderer> _trails = new();
        int _framesSinceUIUpdate = int.MaxValue/2;
        private int _currentAmmo;
        private bool _enableCallbacks;
        #endregion

        #region Methods
        private void Start()
        {
            _shooter.OnShoot += OnShoot.Invoke;
            _shooter.OnTargetHit += (target) => OnTargetHit.Invoke(target);
            _shooter.OnTargetHit += _ => OnHit.Invoke();

            OnShoot.AddListener(() => GameSystem.EventBus.Invoke("OnPlayerShoot"));
            OnShoot.AddListener(() => { _framesSinceUIUpdate = 0; });

            // REFACTO : Remove deppendencies to UI
            ApplyFeedbacks();

            Reload();
        }

        public void Reload() => _currentAmmo = _maxAmmo;

        private void ApplyFeedbacks()
        {
            /*
            if (_crosshair != null)
            {
                _shooter.OnShoot += _crosshair.SetShooting;
                _shooter.OnShoot += () => { _framesSinceUIUpdate = 0; };
                _shooter.OnTargetHit += _ => _crosshair.SetHit();
                _shooter.OnTargetHit += _ => { _framesSinceUIUpdate = 0; };
            }
            */

            _shooter.OnShoot += MakeTrail;
            
        }
        private void MakeTrail()
        {
            if (_shotTrailPrefab != null)
            {
                LineRenderer trail = GameObject.Instantiate(_shotTrailPrefab);
                trail.transform.position = Vector3.zero;
                Vector3[] positions = new Vector3[2];
                positions[0] = Camera.main.transform.position;
                positions[1] = Camera.main.transform.position + Camera.main.transform.forward * MaxDistance;
                trail.SetPositions(positions);
                _trails.Add(trail);
            }
        }
        private void OnDisable()
        {
            RemoveFeedbacks();
        }
        private void Update()
        {
            // REFACTO : Feedbacks shouldn't be handled this way
            _framesSinceUIUpdate++;
            if (_framesSinceUIUpdate > 30)
                RemoveFeedbacks();
            

            HandleInputs();
        }

        private void RemoveFeedbacks()
        {
            if (_crosshair != null)
                _crosshair.SetDefault();
            foreach (var trail in _trails)
            {
                if (trail != null)
                    Destroy(trail);
            }
        }

        private void HandleInputs()
        {
            if (Input.GetMouseButtonDown(0))
                TryShoot();
        }

        public bool TryShoot()
        {
            if (_currentAmmo <= 0)
            {
                return false;
            }

            Camera camera = Camera.main;
            Vector3 Pos = camera.transform.position;
            Vector3 Dir = camera.transform.forward;
            List<BulletInfo> bullets;
            bool didShoot = _shooter.TryShoot(Pos, Dir, out bullets);
            if (didShoot)
            {
                Debug.Log("Player has shot");
                WarnShotTargets(bullets);
                _currentAmmo--;
            }
            else
                Debug.Log("Player failed to shoot");

            return didShoot;
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