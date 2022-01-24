/// <summary>
/// PID
/// http://www.codeproject.com/Articles/49548/Industrial-NET-PID-Controllers
/// </summary>

using System;
using UnityEngine;

namespace PID
{
    [Serializable]
    public class InitParams
    {
        public InitParams() { }

        public float pG = 5f;
        public float iG = 1f;
        public float dG = .1f;
        public float oMin = -200f;
        public float oMax = 200f;
    }

    public abstract class PIDBase<T> where T : IEquatable<T>
    {
        public delegate bool Getbool();

        #region Fields
        // Running Values
        protected T errSum;
        protected CircularBuffer<T> lastErrs = new CircularBuffer<T>(10);

        // Reading/Writing Values
        protected Func<T> readPV;   // Process variable
        protected Func<T> readSP;   // Set point
        protected Action<T> writeOV;    // Output variable
        public Getbool isDirty = null;

        protected abstract T AddT(params T[] vals);
        protected abstract T SubtractT(T lhs, T rhs);
        protected abstract T ClampT(T val, float min, float max);
        protected abstract T MultT(T lhs, float rhs);

        //Threading and Timing
        /*
            private float computeHz = 1.0f;
            private Thread runThread;
        */

        #endregion

        #region Properties
        public float PGain { get; private set; }
        public float IGain { get; private set; }
        public float DGain { get; private set; }
        public float OutMin { get; private set; }
        public float OutMax { get; private set; }
        #endregion

        #region Construction / Deconstruction
        public PIDBase(InitParams _params, Func<T> pvFunc, Func<T> spFunc, Action<T> outFunc)
        {
            Reassign(_params, pvFunc, spFunc, outFunc);
        }

        ~PIDBase()
        {
            //Disable();
            readPV = null;
            readSP = null;
            writeOV = null;
        }
        #endregion

        #region Public Methods
        public void Reassign(InitParams _params, Func<T> pvFunc = null, Func<T> spFunc = null, Action<T> outFunc = null)
        {
            PGain = _params.pG;
            IGain = _params.iG;
            DGain = _params.dG;
            OutMax = _params.oMax;
            OutMin = _params.oMin;
            if (pvFunc != null)
            {
                readPV = pvFunc;
            }
            if (spFunc != null)
            {
                readSP = spFunc;
            }
            if (outFunc != null)
            {
                writeOV = outFunc;
            }
            Reset();
        }

        public void Reset()
        {
            errSum = lastErrs.DefaultValue;
            lastErrs.Clear();
        }

        public void Compute(float delta)
        {
            if (readPV == null
                || readSP == null
                || writeOV == null)
            {
                return;
            }

            T sp = readSP();
            T pv = readPV();

            T err = SubtractT(sp, pv);
            errSum = AddT(errSum, MultT(err, delta));
            T errDiff = err;
            if (!AtRest())
            {
                T sum = lastErrs.DefaultValue;
                if (lastErrs.Count > 0)
                {
                    lastErrs.ForEach(val => sum = AddT(sum, val));
                    sum = MultT(sum, 1f / lastErrs.Count);
                }
                errDiff = SubtractT(err, sum);
            }
            if (isDirty != null)
            {
                if (isDirty.Invoke())
                {
                    lastErrs.Push(err);
                }
                else if (!lastErrs.IsEmpty())
                {
                    lastErrs.Pop();
                }
            }
            else
            {
                // no isDirty?  no problem, just keep cycling.
                lastErrs.Push(err);
            }

            pv = AddT(pv, MultT(err, PGain * delta), MultT(errSum, IGain), MultT(errDiff, DGain));
            pv = ClampT(pv, OutMin, OutMax);

            writeOV(pv);
        }

        public void Compute()
        {
            if (Mathf.Approximately(Time.timeScale, 0f)) return;
            Compute(Time.deltaTime);
        }

        public abstract bool AtRest();
        #endregion
    }

    public class PIDFloat : PIDBase<float>
    {
        public PIDFloat(InitParams _params, Func<float> pvFunc, Func<float> spFunc, Action<float> outFunc) : base(_params, pvFunc, spFunc, outFunc) { }

        protected override float AddT(params float[] vals)
        {
            float retval = 0;
            Array.ForEach(vals, val => retval += val);
            return retval;
        }
        protected override float SubtractT(float lhs, float rhs)
        {
            return lhs - rhs;
        }
        protected override float ClampT(float val, float min, float max)
        {
            return Mathf.Clamp(val, min, max);
        }
        protected override float MultT(float lhs, float rhs)
        {
            return lhs * rhs;
        }

        public override bool AtRest()
        {
            if (Mathf.Abs(readSP() - readPV()) > Mathf.Epsilon || errSum > Mathf.Epsilon)
            {
                // Early out
                return false;
            }

            bool activity = false;
            lastErrs.ForEach((v) =>
            {
                activity |= Mathf.Abs(v) > Mathf.Epsilon;
            });
            return !activity;
        }
    }

    public class PIDVector3 : PIDBase<Vector3>
    {
        public PIDVector3(InitParams _params, Func<Vector3> pvFunc, Func<Vector3> spFunc, Action<Vector3> outFunc) : base(_params, pvFunc, spFunc, outFunc) { }

        protected override Vector3 AddT(params Vector3[] vals)
        {
            Vector3 retval = new Vector3();
            Array.ForEach(vals, val => retval += val);
            return retval;
        }
        protected override Vector3 SubtractT(Vector3 lhs, Vector3 rhs)
        {
            return lhs - rhs;
        }
        protected override Vector3 ClampT(Vector3 val, float min, float max)
        {
            Vector3 retVal = new Vector3(Mathf.Clamp(val.x, min, max)
                , Mathf.Clamp(val.y, min, max)
                , Mathf.Clamp(val.z, min, max));

            return retVal;
        }
        protected override Vector3 MultT(Vector3 lhs, float rhs)
        {
            return lhs * rhs;
        }

        private readonly float SQUARE_MAG_EPSILON = 0.00000001f;
        private Vector3 _cached = new Vector3();
        public override bool AtRest()
        {
            _cached = readSP() - readPV();
            if (_cached.sqrMagnitude > SQUARE_MAG_EPSILON || errSum.sqrMagnitude > SQUARE_MAG_EPSILON)
            {
                // Early out
                return false;
            }

            bool activity = false;
            lastErrs.ForEach((v) => {
                activity |= v.sqrMagnitude > SQUARE_MAG_EPSILON;
            });
            return !activity;
        }
    }
}