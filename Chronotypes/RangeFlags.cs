using System;

namespace ChronoTypes
{
    [Flags]
    public enum RangeFlags
    {
        /// <summary>
        /// Indicates whether the first point of the range is closed;
        /// otherwise it is open.
        /// </summary>
        StartClosed = 1,

        /// <summary>
        /// Indicates whether the last point of the range is closed;
        /// otherwise it is open.
        /// </summary>
        EndClosed = 2,

        /// <summary>
        /// Indicates that the range is a normal range case;
        /// otherwise it is a degenerate range case.
        /// </summary>
        NormalRange = 4,
    }
}