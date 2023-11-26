/*************************************************************************** 
 *                                                                           
 *    The MondoCore Libraries  							                     
 *                                                                           
 *        Namespace: MondoCore.Common							             
 *             File: ObjectExtensions.cs					    		         
 *        Class(es): ObjectExtensions				         		             
 *          Purpose: Extensions for objects. Note: This code copied from the main MondoCore repository
 *                                                                           
 *  Original Author: Jim Lightfoot                                           
 *    Creation Date: 1 Jan 2020                                              
 *                                                                           
 *   Copyright (c) 2005-2023 - Jim Lightfoot, All rights reserved            
 *                                                                           
 *  Licensed under the MIT license:                                          
 *    http://www.opensource.org/licenses/mit-license.php                     
 *                                                                           
 ****************************************************************************/

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("MondoCore.Log.UnitTests")]

namespace MondoCore.Log
{
    internal static class DictionaryExtensions
    {
        /****************************************************************************/
        internal static IDictionary<K, V> Merge<K, V>(this IDictionary<K, V> dict1, IDictionary<K, V> dict2)
        {
            if(dict2 == null || dict2.Count == 0)
                return dict1;
       
            if(dict1 == null)
                return dict2;
       
            foreach(var kv in dict2)
                dict1[kv.Key] = kv.Value;

            return dict1;
        }    

        /****************************************************************************/
        internal static IDictionary<string, object> MergeData(this IDictionary<string, object> dict, Exception ex)
        {
            if(dict == null)
                dict = new Dictionary<string, object>();

            dict.Merge(ex.Data.ToDictionary());

            if(ex.InnerException != null)
                dict.MergeData(ex.InnerException);

            if(ex is AggregateException aex)
            {
                foreach(var innerException in aex.InnerExceptions)
                    dict.MergeData(innerException);
            }

            return dict;
        }    
    }
}
