﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using com.spacepuppy.Tween;
namespace CloudMacaca.ViewSystem
{
    [DisallowMultipleComponent]
    public class ViewElement : MonoBehaviour
    {
        #region V2
        public static ViewElementRuntimePool runtimePool;
        public static ViewElementPool viewElementPool;
        [NonSerialized]
        public int PoolKey;
        public bool IsUnique = false;

        bool hasGroupSetup = false;
        private ViewElementGroup _viewElementGroup;
        public ViewElementGroup viewElementGroup
        {
            get
            {

                if (_viewElementGroup == null)
                {
                    _viewElementGroup = GetComponent<ViewElementGroup>();
                    if (hasGroupSetup)
                    {
                        return _viewElementGroup;
                    }
                    hasGroupSetup = true;
                }

                return _viewElementGroup;
            }
        }

        private ViewRuntimeOverride _runtimeOverride;
        public ViewRuntimeOverride runtimeOverride
        {
            get
            {
                if (_runtimeOverride == null)
                {
                    _runtimeOverride = GetComponent<ViewRuntimeOverride>();
                }
                if (_runtimeOverride == null)
                {
                    _runtimeOverride = gameObject.AddComponent<ViewRuntimeOverride>();
                }
                return _runtimeOverride;
            }
        }
        public void ApplyNavigation(IEnumerable<ViewElementNavigationData> navigationDatas)
        {
            if (navigationDatas == null)
            {
                return;
            }
            if (navigationDatas.Count() == 0)
            {
                return;
            }
            runtimeOverride.ApplyNavigation(navigationDatas);
        }
        public void ApplyEvent(IEnumerable<ViewElementEventData> eventDatas)
        {
            if (eventDatas == null)
            {
                return;
            }
            if (eventDatas.Count() == 0)
            {
                return;
            }
            runtimeOverride.SetEvent(eventDatas);
        }
        public void ApplyOverrides(IEnumerable<ViewElementPropertyOverrideData> overrideDatas)
        {
            runtimeOverride.ClearAllEvent();
            runtimeOverride.ResetToDefaultValues();
            runtimeOverride.RevertToLastNavigation();
            if (overrideDatas == null)
            {
                return;
            }
            if (overrideDatas.Count() == 0)
            {
                return;
            }
            runtimeOverride.ApplyOverride(overrideDatas);
        }

        public virtual Selectable[] GetSelectables()
        {
            return GetComponentsInChildren<Selectable>();
        }

        #endregion

        public static ViewControllerBase viewController;

        //ViewElementLifeCycle
        protected IViewElementLifeCycle[] lifeCyclesObjects;
        public enum TransitionType
        {
            Animator,
            CanvasGroupAlpha,
            ActiveSwitch,
            Custom
        }
        public TransitionType transition = TransitionType.Animator;
        public enum AnimatorTransitionType
        {
            Direct,
            Trigger
        }
        //Animator
        public AnimatorTransitionType animatorTransitionType = AnimatorTransitionType.Direct;
        public string AnimationStateName_In = "In";
        public string AnimationStateName_Out = "Out";
        public string AnimationStateName_Loop = "Loop";
        const string ButtonAnimationBoolKey = "IsLoop";
        bool hasLoopBool = false;

        //CanvasGroup
        public float canvasInTime = 0.4f;
        public float canvasOutTime = 0.4f;
        public EaseStyle canvasInEase = EaseStyle.QuadEaseOut;

        public EaseStyle canvasOutEase = EaseStyle.QuadEaseOut;
        private CanvasGroup _canvasGroup;
        public CanvasGroup canvasGroup
        {
            get
            {
                if (_canvasGroup == null)
                {
                    _canvasGroup = GetComponent<CanvasGroup>();
                }
                if (_canvasGroup == null)
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
                return _canvasGroup;
            }
        }
        //Custom
        public ViewElementEvent OnShowHandle;
        public ViewElementEvent OnLeaveHandle;

