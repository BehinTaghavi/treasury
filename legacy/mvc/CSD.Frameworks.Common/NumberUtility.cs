using System;
using System.Collections.Generic;

namespace CSD.Common.Utility
{
    public class NumberUtility
    {
        public static List<int> CreateRandomListNumber(int max, int count)
        {
            count = max >= count ? count : max - 1;
            Random rnd = new Random();
            List<int> lst = new List<int>();
            for (int i = 0; i < count; )
            {
                int rndNo = rnd.Next(max);
                if (!lst.Contains(rndNo))
                {
                    lst.Add(rndNo);
                    i++;
                }
                //yield return rnd.Next(max);
            }
            return lst;
        }
    }
}
