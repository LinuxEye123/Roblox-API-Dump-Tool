using System.Collections.Generic;

namespace RobloxApiDumpTool
{
    public sealed class EventDescriptor : MemberDescriptor
    {
        public Security Security;
        public Parameters Parameters;

        public override string GetSchema(bool detailed = true)
        {
            string schema = base.GetSchema();

            if (detailed)
                schema += "{Parameters} {Tags} {Capabilities} {Security} {ThreadSafety}";

            return schema;
        }
    }
}