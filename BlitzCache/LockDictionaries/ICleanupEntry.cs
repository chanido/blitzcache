
using System;

namespace BlitzCacheCore.LockDictionaries
{
    /// <summary>
    /// Interface for entries that can be cleaned up based on last access time.
    /// </summary>
    public interface ICleanupEntry
    {
        /// <summary>
        /// When this entry was last accessed.
        /// </summary>
        DateTime LastAccessed { get; set; }

        /// <summary>
        /// Updates the last accessed time to now.
        /// </summary>
        void UpdateAccess();
    }
}