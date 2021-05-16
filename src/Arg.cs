namespace PinkTrombone {
    using System;
    using System.Runtime.CompilerServices;

    static class Arg {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        public static float Check01(float value, [CallerMemberName] string property = null) {
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            if (value < 0 || value > 1)
                throw new ArgumentOutOfRangeException(paramName: property);
            return value;
        }
    }
}
