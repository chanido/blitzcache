
using System;

namespace BlitzCacheCore.LockDictionaries
{
    /// <summary>
    /// Interface for entries that can be cleaned up based on last access time.
    /// </summary>
    public interface ICleanupEntry
    {
        /// <summary>
        /// Attempts to dispose the entry and return true if successful.
        /// </summary>
        /// <returns>True if the entry was successfully disposed, false otherwise.</returns>
        bool AttemptDispose();
    }
}