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

    public class PhongThiViewModel : BaseViewModel
    {
        private readonly ThiSinhService _thiSinhService;
        private readonly KyThiService _kyThiService;
        private readonly PhongThiAllocator _allocator;

        public ObservableCollection<SubjectCount> SubjectCounts { get; set; } = new();
        public ObservableCollection<RoomEntryDisplay> RoomEntries { get; set; } = new();

        public string KyThiMacDinhText { get; set; } = string.Empty;

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

        public PhongThiViewModel()
        {
            _thiSinhService = new ThiSinhService();
            _kyThiService = new KyThiService();
            _allocator = new PhongThiAllocator();

            LoadSubjectStatsCommand = new RelayCommand(_ => LoadSubjectStats());
            AllocateCommand = new RelayCommand(_ => Allocate(), _ => SubjectCounts.Any() && RoomCapacity > 0);

            LoadSubjectStats();
        }

        private void LoadSubjectStats()
        {
            SubjectCounts.Clear();
            RoomEntries.Clear();

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
            }

            OnPropertyChanged(nameof(KyThiMacDinhText));
            OnPropertyChanged(nameof(TotalRooms));
        }

        private void Allocate()
        {
            RoomEntries.Clear();
            var plans = _allocator.Allocate(SubjectCounts.ToList(), RoomCapacity, SelectedStrategy);

            foreach (var room in plans)
            {
                foreach (var entry in room.Entries)
                {
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

            OnPropertyChanged(nameof(TotalRooms));
        }
    }
}
