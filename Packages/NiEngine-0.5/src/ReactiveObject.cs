#define REACTIVEOBJECTEXT
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

namespace Nie
{
    [System.Serializable]
    public struct AnimatorStateReference
    {
        public Animator Animator;
        public string State;
        public int StateHash;
    }

    /// <summary>
    /// A reactive object will trigger a reaction when it collide with a matching ReactiveObject
    /// </summary>
    [AddComponentMenu("Nie/Object/ReactiveObject")]
    public class ReactiveObject : MonoBehaviour
    {
        #region Data
        [Header("Reaction Condition:")]

        [Tooltip("Name for this object.")]
        public string ThisReactionName;

        [Tooltip("Name for the other object. \n\rFor the reaction to happen between 2 ReactiveObject: \n\r* this.ThisReactionName must be equal to other.OtherReactionName \n\r AND \n\r\t* this.OtherReactionName must be equal to other.ThisReactionName.")]
        public string OtherReactionName;

        [Tooltip("Time in second to delay the reaction.")]
        public float ReactionDelay = 0;

        [Tooltip("If check and reaction is delayed, the 2 objects must be touching for the full duration of the delay.")]
        public bool MustTouchDuringDelay;

        [Tooltip("Once this ReactiveObject reacts with another ReactiveObject, the same reaction cannot be triggered again within the cooldown period, in seconds.")]
        public float ReactionCooldown = 0;

        [Tooltip("If reaction is delayed, do not trigger new reactions during the delay.")]
        public bool SingleAtOnce = false;
        /// <summary>
        /// set only if Multiple is false
        /// </summary>
        private ReactiveObject m_CurrentSingleReaction;

        public bool ReactToCollision = true;
        public bool ReactToTrigger = true;

        public AnimatorStateReference MustBeInAnimatorState;

        [Header("Reaction:")]

        [Tooltip("When reaction is activated, destroy this GameObject")]
        public bool DestroyGameObject;

        [Tooltip("If Destroy is checked, instantiate the provided GameObject in its place, with the same transform.")]
        public GameObject ReplaceWith;

        [Tooltip("If set, instantiate the provided GameObject at the collision point")]
        public GameObject SpawnAtCollision;

        [Tooltip("If set, move the other ReactiveObject's transform to the provided Transform.")]
        public Transform MoveOtherAt;

        [Tooltip("Will release this object if it has a Grabbable component and is currently grabbed")]
        public bool ReleaseGrabbed;

        public AnimatorStateReference PlayAnimatorState;

        [Header("Debug:")]
        [Tooltip("Print to console events caused by this ReactiveObject")]
        public bool DebugLog = false;

        [Header("Events:")]

        [SerializeField]
        [Tooltip("Event called when the reaction happens")]
        UnityEvent<ReactiveObject, ReactiveObject> OnReact;

#if REACTIVEOBJECTEXT
        [SerializeField]
        [Tooltip("Event called when this ReactiveItem starts touching another ReactiveItem with matching respective names. Parameters are (ReactiveItem this, ReactiveItem other)")]
        UnityEvent<ReactiveObject, ReactiveObject> OnTouchBegin;

        [SerializeField]
        [Tooltip("Event called when this ReactiveItem stops touching another ReactiveItem with matching respective names. Parameters are (ReactiveItem this, ReactiveItem other)")]
        UnityEvent<ReactiveObject, ReactiveObject> OnTouchEnd;

        [SerializeField]
        [Tooltip("Event called when this ReactiveItem is touching another ReactiveItem with matching respective names. Parameters are (ReactiveItem this, ReactiveItem other)")]
        UnityEvent<ReactiveObject, ReactiveObject> OnTouching;
#endif

        // Keep track of what ReactiveObject are currently touching
        List<ReactiveObject> m_TouchingWith = new();

        // Keep track of what Reaction is currently on cooldown
        List<Reaction> m_CooldownWith = new();

        [System.Serializable]
        public class Reaction
        {
            public ReactiveObject Other;
            public Vector3 Position;
            public float TimerCountdown;
            public Reaction(ReactiveObject other, Vector3 position, float delay)
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
        // Keep track of all Reaction currently on a delay
        List<Reaction> m_Reactions = new();
        #endregion

        public void React(Reaction reaction)
        {
            
            if (DebugLog)
                Debug.Log($"[{Time.frameCount}] ReactiveObject '{ThisReactionName}' reacts to '{reaction.Other.ThisReactionName}'");

            if (MoveOtherAt != null)
            {
                reaction.Other.transform.position = MoveOtherAt.transform.position;
                reaction.Other.transform.rotation = MoveOtherAt.transform.rotation;
                if (reaction.Other.TryGetComponent<Grabbable>(out var grabbable))
                    grabbable.ReleaseIfGrabbed();
            }

            if (SpawnAtCollision != null)
            {
                Instantiate(SpawnAtCollision, reaction.Position, Quaternion.identity);
            }

            if (ReleaseGrabbed && TryGetComponent<Grabbable>(out var grabbable2))
                grabbable2.ReleaseIfGrabbed();
            if(PlayAnimatorState.Animator != null)
                PlayAnimatorState.Animator.Play(PlayAnimatorState.StateHash);
            OnReact?.Invoke(this, reaction.Other);

#if REACTIVEOBJECTEXT
            OnTouchBegin?.Invoke(this, reaction.Other);
#endif

            if (DestroyGameObject)
            {
                var pos = transform.position;
                var rot = transform.rotation;
                Destroy(gameObject);
                if (ReplaceWith != null)
                    Instantiate(ReplaceWith, pos, rot);
            }
            else if (ReactionCooldown > 0)
            {
                reaction.TimerCountdown = ReactionCooldown;
                m_CooldownWith.Add(reaction);
            }
        }

