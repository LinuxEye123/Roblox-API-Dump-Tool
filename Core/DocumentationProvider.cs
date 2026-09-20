using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RobloxApiDumpTool
{
    public static class DocumentationProvider
    {
        private const string BaseUrl =
            "https://raw.githubusercontent.com/Roblox/creator-docs/main/content/en-us/reference/engine/classes/";
        private const string RenderedBaseUrl =
            "https://create.roblox.com/docs/reference/engine/classes/";
        private const int RequestDelayMilliseconds = 200;

        public static async Task EnrichAsync(ReflectionDatabase database)
        {
            if (database == null || database.Classes == null)
                return;

            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.UserAgent] = "Roblox-API-Dump-Tool";
                string cacheDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RobloxApiDumpFiles", "Documentation");
                Directory.CreateDirectory(cacheDirectory);

                foreach (ClassDescriptor classDescriptor in database.Classes.Values)
                {
                    try
                    {
                        string cachePath = Path.Combine(cacheDirectory, classDescriptor.Name + ".yaml");
                        string yaml = await DownloadOrReadCachedAsync(
                            client,
                            BaseUrl + Uri.EscapeDataString(classDescriptor.Name) + ".yaml",
                            cachePath);
                        ApplyYaml(classDescriptor, yaml);

                        string markdown = await DownloadOrReadCachedAsync(
                            client,
                            RenderedBaseUrl + Uri.EscapeDataString(classDescriptor.Name) + ".md",
                            Path.Combine(cacheDirectory, classDescriptor.Name + ".md"));
                        ApplyCodeSamples(classDescriptor, markdown);
                        await Task.Delay(RequestDelayMilliseconds);
                    }

                    catch (WebException)
                    {
                        // Documentation is supplemental; an unavailable page must not stop a dump.
                    }
                }

            }
        }

        private static async Task<string> DownloadOrReadCachedAsync(
            WebClient client, string url, string cachePath)
        {
            if (File.Exists(cachePath))
                return File.ReadAllText(cachePath);

            string content = await client.DownloadStringTaskAsync(url);
            File.WriteAllText(cachePath, content);
            return content;
        }

        private static void ApplyCodeSamples(ClassDescriptor classDescriptor, string markdown)
        {
            foreach (MemberDescriptor member in classDescriptor.Members)
            {
                string heading = "### Method: " + classDescriptor.Name + ":" + member.Name;
                int start = markdown.IndexOf(heading, StringComparison.Ordinal);
                if (start < 0)
                    continue;

                int end = markdown.IndexOf("\n### ", start + heading.Length, StringComparison.Ordinal);
                if (end < 0)
                    end = markdown.Length;

                string section = markdown.Substring(start, end - start);
                Match sample = Regex.Match(section, @"```(?:lua|luau)\s*\r?\n(?<code>.*?)(?:\r?\n)```",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (!sample.Success)
                    continue;

                string code = sample.Groups["code"].Value.Trim();
                if (code.Length == 0)
                    continue;

                member.Documentation = member.Documentation.TrimEnd()
                    + Environment.NewLine + Environment.NewLine
                    + "Code Sample" + Environment.NewLine
                    + "```lua" + Environment.NewLine
                    + code + Environment.NewLine + "```";
            }
        }

        private static void ApplyYaml(ClassDescriptor classDescriptor, string yaml)
        {
            classDescriptor.Documentation = ReadBlock(yaml, "description:", 0);

            int methodsStart = yaml.IndexOf("\nmethods:", StringComparison.Ordinal);
            if (methodsStart < 0)
                return;

            string methods = yaml.Substring(methodsStart);
            foreach (MemberDescriptor member in classDescriptor.Members)
            {
                string marker = "\n  - name: " + classDescriptor.Name + ":" + member.Name;
                int start = methods.IndexOf(marker, StringComparison.Ordinal);
                if (start < 0)
                    continue;

                int end = methods.IndexOf("\n  - name: ", start + marker.Length, StringComparison.Ordinal);
                if (end < 0)
                    end = methods.Length;

                string methodYaml = methods.Substring(start, end - start);
                string summary = ReadBlock(methodYaml, "summary:", 4);
                string description = ReadBlock(methodYaml, "description:", 4);
                member.Documentation = JoinBlocks(summary, description);
            }
        }

        private static string ReadBlock(string text, string key, int minimumIndent)
        {
            string marker = new string(' ', minimumIndent) + key;
            int keyIndex = text.IndexOf(marker, StringComparison.Ordinal);
            if (keyIndex < 0)
                return "";

            int lineEnd = text.IndexOf('\n', keyIndex);
            if (lineEnd < 0)
                return "";

            var lines = new List<string>();
            string first = text.Substring(keyIndex + marker.Length, lineEnd - keyIndex - marker.Length).Trim();
            if (first.Length > 0 && first != "|")
                lines.Add(Unquote(first));

            int position = lineEnd + 1;
            while (position < text.Length)
            {
                int nextEnd = text.IndexOf('\n', position);
                if (nextEnd < 0)
                    nextEnd = text.Length;

                string line = text.Substring(position, nextEnd - position).TrimEnd('\r');
                int indent = line.TakeWhile(c => c == ' ').Count();
                if (line.Length > 0 && indent <= minimumIndent)
                    break;

                lines.Add(indent > minimumIndent
                    ? line.Substring(Math.Min(minimumIndent + 2, line.Length))
                    : "");

                position = nextEnd + 1;
            }

            while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
                lines.RemoveAt(0);
            while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[lines.Count - 1]))
                lines.RemoveAt(lines.Count - 1);

            return string.Join(Environment.NewLine, lines);
        }

        private static string JoinBlocks(string summary, string description)
        {
            if (string.IsNullOrWhiteSpace(summary))
                return description;
            if (string.IsNullOrWhiteSpace(description) ||
                string.Equals(summary.Trim(), description.Trim(), StringComparison.Ordinal))
                return summary;
            return summary.Trim() + Environment.NewLine + Environment.NewLine + description.Trim();
        }

        private static string Unquote(string value)
        {
            if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                return value.Substring(1, value.Length - 2);
            return value;
        }
    }
}
