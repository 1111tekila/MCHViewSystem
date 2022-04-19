using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;
using System;
using UnityEngine.UI;

namespace MacacaGames.ViewSystem
{
    [DisallowMultipleComponent]
    public class ViewRuntimeOverride : MonoBehaviour
    {
        #region NavigationOverride
        // ViewElementNavigationData[] navigationDatas;
        internal void ApplyNavigation(IEnumerable<ViewElementNavigationData> navigationDatas)
        {
            //this.navigationDatas = navigationDatas.ToArray();
            foreach (var item in navigationDatas)
            {
                Transform targetTansform = GetTransform(item.targetTransformPath);
                if (targetTansform == null)
                {
                    ViewSystemLog.LogError($"Target GameObject cannot be found [{transform.name} / {item.targetTransformPath}]");
                    continue;
                }

                var result = GetCachedComponent(targetTansform, item.targetTransformPath, item.targetComponentType);
                SetPropertyValue(result.Component, item.targetPropertyName, item.navigation);
            }
        }
        Dictionary<int, UnityEngine.UI.Navigation.Mode> lastNavigationDatas = new Dictionary<int, Navigation.Mode>();
        internal void DisableNavigation()
        {
            lastNavigationDatas.Clear();
            var selectables = GetComponentsInChildren<Selectable>();
            foreach (var item in selectables)
            {
                var nav = item.navigation;
                lastNavigationDatas.Add(item.GetInstanceID(), nav.mode);
                nav.mode = UnityEngine.UI.Navigation.Mode.None;
                item.navigation = nav;
            }
        }

        public void RevertToLastNavigation()
        {
            var selectables = GetComponentsInChildren<Selectable>();
            foreach (var item in selectables)
            {
                if (lastNavigationDatas.TryGetValue(item.GetInstanceID(), out Navigation.Mode mode))
                {
                    var nav = item.navigation;
                    nav.mode = mode;
                    item.navigation = nav;
                }
            }
        }

        #endregion
        #region EventOverride
        [SerializeField]
        ViewElementEventData[] currentEventDatas;
        class EventRuntimeDatas
        {
            public EventRuntimeDatas(UnityEventBase unityEvent, Component selectable)
            {
                this.unityEvent = unityEvent;
                this.component = selectable;
            }
            public UnityEventBase unityEvent;
            public Component component;
        }
        delegate void EventDelegate<Self>(Self selectable);
        private static EventDelegate<Component> CreateOpenDelegate(string method, Component target)
        {
            return (EventDelegate<Component>)
                Delegate.CreateDelegate(type: typeof(EventDelegate<Component>), target, method, true, true);
        }
        static Dictionary<string, EventDelegate<Component>> cachedDelegate = new Dictionary<string, EventDelegate<Component>>();
        Dictionary<string, EventRuntimeDatas> cachedUnityEvent = new Dictionary<string, EventRuntimeDatas>();
        public void ClearAllEvent()
        {
            currentEventDelegates.Clear();
            // foreach (var item in cachedUnityEvent)
            // {
            //     item.Value.unityEvent.RemoveAllListeners();
            // }
        }

