
using System;

namespace BlitzCacheCore
{
    public class Nuances
    {
        /// <summary>
        /// Retention for this key in milliseconds
        /// </summary>
        public long? CacheRetention { get; set; }

        public static implicit operator Func<object, object>(Nuances v)
        {
            throw new NotImplementedException();
        }
    }
}
