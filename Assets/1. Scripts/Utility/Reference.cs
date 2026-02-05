using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameSystem
{
    /// <summary>
    /// The reference manager holds a reference to every kitchen element, and eases reference setup
    /// </summary>
    public class Reference
    {
        #region Properties
        // Lazy initialization
        public static Reference Instance { get { if (_instance == null) _instance = new Reference(); return _instance; } }

        public Dictionary<Type, IManager> Managers => _managers;


        #endregion

        #region Fields

        private Type[] _trackedTypes =
        {
            typeof(Shooting.ShooterComponent),

        };
        private Dictionary<Type, List<MonoBehaviour>> _elementDict;
        private Dictionary<Type, IManager> _managers;

        private static Reference _instance;
        #endregion

        #region Methods

        public Reference()
        {
            _elementDict = new();
            _managers = new();
            ScanHierarchy();
        }

        /// <summary>
        /// Recreates a new instance, clearing all the references held so far.
        /// </summary>
        public static void Reset()
        {
            _instance = new();
        }
        /// <summary>
        /// Store the reference to this manager to be accessible everywhere
        /// </summary>
        /// <typeparam name="T">Manager type, implements IManager</typeparam>
        /// <param name="manager"></param>
        public static void SetManager<T>(T manager) where T : IManager => Instance.SetManager_Instance(manager);
        /// <summary>
        /// Remove the manager associated to this type from the global reference index.
        /// This will NOT break or update existing references that other scripts might hold to this manager.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void RemoveManager<T>() where T : IManager => Instance.RemoveManager_Instance<T>();
        /// <summary>
        /// Remove the manager associated to this type from the global reference index.
        /// This will NOT break or update existing references that other scripts might hold to this manager.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void RemoveManager<T>(T manager) where T : IManager => Instance.RemoveManager_Instance<T>();
        /// <summary>
        /// Removes all the managers from the global reference index.
        /// This will NOT break or update existing references that other scripts might hold to these managers.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void RemoveAllManagers() => Instance.RemoveAllManagers_Instance();
        /// <summary>
        /// Get a reference to a manager that registered to the reference index
        /// </summary>
        /// <typeparam name="T">Type of the requested manager</typeparam>
        /// <returns></returns>
        public static T GetManager<T>() where T : class, IManager => Instance.GetManager_Instance<T>();
        /// <summary>
        /// Get a reference to a manager that registered to the reference index
        /// </summary>
        /// <typeparam name="T">Type of the requested manager</typeparam>
        /// <returns></returns>
        public static T GetManager<T>(out T manager) where T : class, IManager => manager = GetManager<T>();
        public static bool TryGetManager<T>(out T manager) where T : class, IManager => Instance.TryGetManager_Instance(out manager);
        /// <summary>
        /// Retrieves all the objects of this specific class that are in the reference index.
        /// To ensure all objects from the current scene are indexed, call Reference.ScanHierarchy().
        /// The retrieved object cannot be of an inheriting class.
        /// The given type must be marked as a tracked type to be retrieved.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static List<T> GetAll<T>() where T : class => Instance.GetElementsOfType<T>();
        public static List<T> GetAll<T>(List<T> output) where T : class => output = Instance.GetElementsOfType<T>();
        /// <summary>
        /// Tries to retrieve an object of a given specific type from the scene.
        /// The type must be marked as a tracked type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool TryGetObject<T>(out T obj) where T : class
        {
            obj = Instance.GetElementOfType<T>();
            return obj != null;
        }
        /// <summary>
        /// Get the first indexed object of the given type.
        /// The type must be marked as a tracked type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T GetObject<T>(out T obj) where T : class
        {
            obj = GetObject<T>();
            return obj;
        }
        public static T GetObject<T>() where T : class => Instance.GetElementOfType<T>();






        protected void SetManager_Instance<T>(T manager) where T : IManager
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (_managers.ContainsKey(typeof(T)) && _managers[typeof(T)] != null)
            {
                Debug.LogWarning("A manager '" + nameof(manager) + "' tried to register on the ReferenceManager, but an instance was already registered.");
            }
            _managers[typeof(T)] = manager;
        }
        protected void RemoveManager_Instance<T>() where T : IManager
        {
            if (!(_managers.ContainsKey(typeof(T)) && _managers[typeof(T)] != null))
            {
                Debug.LogWarning("A manager '" + typeof(T).FullName + "' tried to unregister on the ReferenceManager, but no instance was already registered.");
            }
            _managers[typeof(T)] = null;
        }
        protected void RemoveAllManagers_Instance() => _managers.Clear();
        protected T GetManager_Instance<T>() where T : class, IManager
        {
            if (!_managers.ContainsKey(typeof(T)) || _managers[typeof(T)] == null)
            {
                Debug.LogWarning("No manager of type " + typeof(T).FullName + " was found. Key contained : " + (_managers.ContainsKey(typeof(T)) ? "true" : "false"));
                return default(T);
            }
            return _managers[typeof(T)] as T;
        }
        protected bool TryGetManager_Instance<T>(out T manager) where T : class, IManager
        {
            manager = GetManager_Instance<T>();
            return manager != null;
        }

        public List<T> GetElementsOfType<T>() where T : class
        {
            if (!_elementDict.ContainsKey(typeof(T))) return new List<T>();
            return _elementDict[typeof(T)].Select((mb) => mb as T).ToList();
        }
        public T GetElementOfType<T>() where T : class => GetElementsOfType<T>().FirstOrDefault();

        public void TryAddElement<T>(T element) where T : MonoBehaviour
        {
            if (!_elementDict.ContainsKey(typeof(T)))
                _elementDict.Add(typeof(T), new List<MonoBehaviour>());
            if (!_elementDict[typeof(T)].Contains(element))
                _elementDict[typeof(T)].Add(element);
        }

        public void ScanHierarchy(bool clearDictBeforeScan = true)
        {
            if (clearDictBeforeScan)
                _elementDict.Clear();

            foreach (var type in _trackedTypes)
            {
                if (!_elementDict.ContainsKey(type))
                    _elementDict.Add(type, new List<MonoBehaviour>());

                _elementDict[type].InsertRange(0, (IEnumerable<MonoBehaviour>)GameObject.FindObjectsByType(type, FindObjectsSortMode.None));
            }
        }

        public void DebugTrackedObjectsCount()
        {
            string toPrint = "References tracked :\n";

            foreach (var kvp in _elementDict)
            {
                toPrint += kvp.Key.Name + " : ";
                toPrint += kvp.Value?.Count + "\n";
            }

            Debug.Log(toPrint);
        }

        public void ResetInstance()
        {
            _instance = null;
        }

        #endregion
    }
}