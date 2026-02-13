using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Bezier
{
    [ExecuteInEditMode]
    public class BezierCurve : MonoBehaviour
    {
        [System.Serializable]
        public class ControlPoint
        {
            public Vector3      m_vPosition;
            public Vector3      m_vTangent;
            public float        m_fDistance;
        }

        [SerializeField]
        public List<ControlPoint> m_points = new List<ControlPoint>();

        #region Properties

        public bool IsEmpty => m_points.Count == 0;

        public ControlPoint FirstPoint => !IsEmpty ? m_points[0] : null;

        public ControlPoint LastPoint => !IsEmpty ? m_points[m_points.Count - 1] : null;

        public float TotalDistance => IsEmpty ? 0.0f : LastPoint.m_fDistance;

        public IEnumerable<ControlPoint> Points
        {
            get
            {
                foreach (ControlPoint cp in m_points)
                {
                    yield return cp;
                }
            }
        }

        #endregion

        private void OnEnable()
        {
            UpdateDistances();
        }

        public Pose GetPose(float fDistanceAlongCurve)
        {
            if (IsEmpty)
            {
                throw new System.Exception("Empty BezierCurve");
            }

            // smaller than first point?
            if (fDistanceAlongCurve <= FirstPoint.m_fDistance)
            {
                return new Pose
                {
                    position = FirstPoint.m_vPosition,
                    rotation = Quaternion.LookRotation(FirstPoint.m_vTangent)
                };
            }

            // larger than last point?
            if (fDistanceAlongCurve >= LastPoint.m_fDistance)
            {
                return new Pose
                {
                    position = LastPoint.m_vPosition,
                    rotation = Quaternion.LookRotation(LastPoint.m_vTangent)
                };
            }

            // find segment
            for (int i = 1; i < m_points.Count; i++)
            {
                ControlPoint A = m_points[i - 1];
                ControlPoint B = m_points[i];

                if (fDistanceAlongCurve <= B.m_fDistance)
                {
                    // blend between A & B
                    float fBlend = Mathf.InverseLerp(A.m_fDistance, B.m_fDistance, fDistanceAlongCurve);
                    return new Pose
                    {
                        position = GetPosition(A, B, fBlend),
                        rotation = Quaternion.LookRotation(GetForward(A, B, fBlend))
                    };
                }
            }

            // should never happen :(
            throw new System.Exception("Should never happen");
        }

        public static Vector3 GetPosition(ControlPoint A, ControlPoint B, float f)
        {
            Vector3 p0 = A.m_vPosition;
            Vector3 p1 = A.m_vPosition + A.m_vTangent;
            Vector3 p2 = B.m_vPosition - B.m_vTangent;
            Vector3 p3 = B.m_vPosition;

            float fOneMinusT = 1.0f - f;
            return p0 * fOneMinusT * fOneMinusT * fOneMinusT +
                   p1 * 3 * fOneMinusT * fOneMinusT * f +
                   p2 * 3 * fOneMinusT * f * f +
                   p3 * f * f * f;
        }

        public static Vector3 GetForward(ControlPoint A, ControlPoint B, float f)
        {
            Vector3 p0 = A.m_vPosition;
            Vector3 p1 = A.m_vPosition + A.m_vTangent;
            Vector3 p2 = B.m_vPosition - B.m_vTangent;
            Vector3 p3 = B.m_vPosition;

            f = Mathf.Clamp01(f);
            float fOneMinusT = 1f - f;
            return 3f * fOneMinusT * fOneMinusT * (p1 - p0) +
                   6f * fOneMinusT * f * (p2 - p1) +
                   3f * f * f * (p3 - p2);
        }


        public void UpdateDistances()
        {
            if (IsEmpty)
            {
                return;
            }

            // start at distance zero!
            m_points[0].m_fDistance = 0.0f;

            // add up bezier curve segment distances
            for (int i = 1; i < m_points.Count; ++i)
            {
                ControlPoint A = m_points[i - 1];
                ControlPoint B = m_points[i];
                B.m_fDistance = A.m_fDistance + CalculateDistance(A, B);
            }
        }

        protected static float CalculateDistance(ControlPoint A, ControlPoint B, int iNumSegments = 20)
        {
            float fDistance = 0.0f;
            Vector3 vLast = A.m_vPosition;
            for(int i=1; i<=iNumSegments; i++) 
            {
                float f = i / (float)iNumSegments;
                Vector3 vCurr = GetPosition(A, B, f);
                fDistance += Vector3.Distance(vLast, vCurr);
                vLast = vCurr;
            }

            return fDistance;
        }
    }
}