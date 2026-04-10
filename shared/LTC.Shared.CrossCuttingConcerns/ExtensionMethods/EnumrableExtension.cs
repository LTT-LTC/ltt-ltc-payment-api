using System;
using System.Collections.Generic;
using System.Text;

namespace LTC.Shared.CrossCuttingConcerns.ExtensionMethods
{
    public static class EnumrableExtension
    {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            return enumerable == null || !enumerable.Any();
        }
    }
}
