using System.Collections.Generic;
using UnityEngine;

namespace Awaken.TG.Main.Fights {
    /// <summary>
    /// Allows for comparison between RaycastHit objects.
    /// </summary>
    public class RaycastHitComparer : IComparer<RaycastHit> {
        /// <summary>
        /// Compare RaycastHit x to RaycastHit y. If x has a smaller distance value compared to y then a negative value will be returned.
        /// If the distance values are equal then 0 will be returned, and if y has a smaller distance value compared to x then a positive value will be returned.
        /// </summary>
        /// <param name="x">The first RaycastHit to compare.</param>
        /// <param name="y">The second RaycastHit to compare.</param>
        /// <returns>The resulting difference between RaycastHit x and y.</returns>
        public int Compare(RaycastHit x, RaycastHit y) {
            if (x.transform == null) {
                return int.MaxValue;
            }

            if (y.transform == null) {
                return int.MinValue;
            }

            return x.distance.CompareTo(y.distance);
        }
    }
}