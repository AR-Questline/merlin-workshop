using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding {
	/// <summary>
	/// Smoothing by dividing path into turns and straight segments.
	///
	/// Deprecated: This modifier is deprecated
	/// </summary>
	[System.Serializable]
	[System.Obsolete("This modifier is deprecated")]
	[HelpURL("https://arongranberg.com/astar/documentation/stable/advancedsmooth.html")]
	public class AdvancedSmooth : MonoModifier {
		public override int Order { get { return 40; } }

		public float turningRadius = 1.0F;

		public MaxTurn turnConstruct1 = new MaxTurn();
		public ConstantTurn turnConstruct2 = new ConstantTurn();

		public override void Apply (Path p)
        {
        }

        void EvaluatePaths(List<Turn> turnList, List<Vector3> output)
        {
        }

        [System.Serializable]
        /// <summary>Type of turn.</summary>
        public class MaxTurn : TurnConstructor
        {
			Vector3 preRightCircleCenter = Vector3.zero;
			Vector3 preLeftCircleCenter = Vector3.zero;

			Vector3 rightCircleCenter;
			Vector3 leftCircleCenter;

			double vaRight, vaLeft, preVaLeft, preVaRight;

			double gammaLeft, gammaRight;

			double betaRightRight, betaRightLeft, betaLeftRight, betaLeftLeft;

			double deltaRightLeft, deltaLeftRight;

			double alfaRightRight, alfaLeftLeft, alfaRightLeft, alfaLeftRight;

			public override void OnTangentUpdate () {
            }

            public override void Prepare (int i, Vector3[] vectorPath)
            {
            }

            public override void TangentToTangent(List<Turn> turnList)
            {
            }

            public override void PointToTangent(List<Turn> turnList)
            {
            }

            public override void TangentToPoint(List<Turn> turnList)
            {
            }

            public override void GetPath(Turn turn, List<Vector3> output)
            {
            }
        }

        [System.Serializable]
        /// <summary>Constant turning speed.</summary>
        public class ConstantTurn : TurnConstructor
        {
            Vector3 circleCenter;
            double gamma1;
            double gamma2;

            bool clockwise;

            public override void Prepare(int i, Vector3[] vectorPath)
            {
            }

            public override void TangentToTangent (List<Turn> turnList)
            {
            }

            public override void GetPath(Turn turn, List<Vector3> output)
            {
            }
        }

        /// <summary>Abstract turn constructor.</summary>
        public abstract class TurnConstructor
        {
            /// <summary>
            /// Constant bias to add to the path lengths.
            /// This can be used to favor certain turn types before others.
            /// By for example setting this to -5, paths from this path constructor will be chosen
            /// if there are no other paths more than 5 world units shorter than this one (as opposed to just any shorter path)
            /// </summary>
            public float constantBias = 0;

            /// <summary>
            /// Bias to multiply the path lengths with. This can be used to favor certain turn types before others.
            /// See: <see cref="constantBias"/>
            /// </summary>
            public float factorBias = 1;

            public static float turningRadius = 1.0F;

            public const double ThreeSixtyRadians = Math.PI * 2;

            public static Vector3 prev, current, next; //The current points
            public static Vector3 t1, t2; //The current tangents - t2 is at 'current', t1 is at 'prev'
            public static Vector3 normal, prevNormal; //Normal at 'current'

            public static bool changedPreviousTangent = false;

            public abstract void Prepare(int i, Vector3[] vectorPath);
            public virtual void OnTangentUpdate()
            {
            }

            public virtual void PointToTangent(List<Turn> turnList)
            {
            }

            public virtual void TangentToPoint(List<Turn> turnList)
            {
            }

            public virtual void TangentToTangent(List<Turn> turnList)
            {
            }

            public abstract void GetPath(Turn turn, List<Vector3> output);
            //abstract void Evaluate (Turn turn);

            public static void Setup(int i, Vector3[] vectorPath)
            {
            }

            public static void PostPrepare()
            {
            }

            //Utilities

            public void AddCircleSegment(double startAngle, double endAngle, bool clockwise, Vector3 center, List<Vector3> output, float radius)
            {
            }

            public void DebugCircleSegment(Vector3 center, double startAngle, double endAngle, double radius, Color color)
            {
            }

            public void DebugCircle(Vector3 center, double radius, Color color)
            {
            }

            /// <summary>Returns the length of an circular arc with a radius and angle. Angle is specified in radians</summary>
            public double GetLengthFromAngle(double angle, double radius)
            {
                return default;
            }

            /// <summary>Returns the angle between from and to in a clockwise direction</summary>
            public double ClockwiseAngle(double from, double to)
            {
                return default;
            }

            /// <summary>Returns the angle between from and to in a counter-clockwise direction</summary>
            public double CounterClockwiseAngle(double from, double to)
            {
                return default;
            }

            public Vector3 AngleToVector(double a)
            {
                return default;
            }

            public double ToDegrees(double rad)
            {
                return default;
            }

            public double ClampAngle(double a)
            {
                return default;
            }

            public double Atan2(Vector3 v)
            {
                return default;
            }
        }

        //Turn class
        /// <summary>Represents a turn in a path.</summary>
        public struct Turn : IComparable<Turn>
        {
            public float length;
            public int id;

            public TurnConstructor constructor;

            public float score
            {
                get
                {
                    return length * constructor.factorBias + constructor.constantBias;
                }
            }

            public Turn(float length, TurnConstructor constructor, int id = 0) : this()
            {
            }

            public void GetPath(List<Vector3> output)
            {
            }

            public int CompareTo(Turn t)
            {
                return default;
            }

            public static bool operator <(Turn lhs, Turn rhs)
            {
                return default;
            }

            public static bool operator >(Turn lhs, Turn rhs)
            {
                return default;
            }
        }
	}
}
