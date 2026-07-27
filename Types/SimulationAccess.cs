using Newtonsoft.Json.Linq;

namespace RobloxApiDumpTool
{
    public struct SimulationAccess
    {
        public bool Enabled;

        public string Describe(bool isDiff = false)
        {
            if (isDiff && !Enabled)
                return "{🚫None}";
            
            return Enabled ? "{🚀SimAccess}" : "";
        }

        public static implicit operator bool(SimulationAccess access) => access.Enabled;
        public static implicit operator SimulationAccess(bool enabled) => new SimulationAccess { Enabled = enabled };

        public SimulationAccess(bool enabled)
        {
            Enabled = enabled;
        }

        public override string ToString() => Describe();
        public string Value => Describe();
    }
}