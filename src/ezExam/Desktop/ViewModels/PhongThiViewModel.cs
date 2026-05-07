using Desktop.Helpers;
using Desktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Desktop.ViewModels
{
    public class RoomEntryDisplay
    {
        public int RoomNumber { get; set; }
        public string Subject { get; set; } = string.Empty;
        public int Count { get; set; }
        public int StartSbd { get; set; }
        public int EndSbd { get; set; }
    }

    public class CandidateRoomDisplay
    {
        public int RoomNumber { get; set; }
        public string Sbd { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public string Lop { get; set; } = string.Empty;
    }

    public class RoomDisplayRow
    {
        public int RoomNumber { get; set; }
        public string SubjectsSummary { get; set; } = string.Empty;
        public int TotalCandidates { get; set; }
        public int Capacity { get; set; }
        public string OccupancyText => $"{TotalCandidates}/{Capacity}";
        public double OccupancyPercent => Capacity <= 0 ? 0 : (double)TotalCandidates / Capacity * 100;
        public string SbdRange { get; set; } = string.Empty;
    }

    public class PhongThiViewModel : BaseViewModel
    {
        private readonly ThiSinhService _thiSinhService;
        private readonly KyThiService _kyThiService;
        private readonly PhongThiAllocator _allocator;
        private readonly DatabaseService _databaseService;

        public ObservableCollection<SubjectCount> SubjectCounts { get; set; } = new();
        public ObservableCollection<RoomEntryDisplay> RoomEntries { get; set; } = new();
        public ObservableCollection<RoomDisplayRow> RoomDisplayRows { get; set; } = new();
        public ObservableCollection<string> AvailableSubjects { get; set; } = new();
        public ObservableCollection<RoomDisplayRow> FilteredRoomRows { get; set; } = new();
        public ObservableCollection<CandidateRoomDisplay> CandidateRoomEntries { get; set; } = new();

        private readonly List<ThiSinh> _lastCandidates = new();
        private readonly Dictionary<string, List<ThiSinh>> _subjectCandidateMap = new(StringComparer.OrdinalIgnoreCase);

        public string KyThiMacDinhText { get; set; } = string.Empty;
        private string _selectedSubject = string.Empty;
        public string SelectedSubject
        {
            get => _selectedSubject;
            set
            {
                _selectedSubject = value ?? string.Empty;
                OnPropertyChanged();
                BuildSubjectRoomDetails();
            }
        }

        private int _roomCapacity = 24;
        public int RoomCapacity
        {
            get => _roomCapacity;
            set { _roomCapacity = value; OnPropertyChanged(); }
        }

        private RoomAllocationStrategy _selectedStrategy = RoomAllocationStrategy.MaxMerge;
        public RoomAllocationStrategy SelectedStrategy
        {
            get => _selectedStrategy;
            set { _selectedStrategy = value; OnPropertyChanged(); }
        }

        public Array Strategies => Enum.GetValues(typeof(RoomAllocationStrategy));

        public int TotalRooms => RoomEntries.Select(x => x.RoomNumber).Distinct().Count();

        public ICommand LoadSubjectStatsCommand { get; }
        public ICommand AllocateCommand { get; }

        // Constructor mặc định để tương thích UI hiện tại.
        public PhongThiViewModel() : this(new ThiSinhService(), new KyThiService(), new PhongThiAllocator(), new DatabaseService())
        {
        }

        // Constructor DI: dễ unit test và dễ thay thế service/allocator theo MVVM.
        public PhongThiViewModel(ThiSinhService thiSinhService, KyThiService kyThiService, PhongThiAllocator allocator, DatabaseService databaseService)
        {
            _thiSinhService = thiSinhService ?? throw new ArgumentNullException(nameof(thiSinhService));
            _kyThiService = kyThiService ?? throw new ArgumentNullException(nameof(kyThiService));
            _allocator = allocator ?? throw new ArgumentNullException(nameof(allocator));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));

            LoadSubjectStatsCommand = new RelayCommand(_ => LoadSubjectStats());
            AllocateCommand = new RelayCommand(_ => Allocate(), _ => SubjectCounts.Any() && RoomCapacity > 0);

            LoadSubjectStats();
        }

        private void LoadSubjectStats()
        {
            SubjectCounts.Clear();
            RoomEntries.Clear();
            RoomDisplayRows.Clear();
            AvailableSubjects.Clear();
            FilteredRoomRows.Clear();
            CandidateRoomEntries.Clear();
            _lastCandidates.Clear();
            _subjectCandidateMap.Clear();

            var kyThi = _kyThiService.GetKyThiMacDinh();
            if (kyThi == null)
            {
                KyThiMacDinhText = "Chưa có kỳ thi mặc định!";
                OnPropertyChanged(nameof(KyThiMacDinhText));
                OnPropertyChanged(nameof(TotalRooms));
                return;
            }

            KyThiMacDinhText = $"Kỳ thi mặc định: {kyThi.TenKyThi} ({kyThi.NgayThi})";
            var thiSinhList = _thiSinhService.GetThiSinhByKyThi(kyThi.Id);
            _lastCandidates.AddRange(thiSinhList);

            var subjectMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var ts in thiSinhList)
            {
                var subjects = (ts.CacMonThi ?? string.Empty)
                    .Split(',')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s));

                foreach (var subject in subjects)
                {
                    if (!subjectMap.ContainsKey(subject)) subjectMap[subject] = 0;
                    subjectMap[subject]++;
                }
            }

            foreach (var item in subjectMap.OrderByDescending(x => x.Value))
            {
                SubjectCounts.Add(new SubjectCount { Subject = item.Key, Count = item.Value });
                AvailableSubjects.Add(item.Key);
            }
            if (AvailableSubjects.Any())
            {
                SelectedSubject = AvailableSubjects.First();
            }

            OnPropertyChanged(nameof(KyThiMacDinhText));
            OnPropertyChanged(nameof(TotalRooms));
        }

        private void Allocate()
        {
            RoomEntries.Clear();
            RoomDisplayRows.Clear();
            FilteredRoomRows.Clear();
            CandidateRoomEntries.Clear();
            _subjectCandidateMap.Clear();
            var kyThi = _kyThiService.GetKyThiMacDinh();
            if (kyThi == null)
            {
                OnPropertyChanged(nameof(TotalRooms));
                return;
            }

            var plans = _allocator.Allocate(SubjectCounts.ToList(), RoomCapacity, SelectedStrategy);
            var persistedRows = new List<RoomAllocationRow>();

            foreach (var room in plans)
            {
                foreach (var entry in room.Entries)
                {
                    persistedRows.Add(new RoomAllocationRow
                    {
                        RoomNumber = room.RoomNumber,
                        Subject = entry.Subject,
                        CandidateCount = entry.Count,
                        StartSbd = entry.StartSbd,
                        EndSbd = entry.EndSbd
                    });

                    RoomEntries.Add(new RoomEntryDisplay
                    {
                        RoomNumber = room.RoomNumber,
                        Subject = entry.Subject,
                        Count = entry.Count,
                        StartSbd = entry.StartSbd,
                        EndSbd = entry.EndSbd
                    });
                }
            }


            foreach (var room in plans)
            {
                var subjectSummary = string.Join(", ", room.Entries.Select(e => $"{e.Subject}: {e.Count}"));
                var minSbd = room.Entries.Min(e => e.StartSbd);
                var maxSbd = room.Entries.Max(e => e.EndSbd);

                RoomDisplayRows.Add(new RoomDisplayRow
                {
                    RoomNumber = room.RoomNumber,
                    SubjectsSummary = subjectSummary,
                    TotalCandidates = room.Total,
                    Capacity = RoomCapacity,
                    SbdRange = $"{minSbd} - {maxSbd}"
                });
            }

            _databaseService.SaveRoomAllocations(kyThi.Id, persistedRows, SelectedStrategy.ToString(), RoomCapacity);
            BuildSubjectRoomDetails();

            OnPropertyChanged(nameof(TotalRooms));
        }

        private void BuildSubjectRoomDetails()
        {
            FilteredRoomRows.Clear();
            CandidateRoomEntries.Clear();
            _subjectCandidateMap.Clear();

            if (string.IsNullOrWhiteSpace(SelectedSubject) || !_lastCandidates.Any() || RoomCapacity <= 0)
                return;

            var orderedCandidates = _lastCandidates
                .Where(ts => CandidateContainsSubject(ts, SelectedSubject))
                .OrderBy(ParseSbd)
                .ThenBy(ts => ts.SBD)
                .ToList();

            if (!orderedCandidates.Any()) return;
            _subjectCandidateMap[SelectedSubject] = orderedCandidates;

            var roomNo = 1;
            for (var i = 0; i < orderedCandidates.Count; i += RoomCapacity)
            {
                var roomCandidates = orderedCandidates.Skip(i).Take(RoomCapacity).ToList();
                var startSbd = roomCandidates.First().SBD;
                var endSbd = roomCandidates.Last().SBD;

                FilteredRoomRows.Add(new RoomDisplayRow
                {
                    RoomNumber = roomNo,
                    SubjectsSummary = SelectedSubject,
                    TotalCandidates = roomCandidates.Count,
                    Capacity = RoomCapacity,
                    SbdRange = $"{startSbd} - {endSbd}"
                });

                foreach (var ts in roomCandidates)
                {
                    CandidateRoomEntries.Add(new CandidateRoomDisplay
                    {
                        RoomNumber = roomNo,
                        Sbd = ts.SBD,
                        HoTen = ts.HoTen,
                        Lop = ts.Lop
                    });
                }

                roomNo++;
            }
        }

        private static bool CandidateContainsSubject(ThiSinh thiSinh, string subject)
        {
            return (thiSinh.CacMonThi ?? string.Empty)
                .Split(',')
                .Select(x => x.Trim())
                .Any(x => x.Equals(subject, StringComparison.OrdinalIgnoreCase));
        }

        private static int ParseSbd(ThiSinh thiSinh)
        {
            if (int.TryParse((thiSinh.SBD ?? string.Empty).Trim(), out var sbd)) return sbd;
            return int.MaxValue;
        }
    }
}
