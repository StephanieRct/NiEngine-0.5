using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

namespace Nie
{
    [AddComponentMenu("Nie/Object/ReactOnCollision")]
    public class ReactOnCollision : MonoBehaviour
    {
        #region Data
        [Header("Condition:")]

        [Tooltip("Time in second to delay the reaction.")]
        public float ReactionDelay = 0;

        [Tooltip("If check and reaction is delayed, the 2 objects must be touching for the full duration of the delay.")]
        public bool MustTouchDuringDelay;

        [Tooltip("Once Triggered, cannot triggered again within the cooldown period, in seconds.")]
        public float ReactionCooldown = 0;

        [Tooltip("If reaction is delayed, do not trigger new reactions during the delay.")]
        public bool SingleAtOnce = false;

        [Tooltip("Only react to collision with objects of these layers")]
        public LayerMask ObjectLayerMask = -1;

        public bool ReactToCollision = true;
        public bool ReactToTrigger = true;

        public AnimatorStateReference MustBeInAnimatorState;
        public ReactionStateReference MustBeInReactionState;

        public List<Reaction> Reactions;
        public List<ReactionStateReference> ReactionStates;

        [Tooltip("Print to console events caused by this ReactOnCollision")]
        public bool DebugLog = false;

#if NIE_EXTRAEVENT
        [Header("Events:")]

        [SerializeField]
        [Tooltip("Event called when the reaction happens. (ReactOnCollision this, GameObject other)")]
        UnityEvent<ReactOnCollision, GameObject> OnReact;

        [SerializeField]
        [Tooltip("Event called when this GameObject starts touching another GameObject. Parameters are (ReactOnCollision this, GameObject other)")]
        UnityEvent<ReactOnCollision, GameObject> OnTouchBegin;

        [SerializeField]
        [Tooltip("Event called when this GameObject stops touching another GameObject. Parameters are (ReactOnCollision this, GameObject other)")]
        UnityEvent<ReactOnCollision, GameObject> OnTouchEnd;

        [SerializeField]
        [Tooltip("Event called when this GameObject is touching another GameObject. Parameters are (ReactOnCollision this, GameObject other)")]
        UnityEvent<ReactOnCollision, GameObject> OnTouching;
#endif

        // Keep track of what ReactiveObject are currently touching
        List<GameObject> m_TouchingWith = new();


        float m_CooldownTimer;

        /// <summary>
        /// set only if SingleAtOnce is true
        /// </summary>
        GameObject m_CurrentSingleReaction;

        [System.Serializable]
        public class DelayedReaction
        {
            public GameObject Other;
            public Vector3 Position;
            public float TimerCountdown;
            public DelayedReaction(GameObject other, Vector3 position, float delay)
            {
                Other = other;
                Position = position;
                TimerCountdown = delay;
            }
            public bool Tick()
            {
                if (TimerCountdown >= 0)
                {
                    TimerCountdown -= Time.deltaTime;
                    return TimerCountdown < 0;
                }
                return false;
            }
        }

        // Keep track of all reactions currently on a delay
        List<DelayedReaction> m_DelayedReactions = new();
#endregion

        public void React(DelayedReaction delayedReaction)
        {

            if (DebugLog)
                Debug.Log($"[{Time.frameCount}] ReactOnCollision '{name}' reacts to '{delayedReaction.Other.name}'");

            foreach(var reaction in Reactions)
                reaction.TryReact(delayedReaction.Other.gameObject, delayedReaction.Position);
            foreach (var reaction in ReactionStates)
                reaction.TryReact(delayedReaction.Other.gameObject, delayedReaction.Position);

#if NIE_EXTRAEVENT
            OnReact?.Invoke(this, delayedReaction.Other);
#endif

            if (ReactionCooldown > 0)
                m_CooldownTimer = ReactionCooldown;
        }

