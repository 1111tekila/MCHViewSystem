﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Unity.Linq;
using UnityEngine;

namespace CloudMacaca.ViewSystem
{
    public class ViewController : MonoBehaviour
    {
        [SerializeField]
        public string InitViewPageName;
        public event EventHandler<ViewStateEventArgs> OnViewStateChange;

        /// <summary>
        /// OnViewPageChange Calls on last page has leave finished, next page is ready to show.
        /// </summary>
        public event EventHandler<ViewPageEventArgs> OnViewPageChange;
        /// <summary>
        /// OnViewPageChangeStart Calls on page is ready to change with no error(eg. no page fonud etc.), and in this moment last page is still in view. 
        /// </summary>
        public event EventHandler<ViewPageTrisitionEventArgs> OnViewPageChangeStart;
        /// <summary>
        /// OnViewPageChangeEnd Calls on page is changed finish, all animation include in OnShow or OnLeave is finished.
        /// </summary>
        public event EventHandler<ViewPageEventArgs> OnViewPageChangeEnd;
        public event EventHandler<ViewPageEventArgs> OnOverlayPageShow;
        public event EventHandler<ViewPageEventArgs> OnOverlayPageLeave;
        public class ViewStateEventArgs : EventArgs
        {
            public ViewState currentViewState;
            public ViewState lastViewState;
            public ViewStateEventArgs(ViewState CurrentViewState, ViewState LastVilastViewState)
            {
                this.currentViewState = CurrentViewState;
                this.lastViewState = LastVilastViewState;
            }
        }
        public class ViewPageEventArgs : EventArgs
        {
            // ...省略額外參數
            public ViewPage currentViewPage;
            public ViewPage lastViewPage;
            public ViewPageEventArgs(ViewPage CurrentViewPage, ViewPage LastViewPage)
            {
                this.currentViewPage = CurrentViewPage;
                this.lastViewPage = LastViewPage;
            }
        }
        public class ViewPageTrisitionEventArgs : EventArgs
        {
            // ...省略額外參數
            public ViewPage viewPageWillLeave;
            public ViewPage viewPageWillShow;
            public ViewPageTrisitionEventArgs(ViewPage viewPageWillLeave, ViewPage viewPageWillShow)
            {
                this.viewPageWillLeave = viewPageWillLeave;
                this.viewPageWillShow = viewPageWillShow;
            }
        }
        public static ViewController Instance;
        public List<ViewPage> viewPage = new List<ViewPage>();
        public List<ViewState> viewStates = new List<ViewState>();
        private static IEnumerable<string> viewStatesNames;
        [ReadOnly, SerializeField]
        private List<ViewElement> currentLiveElement = new List<ViewElement>();
        [HideInInspector]
        public ViewPage lastViewPage;
        [HideInInspector]
        public ViewState lastViewState;
        [HideInInspector]
        public ViewPage currentViewPage;
        [HideInInspector]
        public ViewState currentViewState;
        [HideInInspector]
        public ViewPage nextViewPage;
        [HideInInspector]
        public ViewState nextViewState;
        // Use this for initialization
        void Awake()
        {
            Instance = this;
            if (viewElementPool == null)
            {
                try
                {
                    viewElementPool = (ViewElementPool)FindObjectOfType(typeof(ViewElementPool));
                }
                catch
                {

                }
            }

        }
        CloudMacaca.ViewSystem.ViewPageItem.PlatformOption platform;
        IEnumerator Start()
        {
            viewStatesNames = viewStates.Select(m => m.name);

            SetupPlatformDefine();

            if (!string.IsNullOrEmpty(InitViewPageName))
            {
                var vp = GetInitViewPage();
                if (vp == null)
                {
                    yield break;
                }

                //currentLiveElement = GetAllViewPageItemInViewPage(vp).Select(m => m.viewElement).ToList();
                //wait one frame that other script need register the event 
                yield return null;
                // foreach (var item in currentLiveElement)
                // {
                //     item.SampleToLoopState();
                // }
                //UpdateCurrentViewStateAndNotifyEvent(vp);
                ChangePageTo(InitViewPageName);
            }

            //開啟無限檢查自動離場的迴圈
            StartCoroutine(AutoLeaveOverlayPage());
            
            //Debug Code

            // var a = viewPage.Select(m => m.viewPageItem);
            // var b = viewStates.Select(m => m.viewPageItems);

            // foreach (var item in viewPage)
            // {
            //     foreach (var pageItem in item.viewPageItem)
            //     {
            //         if (pageItem.TweenTime < 0)
            //         {
            //             Debug.LogError("ViewPage : " + item.name + " , ViewElement" + pageItem.viewElement.name);
            //         }
            //     }
            // }

            // foreach (var item in viewStates)
            // {
            //     foreach (var pageItem in item.viewPageItems)
            //     {
            //         if (pageItem.TweenTime < 0)
            //         {
            //             Debug.LogError("ViewPage : " + item.name + " , ViewElement" + pageItem.viewElement.name);
            //         }
            //     }
            // }
        }
        public ViewPage GetInitViewPage()
        {
            return viewPage.SingleOrDefault(m => m.name == InitViewPageName);
        }

