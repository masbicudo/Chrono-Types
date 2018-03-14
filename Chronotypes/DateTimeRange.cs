using System;

namespace ChronoTypes
{
    /// <summary>
    /// Represents a range of the DateTime type,
    /// starting on a specific DateTime and ending on another DateTime.
    /// Both ends can be configured to be inclusive or exclusive,
    /// and the range can be a negative range (i.e. a range that is excluded).
    /// Also, degenerate cases are supported: zero point ranges,
    /// single point ranges and pair of points ranges.
    /// </summary>
    public struct DateTimeRange :
        IEquatable<DateTimeRange>
    {
        // Table of possible states for this type
        // ======================================
        //
        // Segments are the parts of the range that are not points.
        // Segments can be either part of the range or not:
        //  true: the segment is included in the current range
        //  false: the segment is not included in the current range
        // Same with points: can be part of the range or not.
        //
        // The columns marked with S are segments,
        // the others marked with p are points.
        // Note that each table can have a single point,
        // or a pair of points.
        //
        // The header of tables with a single point is:
        //      "S p S" - two segment columns and one point column
        // The header of tables with a a pair of point is:
        //      "SpSpS" - 3 segments, 2 points
        //
        // A segment column may be marked with:
        //  - (dash) => not part of the range
        //  x        => part of the range
        // A point column may be marked with:
        //  o => open point = not part of the range
        //  x => closed point = part of the range
        //
        // The master columns are:
        //  Range - divided in subcolumns for each segment or point
        //  I - interval positive (+), negative (-) or zero (0)
        //  S - start open (O) or closed (C)
        //  E - end open (O) or closed (C)
        //  D - degenarate range case (D) or normal range case (N)
        //
        //  Range I S E D
        //
        //  S p S           (single point range)
        //  --o-- 0 O O D
        //  xxoxx 0 O C D
        //  --x-- 0 C O D
        //  xxxxx 0 C C D
        //
        //  SpSpS           (double point range)
        //  -o-o- + O O D
        //  -o-x- + O C D
        //  -x-o- + C O D
        //  -x-x- + C C D
        //
        //  SpSpS           (double point range)
        //  xoxox - O O D
        //  xxxox - O C D
        //  xoxxx - C O D
        //  xxxxx - C C D
        //
        //  S p S           (single point range)
        //  xxo-- 0 O O N
        //  --oxx 0 O C N
        //  xxx-- 0 C O N
        //  --xxx 0 C C N
        //
        //  SpSpS           (double point range)
        //  -oxo- + O O N
        //  -oxx- + O C N
        //  -xxo- + C O N
        //  -xxx- + C C N
        //
        //  SpSpS           (double point range)
        //  xo-ox - O O N
        //  xx-ox - O C N
        //  xo-xx - C O N
        //  xx-xx - C C N

        public DateTimeRange(DateTime start, TimeSpan interval)
        {
            this.Flags = RangeFlags.StartClosed | RangeFlags.NormalRange;
            this.Start = start;
            this.Interval = interval;
        }

        public DateTimeRange(DateTime start, DateTime end)
        {
            this.Flags = RangeFlags.StartClosed | RangeFlags.EndClosed;
            this.Start = start;
            this.Interval = end - start;
        }

        public DateTimeRange(DateTime start, DateTime end, RangeFlags flags)
        {
            this.Flags = flags;
            this.Start = start;
            this.Interval = end - start;
        }

        public DateTimeRange(DateTime start, TimeSpan interval, RangeFlags flags)
        {
            this.Flags = flags;
            this.Start = start;
            this.Interval = interval;
        }

        /// <summary>
        /// Determines whether the given date time is inside the range.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public bool Contains(DateTime dateTime)
        {
            if (this.Interval.Ticks > 0)
            {
                if (dateTime < this.Start)
                    return false;

                if (dateTime == this.Start)
                    return (this.Flags & RangeFlags.StartClosed) != 0;

                var end = this.End;
                if (dateTime > end)
                    return false;

                if (dateTime == end)
                    return (this.Flags & RangeFlags.EndClosed) != 0;

                return (this.Flags & RangeFlags.NormalRange) != 0;
            }

            if (this.Interval.Ticks == 0)
            {
                if (dateTime == this.Start)
                    return (this.Flags & RangeFlags.StartClosed) != 0;

                if ((this.Flags & RangeFlags.NormalRange) == 0)
                    return (this.Flags & RangeFlags.EndClosed) != 0;

                return (dateTime > this.Start) == ((this.Flags & RangeFlags.EndClosed) != 0);
            }

            {
                if (dateTime > this.Start)
                    return true;

                if (dateTime == this.Start)
                    return (this.Flags & RangeFlags.StartClosed) != 0;

                var end = this.End;
                if (dateTime < end)
                    return true;

                if (dateTime == end)
                    return (this.Flags & RangeFlags.EndClosed) != 0;

                return (this.Flags & RangeFlags.NormalRange) == 0;
            }
        }

        public DateTimeRange Intersect(DateTimeRange other)
        {
            DateTime d1, d2, d3, d4;
            bool seg0, seg1, seg2, seg3, seg4;
            bool c1, c2, c3, c4;

            {
                this.GetSegments(out bool tSeg0, out bool tSeg1, out bool tSeg2);
                other.GetSegments(out bool oSeg0, out bool oSeg1, out bool oSeg2);
                seg0 = tSeg0 && oSeg0;
                seg4 = tSeg2 && oSeg2;
                this.SortStartEnd(out d1, out d2, out c1, out c2);
                other.SortStartEnd(out d3, out d4, out c3, out c4);

                // A    12--    12--    13--    13--    14--    14--
                // B    --34    --43    --24    --42    --23    --32
                // Sort O       X       O       X       O       X

                // A    21--    21--    23--    23--    24--    24--
                // B    --34    --43    --14    --41    --13    --31
                // Sort X       X       O       X       O       X

                // A    31--    31--    32--    32--    34--    34--
                // B    --24    --42    --14    --41    --12    --21
                // Sort X       X       X       X       O       X

                // A    41--    41--    42--    42--    43--    43--
                // B    --23    --32    --13    --31    --12    --21
                // Sort X       X       X       X       X       X

                // The ones that have X are eliminated from the next steps.
                // They are not possible.

                var sort0 = SortDateTimePoints(ref d1, ref d3, ref c1, ref c3);

                // A    12--            13--            14--
                // B    --34            --24            --23
                // Sort -               -               -

                // A                    -32-            -42-
                // B                    1--4            1--3
                // Sort                 +               +

                // A                                    -43-
                // B                                    1--2
                // Sort                                 +

                seg1 = sort0 < 0 ? tSeg1 & oSeg0
                    : tSeg0 & oSeg1;

                var sort1 = SortDateTimePoints(ref d2, ref d4, ref c2, ref c4);

                // A    12--            13--            1--4
                // B    --34            --24            -32-
                // Sort --              --              -+

                // A                    -32-            --24
                // B                    1--4            13--
                // Sort                 +-              ++

                // A                                    --34
                // B                                    12--
                // Sort                                 ++

                seg3 = sort1 < 0 ? tSeg2 & oSeg1
                    : tSeg1 & oSeg2;

                var sort2 = SortDateTimePoints(ref d2, ref d3, ref c2, ref c3);

                // A    12--            1-3-            1--4
                // B    --34            -2-4            -23-
                // Sort ---             --+             -++

                // A                    -23-            -2-4
                // B                    1--4            1-3-
                // Sort                 +-+             +++

                // A                                    --34
                // B                                    12--
                // Sort                                 ++-

                seg2 = sort0 < 0 && sort1 < 0 && sort2 < 0 ? tSeg2 & oSeg0
                    : sort0 > 0 && sort1 > 0 && sort2 < 0 ? tSeg0 & oSeg2
                        : tSeg1 & oSeg1;

                var eqs = (sort0 == 0 ? 1 : 0) + (sort2 == 0 ? 2 : 0) + (sort1 == 0 ? 4 : 0);
                switch (eqs)
                {
                    case 0b000:
                        // All different
                        return DateTimeRange.CreateSegments(d1, d2, d3, d4, seg0, c1, seg1, c2, seg2, c3, seg3, c4, seg4);

                    case 0b001:
                        // Only 3rd and 4th are equal
                        return DateTimeRange.CreateSegments(d1, d2, d3, seg0, c1, seg1, c2, seg2, c3 & c4, seg4);

                    case 0b010:
                        // Only 2nd and 3rd are equal
                        return DateTimeRange.CreateSegments(d1, d2, d4, seg0, c1, seg1, c2 & c3, seg3, c4, seg4);

                    case 0b011:
                        // only first is different
                        return DateTimeRange.CreateSegments(d1, d2, seg0, c1, seg1, c2 & c3 & c4, seg4);

                    case 0b100:
                        // Only 1st and 2nd are equal
                        return DateTimeRange.CreateSegments(d1, d3, d4, seg0, c1 & c2, seg2, c3, seg3, c4, seg4);

                    case 0b101:
                        // 1st == 2nd && 3rd == 4th
                        return DateTimeRange.CreateSegments(d1, d3, seg0, c1 & c2, seg2, c3 & c4, seg4);

                    case 0x110:
                        // only last is different
                        return DateTimeRange.CreateSegments(d1, d4, seg0, c1 & c2 & c3, seg3, c4, seg4);

                    case 0b111:
                        // all equal
                        return DateTimeRange.CreateSegments(d1, seg0, c1 & c2 & c3 & c4, seg4);
                }

                throw new InvalidOperationException("Should be impossible exception");
            }
        }

        private void GetSegments(out bool seg0, out bool seg1, out bool seg2)
        {
            // Segments are the parts of the range that are not points.
            // Segments can be either true or false:
            //  true: the segment is included in the current range
            //  false: the segment is not included in the current range

            // The columns marked with S are segments,
            // the others marked with p are points.
            // Note that each table can have a single point,
            // or a pair of points.

            // The header of tables with a single point is:
            //      "S p S" - two segment columns and one point column
            // The header of tables with a a pair of point is:
            //      "SpSpS" - 3 segments, 2 points

            // A segment column may be marked with:
            //  - (dash) => not part of the range
            //  x        => part of the range
            // A point column may be marked with:
            //  o => open point = not part of the range
            //  x => closed point = part of the range

            //  S p S (single point range)
            //  --o-- 0 O O D
            //  xxoxx 0 O C D
            //  --x-- 0 C O D
            //  xxxxx 0 C C D
            //
            //  SpSpS (double point range)
            //  -o-o- + O O D
            //  -o-x- + O C D
            //  -x-o- + C O D
            //  -x-x- + C C D
            //
            //  SpSpS (double point range)
            //  xoxox - O O D
            //  xxxox - O C D
            //  xoxxx - C O D
            //  xxxxx - C C D
            //
            //  S p S (single point range)
            //  xxo-- 0 O O N
            //  --oxx 0 O C N
            //  xxx-- 0 C O N
            //  --xxx 0 C C N
            //
            //  SpSpS (double point range)
            //  -oxo- + O O N
            //  -oxx- + O C N
            //  -xxo- + C O N
            //  -xxx- + C C N
            //
            //  SpSpS (double point range)
            //  xo-ox - O O N
            //  xx-ox - O C N
            //  xo-xx - C O N
            //  xx-xx - C C N

            if ((this.Flags & RangeFlags.NormalRange) == 0)
            {
                seg0 = seg1 = seg2 = this.Interval.Ticks == 0
                    ? (this.Flags & RangeFlags.EndClosed) != 0
                    : this.Interval.Ticks < 0;
                return;
            }

            if (this.Interval.Ticks == 0)
            {
                seg2 = (this.Flags & RangeFlags.EndClosed) != 0;
                seg0 = !seg2;
                seg1 = false;
                return;
            }

            seg1 = this.Interval.Ticks > 0;
            seg0 = seg2 = !seg1;
        }

        public static DateTimeRange CreateSegments(DateTime d1, DateTime d2, DateTime d3, DateTime d4, bool seg0, bool c1, bool seg1, bool c2, bool seg2, bool c3, bool seg3, bool c4, bool seg4)
        {
            if (!TryCreateSegments(d1, d2, d3, d4, seg0, c1, seg1, c2, seg2, c3, seg3, c4, seg4, out DateTimeRange result))
                throw new InvalidOperationException("Can only create alternating segments, or all equal segments, with up to 3 segments.");
            return result;
        }

        public static bool TryCreateSegments(DateTime d1, DateTime d2, DateTime d3, DateTime d4, bool seg0, bool c1, bool seg1, bool c2, bool seg2, bool c3, bool seg3, bool c4, bool seg4, out DateTimeRange result)
        {
            if (d1 >= d2)
                throw new InvalidOperationException("d1 must be lesser than d2.");
            if (d2 >= d3)
                throw new InvalidOperationException("d2 must be lesser than d3.");
            if (d3 >= d4)
                throw new InvalidOperationException("d3 must be lesser than d4.");
            return TryCreateSegments_NoChecks(d1, d2, d3, d4, seg0, c1, seg1, c2, seg2, c3, seg3, c4, seg4, out result);
        }

        private static bool TryCreateSegments_NoChecks(DateTime d1, DateTime d2, DateTime d3, DateTime d4, bool seg0, bool c1, bool seg1, bool c2, bool seg2, bool c3, bool seg3, bool c4, bool seg4, out DateTimeRange result)
        {
            // Must reduce to a case with only three dates
            // by collapsing date/time continuities.

            if (seg3 == c4 && c4 == seg4)
                return TryCreateSegments_NoChecks(d1, d2, d3, seg0, c1, seg1, c2, seg2, c3, seg3, out result);

            if (seg2 == c3 && c3 == seg3)
                return TryCreateSegments_NoChecks(d1, d2, d4, seg0, c1, seg1, c2, seg2, c4, seg4, out result);

            if (seg1 == c2 && c2 == seg2)
                return TryCreateSegments_NoChecks(d1, d3, d4, seg0, c1, seg1, c3, seg3, c4, seg4, out result);

            if (seg0 == c1 && c1 == seg1)
                return TryCreateSegments_NoChecks(d2, d3, d4, seg1, c2, seg2, c3, seg3, c4, seg4, out result);

            result = default(DateTimeRange);
            return false;
        }

        public static DateTimeRange CreateSegments(DateTime d1, DateTime d2, DateTime d3, bool seg0, bool c1, bool seg1, bool c2, bool seg2, bool c3, bool seg3)
        {
            if (!TryCreateSegments(d1, d2, d3, seg0, c1, seg1, c2, seg2, c3, seg3, out DateTimeRange result))
                throw new InvalidOperationException("Can only create alternating segments, or all equal segments, with up to 3 segments.");
            return result;
        }

        public static bool TryCreateSegments(DateTime d1, DateTime d2, DateTime d3, bool seg0, bool c1, bool seg1, bool c2, bool seg2, bool c3, bool seg3, out DateTimeRange result)
        {
            if (d1 >= d2)
                throw new InvalidOperationException("d1 must be lesser than d2.");
            if (d2 >= d3)
                throw new InvalidOperationException("d2 must be lesser than d3.");
            return TryCreateSegments_NoChecks(d1, d2, d3, seg0, c1, seg1, c2, seg2, c3, seg3, out result);
        }

        private static bool TryCreateSegments_NoChecks(DateTime d1, DateTime d2, DateTime d3, bool seg0, bool c1, bool seg1, bool c2, bool seg2, bool c3, bool seg3, out DateTimeRange result)
        {
            // Must reduce to a case with only two dates
            // by collapsing date/time continuities.

            if (seg2 == c3 && c3 == seg3)
                return TryCreateSegments_NoChecks(d1, d2, seg0, c1, seg1, c2, seg2, out result);

            if (seg1 == c2 && c2 == seg2)
                return TryCreateSegments_NoChecks(d1, d3, seg0, c1, seg1, c3, seg3, out result);

            if (seg0 == c1 && c1 == seg1)
                return TryCreateSegments_NoChecks(d2, d3, seg1, c2, seg2, c3, seg3, out result);

            result = default(DateTimeRange);
            return false;
        }

        public static DateTimeRange CreateSegments(DateTime d1, bool seg0, bool c1, bool seg1)
        {
            // Table copied from the header of this class:

            //  --o-- 0 O O D
            //  xxoxx 0 O C D
            //  --x-- 0 C O D
            //  xxxxx 0 C C D

            //  xxo-- 0 O O N
            //  --oxx 0 O C N
            //  xxx-- 0 C O N
            //  --xxx 0 C C N

            RangeFlags flags = 0;
            if (seg0 != seg1) flags |= RangeFlags.NormalRange;
            if (c1) flags |= RangeFlags.StartClosed;
            if (seg1) flags |= RangeFlags.EndClosed;

            return new DateTimeRange(d1, TimeSpan.Zero, flags);
        }

        public static DateTimeRange CreateSegments(DateTime d1, DateTime d2, bool seg0, bool c1, bool seg1, bool c2, bool seg2)
        {
            if (!TryCreateSegments(d1, d2, seg0, c1, seg1, c2, seg2, out DateTimeRange result))
                throw new InvalidOperationException("Can only create alternating segments, or all equal segments.");
            return result;
        }

        public static bool TryCreateSegments(DateTime d1, DateTime d2, bool seg0, bool c1, bool seg1, bool c2, bool seg2, out DateTimeRange result)
        {
            if (d1 >= d2)
                throw new InvalidOperationException("d1 must be lesser than d2.");
            return TryCreateSegments_NoChecks(d1, d2, seg0, c1, seg1, c2, seg2, out result);
        }

        private static bool TryCreateSegments_NoChecks(DateTime d1, DateTime d2, bool seg0, bool c1, bool seg1, bool c2, bool seg2, out DateTimeRange result)
        {
            // Table copied from the header of this class:

            //  -o-o- + O O D
            //  -o-x- + O C D
            //  -x-o- + C O D
            //  -x-x- + C C D
            //
            //  xoxox - O O D
            //  xxxox - O C D
            //  xoxxx - C O D
            //  xxxxx - C C D
            //
            //  -oxo- + O O N
            //  -oxx- + O C N
            //  -xxo- + C O N
            //  -xxx- + C C N
            //
            //  xo-ox - O O N
            //  xx-ox - O C N
            //  xo-xx - C O N
            //  xx-xx - C C N

            RangeFlags flags = 0;

            if (seg0 != seg1 && seg1 != seg2) flags |= RangeFlags.NormalRange;
            if (seg0 != seg1 || seg1 != seg2)
            {
                result = default(DateTimeRange);
                return false;
            }

            if (c2 & seg1 || c1 & !seg1) flags |= RangeFlags.StartClosed;

            if (c1 & seg1 || c2 & !seg1) flags |= RangeFlags.EndClosed;

            result = new DateTimeRange(
                seg1 ? d2 : d1,
                seg1 ? d1 - d2 : d2 - d1,
                flags);

            return true;
        }

        public RangeFlags Flags;
        public readonly DateTime Start;
        public readonly TimeSpan Interval;

        public DateTime End => this.Start + this.Interval;

        static int SortDateTimePoints(ref DateTime a, ref DateTime b, ref bool aC, ref bool bC)
        {
            if (a < b)
                return 1;

            if (a > b)
            {
                var t = a;
                a = b;
                b = t;
                var tC = aC;
                aC = bC;
                bC = tC;
                return -1;
            }

            return 0;
        }

        private void SortStartEnd(out DateTime min, out DateTime max, out bool minClosed, out bool maxClosed)
        {
            if (this.Interval.Ticks < 0)
            {
                maxClosed = (this.Flags & RangeFlags.StartClosed) != 0;
                minClosed = (this.Flags & RangeFlags.EndClosed) != 0;
                max = this.Start;
                min = max + this.Interval;
            }
            else
            {
                minClosed = (this.Flags & RangeFlags.StartClosed) != 0;
                maxClosed = (this.Flags & RangeFlags.EndClosed) != 0;
                min = this.Start;
                max = min + this.Interval;
            }
        }

        public bool Equals(DateTimeRange other)
        {
            if (this.Flags == other.Flags && this.Start.Equals(other.Start) && this.Interval.Equals(other.Interval))
                return true;

            var tid = this.GetTypeId();
            var oid = other.GetTypeId();

            // equality groups:
            //  0 1     eq
            //  8 10 6  cmp 8(d1) 10(d1) 6(d2)
            //  4 9 5   cmp 4(d1) 9(d2) 5(d1)
            //  12 14   eq
            //  else    cmp d1; cmp d2

            if (tid == 1 || tid == 0)
                return oid == 1 || oid == 0;

            if (tid == 12 || tid == 14)
                return oid == 12 || oid == 14;

            if (tid == 6 || tid == 8 || tid == 10)
                return oid == 6 || oid == 8 || oid == 10
                    && (tid == 6 ? this.End : this.Start) == (oid == 6 ? this.End : this.Start);

            if (tid == 4 || tid == 5 || tid == 9)
                return oid == 4 || oid == 5 || oid == 9
                    && (tid == 9 ? this.End : this.Start) == (oid == 9 ? this.End : this.Start);

            return false;
        }

        public int GetTypeId()
        {
            //  --o-- 0 O O D   0
            //  xxoxx 0 O C D   8
            //  --x-- 0 C O D   4
            //  xxxxx 0 C C D   12
            //
            //  -o-o- + O O D   1
            //  -o-x- + O C D   9
            //  -x-o- + C O D   5
            //  -x-x- + C C D   13
            //
            //  xoxox - O O D   2
            //  xxxox - O C D   10
            //  xoxxx - C O D   6
            //  xxxxx - C C D   14
            //
            //  xxo-- 0 O O N   16
            //  --oxx 0 O C N   24
            //  xxx-- 0 C O N   20
            //  --xxx 0 C C N   28
            //
            //  -oxo- + O O N   17
            //  -oxx- + O C N   25
            //  -xxo- + C O N   21
            //  -xxx- + C C N   29
            //
            //  xo-ox - O O N   18
            //  xx-ox - O C N   26
            //  xo-xx - C O N   22
            //  xx-xx - C C N   30

            var tid = ((int)this.Flags << 2) + (this.Interval.Ticks > 0 ? 1 : this.Interval.Ticks == 0 ? 0 : 2);
            return tid;
        }



        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is DateTimeRange && Equals((DateTimeRange)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var tid = this.GetTypeId();

                // equality groups:
                //  0 1     eq
                //  8 10 6  cmp 8(d1) 10(d1) 6(d2)
                //  4 9 5   cmp 4(d1) 9(d2) 5(d1)
                //  12 14   eq
                //  else    cmp d1; cmp d2

                if (tid == 1 || tid == 0)
                    return 1 * 397;

                if (tid == 12 || tid == 14)
                    return 14 * 397;

                if (tid == 6 || tid == 8 || tid == 10)
                    return (10 * 397) ^ (tid == 6 ? this.End : this.Start).GetHashCode();

                if (tid == 4 || tid == 5 || tid == 9)
                    return (9 * 397) ^ (tid == 9 ? this.End : this.Start).GetHashCode();

                var hashCode = tid;
                hashCode = (hashCode * 397) ^ this.Start.GetHashCode();
                hashCode = (hashCode * 397) ^ this.Interval.GetHashCode();
                return hashCode;
            }
        }
    }
}