﻿using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using CloudMacaca.ViewSystem;
using UnityEngine.UIElements;

namespace CloudMacaca.ViewSystem.NodeEditorV2
{
    public class ViewSystemNodeEditor : EditorWindow
    {
        public static ViewSystemNodeEditor Instance;
        static IViewSystemDateReader dataReader;
        public static ViewSystemNodeInspector inspector;
        static ViewSystemGlobalSettingWindow globalSettingWindow;
        public OverridePopupWindow overridePopupWindow;
        public ViewPageNavigationWindow navigationWindow;
        private ViewSystemVerifier viewSystemVerifier;
        public static ViewSystemSaveData saveData;
        bool isInit = false;
        public static bool allowPreviewWhenPlaying = false;
        public static bool overrideFromOrginal = false;
        public Transform ViewControllerRoot;

        [MenuItem("CloudMacaca/ViewSystem/Visual Editor")]
        private static void OpenWindow()
        {
            Instance = GetWindow<ViewSystemNodeEditor>();
            Instance.titleContent = new GUIContent("View System Visual Editor");
            Instance.minSize = new Vector2(600, 400);
            EditorApplication.playModeStateChanged += playModeStateChanged;
        }


        public VisualElement nodeViewContianer;
        public VisualElement toolbarContianer;
        public VisualElement inspectorContianer;
        public VisualElement floatWindowContianer;

        bool inspectorResize = false;
        public void OnEnable()
        {
            Instance = this;
            RefreshData();

            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // // VisualElements objects can contain other VisualElement following a tree hierarchy.
            // VisualElement label = new Label("Hello World! From C#");
            // root.Add(label);

            // Import UXML
            var visualTree = Resources.Load<VisualTreeAsset>("ViewSystemNodeEditorUIElementUxml");
            VisualElement visulaElementFromUXML = visualTree.CloneTree();
            var styleSheet = Resources.Load<StyleSheet>("ViewSystemNodeEditorUIElementUss");
            visulaElementFromUXML.styleSheets.Add(styleSheet);
            visulaElementFromUXML.style.flexGrow = 1;

            toolbarContianer = new IMGUIContainer(DrawMenuBar);
            toolbarContianer.style.flexGrow = 1;
            visulaElementFromUXML.Q("toolbar").Add(toolbarContianer);

            inspectorContianer = new IMGUIContainer(DrawInspector);
            inspectorContianer.style.flexGrow = 1;
            visulaElementFromUXML.Q("inspector").Add(inspectorContianer);


            nodeViewContianer = new IMGUIContainer(DrawNode);
            nodeViewContianer.style.flexGrow = 1;
            visulaElementFromUXML.Q("node-view").Add(nodeViewContianer);

            var dragger = visulaElementFromUXML.Q("dragger");
            dragger.AddManipulator(new VisualElementResizer());

            // floatWindowContianer = new IMGUIContainer(DrawInspector);
            // visulaElementFromUXML.Q("float-window").Add(floatWindowContianer);
            // drager_line_content.RegisterCallback<MouseUpEvent>(
            //     (evt) =>
            //     {
            //         evt.StopPropagation();

            //         //inspectorResize = false;
            //     }
            // );
            // drager_line_content.RegisterCallback<MouseDownEvent>(
            //     (evt) =>
            //     {
            //         evt.StopPropagation();

            //         inspectorResize = true;
            //     }
            // );
            // drager_line_content.RegisterCallback<MouseMoveEvent>(
            //     (evt) =>
            //     {
            //         if (inspectorResize)
            //         {
            //             var i = inspectorContianer.style.width.value;
            //             int newvalue = (int)(i.value + evt.mousePosition.x);
            //             inspectorContianer.style.width = newvalue;
            //         }
            //     }
            // );

            root.Add(visulaElementFromUXML);

            // A stylesheet can be added to a VisualElement.
            // The style will be applied to the VisualElement and all of its children.
            // root.Add(labelWithStyle);

        }

        private static void playModeStateChanged(PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.EnteredPlayMode)
            {
                dataReader.Normalized();
            }
        }

