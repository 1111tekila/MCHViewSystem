﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CloudMacaca.ViewSystem
{
    public class ViewElementRuntimePool : MonoBehaviour
    {
        bool init = false;
        ViewElementPool _hierachyPool;
        public void Init(ViewElementPool hierachyPool)
        {
            _hierachyPool = hierachyPool;
            init = true;
        }
        [SerializeField]
        Dictionary<int, Queue<ViewElement>> veDicts = new Dictionary<int, Queue<ViewElement>>();
        [SerializeField]
        Dictionary<int, ViewElement> uniqueVeDicts = new Dictionary<int, ViewElement>();
        Queue<ViewElement> recycleQueue = new Queue<ViewElement>();
        public void QueueViewElementToRecovery(ViewElement toRecovery)
        {
            recycleQueue.Enqueue(toRecovery);
            //ViewSystemLog.Log($"QueueViewElementToRecovery {toRecovery.name}");
        }

        public void RecoveryViewElement(ViewElement toRecovery)
        {
            if (toRecovery.IsUnique)
            {
                //Currentlly nothing needs to do.
            }
            else
            {
                if (!veDicts.TryGetValue(toRecovery.PoolKey, out Queue<ViewElement> veQueue))
                {
                    ViewSystemLog.LogWarning("Cannot find pool of ViewElement " + toRecovery.name + ", Destroy directly.");
                    UnityEngine.Object.Destroy(toRecovery);
                    return;
                }
                veQueue.Enqueue(toRecovery);
            }
        }

        public void RecoveryQueuedViewElement()
        {
            while (recycleQueue.Count > 0)
            {
                var a = recycleQueue.Dequeue();
                //ViewSystemLog.Log($"RecoveryQueuedViewElement {a.name}");
                RecoveryViewElement(a);
            }
        }
        public ViewElement PrewarmUniqueViewElement(ViewElement source)
        {
            if (!source.IsUnique)
            {
                ViewSystemLog.LogWarning("The ViewElement trying to Prewarm is not an unique ViewElement");
                return null;
            }

            if (!uniqueVeDicts.ContainsKey(source.GetInstanceID()))
            {
                var temp = UnityEngine.Object.Instantiate(source, _hierachyPool.rectTransform);
                temp.name = source.name;
                uniqueVeDicts.Add(source.GetInstanceID(), temp);
                temp.gameObject.SetActive(false);
                return temp;
            }
            else
            {
                ViewSystemLog.LogWarning("ViewElement " + source.name + " has been prewarmed");
                return uniqueVeDicts[source.GetInstanceID()];
            }
        }
        public ViewElement RequestViewElement(ViewElement source)
        {
            ViewElement result;

            if (source.IsUnique)
            {
                if (!uniqueVeDicts.TryGetValue(source.GetInstanceID(), out result))
                {
                    result = UnityEngine.Object.Instantiate(source, _hierachyPool.rectTransform);
                    result.name = source.name;
                    uniqueVeDicts.Add(source.GetInstanceID(), result);
                }
            }
            else
            {
                Queue<ViewElement> veQueue;
                if (!veDicts.TryGetValue(source.GetInstanceID(), out veQueue))
                {
                    veQueue = new Queue<ViewElement>();
                    veDicts.Add(source.GetInstanceID(), veQueue);
                }
                if (veQueue.Count == 0)
                {
                    var a = UnityEngine.Object.Instantiate(source, _hierachyPool.rectTransform);
                    a.gameObject.SetActive(false);
                    a.name = source.name;
                    veQueue.Enqueue(a);
                }
                result = veQueue.Dequeue();
            }
            result.PoolKey = source.GetInstanceID();
            return result;
        }
    }
}