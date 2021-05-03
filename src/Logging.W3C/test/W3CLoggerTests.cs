using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.Logging.W3C.Tests
{
    public class W3CLoggerTests : IDisposable
    {

        DateTime _timestampOne = new DateTime(2021, 01, 02, 03, 04, 05);

        public W3CLoggerTests()
        {
            TempPath = Path.GetTempFileName() + "_";
        }

        public string TempPath { get; }
        public void Dispose()
        {
            try
            {
                if (Directory.Exists(TempPath))
                {
                    Directory.Delete(TempPath, true);
                }
            }
            catch
            {
                // ignored
            }
        }

        [Fact]
        public void WritesToTextFile()
        {
            var provider = new TestW3CLoggerProvider(TempPath, W3CLoggingFields.Date | W3CLoggingFields.Time | W3CLoggingFields.TimeTaken);
            var logger = provider.CreateLogger("Microsoft.AspNetCore.W3CLogging");
            var state = new List<KeyValuePair<string, object>>();
            state.Add(new KeyValuePair<string, object>(nameof(DateTime), _timestampOne));

            logger.Log(LogLevel.Information, new EventId(7, "W3CLog"), state, null, (st, ex) => null);
            provider.Dispose();

            var lines = File.ReadAllLines(provider.GetLogFileFullName("Microsoft.AspNetCore.W3CLogging"));
            Assert.Equal("#Version: 1.0", lines[0]);
            Assert.StartsWith("#Start-Date: ", lines[1]);
            var startDate = DateTime.Parse(lines[1].Substring(13), CultureInfo.InvariantCulture);
            // Assert that the log was written in the last 10 seconds
            var now = DateTime.Now;
            Assert.True(now.Subtract(startDate).TotalSeconds < 10);

            Assert.Equal("#Fields: date time time-taken", lines[2]);
            Assert.StartsWith("2021-01-02 03:04:05 ", lines[3]);
            // Assert that the log's time-taken is within 10 seconds of DateTime.Now minus our arbitary start time (01/02/21 at 3:04:05)
            Assert.True(now.Subtract(_timestampOne).TotalSeconds - Convert.ToDouble(lines[3].Substring(20), CultureInfo.InvariantCulture) < 10);
        }
    }
}
