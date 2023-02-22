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
        public bool DebugLog;

        public AnimatorStateReference MustBeInAnimatorState;
        public ReactionStateReference MustBeInReactionState;

        [SerializeField]
        [Tooltip("Event called when this grabbable is grabbed by a GrabberController")]
        UnityEvent<Grabbable, GrabberController> OnGrab;

        [SerializeField]
        [Tooltip("Event called when this grabbable is release by a GrabberController")]
        UnityEvent<Grabbable, GrabberController> OnRelease;

        [SerializeField]
        [Tooltip("Event called when a GrabberController looks at this grabbable before grabbing it.")]
        UnityEvent<Grabbable, GrabberController> OnFocus;

        [SerializeField]
        [Tooltip("Event called when a GrabberController either stops looking at this grabbable or has grabbed it after focusing on it.")]
        UnityEvent<Grabbable, GrabberController> OnUnfocus;

        public bool IsGrabbed => GrabbedBy != null;
        public GrabberController GrabbedBy { get; private set; }

        public void ReleaseIfGrabbed()
        {
            GrabbedBy?.ReleaseGrabbed();
        }

        public bool CanGrab(GrabberController by, Vector3 position)
        {
            if (!enabled) return false;
            if (MustBeInReactionState.Object != null && !MustBeInReactionState.IsActiveState) return false;
            if (MustBeInAnimatorState.Animator != null)
                if (MustBeInAnimatorState.Animator.GetCurrentAnimatorStateInfo(0).shortNameHash != MustBeInAnimatorState.StateHash)
                    return false;
            return true;
        }
        /// <summary>
        /// Call when a GrabberController grabs this grabbable
        /// </summary>
        /// <param name="by"></param>
        public void GrabBy(GrabberController by)
        {
            if (DebugLog)
                Debug.Log($"[{Time.frameCount}] Grabbable '{name}' Grab By '{by.name}'");
            GrabbedBy = by;
            OnGrab?.Invoke(this, by);
        }

        /// <summary>
        /// Call when a GrabberController release this grabbable
        /// </summary>
        /// <param name="by"></param>
        public void ReleaseBy(GrabberController by)
        {
            if (DebugLog)
                Debug.Log($"[{Time.frameCount}] Grabbable '{name}' Release By '{by.name}'");
            OnRelease?.Invoke(this, by);
            GrabbedBy = null;
        }

        public void Focus(GrabberController by)
        {
            if (DebugLog)
                Debug.Log($"[{Time.frameCount}] Grabbable '{name}' Focus By '{by.name}'");
            OnFocus?.Invoke(this, by);
        }
        public void Unfocus(GrabberController by)
        {
            if (DebugLog)
                Debug.Log($"[{Time.frameCount}] Grabbable '{name}' Unfocus By '{by.name}'");
            OnUnfocus?.Invoke(this, by);
        }

    }
}