        public void RefreshData(bool hardRefresh = true)
        {
            ClearEditor();
            console = new ViewSystemNodeConsole();
            dataReader = new ViewSystemDataReaderV2(this);
            isInit = dataReader.Init();
            inspector = new ViewSystemNodeInspector(this);
            saveData = ((ViewSystemDataReaderV2)dataReader).GetGlobalSetting();
            ViewControllerRoot = ((ViewSystemDataReaderV2)dataReader).GetViewControllerRoot();
            globalSettingWindow = new ViewSystemGlobalSettingWindow("Global Setting", this, (ViewSystemDataReaderV2)dataReader);
            overridePopupWindow = new OverridePopupWindow("Override", this, inspector);
            navigationWindow = new ViewPageNavigationWindow("Navigation Setting", this);
            viewSystemVerifier = new ViewSystemVerifier(this, saveData);
            viewStatesPopup.Add("All");
            viewStatesPopup.Add("Overlay Only");
            viewStatesPopup.AddRange(viewStateList.Select(m => m.viewState.name));

            if (hardRefresh == false && lastSelectNode != null)
            {
                inspector.SetCurrentSelectItem(lastSelectNode);
            }
        }
        public void ClearEditor()
        {
            nodeConnectionLineList.Clear();
            viewStateList.Clear();
            viewPageList.Clear();
            viewStatesPopup.Clear();
            //inspector.SetCurrentSelectItem(null);
        }
        void OnDestroy()
        {
            dataReader.Normalized();
            EditorApplication.playModeStateChanged -= playModeStateChanged;
        }
        // void OnFocus()
        // {
        //     if (dataReader == null) dataReader = new ViewSystemDataReaderV2(this);
        //     if (console == null) console = new ViewSystemNodeConsole();
        //     if (inspector == null) inspector = new ViewSystemNodeInspector(this);
        //     Instance = this;
        // }

        List<ViewPageNode> viewPageList = new List<ViewPageNode>();
        List<ViewStateNode> viewStateList = new List<ViewStateNode>();
        List<ViewSystemNodeLine> nodeConnectionLineList = new List<ViewSystemNodeLine>();
        public ViewSystemNodeConsole console;
        public static float zoomScale = 1.0f;
        Rect scriptViewRect;
        public static Vector2 viewPortScroll;
        Vector2 zoomScaleMinMax = new Vector2(0.25f, 1);
        protected virtual void DoZoom(float delta, Vector2 center)
        {
            var prevZoom = zoomScale;
            zoomScale += delta;
            zoomScale = Mathf.Clamp(zoomScale, zoomScaleMinMax.x, zoomScaleMinMax.y);
            var deltaSize = nodeViewContianer.contentRect.size / prevZoom - nodeViewContianer.contentRect.size / zoomScale;
            var offset = -Vector2.Scale(deltaSize, center);
            viewPortScroll += offset;
        }
        void DrawFloatWindow()
        {
            if (console.show) console.Draw(new Vector2(nodeViewContianer.contentRect.width, nodeViewContianer.contentRect.height));

            BeginWindows();
            if (globalSettingWindow != null) globalSettingWindow.OnGUI();
            if (overridePopupWindow != null) overridePopupWindow.OnGUI();
            if (navigationWindow != null) navigationWindow.OnGUI();
            EndWindows();
        }
        void DrawNode()
        {

            scriptViewRect = new Rect(nodeViewContianer.contentRect.x, nodeViewContianer.contentRect.y - menuBarHeight + 2, nodeViewContianer.contentRect.width / zoomScale, nodeViewContianer.contentRect.height / zoomScale);

            EditorZoomArea.NoGroupBegin(zoomScale, scriptViewRect);
            DrawGrid();
            foreach (var item in nodeConnectionLineList.ToArray())
            {
                item.Draw();
            }
            foreach (var item in viewPageList.ToArray())
            {
                item.Draw();
            }
            foreach (var item in viewStateList.ToArray())
            {
                item.Draw();
            }
            DrawCurrentConnectionLine(Event.current);
            EditorZoomArea.NoGroupEnd();

            DrawFloatWindow();

            ProcessEvents(Event.current);
            CheckRepaint();
        }

        void DrawInspector()
        {
            inspector.Draw();
        }
        // void OnGUI()
        // {

        //     zoomArea = position;
        //     zoomArea.height -= menuBarHeight;
        //     zoomArea.y = menuBarHeight;
        //     zoomArea.x = 0;

        //     Rect scriptViewRect = new Rect(0, 0, this.position.width / zoomScale, this.position.height / zoomScale);

