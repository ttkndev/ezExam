using Desktop.Helpers;
using Desktop.Models;
using Desktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Desktop.ViewModels
{
    public class PhongThiViewModel : BaseViewModel
    {
        private readonly ThiSinhService _thiSinhService;
        private readonly KyThiService _kyThiService;
        private readonly PhongThiAllocator _allocator;
        private readonly PhongThiXepLichService _phongThiXepLichService;

        public ObservableCollection<SubjectCount> SubjectCounts { get; set; } = new();
        public ObservableCollection<RoomEntryDisplay> RoomEntries { get; set; } = new();
        public ObservableCollection<RoomDisplayRow> RoomDisplayRows { get; set; } = new();
        public ObservableCollection<string> AvailableSubjects { get; set; } = new();
        public ObservableCollection<RoomDisplayRow> FilteredRoomRows { get; set; } = new();
        public ObservableCollection<CandidateRoomDisplay> CandidateRoomEntries { get; set; } = new();

        private readonly List<ThiSinh> _lastCandidates = new();

        public string KyThiMacDinhText { get; set; } = string.Empty;
        public int SubjectCountVan => SubjectCounts.FirstOrDefault(x => x.Subject.Equals("Văn", StringComparison.OrdinalIgnoreCase))?.Count ?? 0;
        public int SubjectCountToan => SubjectCounts.FirstOrDefault(x => x.Subject.Equals("Toán", StringComparison.OrdinalIgnoreCase))?.Count ?? 0;
        public int SubjectCountCa1 => _lastCandidates.Count(x => !string.IsNullOrWhiteSpace(x.MonCa1));
        public int SubjectCountCa2 => _lastCandidates.Count(x => !string.IsNullOrWhiteSpace(x.MonCa2));

        private string _selectedSubject = string.Empty;
        public string SelectedSubject
        {
            get => _selectedSubject;
            set { _selectedSubject = value ?? string.Empty; OnPropertyChanged(); BuildSubjectRoomDetails(); }
        }

        private int _roomCapacity = 24;
        public int RoomCapacity { get => _roomCapacity; set { _roomCapacity = value; OnPropertyChanged(); } }

        private RoomAllocationStrategy _selectedStrategy = RoomAllocationStrategy.MaxMerge;
        public RoomAllocationStrategy SelectedStrategy { get => _selectedStrategy; set { _selectedStrategy = value; OnPropertyChanged(); } }

        public Array Strategies => Enum.GetValues(typeof(RoomAllocationStrategy));
        public int TotalRooms => RoomEntries.Select(x => x.RoomNumber).Distinct().Count();

        public ICommand LoadSubjectStatsCommand { get; }
        public ICommand AllocateCommand { get; }

        public PhongThiViewModel() : this(new ThiSinhService(), new KyThiService(), new PhongThiAllocator(), new PhongThiXepLichService()) { }

        public PhongThiViewModel(ThiSinhService thiSinhService, KyThiService kyThiService, PhongThiAllocator allocator, PhongThiXepLichService phongThiXepLichService)
        {
            _thiSinhService = thiSinhService;
            _kyThiService = kyThiService;
            _allocator = allocator;
            _phongThiXepLichService = phongThiXepLichService;
            LoadSubjectStatsCommand = new RelayCommand(_ => LoadSubjectStats());
            AllocateCommand = new RelayCommand(_ => Allocate(), _ => SubjectCounts.Any() && RoomCapacity > 0);
            LoadSubjectStats();
        }

        private void LoadSubjectStats()
        {
            SubjectCounts.Clear(); RoomEntries.Clear(); RoomDisplayRows.Clear(); FilteredRoomRows.Clear(); CandidateRoomEntries.Clear(); _lastCandidates.Clear();
            AvailableSubjects.Clear();

            var kyThi = _kyThiService.GetKyThiMacDinh();
            if (kyThi == null) { KyThiMacDinhText = "Chưa có kỳ thi mặc định!"; OnPropertyChanged(nameof(KyThiMacDinhText)); return; }

            KyThiMacDinhText = $"Kỳ thi mặc định: {kyThi.TenKyThi} ({kyThi.NgayThi})";
            var thiSinhList = _thiSinhService.GetThiSinhByKyThi(kyThi.Id);
            _lastCandidates.AddRange(thiSinhList);

            var subjectMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var ts in thiSinhList)
            foreach (var subject in (ts.CacMonThi ?? string.Empty).Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                if (!subjectMap.ContainsKey(subject)) subjectMap[subject] = 0;
                subjectMap[subject]++;
            }
            foreach (var item in subjectMap.OrderByDescending(x => x.Value)) SubjectCounts.Add(new SubjectCount { Subject = item.Key, Count = item.Value });

            AvailableSubjects.Add("Văn"); AvailableSubjects.Add("Toán"); AvailableSubjects.Add("Ca 1"); AvailableSubjects.Add("Ca 2");
            SelectedSubject = AvailableSubjects.FirstOrDefault() ?? string.Empty;
            OnPropertyChanged(nameof(KyThiMacDinhText)); OnPropertyChanged(nameof(TotalRooms));
            OnPropertyChanged(nameof(SubjectCountVan)); OnPropertyChanged(nameof(SubjectCountToan)); OnPropertyChanged(nameof(SubjectCountCa1)); OnPropertyChanged(nameof(SubjectCountCa2));
        }

        private void Allocate()
        {
            RoomEntries.Clear(); RoomDisplayRows.Clear(); FilteredRoomRows.Clear(); CandidateRoomEntries.Clear();
            var kyThi = _kyThiService.GetKyThiMacDinh(); if (kyThi == null) return;
            var plans = _allocator.Allocate(SubjectCounts.ToList(), RoomCapacity, SelectedStrategy);
            var persistedRows = new List<RoomAllocationRow>();
            foreach (var room in plans)
            foreach (var entry in room.Entries)
            {
                persistedRows.Add(new RoomAllocationRow { RoomNumber = room.RoomNumber, Subject = entry.Subject, CandidateCount = entry.Count, StartSbd = entry.StartSbd, EndSbd = entry.EndSbd });
                RoomEntries.Add(new RoomEntryDisplay { RoomNumber = room.RoomNumber, Subject = entry.Subject, Count = entry.Count, StartSbd = entry.StartSbd, EndSbd = entry.EndSbd });
            }
            _phongThiXepLichService.SaveRoomAllocations(kyThi.Id, persistedRows, SelectedStrategy.ToString(), RoomCapacity);
            BuildSubjectRoomDetails();
            OnPropertyChanged(nameof(TotalRooms));
        }

        private void BuildSubjectRoomDetails()
        {
            FilteredRoomRows.Clear(); CandidateRoomEntries.Clear();
            if (string.IsNullOrWhiteSpace(SelectedSubject) || !_lastCandidates.Any() || RoomCapacity <= 0) return;
            if (SelectedSubject.Equals("Ca 1", StringComparison.OrdinalIgnoreCase) || SelectedSubject.Equals("Ca 2", StringComparison.OrdinalIgnoreCase)) { BuildOptionalSessionDetails(SelectedSubject); return; }
            var fixedSubject = SelectedSubject.Equals("Văn", StringComparison.OrdinalIgnoreCase) ? "Văn" : "Toán";
            var ordered = _lastCandidates.Where(ts => CandidateContainsSubject(ts, fixedSubject)).OrderBy(ParseSbd).ThenBy(ts => ts.SBD).ToList();
            if (ordered.Any()) PopulateSequentialRooms(ordered, fixedSubject);
        }

        private void BuildOptionalSessionDetails(string sessionName)
        {
            var ca1 = sessionName.Equals("Ca 1", StringComparison.OrdinalIgnoreCase);
            var sessionCandidates = _lastCandidates.Where(ts => !string.IsNullOrWhiteSpace(ca1 ? ts.MonCa1 : ts.MonCa2)).OrderBy(ParseSbd).ThenBy(ts => ts.SBD).ToList();
            var roomNo = 1;
            foreach (var grp in sessionCandidates.GroupBy(ts => ca1 ? ts.MonCa1.Trim() : ts.MonCa2.Trim(), StringComparer.OrdinalIgnoreCase).OrderBy(g => g.Key))
            {
                var candidates = grp.ToList();
                for (var i = 0; i < candidates.Count; i += RoomCapacity)
                {
                    var roomCandidates = candidates.Skip(i).Take(RoomCapacity).ToList();
                    FilteredRoomRows.Add(new RoomDisplayRow { RoomNumber = roomNo, SubjectsSummary = grp.Key, TotalCandidates = roomCandidates.Count, Capacity = RoomCapacity, SbdRange = $"{roomCandidates.First().SBD} - {roomCandidates.Last().SBD}" });
                    foreach (var ts in roomCandidates) CandidateRoomEntries.Add(new CandidateRoomDisplay { RoomNumber = roomNo, Subject = grp.Key, Sbd = ts.SBD, HoTen = ts.HoTen, Lop = ts.Lop });
                    roomNo++;
                }
            }
        }

        private void PopulateSequentialRooms(List<ThiSinh> orderedCandidates, string subject)
        {
            var roomNo = 1;
            for (var i = 0; i < orderedCandidates.Count; i += RoomCapacity)
            {
                var roomCandidates = orderedCandidates.Skip(i).Take(RoomCapacity).ToList();
                FilteredRoomRows.Add(new RoomDisplayRow { RoomNumber = roomNo, SubjectsSummary = subject, TotalCandidates = roomCandidates.Count, Capacity = RoomCapacity, SbdRange = $"{roomCandidates.First().SBD} - {roomCandidates.Last().SBD}" });
                foreach (var ts in roomCandidates) CandidateRoomEntries.Add(new CandidateRoomDisplay { RoomNumber = roomNo, Subject = subject, Sbd = ts.SBD, HoTen = ts.HoTen, Lop = ts.Lop });
                roomNo++;
            }
        }

        private static bool CandidateContainsSubject(ThiSinh thiSinh, string subject) => (thiSinh.CacMonThi ?? string.Empty).Split(',').Select(x => x.Trim()).Any(x => x.Equals(subject, StringComparison.OrdinalIgnoreCase));
        private static int ParseSbd(ThiSinh thiSinh) => int.TryParse((thiSinh.SBD ?? string.Empty).Trim(), out var sbd) ? sbd : int.MaxValue;
    }
}