        private RectTransform _rectTransform;
        private RectTransform rectTransform
        {
            get
            {
                if (_rectTransform == null)
                {
                    _rectTransform = GetComponent<RectTransform>();
                }
                return _rectTransform;
            }
        }

        private Animator _animator;
        public Animator animator
        {
            get
            {
                if (_animator) return _animator;
                _animator = GetComponent<Animator>();
                if (_animator) return _animator;
                _animator = GetComponentInChildren<Animator>();
                return _animator;
            }
        }

        void Reset()
        {
            //如果還是沒有抓到 Animator 那就設定成一般開關模式
            if (_animator == null)
                transition = TransitionType.ActiveSwitch;
            Setup();
        }
        // void Start()
        // {
        //     Setup();
        // }
        void Awake()
        {
            Setup();
        }
        public virtual void Setup()
        {
            lifeCyclesObjects = GetComponentsInChildren<IViewElementLifeCycle>();
            // poolParent = viewElementPool.transform;
            // poolScale = transform.localScale;
            // poolPosition = rectTransform.anchoredPosition3D;
            // if (transform.parent == poolParent)
            // SetActive(false);

            CheckAnimatorHasLoopKey();
        }
        void CheckAnimatorHasLoopKey()
        {
            hasLoopBool = false;
            if (transition == TransitionType.Animator)
            {
                foreach (AnimatorControllerParameter param in animator.parameters)
                {
                    if (param.name == ButtonAnimationBoolKey)
                    {
                        hasLoopBool = true;
                    }
                }
            }
        }
        Coroutine AnimationIsEndCheck = null;