        internal void SetEvent(IEnumerable<ViewElementEventData> eventDatas)
        {
            currentEventDatas = eventDatas.ToArray();

            //Group by Component transform_component_property
            //var groupedEventData = eventDatas.GroupBy(item => item.targetTransformPath + ";" + item.targetComponentType + ";" + item.targetPropertyName);

            foreach (var item in eventDatas)
            {
                //string[] p = item.Key.Split(';');
                //p[0] is targetTransformPath
                Transform targetTansform = GetTransform(item.targetTransformPath);
                if (targetTansform == null)
                {
                    ViewSystemLog.LogError($"Target GameObject cannot be found [{transform.name} / {item.targetTransformPath}]");
                    continue;
                }

                EventRuntimeDatas eventRuntimeDatas;
                var id_delegate = item.scriptName + ";" + item.methodName;

                var key = item.targetTransformPath + ";" + item.targetComponentType + ";" + item.targetPropertyName + ";" + id_delegate;

                // Get UnityEvent property instance
                if (!cachedUnityEvent.TryGetValue(key, out eventRuntimeDatas))
                {
                    var result = GetCachedComponent(targetTansform, item.targetTransformPath, item.targetComponentType);
                    string property = item.targetPropertyName;
                    if (item.targetTransformPath.Contains("UnityEngine."))
                    {
                        property = ViewSystemUtilitys.ParseUnityEngineProperty(item.targetPropertyName);
                    }
                    var unityEvent = (UnityEventBase)GetPropertyValue(result.Component, property);
                    eventRuntimeDatas = new EventRuntimeDatas(unityEvent, (Component)result.Component);
                    cachedUnityEvent.Add(key, eventRuntimeDatas);

                    if (eventRuntimeDatas.unityEvent is UnityEvent events)
                    {
                        events.AddListener(() =>
                        {
                            EventHandler(id_delegate);
                        });
                    }
                }

                if (!cachedDelegate.TryGetValue(id_delegate, out EventDelegate<Component> openDelegate))
                {
                    // Get Method
                    Type type = Utility.GetType(item.scriptName);

                    //The method impletmented Object
                    Component scriptInstance = (Component)FindObjectOfType(type);

                    if (scriptInstance == null)
                    {
                        scriptInstance = GenerateScriptInstance(type);
                    }

                    //Create Open Delegate
                    try
                    {
                        openDelegate = CreateOpenDelegate(item.methodName, scriptInstance);
                    }
                    catch (Exception ex)
                    {
                        ViewSystemLog.LogError($"Create event delegate faild, make sure the method or the instance is exinst. Exception:{ex.ToString()}", this);
                    }
                    cachedDelegate.Add(id_delegate, openDelegate);
                }
                currentEventDelegates.Add(id_delegate, openDelegate);
                currentComponent = eventRuntimeDatas.component;
            }
        }
        Dictionary<string, EventDelegate<Component>> currentEventDelegates = new Dictionary<string, EventDelegate<Component>>();
        UnityEngine.Component currentComponent;
        void EventHandler(string key)
        {
            if (ViewController.Instance.IsPageTransition)
            {
                ViewSystemLog.LogWarning("The page is in transition, event will not fire!");
                return;
            }
            Debug.Log($"EventHandler {key}");
            if (currentEventDelegates.TryGetValue(key, out EventDelegate<Component> e))
                e.Invoke(currentComponent);
        }

        const string GeneratedScriptInstanceGameObjectName = "Generated_ViewSystem";
        Component GenerateScriptInstance(Type type)
        {
            var go = GameObject.Find(GeneratedScriptInstanceGameObjectName);

            if (go == null)
            {
                go = new GameObject(GeneratedScriptInstanceGameObjectName);
            }
            return go.AddComponent(type);
        }
        #endregion

        #region  Property Override
        public void ResetToDefaultValues()
        {
            foreach (var item in currentModifiedField)
            {
                PrefabDefaultField defaultField;
                if (prefabDefaultFields.TryGetValue(item, out defaultField))
                {
                    SetPropertyValue(cachedComponent[defaultField.id], defaultField.field, defaultField.defaultValue);
                }
            }
            currentModifiedField.Clear();
        }
        static BindingFlags bindingFlags =
            BindingFlags.NonPublic |
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.Static;
        Dictionary<string, UnityEngine.Object> cachedComponent = new Dictionary<string, UnityEngine.Object>();
        List<UnityEngine.UI.Graphic> requireSetDirtyTarget = new List<UnityEngine.UI.Graphic>();
        internal void ApplyOverride(IEnumerable<ViewElementPropertyOverrideData> overrideDatas)
        {
            requireSetDirtyTarget.Clear();
            foreach (var item in overrideDatas)
            {
                Transform targetTansform = GetTransform(item.targetTransformPath);
                if (targetTansform == null)
                {
                    ViewSystemLog.LogError($"Target GameObject cannot be found [{transform.name} / {item.targetTransformPath}]");
                    continue;
                }

                var result = GetCachedComponent(targetTansform, item.targetTransformPath, item.targetComponentType);
                if (result.Component == null)
                {
                    ViewSystemLog.LogError($"Target Component cannot be found [{item.targetComponentType}]");
                    continue;
                }

                var idForProperty = result.Id + "#" + item.targetPropertyName;
                if (!prefabDefaultFields.ContainsKey(idForProperty))
                {
                    prefabDefaultFields.Add(idForProperty, new PrefabDefaultField(GetPropertyValue(result.Component, item.targetPropertyName), result.Id, item.targetPropertyName));
                }

                currentModifiedField.Add(idForProperty);
                SetPropertyValue(result.Component, item.targetPropertyName, item.Value.GetValue());
                if (item.Value.s_Type == PropertyOverride.S_Type._color)
                {
                    if (result.Component.GetType().IsSubclassOf(typeof(Graphic)))
                        requireSetDirtyTarget.Add(result.Component as Graphic);
                    if (result.Component.GetType().IsSubclassOf(typeof(BaseMeshEffect)))
                        requireSetDirtyTarget.Add((result.Component as Component).GetComponent<Graphic>());
                }
            }
            foreach (var item in requireSetDirtyTarget)
            {
                item?.SetAllDirty();
            }
        }
        public Transform GetTransform(string targetTransformPath)
        {
            if (string.IsNullOrEmpty(targetTransformPath))
            {
                return transform;
            }
            else
            {
                return transform.Find(targetTransformPath);
            }
        }
        public (string Id, UnityEngine.Object Component) GetCachedComponent(Transform targetTansform, string targetTransformPath, string targetComponentType)
        {
            UnityEngine.Object c = null;
            var id = targetTransformPath + "#" + targetComponentType;
            if (!cachedComponent.TryGetValue(id, out c))
            {
                if (targetComponentType.Contains("UnityEngine.GameObject"))
                {
                    c = targetTansform.gameObject;
                }
                else
                {
                    c = ViewSystemUtilitys.GetComponent(targetTansform, targetComponentType);
                }
                if (c == null)
                {
                    ViewSystemLog.LogError($"Target Component cannot be found [{targetComponentType}] on GameObject [{transform.name} / {targetTransformPath}]");
                }
                cachedComponent.Add(id, c);
            }
            return (id, c);
        }

