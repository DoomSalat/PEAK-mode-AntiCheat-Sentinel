using System;
using System.IO;
using System.Text;

namespace PeakSentinel.Reporting
{
    public sealed class ReportExport
    {
        private const string FileFormat = "PeakSentinel-report-{0:yyyyMMdd-HHmmss}.txt";
        private const string LineFormat = "[{0:HH:mm:ss}] {1,-10} {2,-8} {3} (actor {4}): {5}";
        private const string RepeatsFormat = " x{0} (last {1:HH:mm:ss})";
        private const string HeaderFormat = "Peak Sentinel report, {0:yyyy-MM-dd HH:mm:ss}, room {1}, {2} findings";

        private readonly string _folder;
        private readonly FindingLog _findings;

        public ReportExport(string folder, FindingLog findings)
        {
            _folder = folder;
            _findings = findings;
        }

        public string Save(string room)
        {
            DateTime now = DateTime.Now;
            StringBuilder text = new StringBuilder();
            text.AppendLine(string.Format(HeaderFormat, now, room, _findings.Count));

            foreach (FindingLog.Finding finding in _findings.Findings)
            {
                text.Append(string.Format(LineFormat, finding.Time, finding.Kind, finding.Category, finding.Name, finding.Actor, finding.Text));

                if (finding.Repeats > 1)
                    text.Append(string.Format(RepeatsFormat, finding.Repeats, finding.LastSeen));

                text.AppendLine();
            }

            string path = Path.Combine(_folder, string.Format(FileFormat, now));
            File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
            return path;
        }
    }
}