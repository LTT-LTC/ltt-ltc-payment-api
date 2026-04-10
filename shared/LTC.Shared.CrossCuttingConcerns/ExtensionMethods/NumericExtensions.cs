using System;
using System.Collections.Generic;
using System.Text;

namespace LTC.Shared.CrossCuttingConcerns.ExtensionMethods
{
    public class NumericExtensions
    {
        public static long RandomNumber(long min = 100000000, long max = 999999999)
        {
            Random random = new Random();
            return random.NextInt64(min, max);
        }
    }
}
