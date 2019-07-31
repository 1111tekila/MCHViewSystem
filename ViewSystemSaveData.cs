﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CloudMacaca.ViewSystem
{
    public class ViewSystemSaveData : ScriptableObject
    {
        public ViewSystemBaseSetting baseSetting;
        public List<ViewStateSaveData> viewStates = new List<ViewStateSaveData>();
        public List<ViewPageSaveData> viewPages = new List<ViewPageSaveData>();

        [System.Serializable]
        public class ViewPageSaveData
        {
            public ViewPageSaveData(Vector2 nodePosition, ViewPage viewPage)
            {
                this.nodePosition = nodePosition;
                this.viewPage = viewPage;
            }
            public Vector2 nodePosition;
            public ViewPage viewPage;
        }

        //Save Data Model
        [System.Serializable]
        public class ViewStateSaveData
        {
            public ViewStateSaveData(Vector2 nodePosition, ViewState viewState)
            {
                this.nodePosition = nodePosition;
                this.viewState = viewState;
            }
            public Vector2 nodePosition;
            public ViewState viewState;
        }

        [System.Serializable]
        public class ViewSystemBaseSetting
        {
            public Vector2 nodePosition = new Vector2(500, 500);
            public string ViewControllerObjectPath;
            public GameObject UIRoot;
            public GameObject UIRootScene;
            public float MaxWaitingTime
            {
                get
                {
                    return Mathf.Clamp01(_maxWaitingTime);
                }
            }
            public float _maxWaitingTime = 1;
        }

    }
    [System.Serializable]
    public class ViewElementPropertyOverrideData
    {
        public string id;
        public string targetTransformPath;
        public string targetComponentType;
        public string targetPropertyName;
        public string targetPropertyType;
        public PropertyOverride Value;

    }
    [System.Serializable]
    public class PropertyOverride
    {
        // public AnimationCurve AnimationCurveValue;

        public bool BooleanValue;

        //public Bounds BoundsValue;

        public Color ColorValue;

        //public double DoubleValue;

        public float FloatValue;

        public int IntValue;

        //public long LongValue;

        public UnityEngine.Object ObjectReferenceValue;

        //public Quaternion QuaternionValue;

        //public Rect RectValue;

        public string StringValue;

        //public Vector2 Vector2Value;

        //public Vector3 Vector3Value;

        //public Vector4 Vector4Value;
    }
}