        public void SetPropertyValue(object inObj, string fieldName, object newValue)
        {
            System.Type t = inObj.GetType();
            //GameObject hack
            // Due to GameObject.active is obsolete and ativeSelf is read only
            // Use a hack function to override GameObject's active status.
            if (t == typeof(GameObject) && fieldName == "m_IsActive")
            {
                ((GameObject)inObj).SetActive((bool)newValue);
                return;
            }

            // Try search Field first than try property
            System.Reflection.FieldInfo fieldInfo = t.GetField(fieldName, bindingFlags);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(inObj, newValue);
                return;
            }
            if (t.ToString().Contains("UnityEngine."))
            {
                fieldName = ViewSystemUtilitys.ParseUnityEngineProperty(fieldName);
            }
            System.Reflection.PropertyInfo info = t.GetProperty(fieldName, bindingFlags);
            if (info != null)
                info.SetValue(inObj, newValue);
        }

        private object GetPropertyValue(object inObj, string fieldName)
        {
            System.Type t = inObj.GetType();
            //GameObject hack
            // Due to GameObject.active is obsolete and ativeSelf is read only
            // Use a hack function to override GameObject's active status.
            if (t == typeof(GameObject) && fieldName == "m_IsActive")
            {
                return ((GameObject)inObj).activeSelf;
            }
            object ret = null;
            // Try search Field first than try property
            System.Reflection.FieldInfo fieldInfo = t.GetField(fieldName, bindingFlags);
            if (fieldInfo != null)
            {
                ret = fieldInfo.GetValue(inObj);
                return ret;
            }
            if (t.ToString().Contains("UnityEngine."))
            {
                fieldName = ViewSystemUtilitys.ParseUnityEngineProperty(fieldName);
            }
            System.Reflection.PropertyInfo info = t.GetProperty(fieldName, bindingFlags);
            if (info != null)
                ret = info.GetValue(inObj);

            //ViewSystemLog.Log($"GetProperty on [{gameObject.name}] Target Object {((UnityEngine.Object)inObj).name} [{t.ToString()}] on [{fieldName}]  Value [{ret}]");
            return ret;
        }

        [SerializeField]
        List<string> currentModifiedField = new List<string>();
        [SerializeField]
        Dictionary<string, PrefabDefaultField> prefabDefaultFields = new Dictionary<string, PrefabDefaultField>();
        struct PrefabDefaultField
        {
            public PrefabDefaultField(object orignalValue, string id, string field)
            {
                this.defaultValue = orignalValue;
                this.id = id;
                this.field = field;
            }
            [SerializeField]
            public object defaultValue;
            public string id;
            public string field;
        }
    }
    #endregion
}
