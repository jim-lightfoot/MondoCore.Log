﻿/*************************************************************************** 
 *                                                                           
 *    The MondoCore Libraries  							                     
 *                                                                           
 *        Namespace: MondoCore.Log							             
 *             File: ObjectExtensions.cs					    		         
 *        Class(es): ObjectExtensions				         		             
 *          Purpose: Extensions for objects. Note: This code copied from the main MondoCore repository
 *                                                                           
 *  Original Author: Jim Lightfoot                                           
 *    Creation Date: 1 Jan 2020                                              
 *                                                                           
 *   Copyright (c) 2005-2024 - Jim Lightfoot, All rights reserved            
 *                                                                           
 *  Licensed under the MIT license:                                          
 *    http://www.opensource.org/licenses/mit-license.php                     
 *                                                                           
 ****************************************************************************/

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("MondoCore.Log.UnitTests")]

namespace MondoCore.Log
{
    internal static class ObjectExtensions
    {
        /// <summary>
        /// Translates an object into a dictionary
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal static IDictionary<string, object>? ToDictionary(this object obj)
        {
            if (obj == null)
                return null;

            if (obj is IDictionary<string, object> dict)
                return dict;

            var result = new Dictionary<string, object>();

            if(obj is IDictionary dict2)
            {
                foreach(var key in dict2.Keys)
                    result.Add(key.ToString(), dict2[key]);
            }
            else if(obj is IEnumerable list)
            {
                foreach(var val in list)
                    result.Add(val.ToString(), val);
            }
            else
            {
                // Get public and instance properties only
                var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where( p=> p.CanRead );

                foreach(var property in properties)
                {
                    try
                    { 
                        var value = property.GetGetMethod().Invoke(obj, null);

                        // Add property name and value to dictionary
                        if (value != null)
                            result.Add(property.Name, value);
                    }
                    catch
                    {
                        // Just ignore it
                    }
                }
            }

            return result;
        }
    }
}
