using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace CloudMacaca.ViewSystem.NodeEditorV2
{
    public class ViewSystemNode
    {

        List<ViewSystemNodeLine> CurrentConnectLine = new List<ViewSystemNodeLine>();
        public ViewSystemNodeLinker nodeConnectionLinker;
        public enum NodeType
        {
            FullPage, Overlay, ViewState, BaseSetting
        }
        public NodeType nodeType;
        public System.Action<IEnumerable<ViewSystemNodeLine>> OnNodeDelete;
        public System.Action<ViewSystemNode> OnNodeSelect;
        public string name;
        protected static int currentMaxId = 0;
        public Rect rect;
        protected Rect drawRect;
        const int lableHeight = 15;
        GUIStyle titleStyle;
        static GUIStyle _TextBarStyle;
        static GUIStyle TextBarStyle
        {
            get
            {
                if (_TextBarStyle == null)
                {
                    _TextBarStyle = new GUIStyle("TE NodeBackground");
                    _TextBarStyle.normal.textColor = Color.white;
                }
                return _TextBarStyle;

            }
        }
        void _OnNodeDelete()
        {
            if (OnNodeDelete != null)
            {
                OnNodeDelete(CurrentConnectLine);
            }
            CurrentConnectLine.Clear();

            if (this is ViewPageNode)
            {
                ((ViewPageNode)this).currentLinkedViewStateNode = null;
            }
            if (this is ViewStateNode)
            {
                ((ViewStateNode)this).currentLinkedViewPageNode.Clear(); ;
            }
        }
        protected void GenerateRightBtnMenu()
        {
            if (nodeType == NodeType.BaseSetting)
            {
                return;
            }
            GenericMenu genericMenu = new GenericMenu();
            genericMenu.AddItem(new GUIContent("Remove node"), false,
                () =>
                {
                    _OnNodeDelete();

                }
            );

            if (nodeType == NodeType.Overlay)
            {
                genericMenu.AddItem(new GUIContent("Conver to FullPage"), false,
                    () =>
                    {
                        nodeType = NodeType.FullPage;
                        SetupNode();
                    }
                );
            }
            if (nodeType == NodeType.FullPage)
            {
                genericMenu.AddItem(new GUIContent("Conver to Overlay"), false,
                    () =>
                    {
                        nodeType = NodeType.Overlay;
                        SetupNode();
                    }
                );
            }
            genericMenu.ShowAsContext();
        }
        public virtual void OnNodeConnect(ViewSystemNode node, ViewSystemNodeLine line)
        {
            CurrentConnectLine.Add(line);
            if (this is ViewPageNode)
            {
                ((ViewPageNode)this).viewPage.viewState = ((ViewStateNode)node).viewState.name;
            }
            if (this is ViewStateNode)
            {
                ((ViewPageNode)node).viewPage.viewState = ((ViewStateNode)this).viewState.name;
            }
        }
        public virtual void Draw() { }
        public virtual void SetupNode() { }
        protected string nodeStyleString = "";
        private Vector2 editorViewPortScroll => ViewSystemNodeEditor.viewPortScroll;
        protected void DrawNode(string _name)
        {
            this.name = _name;
            drawRect = rect;
            drawRect.x += editorViewPortScroll.x;
            drawRect.y += editorViewPortScroll.y;

            //Draw Linker
            if (nodeConnectionLinker != null) nodeConnectionLinker.Draw(drawRect);
            // if (nodeType == ViewStateNode.NodeType.FullPage || nodeType == ViewStateNode.NodeType.Overlay)
            // {
            // }
            if (nodeType == ViewStateNode.NodeType.ViewState)
            {
                GUI.depth = -1;
            }

            if (isSelect)
            {
                GUI.Box(drawRect, "", new GUIStyle(nodeStyleString + " on"));
            }
            else
            {
                GUI.Box(drawRect, "", new GUIStyle(nodeStyleString));
            }

            //Ttiel
            if (!string.IsNullOrEmpty(name))
            {
                if (name.Length > 26) titleStyle = new GUIStyle("MiniLabel");
                else if (name.Length > 15) titleStyle = new GUIStyle("ControlLabel");
                else titleStyle = new GUIStyle("DefaultCenteredLargeText");
                GUI.Label(new Rect(drawRect.x, drawRect.y + 5, drawRect.width, 16), name, titleStyle);
            }

            //Type Bar
            GUI.Label(new Rect(drawRect.x, drawRect.y + drawRect.height - 20, drawRect.width, 16), "  " + nodeType.ToString(), TextBarStyle);

            if (ProcessEvents(Event.current))
            {
                GUI.changed = true;
            }
        }
        public void Drag(Vector2 delta)
        {
            rect = new Rect(rect.x + delta.x, rect.y + delta.y, rect.width, rect.height);
        }


        public bool isSelect = false; public bool isDragged;
        public bool IsInactivable
        {
            get
            {
                return !(
                    ViewSystemNodeGlobalSettingWindow.showGlobalSetting ||
                    OverridePopupWindow.show ||
                    ViewSystemNodeInspector.isMouseInSideBar());
            }
        }
        public bool ProcessEvents(Event e)
        {
            if (IsInactivable == false)
            {
                return false;
            }
            bool isMouseInNode = drawRect.Contains(e.mousePosition); 

            switch (e.type)
            {
                case EventType.KeyDown:
                    if (e.keyCode == KeyCode.Delete)
                    {
                        if (OnNodeDelete != null && isSelect)
                        {
                            _OnNodeDelete();
                        }
                        GUI.changed = true;
                        return true;
                    }
                    break;
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        if (isMouseInNode)
                        {
                            if (OnNodeSelect != null) OnNodeSelect(this);
                            isSelect = true;
                            GUI.changed = true;
                        }
                        else
                        {
                            if (e.control)
                            {
                                return true;
                            }
                            isSelect = false;
                            GUI.changed = true;
                        }
                    }
                    if (e.button == 1)
                    {
                        if (isMouseInNode)
                        {
                            GenerateRightBtnMenu();
                            e.Use();
                            GUI.changed = true;
                        }
                    }
                    break;
                case EventType.MouseUp:

                    break;
                case EventType.MouseDrag:
                    if (e.button == 0 && isSelect)
                    {

                        Drag(e.delta);
                        if (this is ViewStateNode)
                        {
                            if (e.alt)
                            {
                                return true;
                            }
                            foreach (var item in ((ViewStateNode)this).currentLinkedViewPageNode)
                            {
                                item.Drag(e.delta);
                            }
                        }
                        return true;
                    }
                    break;
            }
            // if (isSelect && !string.IsNullOrEmpty(name))
            // {
            //     GUI.Label(new Rect(rect.x, rect.y - 15, 20, 10), "", new GUIStyle("sv_label_1"));
            // }
            return false;
        }
    }
    public class ViewPageNode : ViewSystemNode
    {
        // ViewPageNode 只能連一條線
        public ViewStateNode currentLinkedViewStateNode;
        public Action<ViewStateNode, ViewPageNode> OnNodeTypeConvert;
        public Action<ViewPage> OnPreviewBtnClick;

        public ViewPageNode(Vector2 mousePosition, bool isOverlay, Action<ViewSystemNode> OnConnectionPointClick, Action<ViewStateNode, ViewPageNode> OnNodeTypeConvert, ViewPage viewPage)
        {
            if (viewPage == null)
            {
                this.viewPage = new ViewPage();
                if (isOverlay)
                {
                    this.viewPage.viewPageType = ViewPage.ViewPageType.Overlay;
                }
            }
            else
            {
                this.viewPage = viewPage;
            }
            this.rect = new Rect(mousePosition.x, mousePosition.y, 160, 80);
            this.nodeType = isOverlay ? NodeType.Overlay : NodeType.FullPage;
            this.nodeConnectionLinker = new ViewSystemNodeLinker(nodeType == NodeType.Overlay ? ConnectionPointType.None : ConnectionPointType.Down, this, OnConnectionPointClick);
            this.OnNodeTypeConvert = OnNodeTypeConvert;
            SetupNode();
        }
        public override void SetupNode()
        {
            OnNodeTypeConvert(currentLinkedViewStateNode, this);
            currentLinkedViewStateNode = null;
            var isOverlay = nodeType == NodeType.Overlay;
            if (isOverlay)
            {
                this.viewPage.viewState = "";
            }
            this.viewPage.viewPageType = isOverlay ? ViewPage.ViewPageType.Overlay : ViewPage.ViewPageType.FullPage;
            this.nodeConnectionLinker.type = isOverlay ? ConnectionPointType.None : ConnectionPointType.Down;
            switch (nodeType)
            {
                case NodeType.FullPage:
                    nodeStyleString = "flow node 3";
                    break;
                case NodeType.Overlay:
                    nodeStyleString = "flow node 5";
                    break;
            }
        }
        public override void Draw()
        {
            DrawNode(viewPage.name);
            var btnRect = new Rect(drawRect.x, drawRect.y + drawRect.height - 40, drawRect.width, 18);
            if (GUI.Button(btnRect, "Preview", new GUIStyle("ObjectPickerResultsEven")))
            {
                if (IsInactivable == false) return;
                if (OnPreviewBtnClick != null)
                {
                    OnPreviewBtnClick(viewPage);
                }
            }
            // btnRect.x += rect.width * 0.5f;
            // btnRect.x += 1;
            // if (GUI.Button(btnRect, "Highlight", new GUIStyle("ObjectPickerResultsEven")))
            // {
            //     if (IsInactivable == false) return;
            //     foreach (var item in viewPage.viewPageItems)
            //     {
            //         EditorGUIUtility.PingObject(item.viewElement);
            //     }
            // }
        }

        public ViewPage viewPage;
    }

    public class ViewStateNode : ViewSystemNode
    {
        // ViewStateNode 可以連到多個 ViewPageNode
        public List<ViewPageNode> currentLinkedViewPageNode = new List<ViewPageNode>();
        public ViewStateNode(Vector2 mousePosition, Action<ViewSystemNode> OnConnectionPointClick, ViewState viewState)
        {
            if (viewState == null)
            {
                this.viewState = new ViewState();
            }
            else
            {
                this.viewState = viewState;
            }
            this.rect = new Rect(mousePosition.x, mousePosition.y, 160, 60);
            this.nodeType = NodeType.ViewState;
            this.nodeConnectionLinker = new ViewSystemNodeLinker(ConnectionPointType.Up, this, OnConnectionPointClick);
            nodeStyleString = "flow node 1";
        }

        public override void Draw()
        {
            DrawNode(viewState.name);
        }

        public ViewState viewState;
    }


    public enum ConnectionPointType { Up, Down, None }
    /// <summary>
    /// ViewSystemNodeConnectionPoint 代表一個 Node 上的一個連結點
    /// 一個連結點可以使用 ViewSystemNodeConnectionLine 來進行連線
    /// </summary>
    public class ViewSystemNodeLinker
    {
        //連結點所屬的節點
        public ViewSystemNode viewSystemNode;
        const int ConnectNodeWidth = 28;
        const int ConnectNodeHeight = 28;
        public Rect rect;
        public ConnectionPointType type;
        public GUIStyle style;
        public GUIContent content;
        public Action<ViewSystemNode> OnConnectionPointClick;

        Texture2D ButtonBackground;

        public ViewSystemNodeLinker(ConnectionPointType type, ViewSystemNode viewSystemNode, Action<ViewSystemNode> OnConnectionPointClick)
        {
            this.type = type;
            this.OnConnectionPointClick = OnConnectionPointClick;
            this.viewSystemNode = viewSystemNode;
        }
        public void Draw(Rect target)
        {
            switch (type)
            {
                case ConnectionPointType.Up:
                    rect = new Rect(target.x + target.width * 0.5f - ConnectNodeWidth * 0.5f, target.y + target.height - ConnectNodeHeight * 0.5f, ConnectNodeWidth, ConnectNodeHeight);
                    style = GUIStyle.none;
                    content = new GUIContent(EditorGUIUtility.FindTexture("sv_icon_dot9_pix16_gizmo"));
                    //winbtn_mac_min
                    break;
                case ConnectionPointType.Down:
                    rect = new Rect(target.x + target.width * 0.5f - ConnectNodeWidth * 0.5f, target.y - ConnectNodeHeight * 0.5f, ConnectNodeWidth, ConnectNodeHeight);
                    style = GUIStyle.none;
                    content = new GUIContent(EditorGUIUtility.FindTexture("sv_icon_dot11_pix16_gizmo"));
                    break;
                case ConnectionPointType.None:
                    return;
            }

            GUI.depth = -2;
            if (GUI.Button(rect, content, style))
            {
                if (viewSystemNode.IsInactivable == false) return;
                if (OnConnectionPointClick != null) OnConnectionPointClick(viewSystemNode);
            }
            GUI.Label(rect, new GUIContent(ButtonBackground, "Show Console"));
            // GUI.Label(dotRect, "", dotStyle);
        }
    }

    public class ViewSystemNodeLine
    {
        Color _lineColor;

        Color lineColor
        {
            get
            {
                if (_lineColor == null)
                {
                    _lineColor = new Color(0.34f, 0.56f, 0.92f, 1);
                }
                return _lineColor;
            }
        }
        public ViewStateNode viewStateNode;
        public ViewPageNode viewPageNode;
        private Action<ViewSystemNodeLine> OnRemoveConnectionClick;
        public ViewSystemNodeLine(ViewStateNode viewStateNode, ViewPageNode viewPageNode, Action<ViewSystemNodeLine> OnRemoveConnectionClick)
        {
            this.viewStateNode = viewStateNode;
            this.viewPageNode = viewPageNode;
            this.OnRemoveConnectionClick = OnRemoveConnectionClick;
        }

        public void Draw()
        {
            Vector2 startPoint = viewPageNode.nodeConnectionLinker.rect.center;
            startPoint.y -= viewPageNode.nodeConnectionLinker.rect.height * 0.45f;
            Vector2 endPoint = viewStateNode.nodeConnectionLinker.rect.center;
            endPoint.y += viewStateNode.nodeConnectionLinker.rect.height * 0.4f;

            Handles.DrawBezier(
                startPoint,
                endPoint,
                startPoint,
                endPoint,
                Color.gray,
                null,
                4f
            );
            // Handles.BeginGUI();
            // Handles.color = new Color(0.5f, 0.5f, 0.5f, 1);
            // Handles.DrawLine(viewPageNode.nodeConnectionLinker.rect.center, viewStateNode.nodeConnectionLinker.rect.center);
            // Handles.color = Color.white;
            // Handles.EndGUI();

            var pos = Vector2.Lerp(startPoint, endPoint, 0.5f);
            if (GUI.Button(new Rect(pos.x - 8, pos.y - 8, 16, 16), EditorGUIUtility.FindTexture("d_winbtn_mac_close_h"), GUIStyle.none))
            {
                viewPageNode.viewPage.viewState = "";
                if (OnRemoveConnectionClick != null)
                {
                    OnRemoveConnectionClick(this);
                }
            }
        }
    }

}