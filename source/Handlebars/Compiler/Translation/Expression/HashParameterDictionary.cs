using System;
using System.Collections.Generic;

namespace HandlebarsDotNet.Compiler
{
    public class HashParameterDictionary : Dictionary<string, object>
    {
        internal static readonly IReadOnlyDictionary<string, object> Empty = new HashParameterDictionary();
        
        public HashParameterDictionary()
            :base(StringComparer.OrdinalIgnoreCase)
        {
            
        }
    }
}