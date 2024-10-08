﻿
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

using MondoCore.Collections;
using MondoCore.Log;

namespace MondoCore.Log.UnitTests
{
    [TestClass]
    [TestCategory("Unit Tests")]
    public class RequestLogTest
    {
        private IRequestLog _log;
        private List<Telemetry> _errors = new List<Telemetry>();

        public RequestLogTest()
        {
            var log = new Log();

            log.Register(new TestLog(_errors));

            _log = new RequestLog(log, operationName: "top", correlationId: "xyz");
        }

        [TestMethod]
        public async Task RequestLog_WriteError()
        {
            _log.SetProperty("Model", "Corvette");

            await _log.WriteError(new Exception("Bob's hair is on fire"), properties: new {Make = "Chevy" } );

            Assert.AreEqual(1, _errors.Count);
            Assert.AreEqual(Telemetry.TelemetryType.Error, _errors[0].Type);
            Assert.AreEqual("Bob's hair is on fire", _errors[0].Exception?.Message);

            var props = _errors[0].Properties?.ToReadOnlyDictionary();

            Assert.AreEqual("Chevy", props?["Make"]);
            Assert.AreEqual("Corvette", props?["Model"]);
        }

        [TestMethod]
        public async Task RequestLog_WriteError2()
        {
            _log.SetProperty("Make", "Chevy");
            _log.SetProperty("Model", "Corvette");

            await _log.WriteError(new Exception("Bob's hair is on fire"));

            Assert.AreEqual(1, _errors.Count);
            Assert.AreEqual(Telemetry.TelemetryType.Error, _errors[0].Type);
            Assert.AreEqual("Bob's hair is on fire", _errors[0].Exception?.Message);

            var props = _errors[0].Properties?.ToReadOnlyDictionary();

            Assert.AreEqual("Chevy", props?["Make"]);
            Assert.AreEqual("Corvette", props?["Model"]);
        }

        [TestMethod]
        public async Task RequestLog_WriteError3()
        {
            await _log.WriteError(new Exception("Bob's hair is on fire"), properties: new {Make = "Chevy", Model = "Corvette" } );

            Assert.AreEqual(1, _errors.Count);
            Assert.AreEqual(Telemetry.TelemetryType.Error, _errors[0].Type);
            Assert.AreEqual("Bob's hair is on fire", _errors[0].Exception?.Message);

            var props = _errors[0].Properties?.ToReadOnlyDictionary();

            Assert.AreEqual("Chevy", props?["Make"]);
            Assert.AreEqual("Corvette", props?["Model"]);
        }

        [TestMethod]
        public async Task RequestLog_WriteError_nested()
        {
            _log.SetProperty("Town", "Bedrock");

            using(var log = _log.NewRequest(operationName: "2nd", correlationId: "1234"))
            { 
                log.SetProperty("LastName", "Flintstone");

                await log.WriteError(new Exception("Fred's hair is on fire"), properties: new {Make = "Chevy", Model = "Corvette" } );
            }

            await _log.WriteError(new Exception("Barney's hair is on fire"), properties: new {Make = "Chevy", Model = "Corvette" } );

            Assert.AreEqual(2, _errors.Count);
            Assert.AreEqual(Telemetry.TelemetryType.Error, _errors[0].Type);
            Assert.AreEqual(Telemetry.TelemetryType.Error, _errors[1].Type);
            Assert.AreEqual("Fred's hair is on fire", _errors[0].Exception?.Message);
            Assert.AreEqual("Barney's hair is on fire", _errors[1].Exception?.Message);
            Assert.AreEqual("1234", _errors[0].CorrelationId);
            Assert.AreEqual("xyz", _errors[1].CorrelationId);
            Assert.AreEqual("2nd", _errors[0].OperationName);
            Assert.AreEqual("top", _errors[1].OperationName);

            var props = _errors[0].Properties?.ToReadOnlyDictionary();

            Assert.AreEqual("Bedrock", props?["Town"]);
            Assert.AreEqual("Chevy", props?["Make"]);
            Assert.AreEqual("Corvette", props?["Model"]);
            Assert.AreEqual("Flintstone", props?["LastName"]);

            var props2 = _errors[1].Properties?.ToReadOnlyDictionary();

            Assert.AreEqual("Bedrock", props2?["Town"]);
            Assert.AreEqual("Chevy", props2?["Make"]);
            Assert.AreEqual("Corvette", props2?["Model"]);
            Assert.IsFalse(props2?.ContainsKey("LastName"));
        }

