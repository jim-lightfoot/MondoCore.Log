﻿/***************************************************************************
 *                                                                          
 *    The MondoCore Libraries  						                        
 *                                                                          
 *        Namespace: MondoCore.Log 			            
 *             File: RequestLog.cs							
 *        Class(es): RequestLog							    
 *          Purpose: Logging during a single request            
 *                                                                          
 *  Original Author: Jim Lightfoot                                          
 *    Creation Date: 8 Aug 2020                                            
 *                                                                          
 *   Copyright (c) 2020-2023 - Jim Lightfoot, All rights reserved                
 *                                                                          
 *  Licensed under the MIT license:                                         
 *    http://www.opensource.org/licenses/mit-license.php                    
 *                                                                          
 ****************************************************************************/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MondoCore.Log
{
    /*************************************************************************/
    /*************************************************************************/
    /// <summary>
    /// Create a log request. Log requests contain request specific data like a 
    ///    correlation id and custom properties that will be logged on all log entries
    /// </summary>
    public class RequestLog : IRequestLog
    {
        private readonly ILog           _log;
        private readonly IDisposable?   _operation;
        private readonly IDictionary<string, object> _properties = new Dictionary<string, object>();
        private readonly string?        _correlationId;
        private readonly string?        _operationName;
                                
        /*************************************************************************/
        public RequestLog(ILog log, string? operationName = null, string? correlationId = null)
        {
            _log = log;

            _correlationId = string.IsNullOrWhiteSpace(correlationId) ? Guid.NewGuid().ToString().ToLower() : correlationId;
            _operationName = operationName;

            if(!string.IsNullOrWhiteSpace(operationName))
                _operation = log.StartOperation(operationName);
            else
                _operation = null;
        }

        /*************************************************************************/
        public IRequestLog NewRequest(string? operationName = null, string? correlationId = null)
        {
            if(string.IsNullOrWhiteSpace(operationName))
                operationName = _operationName;

            if(string.IsNullOrWhiteSpace(correlationId))
                correlationId = _correlationId;

            return new RequestLog(this, operationName, correlationId);
        }

        /*************************************************************************/
        public void SetProperty(string name, object value)
        {
            _properties[name] = value;
        }

        /*************************************************************************/
        public Task WriteTelemetry(Telemetry telemetry)
        {
            if(_properties.Count > 0)
                telemetry.Properties = telemetry?.Properties?.ToDictionary()?.Merge(_properties);

            telemetry.CorrelationId = string.IsNullOrWhiteSpace(telemetry.CorrelationId) ? _correlationId : telemetry.CorrelationId;
            telemetry.OperationName = string.IsNullOrWhiteSpace(telemetry.OperationName) ? _operationName : telemetry.OperationName;

            return _log.WriteTelemetry(telemetry);
        }

        /*************************************************************************/
        public IDisposable StartOperation(string operationName)
        {
           return _log.StartOperation(operationName);
        }
        
        #region IDisposable

        public void Dispose()
        {
            _operation?.Dispose();
        }

        #endregion

    }
}