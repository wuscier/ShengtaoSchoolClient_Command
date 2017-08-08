using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CSharp.RuntimeBinder;

namespace JsonConfig
{
    public static class JsonConfigExtensions
    {
        public static T GetMappedValue<T>(this IEnumerable<dynamic> list, string key, T defaultValue = default(T))
        {
            T result = defaultValue;
            var match = list.SingleOrDefault(item => item.Key == key);
            if (match != null)
            {
                try
                {
                    //Try converting manually a string to int array if that is the types specified.
                    if (typeof(T) == typeof(int[]))
                    {
                        string[] strTokens = match.Value.ToString().Split(",");
                        var convertedVal = strTokens?.Select(int.Parse).ToArray();
                        result = (dynamic)convertedVal;
                    }
                    else //Just try the normal assignment.
                    {
                        result = match.Value;
                    }
                }
                catch (RuntimeBinderException)
                {
                    //Occurs if the value is not directly convertible to the default type. Attempt the IConvertible method of casting instead.
                    result = Convert.ChangeType(match.Value, typeof(T));
                }

            }

            return result;
        }
    }
}