        void SetupPlatformDefine()
        {

#if UNITY_EDITOR
            if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS)
            {
                platform = CloudMacaca.ViewSystem.ViewPageItem.PlatformOption.iOS;
            }
            else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.tvOS)
            {
                platform = CloudMacaca.ViewSystem.ViewPageItem.PlatformOption.tvOS;
            }
            else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
            {
                platform = CloudMacaca.ViewSystem.ViewPageItem.PlatformOption.Android;
            }
            else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.WSAPlayer)
            {
                platform = CloudMacaca.ViewSystem.ViewPageItem.PlatformOption.UWP;
            }
#else
            if (Application.platform == RuntimePlatform.IPhonePlayer) {
                platform = CloudMacaca.ViewSystem.ViewPageItem.PlatformOption.iOS;
            } else if (Application.platform == RuntimePlatform.tvOS) {
                platform = CloudMacaca.ViewSystem.ViewPageItem.PlatformOption.tvOS;
            } else if (Application.platform == RuntimePlatform.Android) {
                platform = CloudMacaca.ViewSystem.ViewPageItem.PlatformOption.Android;
            } else if (Application.platform == RuntimePlatform.WSAPlayerARM ||
                Application.platform == RuntimePlatform.WSAPlayerX64 ||
                Application.platform == RuntimePlatform.WSAPlayerX86) {
                platform = CloudMacaca.ViewSystem.ViewPageItem.PlatformOption.UWP;
            }
