﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;

namespace CloudMacaca.ViewSystem.NodeEditorV2
{
    public class ViewSystemNodeGlobalSettingWindow
    {
        ViewSystemNodeEditor editor;
        ViewSystemDataReaderV2 dataReader;
        public static bool showGlobalSetting;
        bool lastOpen;
        private Rect rect = new Rect(0, 0, 350, 400);
        //public BaseSettingNode node;
        static ViewSystemSaveData saveData => ViewSystemNodeEditor.saveData;

        public ViewSystemNodeGlobalSettingWindow(ViewSystemNodeEditor editor, ViewSystemDataReaderV2 dataReader)
        {
            this.editor = editor;
            this.dataReader = dataReader;
            showGlobalSetting = false;
            m_ShowEventScript = new AnimBool(true);
            m_ShowEventScript.valueChanged.AddListener(editor.Repaint);
        }

        Vector2 scrollPosition;
        AnimBool m_ShowEventScript;

        public void OnGUI()
        {
            rect = GUILayout.Window(11110, rect, Draw, "Base Setting");
            if (lastOpen != showGlobalSetting)
            {
                rect.x = editor.position.width * 0.5f;
                rect.y = editor.position.height * 0.5f;
     
            }
            lastOpen = showGlobalSetting;
        }
        public void Draw(int id)
        {
            //node.clickContainRect = rect;

            using (var scroll = new GUILayout.ScrollViewScope(scrollPosition))
            {
                scrollPosition = scroll.scrollPosition;
                //GUILayout.Label("Base Setting", new GUIStyle("DefaultCenteredLargeText"));
                saveData.globalSetting.ViewControllerObjectPath = EditorGUILayout.TextField("View Controller GameObject", saveData.globalSetting.ViewControllerObjectPath);
                EditorGUILayout.HelpBox("View Controller GameObject is the GameObject name in scene which has ViewController attach on.", MessageType.Info);

                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    saveData.globalSetting.UIRootScene = (GameObject)EditorGUILayout.ObjectField("UI Root Object (In Scene)", saveData.globalSetting.UIRootScene, typeof(GameObject), true);
                    if (check.changed)
                    {
                        if (saveData.globalSetting.UIRootScene == null)
                        {
                            saveData.globalSetting.UIRoot = null;
                        }
                        else
                        {
                            var go = dataReader.SetUIRootObject(saveData.globalSetting.UIRootScene);
                            saveData.globalSetting.UIRoot = go;
                        }
                    }
                }
                using (var disable = new EditorGUI.DisabledGroupScope(true))
                {
                    EditorGUILayout.ObjectField("UI Root Object (In Assets)", saveData.globalSetting.UIRoot, typeof(GameObject), true);
                }
                EditorGUILayout.HelpBox("UI Root Object will generate and set as a child of 'View Controller GameObject' after View System init.", MessageType.Info);

                saveData.globalSetting._maxWaitingTime = EditorGUILayout.Slider(new GUIContent("Change Page Max Waitning", "The max waiting for change page, if previous page need time more than this value ,ViewController wiil force transition to next page."), saveData.globalSetting._maxWaitingTime, 0, 1);
                EditorGUILayout.HelpBox("The max waiting for change page, if previous page need time more than this value ,ViewController wiil force transition to next page.", MessageType.Info);

                using (var horizon = new GUILayout.HorizontalScope())
                {
                    m_ShowEventScript.target = EditorGUILayout.Foldout(m_ShowEventScript.target, "Event Scripts");
                    if (GUILayout.Button("Add", GUILayout.Width(60)))
                    {
                        saveData.globalSetting.EventHandleBehaviour.Add(null);
                    }
                }
                using (var fade = new EditorGUILayout.FadeGroupScope(m_ShowEventScript.faded))
                {
                    if (fade.visible)
                    {
                        for (int i = 0; i < saveData.globalSetting.EventHandleBehaviour.Count; i++)
                        {
                            using (var horizon = new GUILayout.HorizontalScope())
                            {
                                if (GUILayout.Button(GUIContent.none, new GUIStyle("OL Minus"), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                                {
                                    saveData.globalSetting.EventHandleBehaviour.Remove(saveData.globalSetting.EventHandleBehaviour[i]);
                                }
                                saveData.globalSetting.EventHandleBehaviour[i] = (MonoScript)EditorGUILayout.ObjectField(saveData.globalSetting.EventHandleBehaviour[i], typeof(MonoScript), false);

                            }
                        }
                    }
                }

            }

            GUI.DragWindow(new Rect(0, 0, editor.position.width, editor.position.height));

        }
    }

}