        //     EditorZoomArea.Begin(zoomScale, scriptViewRect);
        //     DrawGrid();
        //     foreach (var item in nodeConnectionLineList.ToArray())
        //     {
        //         item.Draw();
        //     }
        //     foreach (var item in viewPageList.ToArray())
        //     {
        //         item.Draw();
        //     }
        //     foreach (var item in viewStateList.ToArray())
        //     {
        //         item.Draw();
        //     }
        //     DrawCurrentConnectionLine(Event.current);
        //     EditorZoomArea.End();

        //     GUI.depth = -100;
        //     DrawMenuBar();

        //     if (console.show) console.Draw(new Vector2(position.width, position.height));
        //     if (inspector.show) inspector.Draw();

        //     BeginWindows();
        //     if (globalSettingWindow != null)
        //         globalSettingWindow.OnGUI();

        //     if (overridePopupWindow != null) overridePopupWindow.OnGUI();
        //     if (navigationWindow != null) navigationWindow.OnGUI();

        //     EndWindows();

        //     ProcessEvents(Event.current);
        //     CheckRepaint();
        // }
        public void CheckRepaint()
        {
            //if (Event.current.type == EventType.Repaint || Event.current.type == EventType.Layout) return;
            if (GUI.changed) Repaint();
        }

        private void ProcessEvents(Event e)
        {
            drag = Vector2.zero;
            switch (e.type)
            {
                case EventType.MouseDrag:
                    if (e.button == 2)
                    {
                        if (scriptViewRect.Contains(e.mousePosition))
                        {
                            OnDrag(e.delta * 1 / zoomScale);
                        }
                    }
                    break;
                case EventType.ScrollWheel:
                    //OnDrag(e.delta * -1);
                    // float target = zoomScale - e.delta.y * 0.1f;
                    // zoomScale = Mathf.Clamp(target, 0.1f, 1f);
                    // GUI.changed = true;
                    Vector2 zoomCenter;
                    zoomCenter.x = e.mousePosition.x / zoomScale / nodeViewContianer.contentRect.width;
                    zoomCenter.y = e.mousePosition.y / zoomScale / nodeViewContianer.contentRect.height;
                    zoomCenter *= zoomScale;
                    DoZoom(-e.delta.y * 0.01f, zoomCenter);
                    e.Use();
                    break;
                case EventType.MouseDown:
                    if (selectedViewPageNode != null || selectedViewStateNode != null)
                    {
                        ClearConnectionSelection();
                        return;
                    }
                    if (e.button == 1)
                    {
                        if (ViewSystemNodeInspector.isMouseInSideBar() && inspector.show)
                        {
                            return;
                        }
                        if (!nodeViewContianer.contentRect.Contains(e.mousePosition))
                        {
                            return;
                        }
                        GenericMenu genericMenu = new GenericMenu();

                        genericMenu.AddItem(new GUIContent("Add FullPage"), false,
                            () =>
                            {
                                Vector2 pos = new Vector2(e.mousePosition.x - inspectorContianer.contentRect.width, e.mousePosition.y - menuBarHeight);
                                Vector2 unScalePos = new Vector2(-ViewSystemNode.ViewSystemNodeWidth * 0.5f, -ViewSystemNode.ViewPageNodeHeight * .5f);
                                AddViewPageNode((pos * 1 / zoomScale + unScalePos) - viewPortScroll, false);
                            }
                        );
                        genericMenu.AddItem(new GUIContent("Add OverlayPage"), false,
                            () =>
                            {
                                Vector2 pos = new Vector2(e.mousePosition.x - inspectorContianer.contentRect.width, e.mousePosition.y - menuBarHeight);
                                Vector2 unScalePos = new Vector2(-ViewSystemNode.ViewSystemNodeWidth * 0.5f, -ViewSystemNode.ViewPageNodeHeight * .5f);
                                AddViewPageNode((pos * 1 / zoomScale + unScalePos) - viewPortScroll, true);
                            }
                        );
                        genericMenu.AddItem(new GUIContent("Add ViewState"), false,
                            () =>
                            {
                                Vector2 pos = new Vector2(e.mousePosition.x - inspectorContianer.contentRect.width, e.mousePosition.y - menuBarHeight);
                                Vector2 unScalePos = new Vector2(-ViewSystemNode.ViewSystemNodeWidth * 0.5f, -ViewSystemNode.ViewStateNodeHeight * .5f);
                                AddViewStateNode((pos * 1 / zoomScale + unScalePos) - viewPortScroll);
                            }
                        );
                        genericMenu.ShowAsContext();
                    }
                    break;
            }
        }

