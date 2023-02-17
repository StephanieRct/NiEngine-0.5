using UnityEngine;
using UnityEngine.Events;

namespace Nie
{
    /// <summary>
    /// Makes the owner Gameobject grabbable by a GrabberController
    /// </summary>
    [AddComponentMenu("Nie/Object/Grabbable")]
    [RequireComponent(typeof(Rigidbody))]
    public class Grabbable : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Event called when this grabbable is grabbed by a GrabberController")]
        UnityEvent<Grabbable, GrabberController> OnGrab;

        [SerializeField]
        [Tooltip("Event called when this grabbable is release by a GrabberController")]
        UnityEvent<Grabbable, GrabberController> OnRelease;

        public bool IsGrabbed => GrabbedBy != null;
        public GrabberController GrabbedBy { get; private set; }

        public void ReleaseIfGrabbed()
        {
            GrabbedBy?.ReleaseGrabbed();
        }

        /// <summary>
        /// Call when a GrabberController grabs this grabbable
        /// </summary>
        /// <param name="by"></param>
        public void GrabBy(GrabberController by)
        {
            GrabbedBy = by;
            OnGrab?.Invoke(this, by);
        }

        /// <summary>
        /// Call when a GrabberController release this grabbable
        /// </summary>
        /// <param name="by"></param>
        public void ReleaseBy(GrabberController by)
        {
            OnRelease?.Invoke(this, by);
            GrabbedBy = null;
        }

    }
}