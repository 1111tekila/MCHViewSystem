using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MacacaGames.ViewSystem
{
    [RequireComponent(typeof(ViewElement))]
    public class ViewElementAnimation : MonoBehaviour
    {
        [SerializeField]
        [Header("By defalut the system use the ViewElement itself as the target, but you can override here.")]
        public RectTransform overrideTarget;

        [SerializeField]
        ViewElementAnimationGroup inAnimation;
        [SerializeField]
        ViewElementAnimationGroup outAnimation;

        RectTransform targetObject
        {
            get
            {
                if (overrideTarget != null)
                {
                    return overrideTarget;
                }
                return viewElement.rectTransform;
            }
        }

        ViewElement _viewElement;

        internal ViewElement viewElement
        {
            get
            {
                if (_viewElement != null)
                {
                    _viewElement = GetComponent<ViewElement>();
                }
                return _viewElement;
            }
        }

        public float GetInDuration()
        {
            return inAnimation.GetDuration();
        }

        public float GetOutDuration()
        {
            return outAnimation.GetDuration();
        }

        public IEnumerator PlayIn(Action value)
        {
            return inAnimation.Play(targetObject, value);
        }

        public IEnumerator PlayOut(Action value)
        {
            return outAnimation.Play(targetObject, value);
        }
    }
}
