/***************************************************************************
 *                                                                          
 *    The MondoCore Libraries  						                        
 *                                                                          
 *        Namespace: MondoCore.Log 			            
 *             File: ILog.cs							
 *        Class(es): ILog							    
 *          Purpose: Generic interface for logging              
 *                                                                          
 *  Original Author: Jim Lightfoot                                          
 *    Creation Date: 1 Aug 2020                                             
 *                                                                          
 *   Copyright (c) 2020-2024 - Jim Lightfoot, All rights reserved                
 *                                                                          
 *  Licensed under the MIT license:                                         
 *    http://www.opensource.org/licenses/mit-license.php                    
 *                                                                          
 ****************************************************************************/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using MondoCore.Collections;

namespace MondoCore.Log
{
    /*************************************************************************/
    /// <summary>
    /// Generic interface for logging
    /// </summary>
    /*************************************************************************/
    public interface ILog
    {
        Task        WriteTelemetry(Telemetry telemetry);
        IDisposable StartOperation(string operationName);
        IRequestLog NewRequest(string? operationName = null, string? correlationId = null, object? properties = null);

        #region Default Methods

        /// <summary>
        /// Write an exception to the log
        /// </summary>
        /// <example>
        ///     Use anonymous objects to pass properties:
        ///     log.WriteEvent("Message received", new { Category = "Blue", Level = 4 });
        /// </example>        
        /// <example>
        ///     Use non-anonymous (POCO) objects to pass properties (all public properties are logged):
        ///     log.WriteEvent("Message received", new ProductInfo { Category = "Blue", Level = 4 });
        /// </example>        
        /// <example>
        ///   Use dictionary to pass properties:
        ///     log.WriteEvent("Message received", new Dictionary<string, object> { {"Category", "Blue"}, {"Level", 4"} });
        /// </example>
        /// <example>
        ///   Use xml to pass properties (only logs elements under root):
        ///     log.WriteEvent("Message received", XmlDoc.LoadXml("<Root><Category>Blue</Category><Level>4</Level></Root>") );
        /// </example>
        /// <param name="ex">Exception to log</param>
        /// <param name="properties">See examples</param>
        /// <param name="correlationId">A value to correlate actions across calls and processes</param>
        Task WriteError(Exception ex, Telemetry.LogSeverity severity = Telemetry.LogSeverity.Error, object? properties = null, string? correlationId = null)
        {
            var props = properties.ToReadOnlyDictionary().MergeData(ex);

            return this.WriteTelemetry(new Telemetry { 
                                                        Type          = Telemetry.TelemetryType.Error, 
                                                        Exception     = ex,
                                                        Severity      = severity,
                                                        CorrelationId = correlationId,
                                                        Properties    = props,
                                                    });
        }

       /// <summary>
        /// Write an event to the log
        /// </summary>
        /// <param name="eventName">Name of event to write</param>
        /// <param name="properties">See examples in WriteError</param>
        /// <param name="metrics">An optional dictionary of metrics to write</param>
        /// <param name="correlationId">A value to correlate actions across calls and processes</param>
        public Task WriteEvent(string eventName, object? properties = null, Dictionary<string, double>? metrics = null, string? correlationId = null)
        {
            return this.WriteTelemetry(new Telemetry { 
                                                        Type          = Telemetry.TelemetryType.Event, 
                                                        Message       = eventName,
                                                        CorrelationId = correlationId,
                                                        Properties    = properties,
                                                        Metrics       = metrics
                                                    });
        }

        /// <summary>
        /// Write a metric to the log
        /// </summary>
        /// <param name="metricName">Name of metric to write</param>
        /// <param name="value">Value of metric</param>
        /// <param name="properties">See examples in WriteError</param>
        /// <param name="correlationId">A value to correlate actions across calls and processes</param>
        public Task WriteMetric(string metricName, double value, object? properties = null, string? correlationId = null)
        {
            return this.WriteTelemetry(new Telemetry { 
                                                        Type          = Telemetry.TelemetryType.Metric, 
                                                        Message       = metricName,
                                                        Value         = value,
                                                        CorrelationId = correlationId,
                                                        Properties    = properties
                                                    });
        }

