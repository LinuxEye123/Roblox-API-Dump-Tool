using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RobloxApiDumpTool
{
    public class ReflectionDumper
    {
        public ReflectionDatabase Database { get; private set; }
        
        public delegate void SignatureWriter(ReflectionDumper buffer, Descriptor desc, int numTabs = 0);
        public delegate string DumpPostProcesser(string result, string workDir = "");

        public readonly StringBuilder Builder = new StringBuilder();
        public static readonly ReflectionHtml Html = new ReflectionHtml();

        public ReflectionDumper(ReflectionDatabase database = null)
        {
            Database = database;
        }

        public string ExportResults(DumpPostProcesser postProcess = null)
        {
            if (Html.HasElements)
                Html.WriteTo(Builder);

            string result = Builder.ToString();
            string post = postProcess?.Invoke(result);

            if (post != null)
                result = post;

            return result;
        }

        private static List<T> Sorted<T>(List<T> list)
        {
            return list
                .OrderBy(elem => elem)
                .ToList();
        }
        
        public void Write(object text)
        {
            Builder.Append(text);
        }

        public void NextLine(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                Write("\r\n");
            }
        }

        public void Tab(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                Write('\t');
            }
        }

        public static SignatureWriter DumpUsingTxt = (buffer, desc, numTabs) =>
        {
            buffer.Tab(numTabs);
            buffer.Write(desc.Signature);
            desc.WriteDocumentation(buffer, numTabs);
        };

        public static SignatureWriter DumpUsingHtml = (buffer, desc, numTabs) =>
        {
            desc.WriteHtml(Html);
        };

        public string DumpApi(SignatureWriter WriteSignature, DumpPostProcesser postProcess = null)
        {
            if (Database == null)
                throw new Exception("Cannot Dump API without a ReflectionDatabase provided.");

            Builder.Clear();
            Html.Clear();
            bool hasEntry = false;

            foreach (ClassDescriptor classDesc in Sorted(Database.Classes.Values.ToList()))
            {
                if (hasEntry)
                    NextLine(2);

                WriteSignature(this, classDesc, 0);
                hasEntry = true;

                foreach (MemberDescriptor memberDesc in Sorted(classDesc.Members))
                {
                    NextLine();
                    WriteSignature(this, memberDesc, 1);
                }
            }

            foreach (EnumDescriptor enumDesc in Sorted(Database.Enums.Values.ToList()))
            {
                if (hasEntry)
                    NextLine(2);

                WriteSignature(this, enumDesc, 0);
                hasEntry = true;

                foreach (EnumItemDescriptor itemDesc in Sorted(enumDesc.Items))
                {
                    NextLine();
                    WriteSignature(this, itemDesc, 1);
                }
            }

            return ExportResults(postProcess);
        }
    }
}