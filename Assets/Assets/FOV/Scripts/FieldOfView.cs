using System;
using System.Collections.Generic;
using UnityEngine;

namespace FOV
{
    public class FieldOfView : MonoBehaviour
    {
        #region Data
        public float radius = 5f;
        [Range(0.1f, 360)] public float angle = 100f;
        public Vector3 offset = Vector3.zero;

        [Space]

        public LayerMask targetMask;
        public LayerMask obstructionMask;
        #endregion

        // The origin point used for all checks
        private Vector3 Origin => transform.position + offset;

        private void OnValidate()
        {
            if (radius <= 0.01f) radius = 0.01f;
        }

        /// <summary>
        /// Check if objects are in range.
        /// </summary>
        /// <typeparam name="T">Wanted type (must be a Component)</typeparam>
        /// <param name="tag">Optional condition tag</param>
        /// <returns>List of wanted type in range</returns>
        public List<T> Field<T>(string tag = null) where T : Component  // <-- constrain T
        {
            List<T> value = new List<T>();

            Vector3 origin = Origin;  // <-- use consistent origin

            Collider[] rangeChecks = Physics.OverlapSphere(origin, radius, targetMask);

            for (int i = 0; i < rangeChecks.Length; i++)
            {
                Transform target = rangeChecks[i].transform;
                Vector3 directionToTarget = (target.position - origin).normalized;  // <-- fixed

                if (tag != null && !target.CompareTag(tag)) continue;  // <-- CompareTag is better

                if (Vector3.Angle(transform.forward, directionToTarget) < angle / 2)
                {
                    float distanceToTarget = Vector3.Distance(origin, target.position);  // <-- fixed

                    if (!Physics.Raycast(origin, directionToTarget, distanceToTarget, obstructionMask))  // <-- fixed
                    {
                        if (target.TryGetComponent<T>(out T t))  // <-- proper usage
                        {
                            value.Add(t);
                            Debug.DrawLine(origin, target.position, Color.green);
                            Debug.Log("Seeing: " + target.name);
                        }
                    }
                    else
                    {
                        Debug.DrawLine(origin, target.position, Color.red);
                    }
                }
            }

            return value;
        }
    }
}