        /// <summary>
        /// Write a trace to the log
        /// </summary>
        /// <param name="message">Message to write</param>
        /// <param name="severity">Severity of trace</param>
        /// <param name="properties">See examples in WriteError</param>
        /// <param name="correlationId">A value to correlate actions across calls and processes</param>
        public Task WriteTrace(string message, Telemetry.LogSeverity severity, object? properties = null, string? correlationId = null)
        {
            return this.WriteTelemetry(new Telemetry { 
                                                        Type          = Telemetry.TelemetryType.Trace, 
                                                        Message       = message,
                                                        Severity      = severity,
                                                        CorrelationId = correlationId,
                                                        Properties    = properties
                                                    });
        }

        /// <summary>
        /// Write a request to the log
        /// </summary>
        /// <param name="name">Name of request</param>
        /// <param name="startTime">Time request started</param>
        /// <param name="duration">Duration of request</param>
        /// <param name="responseCode">Response code returned from request</param>
        /// <param name="success">True if request was successful</param>
        /// <param name="correlationId">A value to correlate actions across calls and processes</param>
        public Task WriteRequest(string name, DateTime startTime, TimeSpan duration, string responseCode, bool success, object? properties = null, string? correlationId = null)
        {
            return this.WriteTelemetry(new Telemetry { 
                                                        Type          = Telemetry.TelemetryType.Request, 
                                                        Message       = name,
                                                        CorrelationId = correlationId,
                                                        Properties    = properties,
                                                        Request       = new Telemetry.RequestParams
                                                        {
                                                            StartTime    = startTime,
                                                            Duration     = duration,
                                                            ResponseCode = responseCode,
                                                            Success      = success
                                                        }
                                                    });
        }


        /// <summary>
        /// Write a request to the log
        /// </summary>
        /// <param name="telemetry">AvailabilityTelemetry to write</param>
        public Task WriteAvailability(AvailabilityTelemetry telemetry)
        {
            return this.WriteTelemetry(telemetry);
        }

        #endregion
    }
       
    /*************************************************************************/
    /*************************************************************************/
    public class Telemetry
    {
        public TelemetryType    Type            { get; set; }
        public string           OperationName   { get; set; } = "";
        public string?          CorrelationId   { get; set; } = "";
        public string           Message         { get; set; } = "";
        public Exception?       Exception       { get; set; }
        public object?          Properties      { get; set; }
        public double           Value           { get; set; }
        public LogSeverity      Severity        { get; set; }
        public DateTimeOffset?  Timestamp       { get; set; }

        public IDictionary<string, double>? Metrics { get; set; }
        public RequestParams?               Request { get; set; }

        public enum TelemetryType
        {
            Error,
            Event,
            Metric,
            Trace,
            Request,
            Availability
        }

        public class RequestParams
        {
            public DateTime StartTime    { get; set; } 
            public TimeSpan Duration     { get; set; } 
            public string   ResponseCode { get; set; } = ""; 
            public bool     Success      { get; set; }
        }

        public enum LogSeverity
        {
            Verbose     = 0,
            Information = 1,
            Warning     = 2,
            Error       = 3,
            Critical    = 4
        }
    }  
    
    /*************************************************************************/
    /*************************************************************************/
    public class AvailabilityTelemetry : Telemetry
    {
        public AvailabilityTelemetry()
        {
            this.Type = TelemetryType.Availability;
        }

        public TimeSpan Duration     { get; set; } 
        public string   TestId       { get; set; } = ""; 
        public string   TestName     { get; set; } = ""; 
        public string   RunLocation  { get; set; } = ""; 
        public string   Sequence     { get; set; } = ""; 
        public bool     Success      { get; set; }

    }
}