        ViewSystemNode lastSelectNode;

        public ViewStateNode AddViewStateNode(Vector2 position, ViewState viewState = null)
        {
            var node = new ViewStateNode(position, CheckCanMakeConnect, viewState);
            node.OnNodeSelect += (m) =>
            {
                lastSelectNode = m;
                inspector.SetCurrentSelectItem(m);
            };
            node.OnNodeDelete += (line) =>
            {
                dataReader.OnViewStateDelete(node);
                viewStateList.Remove(node);
                foreach (var item in line)
                {
                    nodeConnectionLineList.Remove(item);
                }
            };

            if (viewState == null)
            {
                dataReader.OnViewStateAdd(node);
            }
            viewStateList.Add(node);
            return node;
        }

        public ViewPageNode AddViewPageNode(Vector2 position, bool isOverlay, ViewPage viewPage = null)
        {
            var node = new ViewPageNode(position, isOverlay, CheckCanMakeConnect,
                (vsn, vpn) =>
                {
                    var oriLine = FindViewSystemNodeConnectionLine(vsn, vpn);
                    if (vsn != null) vsn.currentLinkedViewPageNode.Remove(vpn);
                    if (oriLine != null) nodeConnectionLineList.Remove(oriLine);
                },
                viewPage
            );
            node.OnPreviewBtnClick += (m) =>
            {
                dataReader.OnViewPagePreview(m);
            };
            node.OnNodeSelect += (m) =>
            {
                lastSelectNode = m;
                inspector.SetCurrentSelectItem(m);
            };
            node.OnNodeDelete += (line) =>
            {
                dataReader.OnViewPageDelete(node);
                viewPageList.Remove(node);
                foreach (var item in line)
                {
                    nodeConnectionLineList.Remove(item);
                }
            };
            if (viewPage == null)
            {
                dataReader.OnViewPageAdd(node);
            }
            viewPageList.Add(node);
            return node;
        }

