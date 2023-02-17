using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nie
{
    /// <summary>
    /// Makes the owner gameobject able to grab and release Grabbable Gameobjects
    /// </summary>
    [AddComponentMenu("Nie/Player/GrabberController")]
    public class GrabberController : MonoBehaviour
    {
        [Tooltip("The currently grabbed object")]
        public Grabbable GrabbedGrabbable { get; private set; }

        [Tooltip("The currently grabbed Rigidbody")]
        public Rigidbody GrabbedRigidbody { get; private set; }

        [Tooltip("Where the grabbed object will be move toward.")]
        public Transform GrabPosition;

        [Tooltip("Object to move to the currently focused grabbable.")]
        public GameObject Hand;

        [Tooltip("Force applied to the grabbed object")]
        public float HoldForce = 100;
        [Tooltip("Physics velocity drag to apply on the held object")]
        public float HoldDrag = 5;
        [Tooltip("Physics angular velocity drag to apply on the held object")]
        public float HoldAngularDrag = 5;

        [Tooltip("If true, set the grabbed object and all its children to a different layer")]
        public bool ChangedGrabbedObjectLayer = false;
        
        [Tooltip("The layer to set on the grabbed object if ChangedGrabbedObjectLayer is checked")]
        public GameObjectLayer GrabbedObjectLayer;

        [Tooltip("Output debug log when objects are grabbed or released")]
        public bool DebugLog;

        int m_PreviousGRabbedObjectLayer;

        // true if currently grabbing a grabbable
        bool m_IsGrabbing;

        // Velocity drag previously set on the currently grabbed grabbable
        float m_GrabbedOldDrag;

        // Angular velocity drag previously set on the currently grabbed grabbable
        float m_GrabbedOldAngularDrag;

        // Relative position where the currently grabbed grabbable was grabbed
        Vector3 m_GrabbedPosition;

        void Update()
        {
            // TODO tie this into the input system
            if (Input.GetMouseButtonDown(0))
            {
                TryGrabInFront();
            }
            if (Input.GetMouseButtonDown(1))
            {
                ReleaseGrabbed();
            }
            if (!m_IsGrabbing && Hand != null)
            {
                var rendererHand = Hand.GetComponent<MeshRenderer>();
                var rendererGrabPosition = GrabPosition.GetComponent<MeshRenderer>();
                if (rendererHand != null) rendererHand.enabled = false;
                if (rendererGrabPosition != null) rendererGrabPosition.enabled = true;

                var ray = new Ray(transform.position, (GrabPosition.position - transform.position).normalized);
                if (Physics.Raycast(ray, out var hit) && hit.rigidbody != null && hit.rigidbody.gameObject.TryGetComponent<Grabbable>(out var grabbable))
                {
                    Hand.transform.position = hit.point;
                    if (rendererHand != null) rendererHand.enabled = true;
                    if (rendererGrabPosition != null) rendererGrabPosition.enabled = false;
                }
            }
            // Move the currently grabbed grabbable
            if (GrabbedGrabbable != null)
            {
                var grabPoint = GrabbedRigidbody.transform.TransformPoint(m_GrabbedPosition);
                var diff = GrabPosition.position - grabPoint;
                GrabbedRigidbody.drag = HoldDrag >= 0 ? HoldDrag : m_GrabbedOldDrag;
                GrabbedRigidbody.angularDrag = HoldAngularDrag >= 0 ? HoldAngularDrag : m_GrabbedOldAngularDrag;
                GrabbedRigidbody.AddForceAtPosition(diff * HoldForce * GrabbedRigidbody.mass, grabPoint);
            }
            else if (m_IsGrabbing)
            {
                // grabbed object was destroyed
                m_IsGrabbing = false;
                if(DebugLog)
                    Debug.Log($"'{name}' Release destroyed object");
            }
        }

        /// <summary>
        /// Will try to grab the first grabbable in front of the controller using a ray-cast
        /// </summary>
        public void TryGrabInFront()
        {
            var ray = new Ray(transform.position, (GrabPosition.position - transform.position).normalized);
            if (Physics.Raycast(ray, out var hit))
            {
                if (hit.rigidbody != null && !hit.rigidbody.isKinematic && hit.rigidbody != null && hit.rigidbody.gameObject.TryGetComponent<Grabbable>(out var grabbable))
                {
                    Grab(grabbable, hit.point);
                }
            }

        }
        public void SetGameObjectLayer(GameObject obj, int layer)
        {
            obj.layer = layer;
            for (int i = 0; i < obj.transform.childCount; i++)
                SetGameObjectLayer(obj.transform.GetChild(i).gameObject, layer);
        }
        /// <summary>
        /// Grab a given grabbable
        /// </summary>
        /// <param name="grabbable"></param>
        /// <param name="grabPosition"></param>
        public void Grab(Grabbable grabbable, Vector3 grabPosition)
        {

            ReleaseGrabbed();
            m_IsGrabbing = true;

            if (Hand != null && Hand.TryGetComponent<MeshRenderer>(out var mr))
                mr.enabled = false;
            GrabbedGrabbable = grabbable;
            GrabbedRigidbody = GrabbedGrabbable.GetComponent<Rigidbody>();

            m_GrabbedOldDrag = GrabbedRigidbody.drag;
            m_GrabbedOldAngularDrag = GrabbedRigidbody.angularDrag;
            if (HoldDrag >= 0) GrabbedRigidbody.drag = HoldDrag;
            if (HoldAngularDrag >= 0) GrabbedRigidbody.angularDrag = HoldAngularDrag;
            if (ChangedGrabbedObjectLayer)
            {
                m_PreviousGRabbedObjectLayer = grabbable.gameObject.layer;

                SetGameObjectLayer(grabbable.gameObject, GrabbedObjectLayer.LayerIndex);
            }
            m_GrabbedPosition = grabbable.transform.InverseTransformPoint(grabPosition);
            grabbable.GrabBy(this);

            if (DebugLog)
                Debug.Log($"'{name}' grabbed '{GrabbedGrabbable.name}'");
        }

        /// <summary>
        /// Release currently grabbed grabbable
        /// </summary>
        public void ReleaseGrabbed()
        {
            if (GrabbedGrabbable == null) return;

            if (DebugLog) 
                Debug.Log($"'{name}' Release '{GrabbedGrabbable.name}'");

            m_IsGrabbing = false;

            if (HoldDrag >= 0) GrabbedRigidbody.drag = m_GrabbedOldDrag;
            if (HoldAngularDrag >= 0) GrabbedRigidbody.angularDrag = m_GrabbedOldAngularDrag;

            if (ChangedGrabbedObjectLayer)
                SetGameObjectLayer(GrabbedGrabbable.gameObject, m_PreviousGRabbedObjectLayer);

            GrabbedGrabbable.ReleaseBy(this);
            GrabbedGrabbable = null;
            GrabbedRigidbody = null;
        }
    }

}