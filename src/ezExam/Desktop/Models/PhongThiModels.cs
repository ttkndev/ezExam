using System.Collections.Generic;
using System.Linq;

namespace Desktop.Models
{
    public enum RoomAllocationStrategy { MaxMerge, SequentialSbd }
    public enum ExamSession { MandatoryVan, MandatoryToan, OptionalCa1, OptionalCa2 }

    public class SubjectCount { public string Subject { get; set; } = string.Empty; public int Count { get; set; } }
    public class RoomEntry { public string Subject { get; set; } = string.Empty; public int Count { get; set; } public int StartSbd { get; set; } public int EndSbd { get; set; } }
    public class RoomPlan { public int RoomNumber { get; set; } public List<RoomEntry> Entries { get; set; } = new(); public int Total => Entries.Sum(e => e.Count); }
    public class RoomAllocationRow { public int RoomNumber { get; set; } public string Subject { get; set; } = string.Empty; public int CandidateCount { get; set; } public int StartSbd { get; set; } public int EndSbd { get; set; } }

    public class RoomEntryDisplay { public int RoomNumber { get; set; } public string Subject { get; set; } = string.Empty; public int Count { get; set; } public int StartSbd { get; set; } public int EndSbd { get; set; } }
    public class CandidateRoomDisplay { public int RoomNumber { get; set; } public string Subject { get; set; } = string.Empty; public string Sbd { get; set; } = string.Empty; public string HoTen { get; set; } = string.Empty; public string Lop { get; set; } = string.Empty; }
    public class RoomDisplayRow { public int RoomNumber { get; set; } public string SubjectsSummary { get; set; } = string.Empty; public int TotalCandidates { get; set; } public int Capacity { get; set; } public string OccupancyText => $"{TotalCandidates}/{Capacity}"; public double OccupancyPercent => Capacity <= 0 ? 0 : (double)TotalCandidates / Capacity * 100; public string SbdRange { get; set; } = string.Empty; }
}