        void CheckCanMakeConnect(ViewSystemNode currentClickNode)
        {
            switch (currentClickNode.nodeType)
            {
                case ViewSystemNode.NodeType.ViewState:
                    selectedViewStateNode = (ViewStateNode)currentClickNode;
                    //如果當前的 ViewPagePoint 不是 null
                    if (selectedViewPageNode != null)
                    {
                        if (selectedViewStateNode.currentLinkedViewPageNode.Count(m => m.nodeType != selectedViewPageNode.nodeType) > 0)
                        {
                            if (EditorUtility.DisplayDialog(
                                "Warring",
                                $"The target ViewState is already connect to one or more {selectedViewPageNode.nodeType} Page.\nSince ViewState can only connect to one type of ViewPage(FullPage or Overlay) you should remove all {selectedViewPageNode.nodeType} connection on ViewState to continue.\nOr press 'OK' the editor will help you do this stuff.",
                                "Go Ahead",
                                "I'll do it myself"))
                            {
                                //刪掉線
                                foreach (var pagenode in selectedViewStateNode.currentLinkedViewPageNode)
                                {
                                    pagenode.viewPage.viewState = string.Empty;
                                    pagenode.currentLinkedViewStateNode = null;
                                    var oriConnect = FindViewSystemNodeConnectionLine(selectedViewStateNode, pagenode);
                                    nodeConnectionLineList.Remove(oriConnect);
                                }
                                selectedViewStateNode.currentLinkedViewPageNode.Clear();
                                CreateConnection();
                                ClearConnectionSelection();
                            }
                            else
                            {
                                ClearConnectionSelection();
                            }
                            return;
                        }


                        //檢查是不是已經有跟這個 ViewPageNode 節點連線過了
                        if (!selectedViewStateNode.currentLinkedViewPageNode.Contains(selectedViewPageNode) &&
                            selectedViewPageNode.currentLinkedViewStateNode == null
                            )
                        {
                            CreateConnection();
                            ClearConnectionSelection();
                        }
                        // 如果連線的節點跟原本的不同 刪掉舊的連線 然後建立新了
                        else if (selectedViewPageNode.currentLinkedViewStateNode != null)
                        {
                            //刪掉 ViewStateNode 裡的 ViewPageNode
                            selectedViewPageNode.currentLinkedViewStateNode.currentLinkedViewPageNode.Remove(selectedViewPageNode);

                            //刪掉線
                            console.LogWarringMessage("Break original link, create new link");
                            var oriConnect = FindViewSystemNodeConnectionLine(selectedViewPageNode.currentLinkedViewStateNode, selectedViewPageNode);
                            nodeConnectionLineList.Remove(oriConnect);
                            CreateConnection();
                            ClearConnectionSelection();
                        }
                        else
                        {
                            console.LogErrorMessage("The node has linked before");
                            ClearConnectionSelection();
                        }
                    }
                    break;
                //被聯線的是 FullPage
                case ViewSystemNode.NodeType.FullPage:
                    selectedViewPageNode = (ViewPageNode)currentClickNode;
                    if (selectedViewStateNode != null)
                    {
                        if (selectedViewStateNode.currentLinkedViewPageNode.Count(m => m.nodeType != selectedViewPageNode.nodeType) > 0)
                        {
                            if (EditorUtility.DisplayDialog(
                                "Warring",
                                $"The target ViewPage is already connect to one or more {selectedViewPageNode.nodeType} Page.\nSince ViewState can only connect to one type of ViewPage(FullPage or Overlay) you should remove all {selectedViewPageNode.nodeType} connection on ViewState to continue.\nOr press 'OK' the editor will help you do this stuff.",
                                "Go Ahead",
                                "I'll do it myself"))
                            {
                                //刪掉線
                                foreach (var pagenode in selectedViewStateNode.currentLinkedViewPageNode)
                                {
                                    pagenode.viewPage.viewState = string.Empty;
                                    pagenode.currentLinkedViewStateNode = null;
                                    var oriConnect = FindViewSystemNodeConnectionLine(selectedViewStateNode, pagenode);
                                    nodeConnectionLineList.Remove(oriConnect);
                                }
                                selectedViewStateNode.currentLinkedViewPageNode.Clear();

                                CreateConnection();
                                ClearConnectionSelection();
                            }
                            else
                            {
                                ClearConnectionSelection();
                            }

                            return;
                        }
                        //檢查是不是已經有跟這個 ViewStateNode 節點連線過了
                        if (selectedViewPageNode.currentLinkedViewStateNode == null)
                        {
                            CreateConnection();
                            ClearConnectionSelection();
                        }
                        // 如果連線的節點跟原本的不同 刪掉舊的連線 然後建立新了
                        else if (selectedViewPageNode.currentLinkedViewStateNode != selectedViewStateNode)
                        {
                            console.LogWarringMessage("Break original link, create new link");
                            //刪掉 ViewStateNode 裡的 ViewPageNode
                            selectedViewPageNode.currentLinkedViewStateNode.currentLinkedViewPageNode.Remove(selectedViewPageNode);

                            //刪掉線
                            var oriConnect = FindViewSystemNodeConnectionLine(selectedViewPageNode.currentLinkedViewStateNode, selectedViewPageNode);
                            nodeConnectionLineList.Remove(oriConnect);
                            CreateConnection();
                            ClearConnectionSelection();
                        }
                        else
                        {
                            console.LogErrorMessage("The FullPage has linked before");
                            ClearConnectionSelection();
                        }
                    }
                    break;

                //Overlay
                case ViewSystemNode.NodeType.Overlay:
                    selectedViewPageNode = (ViewPageNode)currentClickNode;
                    if (selectedViewStateNode != null)
                    {
                        if (selectedViewStateNode.currentLinkedViewPageNode.Count(m => m.nodeType != selectedViewPageNode.nodeType) > 0)
                        {
                            if (EditorUtility.DisplayDialog(
                                "Warring",
                                $"The target ViewPage is already connect to one or more {selectedViewPageNode.nodeType} Page.\nSince ViewState can only connect to one type of ViewPage(FullPage or Overlay) you should remove all {selectedViewPageNode.nodeType} connection on ViewState to continue.\nOr press 'OK' the editor will help you do this stuff.",
                                "Go Ahead",
                                "I'll do it myself"))
                            {
                                //刪掉線
                                foreach (var pagenode in selectedViewStateNode.currentLinkedViewPageNode)
                                {
                                    pagenode.viewPage.viewState = string.Empty;
                                    pagenode.currentLinkedViewStateNode = null;
                                    var oriConnect = FindViewSystemNodeConnectionLine(selectedViewStateNode, pagenode);
                                    nodeConnectionLineList.Remove(oriConnect);
                                }
                                selectedViewStateNode.currentLinkedViewPageNode.Clear();

                                CreateConnection();
                                ClearConnectionSelection();
                            }
                            else
                            {
                                ClearConnectionSelection();
                            }

                            return;
                        }
                        //檢查是不是已經有跟這個 ViewStateNode 節點連線過了
                        if (selectedViewPageNode.currentLinkedViewStateNode == null)
                        {
                            CreateConnection();
                            ClearConnectionSelection();
                        }
                        // 如果連線的節點跟原本的不同 刪掉舊的連線 然後建立新了
                        else if (selectedViewPageNode.currentLinkedViewStateNode != selectedViewStateNode)
                        {
                            console.LogWarringMessage("Break original link, create new link");
                            //刪掉 ViewStateNode 裡的 ViewPageNode
                            selectedViewPageNode.currentLinkedViewStateNode.currentLinkedViewPageNode.Remove(selectedViewPageNode);

                            //刪掉線
                            var oriConnect = FindViewSystemNodeConnectionLine(selectedViewPageNode.currentLinkedViewStateNode, selectedViewPageNode);
                            nodeConnectionLineList.Remove(oriConnect);
                            CreateConnection();
                            ClearConnectionSelection();
                        }
                        else
                        {
                            console.LogErrorMessage("The Overlay has linked before");
                            ClearConnectionSelection();
                        }
                    }
                    break;
            }
        }

