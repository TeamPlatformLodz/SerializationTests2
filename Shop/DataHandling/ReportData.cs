using System;

namespace Shop
{
    public class ReportData
    {
        public DateTime LastCombinedReportDate { get; set; } = DateTime.MinValue;
        public DateTime LastChangeTime { get; set; } = DateTime.MinValue;
        public string LastCombinedReport { get; set; }

        public bool IsReportOutdated()
        {
            return LastCombinedReportDate <= LastChangeTime;
        }
        public void UpdateReportDate()
        {
            LastCombinedReportDate = DateTime.Now;
        }
    }
}