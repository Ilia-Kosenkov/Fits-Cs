using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;

namespace FitsCs
{
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
    internal static class EqualityExtensions
    {
        private static readonly double EpsilonD = Math.Pow(2, -53);
        private static readonly float EpsilonF = (float)Math.Pow(2, -24);

        [Pure]
        public static bool CorrectEquals(this double @this, double that, double eps = 1.0)
        {
            if (double.IsNaN(@this) || double.IsNaN(that))
                return false;

            if (double.IsInfinity(@this) || double.IsInfinity(that))
                return @this == that;

            if (@this == that)
                return true;

            var thisAbs = Math.Abs(@this);
            var thatAbs = Math.Abs(that);

            var delta = eps * EpsilonD;

            Debug.Assert(thisAbs != 0 && thatAbs != 0);

            var fact = 1.0;
            if (thisAbs == 0)
                fact = thatAbs;
            else if (thatAbs == 0)
                fact = thisAbs;
            else if (thisAbs != 0)
                fact = Math.Min(thisAbs, thatAbs);

            return (Math.Abs(@this - that) < fact * delta);
        }

        [Pure]
        public static bool CorrectEquals(this float @this, float that, float eps = 1.0f)
        {
            if (float.IsNaN(@this) || float.IsNaN(that))
                return false;

            if (float.IsInfinity(@this) || float.IsInfinity(that))
                return @this == that;

            if (@this == that)
                return true;

            var thisAbs = Math.Abs(@this);
            var thatAbs = Math.Abs(that);

            var delta = eps * EpsilonF;

            Debug.Assert(thisAbs != 0 && thatAbs != 0);

            var fact = 1.0;
            if (thisAbs == 0)
                fact = thatAbs;
            else if (thatAbs == 0)
                fact = thisAbs;
            else if (thisAbs != 0)
                fact = Math.Min(thisAbs, thatAbs);

            return (Math.Abs(@this - that) < fact * delta);
        }

        [Pure]
        public static bool CorrectEquals(this double @this, float that, float eps = 1.0f)
        {
            if (double.IsNaN(@this) || float.IsNaN(that))
                return false;

            if (double.IsInfinity(@this) || float.IsInfinity(that))
                return @this == that;

            if (@this == that)
                return true;

            var thisAbs = Math.Abs(@this);
            var thatAbs = Math.Abs(that);

            var delta = eps * EpsilonF;

            Debug.Assert(thisAbs != 0 && thatAbs != 0);

            var fact = 1.0;
            if (thisAbs == 0)
                fact = thatAbs;
            else if (thatAbs == 0)
                fact = thisAbs;
            else if (thisAbs != 0)
                fact = Math.Min(thisAbs, thatAbs);

            return (Math.Abs(@this - that) < fact * delta);
        }

        [Pure]
        public static bool CorrectEquals(this float @this, double that, float eps = 1.0f)
            => CorrectEquals(that, @this, eps);

        [Pure]
        public static bool CorrectEquals(this Complex @this, Complex that, double eps = 1.0)
            => CorrectEquals(@this.Real, that.Real, eps)
               && CorrectEquals(@this.Imaginary, that.Imaginary, eps);
    }
}