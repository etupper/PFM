namespace System.Collections.Generic
{
    using System;

    public static class Enumerate
    {
        public static T NthElement<T>(int n, IEnumerable<T> enumerable)
        {
            IEnumerator<T> enumerator = enumerable.GetEnumerator();
            int num = 0;
            while (enumerator.MoveNext())
            {
                if (num++ == n)
                {
                    return enumerator.Current;
                }
            }
            throw new ArgumentOutOfRangeException("n");
        }
    }
}

