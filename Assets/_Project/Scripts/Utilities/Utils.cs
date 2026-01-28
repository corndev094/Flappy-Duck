using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utilities
{
    public class Utils : MonoBehaviour
    {
        /// <summary>
        /// Calculate the direction vector from origin to target
        /// </summary>
        /// <returns>A normalized direction vector 2D</returns>
        public static Vector2 GetDirectionVector2(Vector2 origin, Vector2 target)
        {
            return (target - origin).normalized;
        }
        
        /// <summary>
        /// Calculate the direction vector from origin to target
        /// </summary>
        /// <returns>A normalized direction vector 3D</returns>
        public static Vector3 GetDirectionVector3(Vector3 origin, Vector3 target)
        {
            return (target - origin).normalized;
        }

        public static Vector2 GetRandomPointInCircle(Vector2 origin, float radius)
        {
            float angle = Random.Range(0, 2 * Mathf.PI) * Mathf.Rad2Deg;
            float distanceX = Random.Range(0, radius);
            float distanceY = Random.Range(0, radius);
            float x = Mathf.Sin(angle) * distanceX;
            float y = Mathf.Cos(angle) * distanceY;
            return new Vector2(origin.x + x, origin.y + y);
        }
        
        public static Vector2 GetRandomPointInCircleReverseX(Vector2 origin, float radius, float signOfX)
        {
            signOfX = Mathf.Sign(signOfX);
            float angle = Random.Range(0, 2 * Mathf.PI) * Mathf.Rad2Deg;
            float distanceX = Random.Range(0, radius);
            float distanceY = Random.Range(0, radius);
            float x = Mathf.Sin(angle) * distanceX * (signOfX == 1 ? -1 : 1);
            float y = Mathf.Cos(angle) * distanceY * (signOfX == 1 ? -1 : 1);
            return new Vector2(origin.x + x, origin.y + y);
        }

        public static Vector2 CeilToInt(Vector2 vector)
        {
            return new Vector2(Mathf.CeilToInt(vector.x), Mathf.CeilToInt(vector.y));
        }
    }
}