        public void Reshow()
        {
            OnShow();
        }
        IDisposable OnShowObservable;
        public virtual void ChangePage(bool show, Transform parent, float TweenTime = 0, float delayIn = 0, float delayOut = 0, bool ignoreTransition = false, bool reshowIfSamePage = false)
        {
            viewController.StartUpdateMicroCoroutine(OnChangePageRunner(show, parent, TweenTime, delayIn, delayOut, ignoreTransition, reshowIfSamePage));
            //viewController.StartCoroutine(OnChangePageRunner(show, parent, TweenTime, delayIn, delayOut, ignoreTransition, reshowIfSamePage));
        }
        public IEnumerator OnChangePageRunner(bool show, Transform parent, float TweenTime, float delayIn, float delayOut, bool ignoreTransition, bool reshowIfSamePage)
        {
            if (lifeCyclesObjects != null)
                foreach (var item in lifeCyclesObjects)
                {
                    try
                    {
                        item.OnChangePage(show);
                    }
                    catch (Exception ex) { ViewSystemLog.LogError(ex.ToString(), this); }

                }
            if (show)
            {
                if (parent == null)
                {
                    ViewSystemLog.LogError($"{gameObject.name} does not set the parent for next viewpage.", this);
                    yield break;
                    //throw new NullReferenceException(gameObject.name + " does not set the parent for next viewpage.");
                }
                if (parent.GetInstanceID() != rectTransform.parent.GetInstanceID())
                {
                    if (!gameObject.activeSelf)
                    {
                        rectTransform.SetParent(parent, true);
                        rectTransform.anchoredPosition3D = Vector3.zero;
                        rectTransform.localScale = Vector3.one;
                        float time = 0;
                        while (time > delayIn)
                        {
                            time += GlobalTimer.deltaTime;
                            yield return null;
                        }
                        OnShow();
                        yield break;
                    }
                    else if (TweenTime >= 0)
                    {
                        rectTransform.SetParent(parent, true);

                        var marginFixer = GetComponent<ViewMarginFixer>();
                        viewController.StartUpdateMicroCoroutine(EaseMethods.EaseVector3(
                               rectTransform.anchoredPosition3D,
                               Vector3.zero,
                               TweenTime,
                               EaseMethods.GetEase(EaseStyle.QuadEaseOut),
                               (v) =>
                               {
                                   rectTransform.anchoredPosition3D = v;
                               },
                               () =>
                               {
                                   if (marginFixer) marginFixer.ApplyModifyValue();
                               }
                            ));

                        viewController.StartUpdateMicroCoroutine(EaseMethods.EaseVector3(
                            rectTransform.localScale,
                            Vector3.one,
                            TweenTime,
                            EaseMethods.GetEase(EaseStyle.QuadEaseOut),
                            (v) =>
                            {
                                rectTransform.localScale = v;
                            }
                        ));

                        yield break;
                    }
                    else
                    {
                        float time = 0;
                        while (time > delayIn)
                        {
                            time += GlobalTimer.deltaTime;
                            yield return null;
                        }
                        // yield return Yielders.GetWaitForSecondsRealtime(delayOut);
                        OnLeave(ignoreTransition: ignoreTransition);
                        while (OnLeaveWorking == true)
                        {
                            yield return null;
                        }
                        // yield return new WaitUntil(() => OnLeaveWorking == false);
                        ViewSystemLog.LogWarning("Try to ReShow ", this);
                        rectTransform.SetParent(parent, true);
                        rectTransform.anchoredPosition3D = Vector3.zero;
                        rectTransform.localScale = Vector3.one;
                        time = 0;
                        while (time > delayIn)
                        {
                            time += GlobalTimer.deltaTime;
                            yield return null;
                        }
                        // yield return Yielders.GetWaitForSecondsRealtime(delayIn);
                        OnShow();
                        yield break;
                    }
                }
                else
                {
                    float time = 0;
                    time = 0;
                    while (time > delayIn)
                    {
                        time += GlobalTimer.deltaTime;
                        yield return null;
                    }
                    // yield return Yielders.GetWaitForSecondsRealtime(delayIn);
                    OnShow();
                }
            }
            else
            {
                float time = 0;
                while (time > delayIn)
                {
                    time += GlobalTimer.deltaTime;
                    yield return null;
                }
                // yield return Yielders.GetWaitForSecondsRealtime(delayOut);
                OnLeave(ignoreTransition: ignoreTransition);
                yield break;
            }
        }
        public bool IsShowing
        {
            get
            {
                return showCoroutine != null;
            }
        }
        Coroutine showCoroutine;
        public virtual void OnShow(bool manual = false)
        {
            // if (showCoroutine != null)
            // {
            //     viewController.StopCoroutine(showCoroutine);
            // }
            viewController.StartUpdateMicroCoroutine(OnShowRunner(manual));
            // showCoroutine = viewController.StartCoroutine(OnShowRunner(manual));
        }
        public IEnumerator OnShowRunner(bool manual)
        {
            if (lifeCyclesObjects != null)
                foreach (var item in lifeCyclesObjects)
                {
                    try
                    {
                        item.OnBeforeShow();
                    }
                    catch (Exception ex) { ViewSystemLog.LogError(ex.ToString(), this); }
                }

            SetActive(true);

            if (viewElementGroup != null)
            {
                if (viewElementGroup.OnlyManualMode && manual == false)
                {
                    if (gameObject.activeSelf) SetActive(false);
                    // ViewSystemLog.LogWarning("Due to ignoreTransitionOnce is set to true, ignore the transition");
                    showCoroutine = null;
                    yield break;
                }
                viewElementGroup.OnShowChild();
            }

            if (transition == TransitionType.Animator)
            {
                animator.Play(AnimationStateName_In);

                if (transition == TransitionType.Animator && hasLoopBool)
                {
                    animator.SetBool(ButtonAnimationBoolKey, true);
                }
            }
            else if (transition == TransitionType.CanvasGroupAlpha)
            {
                canvasGroup.alpha = 0;
                viewController.StartCoroutine(EaseMethods.EaseValue(
                    canvasGroup.alpha,
                    1,
                    canvasInTime,
                    EaseMethods.GetEase(canvasInEase),
                    (v) =>
                    {
                        canvasGroup.alpha = v;
                    }
                 ));
            }
            else if (transition == TransitionType.Custom)
            {
                OnShowHandle.Invoke(null);
            }


            if (lifeCyclesObjects != null)
                foreach (var item in lifeCyclesObjects)
                {
                    try
                    {
                        item.OnStartShow();
                    }
                    catch (Exception ex) { ViewSystemLog.LogError(ex.ToString(), this); }

                }

            showCoroutine = null;
            // });
        }
        bool OnLeaveWorking = false;
        //IDisposable OnLeaveDisposable;
        Coroutine OnLeaveCoroutine;
        public virtual void OnLeave(bool NeedPool = true, bool ignoreTransition = false)
        {
            // OnLeaveCoroutine = viewController.StartCoroutine(OnLeaveRunner(NeedPool, ignoreTransition));
            viewController.StartUpdateMicroCoroutine(OnLeaveRunner(NeedPool, ignoreTransition));
        }
        public IEnumerator OnLeaveRunner(bool NeedPool = true, bool ignoreTransition = false)
        {

            //ViewSystemLog.LogError("OnLeave " + name);
            if (transition == TransitionType.Animator && hasLoopBool)
            {
                animator.SetBool(ButtonAnimationBoolKey, false);
            }
            needPool = NeedPool;
            OnLeaveWorking = true;

            if (lifeCyclesObjects != null)
                foreach (var item in lifeCyclesObjects)
                {
                    try
                    {
                        item.OnBeforeLeave();
                    }
                    catch (Exception ex) { ViewSystemLog.LogError(ex.Message, this); }
                }

            if (viewElementGroup != null)
            {
                viewElementGroup.OnLeaveChild(ignoreTransition);
            }

            //在試圖 leave 時 如果已經是 disable 的 那就直接把他送回池子
            //如果 ignoreTransition 也直接把他送回池子
            if (gameObject.activeSelf == false || ignoreTransition)
            {
                OnLeaveAnimationFinish();
                yield break;
            }
            if (transition == TransitionType.Animator)
            {
                try
                {
                    if (animatorTransitionType == AnimatorTransitionType.Direct)
                    {
                        if (animator.HasState(0, Animator.StringToHash(AnimationStateName_Out)))
                            animator.Play(AnimationStateName_Out);
                        else
                            animator.Play("Disable");
                    }
                    else
                    {
                        animator.ResetTrigger(AnimationStateName_Out);
                        animator.SetTrigger(AnimationStateName_Out);
                    }
                    DisableGameObjectOnComplete = true;
                }
                catch
                {
                    OnLeaveAnimationFinish();
                }

            }
            else if (transition == TransitionType.CanvasGroupAlpha)
            {
                if (canvasGroup == null) ViewSystemLog.LogError("No Canvas Group Found on this Object", this);

                //yield return canvasGroup.DOFade(0, canvasOutTime).SetEase(canvasInEase).SetUpdate(true).WaitForCompletion();
                bool tweenFinish = false;
                viewController.StartUpdateMicroCoroutine(EaseMethods.EaseValue(
                      canvasGroup.alpha,
                      0,
                      canvasOutTime,
                      EaseMethods.GetEase(canvasOutEase),
                      (v) =>
                      {
                          canvasGroup.alpha = v;
                      },
                      () =>
                      {
                          tweenFinish = true;
                      }
                   ));
                while (tweenFinish == false)
                {
                    yield return null;
                }

                if (viewElementGroup != null)
                {
                    float waitTime = Mathf.Clamp(viewElementGroup.GetOutDuration() - canvasOutTime, 0, 2);
                    float time = 0;
                    while (time > waitTime)
                    {
                        time += GlobalTimer.deltaTime;
                        yield return null;
                    }
                    //yield return Yielders.GetWaitForSecondsRealtime(waitTime);
                }
                OnLeaveAnimationFinish();
            }
            else if (transition == TransitionType.Custom)
            {
                OnLeaveHandle.Invoke(OnLeaveAnimationFinish);
            }
            else
            {
                if (viewElementGroup != null)
                {
                    float time = 0;
                    while (time > viewElementGroup.GetOutDuration())
                    {
                        time += GlobalTimer.deltaTime;
                        yield return null;
                    }
                    //yield return Yielders.GetWaitForSecondsRealtime(viewElementGroup.GetOutDuration());
                }
                OnLeaveAnimationFinish();
            }

            if (lifeCyclesObjects != null)
            {
                foreach (var item in lifeCyclesObjects)
                {
                    try
                    {
                        item.OnStartLeave();
                    }
                    catch (Exception ex) { ViewSystemLog.LogError(ex.Message, this); }

                }
            }

            // });
        }