        private void CreateConnection()
        {
            CreateConnection(selectedViewStateNode, selectedViewPageNode);
        }
        public void CreateConnection(ViewStateNode viewStateNode)
        {
            if (string.IsNullOrEmpty(viewStateNode.viewState.name))
            {
                return;
            }
            var vps = viewPageList.Where(m => m.viewPage.viewState == viewStateNode.viewState.name);
            if (vps.Count() == 0)
            {
                return;
            }
            foreach (var item in vps)
            {
                // if (item.nodeType == ViewSystemNode.NodeType.Overlay)
                // {
                //     continue;
                // }
                CreateConnection(viewStateNode, item);
            }
        }
        private void CreateConnection(ViewStateNode viewStateNode, ViewPageNode viewPageNode)
        {
            viewStateNode.currentLinkedViewPageNode.Add(viewPageNode);
            viewPageNode.currentLinkedViewStateNode = viewStateNode;
            var line = new ViewSystemNodeLine(
                viewStateNode,
                viewPageNode,
                RemoveConnection);

            viewStateNode.OnNodeConnect(viewPageNode, line);
            viewPageNode.OnNodeConnect(viewStateNode, line);
            // View
            nodeConnectionLineList.Add(line);
            console.LogMessage("Create Link, State:" + viewStateNode.viewState.name + ", Page :" + viewPageNode.viewPage.name);
        }

        ViewSystemNodeLine FindViewSystemNodeConnectionLine(ViewStateNode viewStateNode, ViewPageNode viewPageNode)
        {
            return nodeConnectionLineList.SingleOrDefault(x => x.viewPageNode == viewPageNode && x.viewStateNode == viewStateNode);
        }

        void RemoveConnection(ViewSystemNodeLine connectionLine)
        {
            connectionLine.viewPageNode.currentLinkedViewStateNode = null;
            connectionLine.viewStateNode.currentLinkedViewPageNode.Remove(connectionLine.viewPageNode);
            nodeConnectionLineList.Remove(connectionLine);
        }
        private void ClearConnectionSelection()
        {
            selectedViewStateNode = null;
            selectedViewPageNode = null;
        }

        private void OnDrag(Vector2 delta)
        {
            viewPortScroll += delta * zoomScale;
            GUI.changed = true;
        }

        private Vector2 drag;
        protected void DrawGrid()
        {
            float width = this.position.width / zoomScale;
            float height = this.position.height / zoomScale;
            Color c = Color.gray;
            c.a = 0.5f;
            Handles.color = c;


            float gridSize = 32f;

            float x = viewPortScroll.x % gridSize;
            while (x < width)
            {
                Handles.DrawLine(new Vector2(x, 0), new Vector2(x, height));
                x += gridSize;
            }

            float y = (viewPortScroll.y % gridSize);
            while (y < height)
            {
                if (y >= 0)
                {
                    Handles.DrawLine(new Vector2(0, y), new Vector2(width, y));
                }
                y += gridSize;
            }

            Handles.color = Color.white;
        }