        [TestMethod]
        public async Task RequestLog_WriteError_nested2()
        {
            _log.SetProperty("Town", "Bedrock");

            using var request1 = _log.NewRequest(operationName: "2nd", correlationId: "1234", properties: new { LastName = "Flintstone" } );
            using var request2 = request1.NewRequest(operationName: "2nd", correlationId: "1234", properties: new { FirstName = "Fred" } );

            await request2.WriteError(new Exception("Barney's hair is on fire"), properties: new {Make = "Chevy", Model = "Corvette" } );

            var props = _errors[0].Properties?.ToReadOnlyDictionary();

            Assert.AreEqual("Bedrock",      props?["Town"]);
            Assert.AreEqual("Fred",         props?["FirstName"]);
            Assert.AreEqual("Flintstone",   props?["LastName"]);
            Assert.AreEqual("Chevy",        props?["Make"]);
            Assert.AreEqual("Corvette",     props?["Model"]);
        }

        [TestMethod]
        public async Task RequestLog_WriteError_nested2a()
        {
            _log.SetProperty("Town", "Bedrock");

            { 
                using var request1 = _log.NewRequest(operationName: "2nd", correlationId: "1234", properties: new { LastName = "Flintstone" } );
                using var request2 = request1.NewRequest(operationName: "2nd", correlationId: "1234", properties: new { FirstName = "Fred" } );

                await request2.WriteError(new Exception("Barney's hair is on fire"), properties: new {Make = "Chevy", Model = "Corvette" } );

                var props = _errors[0].Properties?.ToReadOnlyDictionary();

                Assert.AreEqual("Bedrock",      props?["Town"]);
                Assert.AreEqual("Fred",         props?["FirstName"]);
                Assert.AreEqual("Flintstone",   props?["LastName"]);
                Assert.AreEqual("Chevy",        props?["Make"]);
                Assert.AreEqual("Corvette",     props?["Model"]);
            }

            await _log.WriteError(new Exception("Barney's hair is on fire"), properties: new {Make = "Chevy", Model = "Corvette" } );

            var props2 = _errors[1].Properties?.ToReadOnlyDictionary();

            Assert.AreEqual(3, props2.Count());
        }

        [TestMethod]
        public async Task RequestLog_WriteError_nested3()
        {
            _log.SetProperty("Town", "Bedrock");

            using(var log = _log.NewRequest())
            { 
                log.SetProperty("LastName", "Flintstone");

                await log.WriteError(new Exception("Fred's hair is on fire"), properties: new {Make = "Chevy", Model = "Corvette" } );
            }

            await _log.WriteError(new Exception("Barney's hair is on fire"), properties: new {Make = "Chevy", Model = "Corvette" } );

            Assert.AreEqual(2, _errors.Count);
            Assert.AreEqual(Telemetry.TelemetryType.Error, _errors[0].Type);
            Assert.AreEqual(Telemetry.TelemetryType.Error, _errors[1].Type);
            Assert.AreEqual("Fred's hair is on fire", _errors[0].Exception?.Message);
            Assert.AreEqual("Barney's hair is on fire", _errors[1].Exception?.Message);
            Assert.AreEqual("xyz", _errors[0].CorrelationId);
            Assert.AreEqual("xyz", _errors[1].CorrelationId);
            Assert.AreEqual("top", _errors[0].OperationName);
            Assert.AreEqual("top", _errors[1].OperationName);

            var props = _errors[0].Properties?.ToReadOnlyDictionary();

            Assert.AreEqual("Bedrock", props?["Town"]);
            Assert.AreEqual("Chevy", props?["Make"]);
            Assert.AreEqual("Corvette", props?["Model"]);
            Assert.AreEqual("Flintstone", props?["LastName"]);

            var props2 = _errors[1].Properties?.ToReadOnlyDictionary();

            Assert.AreEqual("Bedrock", props2?["Town"]);
            Assert.AreEqual("Chevy", props2?["Make"]);
            Assert.AreEqual("Corvette", props2?["Model"]);
            Assert.IsFalse(props2?.ContainsKey("LastName"));
        }

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
