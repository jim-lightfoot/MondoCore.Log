﻿
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

using MondoCore.Log;
using MondoCore.Collections;

namespace MondoCore.Log.UnitTests
{
    [TestClass]
    [TestCategory("Unit Tests")]
    public class LogTest
    {
        private ILog _log;
        private List<Telemetry> _errors = new List<Telemetry>();

        public LogTest()
        {
            var log = new Log();

            log.Register(new TestLog(_errors));

            _log = log;
        }

        #region WriteError

        [TestMethod]
        public async Task Log_WriteError()
        {
            await _log.WriteError(new Exception("Bob's hair is on fire"));

            Assert.AreEqual(1, _errors.Count);
            Assert.AreEqual(Telemetry.TelemetryType.Error, _errors[0].Type);
            Assert.AreEqual("Bob's hair is on fire", _errors[0].Exception?.Message);
        }

        [TestMethod]
        public async Task Log_WriteError_wData()
        {
            var ex = new Exception("Bob's hair is on fire");

            ex.Data["Model"] = "Chevy";

            await _log.WriteError(ex);

            Assert.AreEqual(1, _errors.Count);
            Assert.AreEqual(Telemetry.TelemetryType.Error, _errors[0].Type);
            Assert.AreEqual("Bob's hair is on fire", _errors[0].Exception?.Message);
            Assert.AreEqual("Chevy", _errors[0].Properties?.ToReadOnlyDictionary()?["Model"]);
        }

        [TestMethod]
        public async Task Log_WriteError_wData_inner_exception()
        {
            var ex1 = new Exception("Bob's hair is on fire");
            var ex2 = new Exception("Wendy's hair is on fire", ex1);
            var ex3 = new Exception("Georges's hair is on fire", ex2);
            var ex4 = new Exception("Alices's hair is on fire", ex3);

            ex1.Data["Make"] = "Chevy";
            ex2.Data["Model"] = "Corvette";
            ex3.Data["Color"] = "Blue";
            ex4.Data["Year"] = "1956";

            await _log.WriteError(ex4);

            Assert.AreEqual(1, _errors.Count);
            Assert.AreEqual(Telemetry.TelemetryType.Error, _errors[0].Type);
            Assert.AreEqual("Alices's hair is on fire", _errors[0].Exception?.Message);

            Assert.AreEqual("Chevy",    _errors[0].Properties?.ToReadOnlyDictionary()["Make"]);
            Assert.AreEqual("Corvette", _errors[0].Properties?.ToReadOnlyDictionary()["Model"]);
            Assert.AreEqual("Blue",     _errors[0].Properties?.ToReadOnlyDictionary()["Color"]);
            Assert.AreEqual("1956",     _errors[0].Properties?.ToReadOnlyDictionary()["Year"]);
        }

        [TestMethod]
        public async Task Log_WriteError_wData_aggregate_exception()
        {
            var ex1 = new Exception("Bob's hair is on fire");
            var ex2 = new Exception("Wendy's hair is on fire");
            var ex3 = new Exception("Georges's hair is on fire");
            var ex4 = new Exception("Alices's hair is on fire");
            var ex5 = new AggregateException(ex1, ex2, ex3, ex4);

            ex1.Data["Make"] = "Chevy";
            ex2.Data["Model"] = "Corvette";
            ex3.Data["Color"] = "Blue";
            ex4.Data["Year"] = "1956";
            ex5.Data["Engine"] = "350";

            await _log.WriteError(ex5);

            Assert.AreEqual(1, _errors.Count);
            Assert.AreEqual(Telemetry.TelemetryType.Error, _errors[0].Type);

            Assert.AreEqual("Chevy",    _errors[0].Properties?.ToReadOnlyDictionary()["Make"]);
            Assert.AreEqual("Corvette", _errors[0].Properties?.ToReadOnlyDictionary()["Model"]);
            Assert.AreEqual("Blue",     _errors[0].Properties?.ToReadOnlyDictionary()["Color"]);
            Assert.AreEqual("1956",     _errors[0].Properties?.ToReadOnlyDictionary()["Year"]);
            Assert.AreEqual("350",     _errors[0].Properties?.ToReadOnlyDictionary()["Engine"]);
        }


        [TestMethod]
        public async Task Log_WriteError_Fallback()
        {
            var log = new Log();
            var errors = new List<Telemetry>();
            var failLog = new Mock<ILog>();

            failLog.Setup( f=> f.WriteTelemetry(It.IsAny<Telemetry>())).Throws(new Exception("Whatever"));

            log.Register(failLog.Object);
            log.Register(new TestLog(errors), true);

            await ((ILog)log).WriteError(new Exception("Bob's hair is on fire"));

            Assert.AreEqual(2, errors.Count);
            Assert.AreEqual(Telemetry.TelemetryType.Error, errors[0].Type);
            Assert.AreEqual(Telemetry.TelemetryType.Error, errors[1].Type);
            Assert.AreEqual("Whatever", errors[0].Exception?.Message);
            Assert.AreEqual("Bob's hair is on fire", errors[1].Exception?.Message);
        }

