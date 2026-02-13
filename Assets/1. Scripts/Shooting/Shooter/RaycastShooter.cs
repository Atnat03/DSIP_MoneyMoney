using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shooting
{

    public class RaycastShooter : IShooter
    {
        #region Properties

        public float MaxDistance { get; set; } = 1000f;
        // Invoked whenever the shooter successfully shoots
        public Action OnShoot { get; set; }
        // Invoked when a target is hit, with said target as parameter
        public Action<ITarget> OnTargetHit { get; set; }
        public bool EnableCallbacks { get; set; } = true;
        // Penetration allows a bullet to pass through and touch multiple targets 
        public bool EnablePenetration { get; set; } = false;

        #endregion


        /// <summary>
        /// Checks if it's possible to shoot, and does it if so
        /// </summary>
        /// <param name="Pos"></param>
        /// <param name="Dir"></param>
        /// <param name="bulletInfos"></param>
        /// <returns>True if a shot was taken, false otherwise</returns>
        public bool TryShoot(Vector3 Pos, Vector3 Dir, out List<BulletInfo> bulletInfos)
        {
            bulletInfos = Shoot(Pos, Dir);
            return true;
        }

        private List<BulletInfo> Shoot(Vector3 Pos, Vector3 Dir)
        {
            // Add logic here to replace raycast shots by physics shots
            OnShoot_Callback();
            var result = ShootByRaycast(Pos, Dir);
            return result;
        }

        // Overload in case we don't need the first target specifically
        private List<BulletInfo> ShootByRaycast(Vector3 Pos, Vector3 Dir)
            => ShootByRaycast(Pos, Dir, out ITarget tempData);

        /// <summary>
        /// By default, shoots a single bullet. Can be overloaded to shoot multiple bullets (like a shotgun)
        /// </summary>
        /// <param name="Pos"></param>
        /// <param name="Dir"></param>
        /// <param name="firstTarget"></param>
        /// <returns>The infos of all the bullets shot</returns>
        private List<BulletInfo> ShootByRaycast(Vector3 Pos, Vector3 Dir, out ITarget firstTarget)
        {
            List<BulletInfo> result = new();
            BulletInfo singleBullet = ShootBulletByRaycast(Pos, Dir, out bool tempData);
            result.Add(singleBullet);

            if (singleBullet.HitTargets.Count > 0)
                firstTarget = singleBullet.HitTargets[0];
            else
                firstTarget = null;

            return result;
        }

        private BulletInfo ShootBulletByRaycast(Vector3 Pos, Vector3 Dir, out bool hasHit)
        {
            hasHit = false;
            BulletInfo result = new BulletInfo(true);

            if (EnablePenetration)
            {
                RaycastHit[] hitInfos = Physics.RaycastAll(Pos, Dir, MaxDistance);
                
                Array.Sort(hitInfos, (a, b) => a.distance.CompareTo(b.distance));

                foreach (RaycastHit hitInfo in hitInfos)
                {
                    if (hitInfo.collider.isTrigger) continue;
            
                    ITarget target = GetTargetFromHit(hitInfo);
            
                    if (target != null)
                    {
                        result.Shooter = this;
                        result.HasHit = true;
                        hasHit = true;
                        result.HitTargets.Add(target);
                        result.Positions.Add(hitInfo.point);
                        OnTargetHit_Callback(target);
                    }
                }
            }
            else
            {
                RaycastHit[] allHits = Physics.RaycastAll(Pos, Dir, MaxDistance);
                Array.Sort(allHits, (a, b) => a.distance.CompareTo(b.distance));
        
                foreach (RaycastHit hitInfo in allHits)
                {
                    if (hitInfo.collider.isTrigger) continue;
            
                    ITarget target = GetTargetFromHit(hitInfo);
            
                    if (target != null)
                    {
                        result.Shooter = this;
                        result.HasHit = true;
                        hasHit = true;
                        result.HitTargets.Add(target);
                        result.Positions.Add(hitInfo.point);
                        OnTargetHit_Callback(target);
                    }
            
                    break;
                }
            }
    
            return result;
        }
        private ITarget GetTargetFromHit(RaycastHit hitInfo)
        {
            foreach (var comp in hitInfo.collider.gameObject.GetComponents<Behaviour>())
            {
                if (comp == null || !comp.isActiveAndEnabled) continue;
                if (comp is ITarget target)
                {
                    return target;
                }
            }
            return null;
        }
        private void OnShoot_Callback()
        {
            if (EnableCallbacks)
                OnShoot.Invoke();
        }

        private void OnTargetHit_Callback(ITarget info)
        {
            if (EnableCallbacks)
                OnTargetHit.Invoke(info);
        }
    }

}