        public bool RequestReaction(GameObject other, Vector3 position)
        {
            if (!enabled) return false;
            if (m_CooldownTimer > 0) return false;
            if (SingleAtOnce && m_CurrentSingleReaction != null) return false;
            if ((ObjectLayerMask.value & (1 << other.layer)) == 0) return false;
            if (Reactions.All(x => !x.CanReact(other.gameObject, position)) && ReactionStates.All(x => !x.CanReact(other.gameObject, position)))
                return false;
            if (MustBeInAnimatorState.Animator != null)
                if (MustBeInAnimatorState.Animator.GetCurrentAnimatorStateInfo(0).shortNameHash != MustBeInAnimatorState.StateHash)
                    return false;
            if (MustBeInReactionState.Object != null && !MustBeInReactionState.IsActiveState) return false;
            if (SingleAtOnce) m_CurrentSingleReaction = other;
            return true;
        }


        private void Update()
        {
            // Update all reaction on delay.
            m_DelayedReactions.RemoveAll(reaction =>
            {
                // abort the reaction if the other object was deleted
                if (reaction.Other == null)
                    return true;

                if (reaction.Tick())
                {
                    React(reaction);
                    return true;
                }
                return false;
            });



            if (m_CurrentSingleReaction != null && m_DelayedReactions.Count == 0)
                m_CurrentSingleReaction = null;
        }

        void LateUpdate()
        {
            // all GameObject in TouchingWith are still touching this frame
            foreach (var other in m_TouchingWith)
                Touching(other);
        }

        void OnDestroy()
        {
            foreach (var other in m_TouchingWith)
                EndTouch(other);
        }

#region Touching state
        void BeginTouch(GameObject other, Vector3 position)
        {
            if (DebugLog)
                Debug.Log($"[{Time.frameCount}] ReactOnCollision '{name}' begins touching '{other.name}'");

            m_TouchingWith.Add(other);

            var reaction = new DelayedReaction(other, position, ReactionDelay);
            if (ReactionDelay == 0)
                React(reaction);
            else
                m_DelayedReactions.Add(reaction);

#if NIE_EXTRAEVENT
            OnTouchBegin?.Invoke(this, other);
#endif
        }

        void Touching(GameObject other)
        {
#if NIE_EXTRAEVENT
            OnTouching?.Invoke(this, other);
#endif
        }

        void EndTouch(GameObject other)
        {
            if (DebugLog)
                Debug.Log($"[{Time.frameCount}] ReactOnCollision '{name}' stopped touching '{other.name}'");

#if NIE_EXTRAEVENT
            OnTouchEnd?.Invoke(this, other);
#endif
        }

        bool EndTouchIfTouching(GameObject other)
        {
            // if reactions require the objects to always touch during the delay, remove all current reaction with the other object.
            if (MustTouchDuringDelay)
                m_DelayedReactions.RemoveAll(reaction => reaction.Other == other);

            if (m_TouchingWith.Remove(other))
            {
                EndTouch(other);
                return true;
            }
            return false;
        }
#endregion

#region Collision Callbacks
        public void OnCollisionEnter(Collision collision)
        {
            if (!enabled) return;
            if (!ReactToCollision) return;
            var position = collision.GetContact(0).point;
            if (RequestReaction(collision.gameObject, position))
                BeginTouch(collision.gameObject, position);
        }

        public void OnCollisionExit(Collision collision)
        {
            if (!ReactToCollision) return;
            EndTouchIfTouching(collision.gameObject);
        }

        private void OnTriggerEnter(Collider otherCollider)
        {
            if (!ReactToTrigger) return;
            if (RequestReaction(otherCollider.gameObject, transform.position))
                BeginTouch(otherCollider.gameObject, transform.position);
        }
        private void OnTriggerEnterExit(Collider otherCollider)
        {
            if (!ReactToTrigger) return;
            EndTouchIfTouching(otherCollider.gameObject);
        }
#endregion
    }

}