        [TestMethod]
        public async Task Log_WriteError_MultipleLoggers()
        {
            var log        = new Log();
            var errors1    = new List<Telemetry>();
            var log1       = new TestLog(errors1);
            var errors2    = new List<Telemetry>();
            var log2       = new TestLog(errors2);
            var errors3    = new List<Telemetry>();
            var log3       = new TestLog(errors3);
            var failErrors = new List<Telemetry>();
            var failLog    = new TestLog(failErrors);

            log.Register(log1);
            log.Register(failLog, true);
            log.Register(log2);
            log.Register(log3);

            await ((ILog)log).WriteError(new Exception("Bob's hair is on fire"));

            Assert.AreEqual(1, errors1.Count);
            Assert.AreEqual(1, errors2.Count);
            Assert.AreEqual(1, errors3.Count);
            Assert.AreEqual(0, failErrors.Count);

            Assert.AreEqual(Telemetry.TelemetryType.Error, errors1[0].Type);
            Assert.AreEqual("Bob's hair is on fire", errors1[0].Exception?.Message);

            Assert.AreEqual(Telemetry.TelemetryType.Error, errors2[0].Type);
            Assert.AreEqual("Bob's hair is on fire", errors2[0].Exception?.Message);

            Assert.AreEqual(Telemetry.TelemetryType.Error, errors3[0].Type);
            Assert.AreEqual("Bob's hair is on fire", errors3[0].Exception?.Message);
        }

        [TestMethod]
        public async Task Log_WriteError_MultipleLoggers_filtered()
        {
            var log        = new Log();
            var errors1    = new List<Telemetry>();
            var log1       = new TestLog(errors1);
            var errors2    = new List<Telemetry>();
            var log2       = new TestLog(errors2);
            var errors3    = new List<Telemetry>();
            var log3       = new TestLog(errors3);
            var errors4    = new List<Telemetry>();
            var log4       = new TestLog(errors4);
            var failErrors = new List<Telemetry>();
            var failLog    = new TestLog(failErrors);

            log.Register(log1);
            log.Register(failLog, true);
            log.Register(log2, types: new List<Telemetry.TelemetryType> { Telemetry.TelemetryType.Trace,   Telemetry.TelemetryType.Error } );
            log.Register(log3, types: new List<Telemetry.TelemetryType> { Telemetry.TelemetryType.Request, Telemetry.TelemetryType.Event } );
            log.Register(log4, types: new List<Telemetry.TelemetryType> { Telemetry.TelemetryType.Trace } );

            await ((ILog)log).WriteError(new Exception("Bob's hair is on fire"));
            await ((ILog)log).WriteEvent("No it's not");

            Assert.AreEqual(2, errors1.Count);
            Assert.AreEqual(1, errors2.Count);
            Assert.AreEqual(1, errors3.Count);
            Assert.AreEqual(0, errors4.Count);
            Assert.AreEqual(0, failErrors.Count);

            Assert.AreEqual(Telemetry.TelemetryType.Error, errors1[0].Type);
            Assert.AreEqual("Bob's hair is on fire", errors1[0].Exception?.Message);
            Assert.AreEqual(Telemetry.TelemetryType.Event, errors1[1].Type);
            Assert.AreEqual("No it's not", errors1[1].Message);

            Assert.AreEqual(Telemetry.TelemetryType.Error, errors2[0].Type);
            Assert.AreEqual("Bob's hair is on fire", errors2[0].Exception?.Message);

            Assert.AreEqual(Telemetry.TelemetryType.Event, errors3[0].Type);
            Assert.AreEqual("No it's not", errors3[0].Message);
        }

        #endregion

        #region WriteEvent

        [TestMethod]
        public async Task Log_WriteEvent()
        {
            var correlationId = Guid.NewGuid().ToString();

            await _log.WriteEvent("Race", new { Model = "Chevy" }, correlationId);

            Assert.AreEqual(1, _errors.Count);
            Assert.AreEqual(Telemetry.TelemetryType.Event, _errors[0].Type);
            Assert.AreEqual("Race", _errors[0].Message);
            Assert.AreEqual(correlationId, _errors[0].CorrelationId);
        }

        #endregion

        #region WriteTrace

        [TestMethod]
        public async Task Log_WriteTrace()
        {
            await _log.WriteTrace("Bob's hair is on fire", Telemetry.LogSeverity.Critical, new { Model = "Chevy" });

            Assert.AreEqual(1, _errors.Count);
            Assert.AreEqual(Telemetry.TelemetryType.Trace, _errors[0].Type);
            Assert.AreEqual("Bob's hair is on fire", _errors[0].Message);
        }

        #endregion

        #region WriteMetric

        [TestMethod]
        public async Task Log_WriteMetric()
        {
            await _log.WriteMetric("Length of Bob's Hair", 42d, new { Model = "Chevy" });

            Assert.AreEqual(1, _errors.Count);
            Assert.AreEqual(Telemetry.TelemetryType.Metric, _errors[0].Type);
            Assert.AreEqual("Length of Bob's Hair", _errors[0].Message);
            Assert.AreEqual(42d, _errors[0]!.Value);
        }

        #endregion

        /*************************************************************************/
        /*************************************************************************/
        internal class TestLog : ILog
        {
            private readonly List<Telemetry> _entries;

            /*************************************************************************/
            internal TestLog(List<Telemetry> entries)
            {
                _entries = entries;
            }

            /*************************************************************************/
            public Task WriteTelemetry(Telemetry telemetry)
            {
                _entries.Add(telemetry);

                return Task.CompletedTask;
            }

            public void SetProperty(string name, string value)
            {
                throw new NotImplementedException();
            }

            public IDisposable StartOperation(string operationName)
            {
                return null;
            }

            public IRequestLog NewRequest(string? operationName = null, string? correlationId = null, object? properties = null)
            {
                throw new NotImplementedException();
            }
        }
    }
}
