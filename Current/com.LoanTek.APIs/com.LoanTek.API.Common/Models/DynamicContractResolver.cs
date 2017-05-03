using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace com.LoanTek.API.Common.Models
{
    /// <summary>
    /// Use to limit or remove unwanted Json properties / data
    /// </summary>
    public class DynamicContractResolver<T,TT> : DefaultContractResolver
    {
        private IList<string> _propertiesToSerialize;

        public DynamicContractResolver(IList<string> propertiesToSerialize)
        {
            _propertiesToSerialize = propertiesToSerialize;
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);
            if (type == typeof(T) || type.GetInterfaces().Any(x => x == typeof(TT)))
                return properties.Where(p => _propertiesToSerialize.Contains(p.PropertyName)).ToList();
            return properties;
        }
    }
}