#endif
        }
        IEnumerable<ViewPageItem> GetAllViewPageItemInViewPage(ViewPage vp)
        {
            List<ViewPageItem> realViewPageItem = new List<ViewPageItem>();

            //先整理出下個頁面應該出現的 ViewPageItem
            ViewState viewPagePresetTemp;
            List<ViewPageItem> viewItemForNextPage = new List<ViewPageItem>();

            //從 ViewState 尋找
            if (!string.IsNullOrEmpty(vp.viewState))
            {
                viewPagePresetTemp = viewStates.SingleOrDefault(m => m.name == vp.viewState);
                if (viewPagePresetTemp != null)
                {
                    viewItemForNextPage.AddRange(viewPagePresetTemp.viewPageItems);
                }
            }

            //從 ViewPage 尋找
            viewItemForNextPage.AddRange(vp.viewPageItems);

            //並排除 Platform 該隔離的 ViewElement 放入 realViewPageItem
            realViewPageItem.Clear();
            foreach (var item in viewItemForNextPage)
            {
                if (item.excludePlatform.Contains(platform))
                {
                    item.parentGameObject.SetActive(false);
                }
                else
                {
                    item.parentGameObject.SetActive(true);
                    realViewPageItem.Add(item);
                }
            }

            return viewItemForNextPage;
        }

        // Stack 後進先出
        private GameObject currentActiveObject;
        private Stack<ViewPage> subPageViewPage = new Stack<ViewPage>();
        private float nextViewPageWaitTime = 0;
        private Dictionary<string, float> lastPageItemTweenOutTimes = new Dictionary<string, float>();
        private Dictionary<string, float> lastPageItemDelayOutTimes = new Dictionary<string, float>();
        private Dictionary<string, float> lastPageItemDelayOutTimesOverlay = new Dictionary<string, float>();
        private Dictionary<string, bool> lastPageNeedLeaveOnFloat = new Dictionary<string, bool>();
        public bool IsPageTransition
        {
            get
            {
                return ChangePageToCoroutine != null;
            }
        }
        Coroutine ChangePageToCoroutine = null;
        public Coroutine ChangePageTo(string viewPageName, Action OnComplete = null, bool AutoWaitPreviousPageFinish = false)
        {
            if (IsPageTransition && AutoWaitPreviousPageFinish == false)
            {
                Debug.LogError("Page is in Transition.");
                return null;
            }
            else if (IsPageTransition && AutoWaitPreviousPageFinish == true)
            {
                Debug.LogError("Page is in Transition but AutoWaitPreviousPageFinish");
                ChangePageToCoroutine = StartCoroutine(WaitPrevious(viewPageName, OnComplete));
                return ChangePageToCoroutine;
            }
            ChangePageToCoroutine = StartCoroutine(ChangePageToBase(viewPageName, OnComplete));
            return ChangePageToCoroutine;
        }

        public IEnumerator WaitPrevious(string viewPageName, Action OnComplete)
        {
            yield return new WaitUntil(() => IsPageTransition == false);
            yield return ChangePageToBase(viewPageName, OnComplete);
        }

        public IEnumerator ChangePageToBase(string viewPageName, Action OnComplete)
        {

            //取得 ViewPage 物件
            var vp = viewPage.Where(m => m.name == viewPageName).SingleOrDefault();

            //沒有找到 
            if (vp == null)
            {
                Debug.LogError("No view page match" + viewPageName + "Found");
                //return;
                ChangePageToCoroutine = null;
                yield break;
            }

            if (vp.viewPageType == ViewPage.ViewPageType.Overlay)
            {
                Debug.LogError("To show Overlay ViewPage use ShowOverlayViewPage() method \n current version will redirect to this method automatically.");
                ShowOverlayViewPageBase(vp, true, OnComplete);
                ChangePageToCoroutine = null;
                //return;

                yield break;
            }

            //所有檢查都通過開始換頁
            //IsPageTransition = true;

            if (OnViewPageChangeStart != null)
            {
                OnViewPageChangeStart(this, new ViewPageTrisitionEventArgs(currentViewPage, vp));
            }
            nextViewPage = vp;
            nextViewState = viewStates.SingleOrDefault(m => m.name == vp.viewState);



            //viewItemNextPage 代表下個 ViewPage 應該出現的所有 ViewPageItem
            var viewItemNextPage = GetAllViewPageItemInViewPage(vp);
            var viewItemCurrentPage = GetAllViewPageItemInViewPage(currentViewPage);


            //尋找這個頁面還在，但下個頁面沒有的元件，這些元件應該先移除
            // 10/13 新邏輯 使用兩個 ViewPageItem 先連集在差集

            // 目前頁面 差集 （目前頁面與目標頁面的交集）
            //var viewElementDoesExitsInNextPage = viewItemCurrentPage.Except(viewItemCurrentPage.Intersect(viewItemNextPage)).Select(m => m.viewElement).ToList();
            var viewElementExitsInBothPage = viewItemNextPage.Intersect(viewItemCurrentPage).Select(m => m.viewElement).ToList();

            List<ViewElement> viewElementDoesExitsInNextPage = new List<ViewElement>();

            //尋找這個頁面還在，但下個頁面沒有的元件
            //就是存在 currentLiveElement 中但不存在 viewItemForNextPage 的傢伙要 ChangePage 
            var allViewElementForNextPage = viewItemNextPage.Select(m => m.viewElement).ToList();
            foreach (var item in currentLiveElement.ToArray())
            {
                //不存在的話就讓他加入應該移除的列表
                if (allViewElementForNextPage.Contains(item) == false)
                {
                    //加入該移除的列表
                    viewElementDoesExitsInNextPage.Add(item);
                }
            }


            currentLiveElement.Clear();
            currentLiveElement = viewItemNextPage.Select(m => m.viewElement).ToList();

            //整理目前在畫面上 Overlay page 的 ViewPageItem
            var CurrentOverlayViewPageItem = new List<ViewPageItem>();

            foreach (var item in overlayPageStates.Select(m => m.Value.viewPage).Select(x => x.viewPageItems))
            {
                CurrentOverlayViewPageItem.AddRange(item);
            }


            var CurrentOverlayViewElement = CurrentOverlayViewPageItem.Select(m => m.viewElement);
            //對離場的呼叫改變狀態
            foreach (var item in viewElementDoesExitsInNextPage)
            {
                //如果 ViewElement 被 Overlay 頁面使用中就不執行 ChangePage
                if (CurrentOverlayViewElement.Contains(item))
                {
                    Debug.Log(item.name);
                    continue;
                }
                float delayOut = 0;
                lastPageItemDelayOutTimes.TryGetValue(item.name, out delayOut);
                item.ChangePage(false, null, 0, 0, delayOut);
            }

            lastPageItemDelayOutTimes.Clear();

            yield return Yielders.GetWaitForSeconds(nextViewPageWaitTime);

            float TimeForPerviousPageOnLeave = 0;
            switch (vp.viewPageTransitionTimingType)
            {
                case ViewPage.ViewPageTransitionTimingType.接續前動畫:
                    TimeForPerviousPageOnLeave = CalculateTimesNeedsForOnLeave(viewItemNextPage.Select(m => m.viewElement));
                    break;
                case ViewPage.ViewPageTransitionTimingType.與前動畫同時:
                    TimeForPerviousPageOnLeave = 0;
                    break;
                case ViewPage.ViewPageTransitionTimingType.自行設定:
                    TimeForPerviousPageOnLeave = vp.customPageTransitionWaitTime;
                    break;
            }
            nextViewPageWaitTime = CalculateWaitingTimeForCurrentOnLeave(viewItemNextPage);


            //等上一個頁面的 OnLeave 結束，注意，如果頁面中有大量的 Animator 這裡只能算出預估的結果 並且會限制最長時間為一秒鐘
            yield return Yielders.GetWaitForSeconds(TimeForPerviousPageOnLeave);

            //對進場的呼叫改變狀態
            foreach (var item in viewItemNextPage)
            {
                //Delay 時間
                if (!lastPageItemDelayOutTimes.ContainsKey(item.viewElement.name))
                    lastPageItemDelayOutTimes.Add(item.viewElement.name, item.delayOut);
                else
                    lastPageItemDelayOutTimes[item.viewElement.name] = item.delayOut;

                //如果 ViewElement 被 Overlay 頁面使用中就不執行 ChangePage
                if (CurrentOverlayViewElement.Contains(item.viewElement))
                {
                    Debug.Log(item.viewElement.name);
                    continue;
                }

                item.viewElement.ChangePage(true, item.parent, item.TweenTime, item.delayIn, item.delayOut);
            }

            float OnShowAnimationFinish = CalculateTimesNeedsForOnShow(viewItemNextPage.Select(m => m.viewElement));

            //更新狀態
            UpdateCurrentViewStateAndNotifyEvent(vp);

            //OnComplete Callback，08/28 雖然增加了計算至下個頁面的所需時間，但依然維持原本的時間點呼叫 Callback
            if (OnComplete != null) OnComplete();

            //通知事件
            yield return Yielders.GetWaitForSeconds(OnShowAnimationFinish);

            ChangePageToCoroutine = null;

            //Callback
            if (OnViewPageChangeEnd != null)
                OnViewPageChangeEnd(this, new ViewPageEventArgs(currentViewPage, lastViewPage));
        }

        public bool HasOverlayPageLive()
        {
            return overlayPageStates.Count > 0;
        }
        public bool IsOverPageLive(string viewPageName)
        {
            return overlayPageStates.ContainsKey(viewPageName);
        }
        public IEnumerable<string> GetCurrentOverpageNames()
        {
            return overlayPageStates.Select(m => m.Key);
        }
        // List<ViewPage> overlayViewPageQueue = new List<ViewPage>();
        [SerializeField]
        Dictionary<string, OverlayPageState> overlayPageStates = new Dictionary<string, OverlayPageState>();

        [SerializeField]
        public class OverlayPageState
        {
            public bool IsTransition = false;
            public ViewPage viewPage;
            public Coroutine pageChangeCoroutine;
        }

        public Coroutine ShowOverlayViewPage(string viewPageName, bool RePlayOnShowWhileSamePage = false, Action OnComplete = null)
        {
            var vp = viewPage.Where(m => m.name == viewPageName).SingleOrDefault();
            return StartCoroutine(ShowOverlayViewPageBase(vp, RePlayOnShowWhileSamePage, OnComplete));
        }
        public IEnumerator ShowOverlayViewPageBase(ViewPage vp, bool RePlayOnShowWhileSamePage, Action OnComplete)
        {
            if (vp == null)
            {
                Debug.Log("ViewPage is null");
                yield break;
            }
            if (vp.viewPageType != ViewPage.ViewPageType.Overlay)
            {
                Debug.LogError("ViewPage " + vp.name + " is not an Overlay page");
                yield break;
            }

            float onShowTime = CalculateTimesNeedsForOnShow(GetAllViewPageItemInViewPage(vp).Select(m => m.viewElement));
            float onShowDelay = CalculateWaitingTimeForCurrentOnShow(GetAllViewPageItemInViewPage(vp));

            //if (OverlayTransitionProtectionCoroutine != null) { StopCoroutine(OverlayTransitionProtectionCoroutine); }
            //StartCoroutine(OverlayTransitionProtection());

            var overlayPageState = new OverlayPageState();
            overlayPageState.viewPage = vp;
            overlayPageState.IsTransition = true;

            if (overlayPageStates.ContainsKey(vp.name) == false)
            {
                overlayPageStates.Add(vp.name, overlayPageState);
                foreach (var item in vp.viewPageItems)
                {
                    //Delay 時間
                    if (!lastPageItemDelayOutTimesOverlay.ContainsKey(item.viewElement.name))
                        lastPageItemDelayOutTimesOverlay.Add(item.viewElement.name, item.delayOut);
                    else
                        lastPageItemDelayOutTimesOverlay[item.viewElement.name] = item.delayOut;

                    item.viewElement.ChangePage(true, item.parent, item.TweenTime, item.delayIn, item.delayOut);
                }
            }
            else
            {
                //如果已經存在的話要更新數值
                overlayPageState = overlayPageStates[vp.name];
                if (overlayPageState.pageChangeCoroutine != null)
                {
                    StopCoroutine(overlayPageState.pageChangeCoroutine);
                }
                overlayPageStates[vp.name].IsTransition = true;
            }


            if (RePlayOnShowWhileSamePage == true)
            {
                foreach (var item in vp.viewPageItems)
                {
                    item.viewElement.OnShow();
                }
            }


            if (vp.autoLeaveTimes > 0)
            {
                var currentAutoLeave = autoLeaveQueue.SingleOrDefault(m => m.name == vp.name);
                if (currentAutoLeave != null)
                {
                    //更新倒數計時器
                    currentAutoLeave.times = vp.autoLeaveTimes;
                }
                else
                {
                    //沒有的話新增一個
                    autoLeaveQueue.Add(new AutoLeaveData(vp.name, vp.autoLeaveTimes));
                }
            }

            if (OnOverlayPageShow != null)
                OnOverlayPageShow(this, new ViewPageEventArgs(vp, null));



            yield return Yielders.GetWaitForSeconds(onShowTime + onShowDelay);

            if (overlayPageStates.ContainsKey(vp.name)) overlayPageStates[vp.name].IsTransition = false;

            if (OnComplete != null)
            {
                OnComplete();
            }
        }

        List<AutoLeaveData> autoLeaveQueue = new List<AutoLeaveData>();
        class AutoLeaveData
        {
            public string name;
            public float times;
            public AutoLeaveData(string _name, float _times)
            {
                name = _name;
                times = _times;
            }
        }
        IEnumerator AutoLeaveOverlayPage()
        {
            float deltaTime = 0;
            while (true)
            {
                //Debug.LogError("Find auto leave count " + autoLeaveQueue.Count);
                deltaTime = Time.deltaTime;
                ///更新每個 倒數值
                for (int i = 0; i < autoLeaveQueue.Count; i++)
                {
                    //Debug.LogError("Update auto leave value " + autoLeaveQueue[i].name);

                    autoLeaveQueue[i].times -= deltaTime;
                    if (autoLeaveQueue[i].times <= 0)
                    {
                        LeaveOverlayViewPage(autoLeaveQueue[i].name);
                        autoLeaveQueue.Remove(autoLeaveQueue[i]);
                    }
                }

                yield return null;
            }
        }
        public void TryLeaveAllOverlayPage()
        {
            //清空自動離場
            autoLeaveQueue.Clear();

            for (int i = 0; i < overlayPageStates.Count; i++)
            {
                var item = overlayPageStates.ElementAt(i);
                StartCoroutine(LeaveOverlayViewPageBase(item.Value, 0.4f, null, true));
            }
        }
        public void LeaveOverlayViewPage(string viewPageName, float tweenTimeIfNeed = 0.4f, Action OnComplete = null)
        {
            // var vp = overlayPageStates.Where(m => m.name == viewPageName).SingleOrDefault();
            OverlayPageState overlayPageState = null;

            overlayPageStates.TryGetValue(viewPageName, out overlayPageState);

            if (overlayPageState == null)
            {
                Debug.LogError("No live overlay viewPage of name: " + viewPageName + "  found");


                //如果 字典裡找不到 則 new 一個
                overlayPageState = new OverlayPageState();
                overlayPageState.viewPage = viewPage.SingleOrDefault(m => m.name == viewPageName);
                if (overlayPageState == null)
                {
                    return;
                }

                Debug.LogError("No live overlay viewPage of name: " + viewPageName + "  found but try hard fix success");
            }

            overlayPageState.pageChangeCoroutine = StartCoroutine(LeaveOverlayViewPageBase(overlayPageState, tweenTimeIfNeed, OnComplete));
        }

        public IEnumerator LeaveOverlayViewPageBase(OverlayPageState overlayPageState, float tweenTimeIfNeed, Action OnComplete, bool ignoreTransition = false)
        {
            //if (OverlayTransitionProtectionCoroutine != null) { StopCoroutine(OverlayTransitionProtectionCoroutine); }
            //StartCoroutine(OverlayTransitionProtection());

            var currentVe = currentViewPage.viewPageItems.Select(m => m.viewElement);
            var currentVs = currentViewState.viewPageItems.Select(m => m.viewElement);

            var finishTime = CalculateTimesNeedsForOnLeave(overlayPageState.viewPage.viewPageItems.Select(m => m.viewElement));
            // overlayViewPageQueue.Remove(vp);
            overlayPageState.IsTransition = true;

            foreach (var item in overlayPageState.viewPage.viewPageItems)
            {
                if (IsPageTransition == false)
                {
                    if (currentVe.Contains(item.viewElement))
                    {
                        //準備自動離場的 ViewElement 目前的頁面正在使用中 所以不要對他操作
                        try
                        {
                            var vpi = currentViewPage.viewPageItems.FirstOrDefault(m => m.viewElement == item.viewElement);
                            Debug.LogWarning("ViewElement : " + item.viewElement.name + "Try to back to origin Transfrom parent : " + vpi.parent.name);
                            item.viewElement.ChangePage(true, vpi.parent, tweenTimeIfNeed, 0, 0);
                        }
                        catch { }
                        continue;
                    }
                    if (currentVs.Contains(item.viewElement))
                    {
                        //準備自動離場的 ViewElement 目前的頁面正在使用中 所以不要對他操作
                        try
                        {
                            var vpi = currentViewState.viewPageItems.FirstOrDefault(m => m.viewElement == item.viewElement);
                            Debug.LogWarning("ViewElement : " + item.viewElement.name + "Try to back to origin Transfrom parent : " + vpi.parent.name);
                            item.viewElement.ChangePage(true, vpi.parent, tweenTimeIfNeed, 0, 0);
                        }
                        catch { }
                        continue;
                    }
                }
                else
                {
                    ///如果 正在換頁應該以下個頁面來檢查
                    // IEnumerable<ViewElement> nextVe = null;
                    // IEnumerable<ViewElement> nextVs = null;

                    // if (nextViewPage != null)
                    // {
                    //     nextVe = nextViewPage.viewPageItem.Select(m => m.viewElement);
                    // }
                    // if (nextViewState != null)
                    // {
                    //     nextVs = currentViewState.viewPageItems.Select(m => m.viewElement);
                    // }

                    // if (currentVe.Contains(item.viewElement))
                    // {
                    //     //準備自動離場的 ViewElement 下一個頁面正在使用中 所以不要對他操作
                    //     try
                    //     {
                    //         var vpi = currentViewPage.viewPageItem.FirstOrDefault(m => m.viewElement == item.viewElement);
                    //         Debug.LogWarning("ViewElement : " + item.viewElement.name + "Try to back to origin Transfrom parent : " + vpi.parent.name);
                    //         item.viewElement.ChangePage(true, vpi.parent, tweenTimeIfNeed, 0, 0);
                    //     }
                    //     catch { }
                    //     continue;
                    // }
                    // if (currentVs.Contains(item.viewElement))
                    // {
                    //     //準備自動離場的 ViewElement 下一個頁面正在使用中 所以不要對他操作
                    //     try
                    //     {
                    //         var vpi = currentViewState.viewPageItems.FirstOrDefault(m => m.viewElement == item.viewElement);
                    //         Debug.LogWarning("ViewElement : " + item.viewElement.name + "Try to back to origin Transfrom parent : " + vpi.parent.name);
                    //         item.viewElement.ChangePage(true, vpi.parent, tweenTimeIfNeed, 0, 0);
                    //     }
                    //     catch { }
                    //     continue;
                    // }
                }

                float delayOut = 0;
                lastPageItemDelayOutTimesOverlay.TryGetValue(item.viewElement.name, out delayOut);
                item.viewElement.ChangePage(false, null, 0, 0, delayOut, ignoreTransition);
            }

            if (OnOverlayPageLeave != null)
                OnOverlayPageLeave(this, new ViewPageEventArgs(overlayPageState.viewPage, null));

            yield return Yielders.GetWaitForSeconds(finishTime);

            overlayPageState.IsTransition = false;

            overlayPageStates.Remove(overlayPageState.viewPage.name);
            if (OnComplete != null)
            {
                OnComplete();
            }
        }

        public float OverlayTransitionProtectionTime = 0.2f;
        public bool IsOverlayTransition
        {
            get
            {
                foreach (var item in overlayPageStates)
                {
                    if (item.Value.IsTransition == true)
                    {
                        Debug.LogError("Due to " + item.Key + "is Transition");
                        return true;
                    }
                }
                return false;
            }
        }

        // Coroutine OverlayTransitionProtectionCoroutine;
        // IEnumerator OverlayTransitionProtection()
        // {
        //     IsOverlayTransition = true;
        //     yield return Yielders.GetWaitForSeconds(OverlayTransitionProtectionTime);
        //     IsOverlayTransition = false;
        // }

        void UpdateCurrentViewStateAndNotifyEvent(ViewPage vp)
        {
            nextViewPage = null;
            nextViewState = null;

            lastViewPage = currentViewPage;
            currentViewPage = vp;
            if (OnViewPageChange != null)
                OnViewPageChange(this, new ViewPageEventArgs(currentViewPage, lastViewPage));

            if (!string.IsNullOrEmpty(vp.viewState) && viewStatesNames.Contains(vp.viewState) && currentViewState.name != vp.viewState)
            {
                lastViewState = currentViewState;
                currentViewState = viewStates.SingleOrDefault(m => m.name == vp.viewState);

                if (OnViewStateChange != null)
                    OnViewStateChange(this, new ViewStateEventArgs(currentViewState, lastViewState));
            }
        }

        static float maxClampTime = 1;

        float CalculateTimesNeedsForOnLeave(IEnumerable<ViewElement> viewElements)
        {
            float maxOutAnitionTime = 0;

            foreach (var item in viewElements)
            {
                float t = 0;
                if (item.transition == ViewElement.TransitionType.Animator)
                {
                    t = item.GetOutAnimationLength();
                }
                else if (item.transition == ViewElement.TransitionType.CanvasGroupAlpha)
                {
                    t = item.canvasOutTime;
                }

                if (t > maxOutAnitionTime)
                {
                    maxOutAnitionTime = t;
                }
            }
            return Mathf.Clamp(maxOutAnitionTime, 0, maxClampTime);
        }

        float CalculateTimesNeedsForOnShow(IEnumerable<ViewElement> viewElements)
        {
            float maxInAnitionTime = 0;

            foreach (var item in viewElements)
            {
                float t = 0;
                if (item.transition == ViewElement.TransitionType.Animator)
                {
                    t = item.GetInAnimationLength();
                }
                else if (item.transition == ViewElement.TransitionType.CanvasGroupAlpha)
                {
                    t = item.canvasInTime;
                }

                if (t > maxInAnitionTime)
                {
                    maxInAnitionTime = t;
                }
            }
            return Mathf.Clamp(maxInAnitionTime, 0, maxClampTime);
            //return maxOutAnitionTime;
        }
        float CalculateWaitingTimeForCurrentOnLeave(IEnumerable<ViewPageItem> viewPageItems)
        {
            float maxDelayTime = 0;
            foreach (var item in viewPageItems)
            {
                float t2 = item.delayOut;
                if (t2 > maxDelayTime)
                {
                    maxDelayTime = t2;
                }
            }
            return maxDelayTime;
        }
        float CalculateWaitingTimeForCurrentOnShow(IEnumerable<ViewPageItem> viewPageItems)
        {
            float maxDelayTime = 0;
            foreach (var item in viewPageItems)
            {
                float t2 = item.delayIn;
                if (t2 > maxDelayTime)
                {
                    maxDelayTime = t2;
                }
            }
            return maxDelayTime;
        }
        public ViewElementPool viewElementPool;
    }
}