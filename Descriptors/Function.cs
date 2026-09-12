using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace RobloxApiDumpTool
{
    public sealed class FunctionDescriptor : MemberDescriptor
    {
        [JsonIgnore]
        public LuaType ReturnType;

        [JsonProperty("ReturnType")]
        internal JToken JsonReturnType
        {
            get => null;

            set
            {
                if (value.Type == JTokenType.Array)
                {
                    ReturnType = new LuaType()
                    {
                        SubTypes = value.ToObject<LuaType[]>(),
                        Category = TypeCategory.Group,
                        Name = "Tuple",
                    };

                    return;
                }

                ReturnType = value.ToObject<LuaType>();
            }
        }

        public Security Security;
        public Parameters Parameters;
        public SimulationAccess SimulationAccess = false;

        public override string GetSchema(bool detailed = true)
        {
            string schema = base.GetSchema(detailed)
                .Replace(".", ":");

            if (detailed)
                schema += "{Parameters} -> {ReturnType} {Tags} {Capabilities} {Security} {ThreadSafety} {SimulationAccess}";

            return schema;
        }
    }
}