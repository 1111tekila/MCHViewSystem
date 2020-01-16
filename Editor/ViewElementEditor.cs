﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;

namespace CloudMacaca.ViewSystem
{


    [CanEditMultipleObjects]

    [CustomEditor(typeof(ViewElement))]
    public class ViewElementEditor : Editor
    {
        private ViewElement viewElement = null;
        private SerializedProperty m_AnimTriggerProperty;
        private SerializedProperty m_Injection;
        private SerializedProperty onShowHandle;
        private SerializedProperty onLeaveHandle;
        AnimBool showV2Setting = new AnimBool(true);

        void OnEnable()
        {
            viewElement = (ViewElement)target;
            onShowHandle = serializedObject.FindProperty("OnShowHandle");
            onLeaveHandle = serializedObject.FindProperty("OnLeaveHandle");
            showV2Setting.valueChanged.AddListener(Repaint);
        }
        void OnDisable()
        {
            showV2Setting.valueChanged.RemoveListener(Repaint);
        }
        public override void OnInspectorGUI()
        {

            viewElement.transition = (ViewElement.TransitionType)EditorGUILayout.EnumPopup("ViewElement Transition", viewElement.transition);

            switch (viewElement.transition)
            {
                case ViewElement.TransitionType.Animator:
                    viewElement.animatorTransitionType = (ViewElement.AnimatorTransitionType)EditorGUILayout.EnumPopup("Animator Transition", viewElement.animatorTransitionType);
                    viewElement.AnimationStateName_In = EditorGUILayout.TextField("Show State Name", viewElement.AnimationStateName_In);
                    viewElement.AnimationStateName_Loop = EditorGUILayout.TextField("Loop State Name", viewElement.AnimationStateName_Loop);
                    viewElement.AnimationStateName_Out = EditorGUILayout.TextField("Leave State Name", viewElement.AnimationStateName_Out);
                    if (viewElement.animator != null)
                    {
                        EditorGUILayout.HelpBox("Sepup Complete!", MessageType.Info);
                        if (GUILayout.Button("Reset", EditorStyles.miniButton))
                        {
                            viewElement.Setup();
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("There is no available Animator on GameObject or child.", MessageType.Error);
                        if (GUILayout.Button("Reset", EditorStyles.miniButton))
                        {
                            viewElement.Setup();
                        }
                    }
                    break;
                case ViewElement.TransitionType.CanvasGroupAlpha:
                    viewElement.canvasInEase = (DG.Tweening.Ease)EditorGUILayout.EnumPopup("Show Curve", viewElement.canvasInEase);
                    viewElement.canvasInTime = EditorGUILayout.FloatField("Show Curve", viewElement.canvasInTime);
                    viewElement.canvasOutEase = (DG.Tweening.Ease)EditorGUILayout.EnumPopup("Leave Curve", viewElement.canvasOutEase);
                    viewElement.canvasOutTime = EditorGUILayout.FloatField("Leave Curve", viewElement.canvasOutTime);
                    
                    if (viewElement.canvasGroup == null)
                    {
                        EditorGUILayout.HelpBox("No CanvasGroup found on this GameObject", MessageType.Error);
                        if (GUILayout.Button("Add one", EditorStyles.miniButton))
                        {
                            viewElement.gameObject.AddComponent<CanvasGroup>();
                            EditorUtility.SetDirty(viewElement.gameObject);
                        }
                    }
                    
                    break;
                case ViewElement.TransitionType.ActiveSwitch:
                    break;
                case ViewElement.TransitionType.Custom:
                    EditorGUILayout.HelpBox("Remember Invoke the 'Action' send from those UnityEvent's parameters at the end of your OnShow/OnLeave handler, or the ViewElement may not recovery to pool correctlly!", MessageType.Info);
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.PropertyField(onShowHandle, true);
                    EditorGUILayout.PropertyField(onLeaveHandle, true);
                    EditorGUILayout.EndVertical();
                    break;
            }


            showV2Setting.target = EditorGUILayout.Foldout(showV2Setting.target, new GUIContent("V2 Setting", "Below scope is only used in V2 Version"));
            string hintText = "";
            using (var fade = new EditorGUILayout.FadeGroupScope(showV2Setting.faded))
            {
                if (fade.visible)
                {
                    viewElement.IsUnique = EditorGUILayout.Toggle("Is Unique", viewElement.IsUnique);

                    if (viewElement.IsUnique)
                    {
                        hintText = "Note : Injection will make target component into Singleton, if there is mutil several same component been inject, you will only be able to get the last injection";
                    }
                    else
                    {
                        hintText = "Only Unique ViewElement can be inject";
                    }
                    EditorGUILayout.HelpBox(hintText, MessageType.Info);
                }
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(viewElement);

        }
    }
}