        public bool RequestReaction(ReactiveObject other)
        {
            if (!enabled) return false;
            if (!other.enabled) return false;
            if (SingleAtOnce && m_CurrentSingleReaction != null) return false;
            if (other.SingleAtOnce && other.m_CurrentSingleReaction != null && other.m_CurrentSingleReaction != this) return false;
            if (other.ThisReactionName != OtherReactionName || other.OtherReactionName != ThisReactionName) return false;
            if (MustBeInAnimatorState.Animator != null)
                if (MustBeInAnimatorState.Animator.GetCurrentAnimatorStateInfo(0).shortNameHash != MustBeInAnimatorState.StateHash)
                    return false;
            if (SingleAtOnce) m_CurrentSingleReaction = other;
            return true;
        }


        private void Update()
        {
            // Update all reaction on delay.
            m_Reactions.RemoveAll(reaction =>
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


            // Update all reactions on cooldown
            m_CooldownWith.RemoveAll(reaction =>
            {
                if (reaction.Other == null)
                    return true;

                if (reaction.Tick())
                {
                    return true;
                }
                return false;
            });

            if (m_CurrentSingleReaction != null && m_Reactions.Count == 0 && m_CooldownWith.Count == 0)
                m_CurrentSingleReaction = null;
        }

        void LateUpdate()
        {
            // all ReactiveObjects in TouchingWith are still touching this frame
            foreach (var other in m_TouchingWith)
                Touching(other);
        }

        void OnDestroy()
        {
            foreach (var other in m_TouchingWith)
            {
                EndTouch(other);
                other.EndTouchIfTouching(this);
            }
        }

        #region Touching state
        void BeginTouch(ReactiveObject other, Vector3 position)
        {
            if (DebugLog)
                Debug.Log($"[{Time.frameCount}] ReactiveObject '{ThisReactionName}' begins touching '{other.ThisReactionName}'");
            m_TouchingWith.Add(other);

#if REACTIVEOBJECTEXT
            OnTouchBegin?.Invoke(this, other);
#endif
            // React if not currently on cooldown
            if (m_CooldownWith.All(x => x.Other != other))
            {
                var reaction = new Reaction(other, position, ReactionDelay);
                if (ReactionDelay == 0)
                    React(reaction);
                else
                    m_Reactions.Add(reaction);
            }
        }

        void Touching(ReactiveObject other)
        {

#if REACTIVEOBJECTEXT
            OnTouching?.Invoke(this, other);
#endif
        }

        void EndTouch(ReactiveObject other)
        {
            if (DebugLog)
                Debug.Log($"[{Time.frameCount}] ReactiveObject '{ThisReactionName}' stopped touching '{other.ThisReactionName}'");
#if REACTIVEOBJECTEXT
            OnTouchEnd?.Invoke(this, other);
#endif
        }

        bool EndTouchIfTouching(ReactiveObject other)
        {
            // if reactions require the objects to always touch during the delay, remove all current reaction with the other object.
            if (MustTouchDuringDelay)
                m_Reactions.RemoveAll(reaction => reaction.Other == other);

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
            if (DebugLog)
            {
                Debug.Log($"[{Time.frameCount}] ReactiveObject '{ThisReactionName}' OnCollisionEnter with GameObject: '{collision.gameObject.name}'");
            }
            //foreach (var other in collision.gameObject.GetComponentsInChildren<ReactiveObject>().Where(other => RequestReaction(other)))
            foreach (var other in collision.gameObject.GetComponents<ReactiveObject>().Where(other => RequestReaction(other)))
                BeginTouch(other, collision.GetContact(0).point);
        }

        public void OnCollisionExit(Collision collision)
        {
            if (!ReactToCollision) return;
            foreach (var other in collision.gameObject.GetComponentsInChildren<ReactiveObject>())
                EndTouchIfTouching(other);
        }

        private void OnTriggerEnter(Collider otherCollider)
        {
            if (!ReactToTrigger) return;
            foreach (var other in otherCollider.GetComponents<ReactiveObject>().Where(other => otherCollider.gameObject.transform.IsChildOf(other.transform) && RequestReaction(other)))
                BeginTouch(other, transform.position);
        }
        private void OnTriggerEnterExit(Collider otherCollider)
        {
            if (!ReactToTrigger) return;
            foreach (var other in otherCollider.GetComponents<ReactiveObject>().Where(other => otherCollider.gameObject.transform.IsChildOf(other.transform) && RequestReaction(other)))
                EndTouchIfTouching(other);

        }
        #endregion
    }

}