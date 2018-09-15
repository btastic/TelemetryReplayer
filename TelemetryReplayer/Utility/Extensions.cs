using System;

namespace TelemetryReplayer.Utility
{
    public static class Extensions
    {
        public static T Next<T>(this T src) where T : struct
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException(string.Format("Argumnent {0} is not an Enum", typeof(T).FullName));
            }

            var Arr = (T[])Enum.GetValues(src.GetType());
            var j = Array.IndexOf<T>(Arr, src) + 1;
            return (Arr.Length == j) ? Arr[0] : Arr[j];
        }
    }
}
