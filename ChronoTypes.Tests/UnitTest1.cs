using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChronoTypes.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var chronoText = new ChronoText();
            chronoText.AddCulture(CultureInfo.GetCultureInfo("en-US"));

        }

        [TestMethod]
        public void YearDateTimePart1()
        {
            var year = new YearDateTimePartition(2018);
            var isCurRng = year.TryGetCurrentRange(new DateTime(2018, 2, 4), out DateTimeRange rng1);
            var isNotCurRng = year.TryGetCurrentRange(new DateTime(2017, 2, 4), out DateTimeRange rng2);
            Assert.IsTrue(isCurRng);
            Assert.IsFalse(isNotCurRng);
            Assert.AreEqual(rng1, new DateTimeRange(new DateTime(2018, 1, 1), new DateTime(2019, 1, 1), RangeFlags.StartClosed));
        }
    }
}
