using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using System.Reflection;
using UnityEditor.IMGUI.Controls;

namespace CloudMacaca.ViewSystem.NodeEditorV2
{
    public class ViewSystemNodeInspector
    {
        private ViewSystemNode currentSelectNode = null;
        ViewSystemNodeEditor editor;
        public bool show = true;
        AnimBool showBasicInfo;
        AnimBool showViewPageItem;
        ReorderableList viewPageItemList;
        GUIStyle removeButtonStyle;
        GUIStyle oddStyle;
        GUIStyle nameStyle;
        GUIStyle nameErrorStyle;
        GUIStyle nameEditStyle;
        OverridePopupWindow popWindow;
        static ViewSystemSaveData saveData => ViewSystemNodeEditor.saveData;
        static GUIContent EditoModifyButton = new GUIContent(Drawer.prefabIcon, "Show/Hide Modified Properties and Events");
        public ViewSystemNodeInspector(ViewSystemNodeEditor editor)
        {
            this.editor = editor;
            show = true;
            showBasicInfo = new AnimBool(true);
            showBasicInfo.valueChanged.AddListener(this.editor.Repaint);

            showViewPageItem = new AnimBool(true);
            showViewPageItem.valueChanged.AddListener(this.editor.Repaint);

            nameStyle = new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.black }
            };
            nameErrorStyle = new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.red }
            };

            nameEditStyle = new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                imagePosition = ImagePosition.ImageOnly,
                stretchWidth = false,
                stretchHeight = false,
                fixedHeight = 14,
                fixedWidth = 14,
                onNormal = { background = EditorGUIUtility.FindTexture("d_editicon.sml") },
                normal = { background = EditorGUIUtility.FindTexture("d_FilterSelectedOnly") }
            };


            removeButtonStyle = new GUIStyle
            {
                fixedWidth = 25f,
                active =
            {
                background = CMEditorUtility.CreatePixelTexture("Dark Pixel (List GUI)", new Color32(100, 100, 100, 255))
            },
                imagePosition = ImagePosition.ImageOnly,
                alignment = TextAnchor.MiddleCenter
            };

            oddStyle = new GUIStyle
            {
                normal ={
                background = CMEditorUtility.CreatePixelTexture("red Pixel (List GUI)", new Color32(0, 0, 0, 20))

                },
                active =
            {
                background = CMEditorUtility.CreatePixelTexture("red Pixel (List GUI)", new Color32(0, 0, 0, 20))
            },
                imagePosition = ImagePosition.ImageOnly,
                alignment = TextAnchor.MiddleCenter,
                stretchWidth = true,
                stretchHeight = false,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };

            excludePlatformOptions.Clear();

            foreach (var item in Enum.GetValues(typeof(ViewPageItem.PlatformOption)))
            {
                excludePlatformOptions.Add(item.ToString());
            }

        }
        public static bool isMouseInSideBar()
        {
            return rect.Contains(Event.current.mousePosition);
        }

        private static Rect rect;
        List<ViewPageItem> list;
        List<EditableLockItem> editableLock = new List<EditableLockItem>();
        class EditableLockItem
        {
            public EditableLockItem(bool defaultValue)
            {
                parent = defaultValue;
                name = defaultValue;
            }
            public bool parent;
            public bool name;
        }
        public void SetCurrentSelectItem(ViewSystemNode currentSelectNode)
        {
            this.currentSelectNode = currentSelectNode;
            if (currentSelectNode == null)
            {
                return;
            }

            if (currentSelectNode is ViewPageNode)
            {
                list = ((ViewPageNode)currentSelectNode).viewPage.viewPageItems;
            }
            if (currentSelectNode is ViewStateNode)
            {
                list = ((ViewStateNode)currentSelectNode).viewState.viewPageItems;
            }

            editableLock.Clear();
            list.All(x =>
            {
                editableLock.Add(new EditableLockItem(true));
                return true;
            });
            RefreshSideBar();
        }
        void RefreshSideBar()
        {
            viewPageItemList = null;
            viewPageItemList = new ReorderableList(list, typeof(List<ViewPageItem>), true, true, true, false);
            viewPageItemList.drawElementCallback += DrawViewItemElement;
            viewPageItemList.drawHeaderCallback += DrawViewItemHeader;
            viewPageItemList.elementHeight = EditorGUIUtility.singleLineHeight * 5f;
            viewPageItemList.onAddCallback += AddItem;
            viewPageItemList.drawElementBackgroundCallback += DrawItemBackground;
            //viewPageItemList.elementHeightCallback += ElementHight;

        }

        private float ElementHight(int index)
        {
            throw new NotImplementedException();
        }

        private void DrawItemBackground(Rect rect, int index, bool isActive, bool isFocused)
        {
            Rect oddRect = rect;
            oddRect.x -= 20;
            oddRect.width += 100;

            if (isFocused)
            {
                ReorderableList.defaultBehaviours.DrawElementBackground(rect, index, isActive, isFocused, true);
                return;
            }

            if (index % 2 == 0) GUI.Box(oddRect, GUIContent.none, oddStyle);
        }

        private void AddItem(ReorderableList rlist)
        {
            list.Add(new ViewPageItem(null));
            editableLock.Add(new EditableLockItem(true));
        }

        private void DrawViewItemHeader(Rect rect)
        {

            float oriWidth = rect.width;
            rect.height = EditorGUIUtility.singleLineHeight;
            rect.x += 15;
            rect.width = 100;
            EditorGUI.LabelField(rect, "ViewPageItem");

            rect.width = 25;
            rect.x = oriWidth - 25;
            if (GUI.Button(rect, ReorderableList.defaultBehaviours.iconToolbarPlus, ReorderableList.defaultBehaviours.preButton))
            {
                AddItem(viewPageItemList);
            }
        }

        const int rightBtnWidth = 0;
        ViewPageItem copyPasteBuffer;
        ViewElementOverridesImporterWindow overrideChecker;
        private void DrawViewItemElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index > list.Count)
            {
                return;
            }
            var e = Event.current;
            if (rect.Contains(e.mousePosition))
            {
                if (e.button == 1 && e.type == EventType.MouseDown)
                {
                    GenericMenu genericMenu = new GenericMenu();

                    genericMenu.AddItem(new GUIContent("Copy"), false,
                        () =>
                        {
                            copyPasteBuffer = new ViewPageItem(list[index].viewElement);
                            copyPasteBuffer.TweenTime = list[index].TweenTime;
                            copyPasteBuffer.delayOut = list[index].delayOut;
                            copyPasteBuffer.delayIn = list[index].delayIn;
                            copyPasteBuffer.parentPath = list[index].parentPath;

                            var originalOverrideDatas = list[index].overrideDatas.Select(x => x).ToList();
                            var copiedOverrideDatas = originalOverrideDatas.Select(x => new ViewElementPropertyOverrideData
                            {
                                targetComponentType = x.targetComponentType,
                                targetPropertyName = x.targetPropertyName,
                                targetPropertyPath = x.targetPropertyPath,
                                targetPropertyType = x.targetPropertyType,
                                targetTransformPath = x.targetTransformPath,
                                Value = new PropertyOverride
                                {
                                    //ColorValue = x.Value.ColorValue,
                                    //BooleanValue = x.Value.BooleanValue,
                                    ObjectReferenceValue = x.Value.ObjectReferenceValue,
                                    s_Type = x.Value.s_Type,
                                    StringValue = x.Value.StringValue,
                                    //FloatValue = x.Value.FloatValue,
                                    //IntValue = x.Value.IntValue
                                }
                            }).ToList();
                            copyPasteBuffer.overrideDatas = copiedOverrideDatas;

                            var originalEventDatas = list[index].eventDatas.Select(x => x).ToList();
                            var copyEventDatas = originalEventDatas.Select(x => new ViewElementEventData
                            {
                                targetComponentType = x.targetComponentType,
                                targetPropertyName = x.targetPropertyName,
                                targetPropertyPath = x.targetPropertyPath,
                                targetPropertyType = x.targetPropertyType,
                                targetTransformPath = x.targetTransformPath,
                                methodName = x.methodName,
                                scriptName = x.scriptName
                            }).ToList();

                            copyPasteBuffer.eventDatas = copyEventDatas;

                            var excludePlatformCopyed = new List<ViewPageItem.PlatformOption>();
                            foreach (var item in list[index].excludePlatform)
                            {
                                switch (item)
                                {
                                    case ViewPageItem.PlatformOption.Android:
                                        excludePlatformCopyed.Add(ViewPageItem.PlatformOption.Android);
                                        break;
                                    case ViewPageItem.PlatformOption.iOS:
                                        excludePlatformCopyed.Add(ViewPageItem.PlatformOption.iOS);
                                        break;
                                    case ViewPageItem.PlatformOption.UWP:
                                        excludePlatformCopyed.Add(ViewPageItem.PlatformOption.UWP);

                                        break;
                                    case ViewPageItem.PlatformOption.tvOS:
                                        excludePlatformCopyed.Add(ViewPageItem.PlatformOption.tvOS);
                                        break;
                                }
                            }
                            copyPasteBuffer.excludePlatform = excludePlatformCopyed;
                            copyPasteBuffer.parent = list[index].parent;
                        }
                    );
                    if (copyPasteBuffer != null)
                    {
                        genericMenu.AddItem(new GUIContent("Paste (Default)"), false,
                            () =>
                            {
                                list[index] = copyPasteBuffer;
                                list[index].eventDatas.Clear();
                                list[index].overrideDatas.Clear();
                                copyPasteBuffer = null;
                                GUI.changed = true;
                            }
                        );
                        genericMenu.AddItem(new GUIContent("Paste (with Property Data)"), false,
                            () =>
                            {
                                list[index] = copyPasteBuffer;
                                list[index].eventDatas.Clear();
                                copyPasteBuffer = null;
                                GUI.changed = true;
                            }
                        );
                        genericMenu.AddItem(new GUIContent("Paste (with Events Data)"), false,
                           () =>
                           {
                               list[index] = copyPasteBuffer;
                               list[index].overrideDatas.Clear();
                               copyPasteBuffer = null;
                               GUI.changed = true;
                           }
                       );
                        genericMenu.AddItem(new GUIContent("Paste (with All Data)"), false,
                           () =>
                           {
                               list[index] = copyPasteBuffer;
                               copyPasteBuffer = null;
                               GUI.changed = true;
                           }
                       );
                    }

                    genericMenu.ShowAsContext();
                }
            }


            EditorGUIUtility.labelWidth = 80.0f;
            // float oriwidth = rect.width;
            // float oriHeigh = rect.height;
            // float oriX = rect.x;
            // float oriY = rect.y;
            Rect oriRect = rect;

            rect.x = oriRect.x;
            rect.width = oriRect.width - rightBtnWidth;
            rect.height = EditorGUIUtility.singleLineHeight;



            rect.y += EditorGUIUtility.singleLineHeight * 0.25f;
            /*Name Part Start */

            var nameRect = rect;
            //nameRect.height += EditorGUIUtility.singleLineHeight * 0.25f;
            nameRect.width = rect.width - 60;
            GUIStyle nameRuntimeStyle;

            if (list.Where(m => m.name == list[index].name).Count() > 1 && !string.IsNullOrEmpty(list[index].name))
            {
                GUI.color = Color.red;
                nameRuntimeStyle = nameErrorStyle;
            }
            else
            {
                GUI.color = Color.white;
                nameRuntimeStyle = nameStyle;
            }

            if (editableLock[index].name)
            {
                string showName = "Unnamed";
                if (!string.IsNullOrEmpty(list[index].name))
                {
                    showName = list[index].name;
                }
                GUI.Label(nameRect, showName, nameRuntimeStyle);
            }
            else
            {
                list[index].name = EditorGUI.TextField(nameRect, GUIContent.none, list[index].name);
            }
            if (e.isMouse && e.type == EventType.MouseDown && e.clickCount == 2 && nameRect.Contains(e.mousePosition))
            {
                editableLock[index].name = !editableLock[index].name;
            }
            nameRect.x += nameRect.width;
            nameRect.width = 16;
            editableLock[index].name = EditorGUI.Toggle(nameRect, new GUIContent("", "Manual Name"), editableLock[index].name, nameEditStyle);

            GUI.color = Color.white;

            /*Name Part End */

            /*Toggle Button Part Start */
            Rect rightRect = rect;

            rightRect.x = rect.width;
            rightRect.width = 20;
            if (GUI.Button(rightRect, new GUIContent(EditorGUIUtility.FindTexture("_Popup"), "More Setting"), removeButtonStyle))
            {
                PopupWindow.Show(rect, new VS_EditorUtility.ViewPageItemDetailPopup(rect, list[index]));
            }

            rightRect.x -= 20;
            if (GUI.Button(rightRect, new GUIContent(EditorGUIUtility.FindTexture("UnityEditor.InspectorWindow"), "Open in new Instpector tab"), removeButtonStyle))
            {
                if (list[index].viewElement == null)
                {
                    editor.console.LogErrorMessage("ViewElement has not been select yet!");
                    return;
                }
                CloudMacaca.CMEditorUtility.InspectTarget(list[index].viewElement.gameObject);
            }

            rightRect.x -= 20;



            /*Toggle Button Part End */


            rect.y += EditorGUIUtility.singleLineHeight * 1.25f;

            var veRect = rect;
            veRect.width = rect.width - 20;
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                string oriViewElement = "";
                if (list[index].viewElement != null)
                {
                    oriViewElement = list[index].viewElement.name;
                }


                list[index].viewElement = (ViewElement)EditorGUI.ObjectField(veRect, "View Element", list[index].viewElement, typeof(ViewElement), true);
                if (check.changed)
                {
                    if (string.IsNullOrEmpty(list[index].viewElement.gameObject.scene.name))
                    {
                        //is prefabs
                        if (list[index].viewElement.gameObject.name != oriViewElement)
                        {
                            list[index].overrideDatas?.Clear();
                            list[index].eventDatas?.Clear();
                        }

                        return;
                    }

                    var cache = list[index].viewElement;
                    var original = PrefabUtility.GetCorrespondingObjectFromOriginalSource(cache);

                    //if (overrideChecker) overrideChecker.Close();
                    overrideChecker = ScriptableObject.CreateInstance<ViewElementOverridesImporterWindow>();
                    overrideChecker.SetData(cache.transform, original.transform, list[index], currentSelectNode);
                    overrideChecker.ShowUtility();

                    list[index].viewElement = original;
                }
            }

            veRect.x += veRect.width;
            veRect.width = 20;

            if (GUI.Button(veRect, EditoModifyButton, removeButtonStyle))
            {
                if (list[index].viewElement == null)
                {
                    editor.console.LogErrorMessage("ViewElement has not been select yet!");
                    return;
                }
                if (OverridePopupWindow.show == false || editor.overridePopupWindow.viewPageItem != list[index])
                {
                    veRect.y += infoAreaRect.height + EditorGUIUtility.singleLineHeight * 4.5f;
                    editor.overridePopupWindow.SetViewPageItem(list[index]);
                    editor.overridePopupWindow.Show(veRect);
                }
                else
                {
                    OverridePopupWindow.show = false;
                }
            }
            rect.y += EditorGUIUtility.singleLineHeight;

            veRect = rect;
            veRect.width = rect.width - 20;
            if (!editableLock[index].parent)
            {
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    list[index].parentPath = EditorGUI.TextField(veRect, new GUIContent("Parent", list[index].parentPath), list[index].parentPath);
                    if (check.changed)
                    {
                        if (!string.IsNullOrEmpty(list[index].parentPath))
                        {
                            var target = GameObject.Find(saveData.globalSetting.ViewControllerObjectPath + "/" + list[index].parentPath);
                            if (target)
                            {
                                list[index].parent = target.transform;
                            }
                        }
                        else
                            list[index].parent = null;
                    }
                }
            }
            else
            {
                string shortPath = "";
                if (!string.IsNullOrEmpty(list[index]?.parentPath))
                {
                    shortPath = list[index].parentPath.Split('/').Last();
                }
                using (var disable = new EditorGUI.DisabledGroupScope(true))
                {
                    EditorGUI.TextField(veRect, new GUIContent("Parent", list[index].parentPath), shortPath);
                }
            }
            veRect.x += veRect.width;
            veRect.width = 20;
            editableLock[index].parent = EditorGUI.Toggle(veRect, new GUIContent("", "Enable Manual Modify"), editableLock[index].parent, new GUIStyle("IN LockButton"));

            rect.y += EditorGUIUtility.singleLineHeight;

            if (editableLock[index].parent)
            {
                var parentFunctionRect = rect;
                parentFunctionRect.width = rect.width * 0.32f;
                parentFunctionRect.x += rect.width * 0.01f;
                if (GUI.Button(parentFunctionRect, new GUIContent("Pick", "Pick Current Select Transform")))
                {
                    var item = Selection.transforms;
                    if (item.Length > 1)
                    {
                        editor.console.LogErrorMessage("Only object can be selected.");
                        goto PICK_BREAK;
                    }
                    if (item.Length == 0)
                    {
                        editor.console.LogErrorMessage("No object is been select, please check object is in scene or not.");
                        goto PICK_BREAK;
                    }
                    list[index].parent = item.First();
                }
            // Due to while using auto layout we cannot return
            // Therefore use goto to escap the if scope
            PICK_BREAK:
                parentFunctionRect.x += parentFunctionRect.width + rect.width * 0.01f;
                if (GUI.Button(parentFunctionRect, new GUIContent("Select Parent", "Highlight parent Transform object")))
                {
                    var go = GameObject.Find(list[index].parentPath);
                    if (go) EditorGUIUtility.PingObject(go);
                    else editor.console.LogErrorMessage("Target parent is not found, or the target parent is inactive.");
                }
                parentFunctionRect.x += parentFunctionRect.width + rect.width * 0.01f;
                if (GUI.Button(parentFunctionRect, new GUIContent("Select Preview", "Highlight the preview ViewElement object (Only work while is preview the selected page)")))
                {
                    var go = GameObject.Find(list[index].parentPath);
                    if (go.transform.childCount > 0) EditorGUIUtility.PingObject(go.transform.GetChild(0));
                    else editor.console.LogErrorMessage("Target parent is not found, or the target parent is inactive.");
                }
            }
            else
            {
                list[index].parent = (Transform)EditorGUI.ObjectField(rect, "Drag to here", list[index].parent, typeof(Transform), true);
            }

            if (list[index].parent != null)
            {
                var path = AnimationUtility.CalculateTransformPath(list[index].parent, null);
                var sp = path.Split('/');
                if (sp.First() == editor.ViewControllerRoot.name)
                {
                    list[index].parentPath = path.Substring(sp.First().Length + 1);
                }
                else
                {
                    editor.console.LogErrorMessage("Selected Parent is not child of ViewController GameObject");
                    Debug.LogError("Selected Parent is not child of ViewController GameObject");
                }
            }

            rect.width = 18;
            rect.x -= 21;
            if (GUI.Button(rect, new GUIContent(EditorGUIUtility.FindTexture("d_TreeEditor.Trash")), removeButtonStyle))
            {
                if (EditorUtility.DisplayDialog("Remove", "Do you really want to remove this item", "Sure", "Not now"))
                {
                    list.RemoveAt(index);
                    editableLock.RemoveAt(index);
                    RefreshSideBar();
                    return;
                }
            }
            // rect.y += EditorGUIUtility.singleLineHeight;

            // list[index].easeType = (DG.Tweening.Ease)EditorGUI.EnumPopup(rect, new GUIContent("Ease", "The EaseType when needs to tween."), list[index].easeType);
            // rect.y += EditorGUIUtility.singleLineHeight;

            // list[index].TweenTime = EditorGUI.Slider(rect, new GUIContent("Tween Time", "Tween Time use to control when ViewElement needs change parent."), list[index].TweenTime, 0, 1);
            // rect.y += EditorGUIUtility.singleLineHeight;

            // list[index].delayIn = EditorGUI.Slider(rect, "Delay In", list[index].delayIn, 0, 1);
            // rect.y += EditorGUIUtility.singleLineHeight;

            // list[index].delayOut = EditorGUI.Slider(rect, "Delay Out", list[index].delayOut, 0, 1);
            // rect.y += EditorGUIUtility.singleLineHeight;

            //rect.y += EditorGUIUtility.singleLineHeight;
            // bool isExcloudAndroid = !list[index].excludePlatform.Contains(ViewPageItem.PlatformOption.Android);
            // bool isExcloudiOS = !list[index].excludePlatform.Contains(ViewPageItem.PlatformOption.iOS);
            // bool isExcloudtvOS = !list[index].excludePlatform.Contains(ViewPageItem.PlatformOption.tvOS);
            // bool isExcloudUWP = !list[index].excludePlatform.Contains(ViewPageItem.PlatformOption.UWP);


            // EditorGUIUtility.labelWidth = 20.0f;
            // rect.width = oriRect.width * 0.25f;

            // string proIconFix = "";
            // if (EditorGUIUtility.isProSkin)
            // {
            //     proIconFix = "d_";
            // }
            // else
            // {
            //     proIconFix = "";
            // }

            // EditorGUI.BeginChangeCheck();
            // using (var check = new EditorGUI.ChangeCheckScope())
            // {
            //     isExcloudAndroid = EditorGUI.Toggle(rect, new GUIContent(EditorGUIUtility.FindTexture(proIconFix + "BuildSettings.Android.Small")), isExcloudAndroid);
            //     rect.x += rect.width;

            //     isExcloudiOS = EditorGUI.Toggle(rect, new GUIContent(EditorGUIUtility.FindTexture(proIconFix + "BuildSettings.iPhone.Small")), isExcloudiOS);
            //     rect.x += rect.width;

            //     isExcloudtvOS = EditorGUI.Toggle(rect, new GUIContent(EditorGUIUtility.FindTexture(proIconFix + "BuildSettings.tvOS.Small")), isExcloudtvOS);
            //     rect.x += rect.width;

            //     isExcloudUWP = EditorGUI.Toggle(rect, new GUIContent(EditorGUIUtility.FindTexture(proIconFix + "BuildSettings.Standalone.Small")), isExcloudUWP);

            //     if (check.changed)
            //     {
            //         list[index].excludePlatform.Clear();

            //         if (!isExcloudAndroid)
            //         {
            //             list[index].excludePlatform.Add(ViewPageItem.PlatformOption.Android);
            //         }
            //         if (!isExcloudiOS)
            //         {
            //             list[index].excludePlatform.Add(ViewPageItem.PlatformOption.iOS);
            //         }
            //         if (!isExcloudtvOS)
            //         {
            //             list[index].excludePlatform.Add(ViewPageItem.PlatformOption.tvOS);
            //         }
            //         if (!isExcloudUWP)
            //         {
            //             list[index].excludePlatform.Add(ViewPageItem.PlatformOption.tvOS);
            //         }
            //     }
            // }

            //rect.y += EditorGUIUtility.singleLineHeight;
            // }
            // catch (Exception ex)
            // {
            //     Debug.Log("index " + index + " ___1" + ex.Message);

            // }
            // Rect rightRect = oriRect;
            // rightRect.x = rect.x;
            // rightRect.y = rect.y;
            // rightRect.height = rect.height;
            // rightRect.width = 20;

        }
        static float InspectorWidth = 350;
        Rect infoAreaRect;
        public Vector2 scrollerPos;

        public void Draw()
        {
            if (show)
                rect = new Rect(0, 20f, InspectorWidth, editor.position.height - 20f);
            else
                rect = Rect.zero;

            GUILayout.BeginArea(rect, "", new GUIStyle("flow node 0"));

            if (currentSelectNode != null)
            {
                if (currentSelectNode.nodeType == ViewStateNode.NodeType.FullPage || currentSelectNode.nodeType == ViewStateNode.NodeType.Overlay)
                {
                    DrawViewPageDetail(((ViewPageNode)currentSelectNode));
                }
                if (currentSelectNode.nodeType == ViewStateNode.NodeType.ViewState)
                {
                    DrawViewStateDetail(((ViewStateNode)currentSelectNode));
                }
                infoAreaRect = GUILayoutUtility.GetLastRect();
                showViewPageItem.target = EditorGUILayout.Foldout(showViewPageItem.target, "ViewPageItems");
                using (var scroll = new EditorGUILayout.ScrollViewScope(scrollerPos))
                {
                    scrollerPos = scroll.scrollPosition;
                    using (var fade = new EditorGUILayout.FadeGroupScope(showViewPageItem.faded))
                    {
                        if (viewPageItemList != null && fade.visible) viewPageItemList.DoLayoutList();
                    }
                }
            }

            GUILayout.EndArea();
            DrawResizeBar();
        }
        Rect ResizeBarRect;
        bool resizeBarPressed = false;
        void DrawResizeBar()
        {
            ResizeBarRect = new Rect(rect.x + InspectorWidth, rect.y, 4, rect.height);
            EditorGUIUtility.AddCursorRect(ResizeBarRect, MouseCursor.ResizeHorizontal);

            GUI.Box(ResizeBarRect, "");
            if (ResizeBarRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    resizeBarPressed = true;
                }
            }
            if (Event.current.type == EventType.MouseUp)
            {
                resizeBarPressed = false;
            }
            if (resizeBarPressed && Event.current.type == EventType.MouseDrag)
            {
                InspectorWidth += Event.current.delta.x;
                InspectorWidth = Mathf.Clamp(InspectorWidth, 270, 450);
                Event.current.Use();
                GUI.changed = true;
            }
        }

        static List<string> excludePlatformOptions = new List<string>();
        int currentSelect;
        void DrawViewPageDetail(ViewPageNode viewPageNode)
        {
            var vp = viewPageNode.viewPage;
            using (var vertial = new EditorGUILayout.VerticalScope())
            {
                GUILayout.Label(string.IsNullOrEmpty(vp.name) ? "Unnamed" : vp.name, new GUIStyle("DefaultCenteredLargeText"));
                showBasicInfo.target = EditorGUILayout.Foldout(showBasicInfo.target, "Basic Info");
                using (var fade = new EditorGUILayout.FadeGroupScope(showBasicInfo.faded))
                {
                    if (fade.visible)
                    {
                        using (var vertial_in = new EditorGUILayout.VerticalScope())
                        {
                            vp.name = EditorGUILayout.TextField("Name", vp.name);
                            vp.viewPageTransitionTimingType = (ViewPage.ViewPageTransitionTimingType)EditorGUILayout.EnumPopup("ViewPageTransitionTimingType", vp.viewPageTransitionTimingType);
                            using (var disable = new EditorGUI.DisabledGroupScope(vp.viewPageType != ViewPage.ViewPageType.Overlay))
                            {
                                vp.autoLeaveTimes = EditorGUILayout.FloatField("AutoLeaveTimes", vp.autoLeaveTimes);
                            }
                            using (var disable = new EditorGUI.DisabledGroupScope(vp.viewPageTransitionTimingType != ViewPage.ViewPageTransitionTimingType.自行設定))
                            {
                                vp.customPageTransitionWaitTime = EditorGUILayout.FloatField("CustomPageTransitionWaitTime", vp.customPageTransitionWaitTime);
                            }

                            using (var disable = new EditorGUI.DisabledGroupScope(true))
                            {
                                EditorGUILayout.IntField("TargetFrameRate", -1);
                            }
                        }
                    }
                }

            }
        }
        void DrawViewStateDetail(ViewStateNode viewStateNode)
        {
            var vs = viewStateNode.viewState;

            using (var vertial = new EditorGUILayout.VerticalScope())
            {
                GUILayout.Label(string.IsNullOrEmpty(vs.name) ? "Unnamed" : vs.name, new GUIStyle("DefaultCenteredLargeText"));

                showBasicInfo.target = EditorGUILayout.Foldout(showBasicInfo.target, "Basic Info");
                using (var fade = new EditorGUILayout.FadeGroupScope(showBasicInfo.faded))
                {
                    if (fade.visible)
                    {
                        using (var vertical = new EditorGUILayout.VerticalScope(GUILayout.Height(70)))
                        {
                            using (var check = new EditorGUI.ChangeCheckScope())
                            {
                                vs.name = EditorGUILayout.TextField("name", vs.name);
                                if (check.changed)
                                {
                                    viewStateNode.currentLinkedViewPageNode.All(
                                        m =>
                                        {
                                            m.viewPage.viewState = vs.name;
                                            return true;
                                        }
                                    );
                                }
                            }
                            using (var disable = new EditorGUI.DisabledGroupScope(true))
                            {
                                EditorGUILayout.EnumPopup("ViewPageTransitionTimingType", ViewPage.ViewPageTransitionTimingType.接續前動畫);
                                EditorGUILayout.FloatField("AutoLeaveTimes", 0);
                                EditorGUILayout.FloatField("CustomPageTransitionWaitTime", 0);
                            }
                            vs.targetFrameRate = EditorGUILayout.IntField("TargetFrameRate", vs.targetFrameRate);
                        }
                    }
                }

                // showViewPageItem.target = EditorGUILayout.Foldout(showViewPageItem.target, "ViewPageItems");

                // using (var scroll = new EditorGUILayout.ScrollViewScope(scrollerPos))
                // {
                //     scrollerPos = scroll.scrollPosition;
                //     using (var fade = new EditorGUILayout.FadeGroupScope(showViewPageItem.faded))
                //     {
                //         if (viewPageItemList != null && fade.visible) viewPageItemList.DoLayoutList();
                //     }
                // }
            }
        }




    }
}