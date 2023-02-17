using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nie
{
    /// <summary>
    /// Move this object to a distance from it's parent's Z axis.
    /// The distance change with the mouse wheel
    /// </summary>
    [AddComponentMenu("Nie/Object/Constraints/ForwardPosition")]
    public class ForwardPosition : MonoBehaviour
    {

        [Tooltip("Distance to move to from the parent's transform position on the forward axis")]
        public float Distance;

        [Tooltip("The speed coefficient applied to the scrolling input to move on the parent's transform forward axis")]
        public float ScrollSpeed = 0.3f;

        /// <summary>
        /// Offset from the parent's transform position to start with.
        /// </summary>
        Vector3 Offset;

        void Start()
        {
            Offset = transform.localPosition;
            Move();
        }

        void Update()
        {
            // TODO, tie this to the input system
            if (Input.mouseScrollDelta.y != 0)
            {
                Distance += Input.mouseScrollDelta.y * ScrollSpeed;
                Move();
            }
        }

        void Move()
        {
            transform.localPosition = new Vector3(0, 0, Distance) + Offset;
        }
    }
}