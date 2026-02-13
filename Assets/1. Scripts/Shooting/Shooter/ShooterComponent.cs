using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Shooting
{

    public class ShooterComponent : NetworkBehaviour
    {
        #region Properties
        public int AmmoCount => _currentAmmo;
        public int MaxAmmoCount => _maxAmmo;
        public float MaxDistance => maxDistance;
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
        [SerializeField] private int _maxAmmo = 100000;

        [Header("Events")]
        [SerializeField] private UnityEvent _onShoot;
        [SerializeField] private UnityEvent _onHit;

        // Private fields
        private int _currentAmmo;
        private int _previousAmmoCount;
        private bool _enableCallbacks;
        private Camera _playerCamera;
        [SerializeField] private Transform _instantPos;
        [SerializeField] private float reloadingTime = 2f;
        [SerializeField] private float maxDistance = 100f;
        [SerializeField] private Animator gunAnimator;
        [SerializeField] private GameObject muzzleFlashEffect;
        private float elpased = 0;
        [SerializeField] public bool canShoot = false;
        [SerializeField] private float FireRate = 0.1f;

        [SerializeField] private AudioClip shootClip;
        
        #endregion

        #region Methods
        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;
            
            _shooter.OnShoot += OnShoot.Invoke;
            _shooter.OnTargetHit += (target) => OnTargetHit.Invoke(target);
            _shooter.OnTargetHit += _ => OnHit.Invoke();

            OnShoot.AddListener(() => EventBus.Invoke("OnPlayerShoot"));
            
            _shooter.OnShoot += MakeTrail;
            Reload();
        }

        public void Reload()
        {
            _currentAmmo = _maxAmmo;
            gunAnimator.SetTrigger("Reload");
            
        }

        public void StartToReload()
        {
            StartCoroutine(Reloading());
        }
        
        public IEnumerator Reloading()
        {
            FPSControllerMulti a = GetComponent<FPSControllerMulti>();
            
            if(a.animator)
                a.animator.SetTrigger("Reload");
            
            a.StartFreeze();
            SFX_Manager.instance.PlaySFX(5);
            Reload();
            Image circleCD = VariableManager.instance.circleCD;
            float count = reloadingTime;
            while (count > 0)
            {
                count -= Time.deltaTime;
                yield return null;
                circleCD.fillAmount =  count / reloadingTime;
            }
            a.StopFreeze();
        }

        private void MakeTrail()
        {
            if(GetComponent<FPSControllerMulti>().hasSomethingInHand)
                return;
            
            if (GetComponent<FPSControllerMulti>().isMapActive)
                return;
            
            if (GetComponent<FPSControllerMulti>().IsFreeze)
                return;

            Camera camera = GetComponent<FPSControllerMulti>().MyCamera();
            Vector3 startPos = _instantPos.position;
            Vector3 endPos = startPos + camera.transform.forward * MaxDistance;

            if (IsOwner)
            {
                ShootTrailServerRpc(startPos, endPos);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void ShootTrailServerRpc(Vector3 start, Vector3 end)
        {
            ShootTrailClientRpc(start, end);
        }

        [ClientRpc]
        private void ShootTrailClientRpc(Vector3 start, Vector3 end)
        {
            if (TryGetComponent(out TrailMaker maker))
            {
                maker.Make(start, end, GetComponent<PlayerCustom>().colorPlayer.Value);
            }
        }
      
        private void Update()
        {
            if (!IsOwner) return;

            if (elpased > 0)
            {
                canShoot = false;
                elpased -= Time.deltaTime;
            }
            else
            {
                canShoot = true;
                GetComponent<FPSControllerMulti>().animator.SetBool("Shoot", false);
            }
            
            HandleInputs();

            CheckDirty();
        }

        private void CheckDirty()
        {
            if (_previousAmmoCount != _currentAmmo)
            {
                _previousAmmoCount = _currentAmmo;
                EventBus.Invoke("AmmoCount_DirtyFlag", new DataPacket(_currentAmmo));
            }
        }

        private void HandleInputs()
        {
            if (Input.GetMouseButtonDown(0))
                TryShoot();
        }

        public bool TryShoot()
        {
            FPSControllerMulti fps = GetComponent<FPSControllerMulti>();
            
            if(fps.hasSomethingInHand)
                return false;

            if (fps.isMapActive)
                return false;
            
            if (fps.IsFreeze)
                return false;

            if (fps.isDriver)
                return false;
            
            if (_currentAmmo <= 0)
            {
                SFX_Manager.instance.PlaySFX(4);
                return false;
            }
            
            if(!canShoot) return false;

            Camera camera = GetComponent<FPSControllerMulti>().MyCamera();
            
            Vector3 Pos = camera.transform.position;
            Vector3 Dir = camera.transform.forward;
            List<BulletInfo> bullets;
            bool didShoot = _shooter.TryShoot(Pos, Dir, out bullets);
            if (didShoot)
            {
                WarnShotTargets(bullets);
                _currentAmmo--;
            }
            
            gunAnimator.SetTrigger("Shoot");
            fps.animator.SetBool("Shoot", true);

            SpawnMuzzleFlashLocal();

            elpased = FireRate;

            return didShoot;
        }
        
        private void SpawnMuzzleFlashLocal()
        {
            GameObject muzzleFlash = Instantiate(muzzleFlashEffect, _instantPos.position, Quaternion.identity);
            muzzleFlash.transform.SetParent(_instantPos);
            SFX_Manager.instance.PlaySFX(3);
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