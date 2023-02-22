using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nie
{
    [AddComponentMenu("Nie/Player/ToucherController")]
    public class ToucherController : MonoBehaviour
    {
        [Tooltip("Where the ray cast to detect any touchable will be directer toward. If left null, will ray cast in the middle of the screen")]
        public Transform TouchPositionObject;

        [Tooltip("Object to move to the currently focused touchable.")]
        public GameObject Hand;

        [Tooltip("Output debug log when objects are grabbed or released")]
        public bool DebugLog;

        Touchable m_Touching;
        Touchable m_Focus;

        public Vector3 TouchPosition => TouchPositionObject != null ? TouchPositionObject.position : transform.position + transform.forward;
        void Update()
        {
            // Update Touchable Focus 
            if (m_Touching == null)
            {
                var ray = new Ray(transform.position, (TouchPosition - transform.position).normalized);
                if (Physics.Raycast(ray, out var hit) && hit.collider.gameObject.TryGetComponent<Touchable>(out var touchable) && touchable.CanTouch(this, hit.point))
                {
                    ShowHand(hit.point);
                    if (m_Focus != touchable)
                        Focus(touchable);
                }
                else
                {
                    Unfocus();
                }
            }


            // TODO tie this into the input system
            if (Input.GetMouseButton(0))
                TryTouchInFront();
            else
                Release();


        }
        void HideHand()
        {
            if (Hand != null && Hand.TryGetComponent<MeshRenderer>(out var rendererHand))
                rendererHand.enabled = false;
            if (TouchPositionObject.TryGetComponent<MeshRenderer>(out var rendererGrabPosition))
                rendererGrabPosition.enabled = true;
        }
        void ShowHand(Vector3 position)
        {
            if (Hand != null)
            {
                Hand.transform.position = position;
                if (Hand.TryGetComponent<MeshRenderer>(out var rendererHand))
                    rendererHand.enabled = true;
            }
            if (TouchPositionObject.TryGetComponent<MeshRenderer>(out var rendererGrabPosition))
                rendererGrabPosition.enabled = false;
        }
        public void TryTouchInFront()
        {
            var ray = new Ray(transform.position, (TouchPosition - transform.position).normalized);
            if (Physics.Raycast(ray, out var hit) && hit.collider.gameObject.TryGetComponent<Touchable>(out var touchable) && touchable.CanTouch(this, hit.point))
                Touch(touchable, hit.point);
            else
                Release();
        }

        public void Touch(Touchable touchable, Vector3 position)
        {
            Unfocus();
            if (m_Touching == touchable) return;
            if (DebugLog)
                Debug.Log($"[{Time.frameCount}] ToucherController '{name}' touches '{touchable.name}'");
            m_Touching = touchable;
            touchable.Touch(this, position);
        }

        public void Release()
        {
            if (m_Touching == null) return;
            if (DebugLog)
                Debug.Log($"[{Time.frameCount}] ToucherController '{name}' Release Touchable '{m_Focus.name}'");
            m_Touching.Release(this);
            m_Touching = null;
        }
        public void Focus(Touchable touchable)
        {
            Unfocus();
            if (DebugLog)
                Debug.Log($"[{Time.frameCount}] ToucherController '{name}' Focus Touchable '{touchable.name}'");
            m_Focus = touchable;
            m_Focus.Focus(this);
        }
        public void Unfocus()
        {
            if (m_Focus == null) return;
            if (DebugLog)
                Debug.Log($"[{Time.frameCount}] ToucherController '{name}' Unfocus Touchable '{m_Focus.name}'");
            m_Focus.Unfocus(this);
            m_Focus = null;
            HideHand();
        }
    }
}