        private float menuBarHeight = 20f;
        private Rect menuBar;
        List<string> viewStatesPopup = new List<string>();

        int lastInspectorWidth;
        private void DrawMenuBar()
        {
            menuBar = new Rect(0, 0, position.width, menuBarHeight);

            using (var area = new GUILayout.AreaScope(menuBar, "", EditorStyles.toolbar))
            {
                using (var horizon = new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(5);
                    if (GUILayout.Button(new GUIContent($"Save{(((ViewSystemDataReaderV2)dataReader).isDirty ? "*" : "")}"), EditorStyles.toolbarButton, GUILayout.Width(50)))
                    {
                        if (isInit == false)
                        {
#if UNITY_2019_1_OR_NEWER
                            ShowNotification(new GUIContent("Editor is not Initial."), 2);
#else
                            ShowNotification(new GUIContent("Editor is not Initial."));
#endif
                            return;
                        }
                        if (EditorUtility.DisplayDialog("Save", "Save action will also delete all ViewElement in scene. \nDo you really want to continue?", "Yes", "No"))
                        {
                            dataReader.Save(viewPageList, viewStateList);
                        }
                    }
                    if (GUILayout.Button(new GUIContent("Reload", Drawer.refreshIcon, "Reload data"), EditorStyles.toolbarButton, GUILayout.Width(80)))
                    {
                        if (overridePopupWindow != null) overridePopupWindow.show = false;
                        RefreshData();
                    }
                    GUILayout.Space(5);
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        inspector.show = GUILayout.Toggle(inspector.show, new GUIContent(Drawer.sideBarIcon, "Show SideBar"), EditorStyles.toolbarButton, GUILayout.Height(menuBarHeight), GUILayout.Width(25));
                        if (check.changed)
                        {
                            inspectorContianer.parent.style.display = inspector.show ? DisplayStyle.Flex : DisplayStyle.None;
                        }
                    }
                    GUILayout.Space(5);
                    console.show = GUILayout.Toggle(console.show, new GUIContent(Drawer.miniErrorIcon, "Show Console"), EditorStyles.toolbarButton, GUILayout.Height(menuBarHeight), GUILayout.Width(25));
                    GUILayout.Space(5);
                    if (globalSettingWindow != null)
                        globalSettingWindow.show = GUILayout.Toggle(globalSettingWindow.show, new GUIContent("Global Setting", EditorGUIUtility.FindTexture("SceneViewTools")), EditorStyles.toolbarButton, GUILayout.Height(menuBarHeight));

                    GUILayout.Space(5);

                    if (GUILayout.Button(new GUIContent("Verifiers"), EditorStyles.toolbarDropDown, GUILayout.Height(menuBarHeight)))
                    {
                        //viewSystemVerifier.VerifyPagesAndStates();
                        GenericMenu genericMenu = new GenericMenu();
                        genericMenu.AddItem(new GUIContent("Verify GameObjects"), false,
                            () =>
                            {
                                viewSystemVerifier.VerifyGameObject(ViewSystemVerifier.VerifyTarget.All);
                            }
                        );
                        genericMenu.AddItem(new GUIContent("Verify Overrides"), false,
                            () =>
                            {
                                viewSystemVerifier.VerifyComponent(ViewSystemVerifier.VerifyTarget.Override);
                            }
                        );
                        genericMenu.AddItem(new GUIContent("Verify Events"), false,
                            () =>
                            {
                                viewSystemVerifier.VerifyComponent(ViewSystemVerifier.VerifyTarget.Event);
                            }
                        );
                        genericMenu.AddItem(new GUIContent("Verify Pages and States"), false,
                            () =>
                            {
                                viewSystemVerifier.VerifyPagesAndStates();
                            }
                        );
                        genericMenu.ShowAsContext();
                    }

                    allowPreviewWhenPlaying = GUILayout.Toggle(allowPreviewWhenPlaying, new GUIContent("Allow Preview when Playing"), EditorStyles.toolbarButton, GUILayout.Height(menuBarHeight));
                    overrideFromOrginal = GUILayout.Toggle(overrideFromOrginal, new GUIContent("Get Override From Orginal"), EditorStyles.toolbarButton, GUILayout.Height(menuBarHeight));
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(new GUIContent(Drawer.zoomIcon, "Zoom"), GUIStyle.none);
                    zoomScale = EditorGUILayout.Slider(zoomScale, zoomScaleMinMax.x, zoomScaleMinMax.y, GUILayout.Width(120));

                    if (GUILayout.Button(new GUIContent(EditorGUIUtility.FindTexture("AvatarCompass"), "Reset viewport to (0,0)"), EditorStyles.toolbarButton, GUILayout.Width(30)))
                    {
                        viewPortScroll = Vector2.zero;
                    }

                    // GUILayout.Label("ViewState:");
                    // int newIndex = EditorGUILayout.Popup(currentIndex, viewStatesPopup.ToArray(),
                    //     EditorStyles.toolbarPopup, GUILayout.Width(80));
                    // if (newIndex != currentIndex)
                    // {
                    //     currentIndex = newIndex;
                    //     targetViewState = viewStatesPopup[currentIndex];
                    // }

                    if (GUILayout.Button(new GUIContent(Drawer.bakeScritpIcon, "Bake ViewPage and ViewState to script"), EditorStyles.toolbarButton, GUILayout.Width(50)))
                    {
                        ViewSystemScriptBaker.BakeAllViewPageName(viewPageList.Select(m => m.viewPage).ToList(), viewStateList.Select(m => m.viewState).ToList());
                    }

                    if (GUILayout.Button(new GUIContent("Clear Preview", "Clear all preview item"), EditorStyles.toolbarButton))
                    {
                        ((ViewSystemDataReaderV2)dataReader).ClearAllViewElementInScene();
                    }
                    if (GUILayout.Button(new GUIContent("Normalized", "Normalized all item (Will Delete the Canvas Root Object in Scene)"), EditorStyles.toolbarButton))
                    {
                        overridePopupWindow.show = false;
                        dataReader.Normalized();
                    }
                }
            }
        }

        private ViewStateNode selectedViewStateNode;
        private ViewPageNode selectedViewPageNode;
        private void DrawCurrentConnectionLine(Event e)
        {
            if (selectedViewStateNode != null && selectedViewPageNode == null)
            {
                Handles.DrawLine(e.mousePosition, selectedViewStateNode.nodeConnectionLinker.rect.center);
                GUI.changed = true;
            }

            if (selectedViewPageNode != null && selectedViewStateNode == null)
            {
                Handles.DrawLine(e.mousePosition, selectedViewPageNode.nodeConnectionLinker.rect.center);
                GUI.changed = true;
            }
        }

        public bool IsNodeInactivable
        {
            get
            {
                return !(
                    globalSettingWindow.show ||
                    overridePopupWindow.show ||
                    navigationWindow.show ||
                    ViewSystemNodeInspector.isMouseInSideBar());
            }
        }


        class VisualElementResizer : MouseManipulator
        {
            private Vector2 m_Start;
            protected bool m_Active;
            public VisualElementResizer()
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
                m_Active = false;
            }

            protected override void RegisterCallbacksOnTarget()
            {

                target.RegisterCallback<MouseDownEvent>(OnMouseDown);
                target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
                target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
                target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
                target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            }

            protected void OnMouseDown(MouseDownEvent e)
            {
                if (m_Active)
                {
                    e.StopImmediatePropagation();
                    return;
                }

                if (CanStartManipulation(e))
                {
                    m_Start = e.localMousePosition;

                    m_Active = true;
                    target.CaptureMouse();
                    e.StopPropagation();
                }
            }

            protected void OnMouseMove(MouseMoveEvent e)
            {
                if (!m_Active || !target.HasMouseCapture())
                    return;

                Vector2 diff = e.localMousePosition - m_Start;

                //target.parent.style.height = target.parent.layout.height + diff.x;
                var t = target.parent.parent.ElementAt(0);

                int w = (int)t.style.width.value.value;
                t.style.width = Mathf.Clamp(w + diff.x, 250, 450);

                e.StopPropagation();
            }

            protected void OnMouseUp(MouseUpEvent e)
            {
                if (!m_Active || !target.HasMouseCapture() || !CanStopManipulation(e))
                    return;

                m_Active = false;
                target.ReleaseMouse();
                e.StopPropagation();
            }
        }

        [UnityEditor.Callbacks.OnOpenAsset(0)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var asset = EditorUtility.InstanceIDToObject(instanceID) as ViewSystemSaveData;
            if (asset == null)
                return false;

            OpenWindow();

            return true;
        }
    }
}