        public bool IsShowed
        {
            get
            {
                return rectTransform.parent != viewElementPool.transformCache;
            }
        }

        /// <summary>
        /// A callback to user do something before recovery
        /// </summary>
        public Action OnBeforeRecoveryToPool;
        protected bool needPool = true;
        public void OnLeaveAnimationFinish()
        {
            OnLeaveWorking = false;
            OnLeaveCoroutine = null;

            SetActive(false);

            if (needPool == false)
            {
                return;
            }
            // Debug.LogError(" rectTransform.SetParent(viewElementPool.transformCache, true);");

            //rectTransform.SetParent(viewElementPool.transformCache, true);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = Vector3.one;

            if (runtimePool != null)
            {
                runtimePool.QueueViewElementToRecovery(this);
                OnBeforeRecoveryToPool?.Invoke();
                OnBeforeRecoveryToPool = null;
                if (runtimeOverride != null) runtimeOverride.ResetToDefaultValues();
            }
            else
            {
                // if there is no runtimePool instance, destroy the viewelement.
                Destroy(gameObject);
            }
        }
        public bool DisableGameObjectOnComplete = true;
        void SetActive(bool active)
        {
            gameObject.SetActive(active);
            // if (gameObject.activeSelf == false) gameObject.SetActive(true);
            // canvasGroup.alpha = active ? 1 : 0;
            // canvasGroup.interactable = active;
            // canvasGroup.blocksRaycasts = active;
        }
        public virtual float GetOutDuration()
        {
            float result = 0;

            if (viewElementGroup != null)
            {
                result = Mathf.Max(result, viewElementGroup.GetOutDuration());
            }

            if (transition == ViewElement.TransitionType.Animator)
            {
                var clip = animator?.runtimeAnimatorController.animationClips.SingleOrDefault(m => m.name.Contains("_" + AnimationStateName_Out));
                if (clip != null)
                {
                    result = Mathf.Max(result, clip.length);
                }
            }
            else if (transition == ViewElement.TransitionType.CanvasGroupAlpha)
            {
                result = Mathf.Max(result, canvasOutTime);
            }

            return result;
        }
        public virtual float GetInDuration()
        {
            float result = 0;
            if (viewElementGroup != null)
            {
                result = Mathf.Max(result, viewElementGroup.GetInDuration());
            }

            if (transition == ViewElement.TransitionType.Animator)
            {
                var clip = animator?.runtimeAnimatorController.animationClips.SingleOrDefault(m => m.name.Contains("_" + AnimationStateName_In));
                if (clip != null)
                {
                    result = Mathf.Max(result, clip.length);
                }
            }
            else if (transition == ViewElement.TransitionType.CanvasGroupAlpha)
            {
                result = Mathf.Max(result, canvasInTime);
            }

            return result;
        }
    }

}

[System.Serializable]
public class ViewElementEvent : UnityEvent<Action>
{

}