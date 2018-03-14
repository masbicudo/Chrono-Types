using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace ChronoTypes
{
    public class ChronoText
    {
        public void AddCulture(CultureInfo culture)
        {
            var dtfmt = culture.DateTimeFormat;
            var allfmts = dtfmt.GetAllDateTimePatterns();

            // Reading all patterns and creating a parse tree
            // that can distinguish them.
            var rootNode = new FormatNode<DateTimePartition>();
            foreach (var eachfmt in allfmts)
            {
                var currentNode = rootNode.Branch();
                var chx = '\0';
                var cnt = 0;
                for (int itch = 0; itch < eachfmt.Length; itch++)
                {

                }
            }

            //var numNode0 = branch
            //    .Branch(numParser)
            //    .Result(n => new GregorianYearValue(n.Value));
            //var sepNode0 = startNum2.Next(dateSeparatorParser);
            //var numNode1 = sepNode0
            //    .Branch(numParser)
            //    .Result(n => new MonthOfYearValue(n.Value));
            //var sepNode1 = numNode1.Next(dateSeparatorParser);
            //var numNode2 = sepNode1
            //    .Branch(numParser)
            //    .Result(n => new DayOfMonthValue(n.Value));
        }

        public virtual DateTime Parse()
        {
            return DateTime.MaxValue;
        }
    }

    public class FormatNode<TResult>
    {
        private readonly List<FormatNode<TResult>> branches = new List<FormatNode<TResult>>();

        public FormatNode<TResult> Branch()
        {
            var formatNode = new FormatNode<TResult>();
            branches.Add(formatNode);
            return formatNode;
        }

        public TResult Parse(string text)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Collection of DateTimePartition objects, which can be used to get the
    /// existing intersection ranges, and also to create a ContinuousDateTimeSet.
    /// </summary>
    public sealed class DateTimePartitionCollection :
        IList<DateTimePartition>
    {
        private DateTimePartition[] partitions;
        private int count;

        public DateTimePartitionCollection()
        {
            this.partitions = new DateTimePartition[8];
        }

        public IEnumerator<DateTimePartition> GetEnumerator()
            => ((IEnumerable<DateTimePartition>)this.partitions).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public void Add(DateTimePartition item)
        {
            if (this.count + 1 >= this.partitions.Length)
                Array.Resize(ref this.partitions, this.partitions.Length * 2);
            this.partitions[this.count] = item;
            this.count++;
        }

        public void Clear()
        {
            Array.Clear(this.partitions, 0, this.partitions.Length);
            this.count = 0;
        }

        public bool Contains(DateTimePartition item)
            => Array.IndexOf(this.partitions, item) >= 0;

        public void CopyTo(DateTimePartition[] array, int arrayIndex)
            => Array.Copy(this.partitions, 0, array, arrayIndex, this.partitions.Length);

        public bool Remove(DateTimePartition item)
        {
            var index = this.IndexOf(item);
            if (index < 0) return false;
            this.RemoveAt(index);
            return true;
        }

        public int Count => this.count;

        public bool IsReadOnly => false;

        public int IndexOf(DateTimePartition item)
            => Array.IndexOf(this.partitions, item);

        public void Insert(int index, DateTimePartition item)
        {
            if (this.count + 1 >= this.partitions.Length)
                Array.Resize(ref this.partitions, this.partitions.Length * 2);
            var itemsToMove = this.count - index;
            if (itemsToMove > 0)
                Array.Copy(this.partitions, index, this.partitions, index + 1, itemsToMove);
            this.partitions[index] = item;
            this.count++;
        }

        public void RemoveAt(int index)
        {
            var itemsToMove = this.count - index - 1;
            if (itemsToMove > 0)
                Array.Copy(this.partitions, index + 1, this.partitions, index, itemsToMove);
            this.count--;
            this.partitions[this.count] = null;
        }

        public DateTimePartition this[int index]
        {
            get => this.partitions[index];
            set => this.partitions[index] = value;
        }
    }

    /// <summary>
    /// Base class representing a partition of the DateTime domain,
    /// or a subset of the given partition.
    /// <para>
    /// Partitions may have 1 or 2 levels. The year partition class has 1 level.
    /// The day of year partition class has 2 levels.
    /// </para>
    /// <para>Partition items may have special names and other properties.</para>
    /// </summary>
    /// <remarks>
    /// All DateTimePartition classes must be able to represent the whole DateTime domain.
    /// <para>
    /// E.g.1: The year partition class must be able to represent any set of years.
    /// </para><para>
    /// E.g.2: The month of year partition class must be able to represent any set of the 12 months of the year.
    /// </para>
    /// </remarks>
    public abstract class DateTimePartition
    {
        protected DateTimePartition()
        {
        }

        /// <summary>
        /// Returns the values in the subset of this partition.
        /// <para>
        /// Note that this can 
        /// </para>
        /// </summary>
        /// <returns></returns>
        public abstract int[] GetValues();

        /// <summary>
        /// Returns the name of specific items of the partition.
        /// <para>
        /// E.g.: the name of the value 0 of the day of week partition is sunday,
        /// given that the first day of the week is set to be sunday.
        /// </para>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual string GetName(int value) => value.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Gets the estimated number of ranges returned per day.
        /// <para>
        /// For example, there are 24 of the 1st minute of any hour in a day.
        /// </para><para>
        /// The 1st day of the month is about 12/365.25.
        /// </para><para>
        /// The 29th day of the month is about (11*4+1)/(365.25*4).
        /// </para><para>
        /// The 30th day of the month is about 11/365.25.
        /// </para><para>
        /// The 31st day of the month is about 7/365.25.
        /// </para><para>
        /// A specific year has a value of zero, because years are not recurrent,
        /// unless it is a two digit year.
        /// </para>
        /// </summary>
        /// <remarks>
        /// This is useful when intersecting custom date-partitions.
        /// The lower the value, the faster the iteration is.
        /// </remarks>
        public abstract float RangeRatioPerDay { get; }

        /// <summary>
        /// Gets the amount of time represented by this partition related to the total amount of time.
        /// For example, the 1st second of a minute is 1/60 of the total available time.
        /// <para>
        /// The 1st minute of an hour is also 1/60.
        /// </para><para>
        /// The 1st second of an hour is 1/3600.
        /// </para><para>
        /// The 1st day of the month is 12/365.25.
        /// </para><para>
        /// A specific year has a value of zero, because years are not recurrent.
        /// Unless it is a two-digit year specifier.
        /// </para>
        /// </summary>
        public abstract float TimeRatio { get; }

        /// <summary>
        /// Gets the total number of ranges being represented if finite;
        /// otherwise, return null.
        /// </summary>
        public abstract int? Count { get; }

        /// <summary>
        /// Gets the span of time, comprising the minimum multiple of the current
        /// recurrent rule, that is fixed.
        /// <para>
        /// E.g.: leap years occurs every 4 years, except on years that are multipe of 100 but not of 400.
        /// This makes the minimum fixed span of time for the year part to be [(365*3 + 366)*100 - 3] fixed days.
        /// </para>
        /// </summary>
        public abstract TimeSpan MinimumMultipleFixedInterval_TimeSpan { get; }

        /// <summary>
        /// Gets the number of partitions, comprising the minimum multiple of the current
        /// recurrent rule, that has a fixed span of time.
        /// <para>
        /// E.g.: leap years occurs every 4 years, except on years that are multipe of 100 but not of 400.
        /// This makes the minimum fixed span of time for the year part to have 400 years.
        /// </para>
        /// </summary>
        public abstract int MinimumMultipleFixedInterval_RangeCount { get; }

        /// <summary>
        /// Gets the amount of time between successive dates when possible.
        /// When the value is variable, then the return is null.
        /// This value is not an approximation, it is the exact value.
        /// <para>
        /// For this reason, two digit year interval is not fixed because
        /// centuries are not fixed in side. Some centuries have 25 leap years,
        /// others have 24 leap years.
        /// </para>
        /// </summary>
        public abstract TimeSpan? IntervalBetweenSuccessiveRanges { get; }

        public abstract bool TryGetPreviousRange(DateTime endDateTime, out DateTimeRange range);
        public abstract bool TryGetNextRange(DateTime startDateTime, out DateTimeRange range);
        public abstract bool TryGetCurrentRange(DateTime currentDateTime, out DateTimeRange range);
        public abstract IEnumerable<DateTimeRange> GetRangesAfter(DateTime startDateTime, bool includeCurrent = false);
        public abstract IEnumerable<DateTimeRange> GetRangesBefore(DateTime endDateTime, bool includeCurrent = false);

        public override string ToString()
        {
            return this.Value.ToString();
        }
    }

    /// <summary>
    /// Represents the year in a DateTime.
    /// </summary>
    public sealed class YearDateTimePartition : DateTimePartition
    {
        public YearDateTimePartition(int value)
            : base(value)
        {
        }

        /// <inheritdoc />
        public override float RangeRatioPerDay => 0f;

        /// <inheritdoc />
        public override float TimeRatio => 0f;

        /// <inheritdoc />
        public override int? Count => 1;

        /// <inheritdoc />
        public override TimeSpan? IntervalBetweenSuccessiveRanges => null;

        public override bool TryGetPreviousRange(DateTime endDateTime, out DateTimeRange range)
        {
            var ret = this.Value < endDateTime.Year;
            range = ret ? this.Range : default(DateTimeRange);
            return ret;
        }

        public override bool TryGetNextRange(DateTime startDateTime, out DateTimeRange range)
        {
            var ret = this.Value > startDateTime.Year;
            range = ret ? this.Range : default(DateTimeRange);
            return ret;
        }

        public override bool TryGetCurrentRange(DateTime currentDateTime, out DateTimeRange range)
        {
            var ret = this.Value == currentDateTime.Year;
            range = ret ? this.Range : default(DateTimeRange);
            return ret;
        }

        public override IEnumerable<DateTimeRange> GetRangesAfter(DateTime startDateTime, bool includeCurrent = false)
        {
            if (includeCurrent && this.Value == startDateTime.Year || this.Value > startDateTime.Year)
                yield return this.Range;
        }

        public override IEnumerable<DateTimeRange> GetRangesBefore(DateTime endDateTime, bool includeCurrent = false)
        {
            if (includeCurrent && this.Value == endDateTime.Year || this.Value < endDateTime.Year)
                yield return this.Range;
        }

        private DateTimeRange Range
            => new DateTimeRange(
                new DateTime(this.Value, 1, 1),
                new DateTime(this.Value, 31, 12),
                RangeFlags.StartClosed | RangeFlags.EndClosed);
    }

    /// <summary>
    /// Represents the year of the century in a DateTime.
    /// </summary>
    public sealed class YearOfCenturyDateTimePartition : DateTimePartition
    {
        public YearOfCenturyDateTimePartition(int value) : base(value)
        {
        }

        public override float RangeRatioPerDay => 1f / 100f;
        public override float TimeRatio => 1f / 100f;
        public override int? Count => null;
        public override TimeSpan? IntervalBetweenSuccessiveRanges { get; }
        public override bool TryGetPreviousRange(DateTime endDateTime, out DateTimeRange range)
        {
            throw new NotImplementedException();
        }

        public override bool TryGetNextRange(DateTime startDateTime, out DateTimeRange range)
        {
            throw new NotImplementedException();
        }

        public override bool TryGetCurrentRange(DateTime currentDateTime, out DateTimeRange range)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<DateTimeRange> GetRangesAfter(DateTime startDateTime, bool includeCurrent = false)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<DateTimeRange> GetRangesBefore(DateTime endDateTime, bool includeCurrent = false)
        {
            throw new NotImplementedException();
        }
    }

    public struct Substring
    {
        public Substring(string source, int index, int length)
        {
            this.Source = source;
            this.Index = index;
            this.Length = length;
        }

        public string Source { get; }
        public int Index { get; }
        public int Length { get; }

        public override string ToString()
        {
            return this.Source.Substring(this.Index, this.Length);
        }
    }
}
