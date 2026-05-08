using ezExam.App.ViewModels.Base;
using ezExam.Core.Models;
using ezExam.Core.Services;
using ezExam.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ezExam.App.ViewModels
{
    /// <summary>
    /// ViewModel quản lý phòng thi:
    /// - Xếp phòng thi (gọi RoomSchedulerService)
    /// - Hiển thị 4 tab: Văn, Toán, Ca 1, Ca 2
    /// - Mỗi tab: danh sách phòng bên trái, danh sách TS phòng được chọn bên phải
    /// </summary>
    public class RoomViewModel : BaseViewModel
    {
        private readonly IRoomSchedulerService _roomScheduler;
        private readonly IShiftSchedulerService _shiftScheduler;
        private readonly ExamRoomRepository _roomRepo;

        private ExamSession? _currentSession;

        // --- Kết quả xếp ca (cần có trước khi xếp phòng) ---
        private ShiftScheduleResult? _shiftResult;
        public ShiftScheduleResult? ShiftResult
        {
            get => _shiftResult;
            set => SetProperty(ref _shiftResult, value);
        }

        // --- Tab đang chọn: 0=Văn, 1=Toán, 2=Ca1, 3=Ca2 ---
        private int _selectedTab;
        public int SelectedTab
        {
            get => _selectedTab;
            set
            {
                SetProperty(ref _selectedTab, value);
                RefreshCurrentTabRooms();
            }
        }

        // --- Danh sách phòng theo từng loại ---
        public ObservableCollection<ExamRoom> LiteratureRooms { get; } = new();
        public ObservableCollection<ExamRoom> MathRooms { get; } = new();
        public ObservableCollection<ExamRoom> Shift1Rooms { get; } = new();
        public ObservableCollection<ExamRoom> Shift2Rooms { get; } = new();

        // --- Phòng đang chọn ---
        private ExamRoom? _selectedRoom;
        public ExamRoom? SelectedRoom
        {
            get => _selectedRoom;
            set
            {
                SetProperty(ref _selectedRoom, value);
                LoadRoomCandidates(value);
                OnPropertyChanged(nameof(HasSelectedRoom));
                OnPropertyChanged(nameof(SelectedRoomTitle));
            }
        }

        public bool HasSelectedRoom => _selectedRoom != null;

        public string SelectedRoomTitle => _selectedRoom == null
            ? "Chọn phòng để xem danh sách"
            : $"{_selectedRoom.RoomName} — {_selectedRoom.SubjectNames} ({_selectedRoom.Assignments?.Count ?? 0} TS)";

        // --- Danh sách TS trong phòng đang chọn ---
        public ObservableCollection<RoomAssignment> RoomCandidates { get; } = new();

        // --- Tổng kết từng loại phòng ---
        public string LiteratureSummary => BuildSummary(LiteratureRooms);
        public string MathSummary => BuildSummary(MathRooms);
        public string Shift1Summary => BuildSummary(Shift1Rooms);
        public string Shift2Summary => BuildSummary(Shift2Rooms);

        // --- Trạng thái ---
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isStatusError;
        public bool IsStatusError
        {
            get => _isStatusError;
            set => SetProperty(ref _isStatusError, value);
        }

        // --- Sức chứa phòng (mặc định 24) ---
        private int _roomCapacity = 24;
        public int RoomCapacity
        {
            get => _roomCapacity;
            set => SetProperty(ref _roomCapacity, value);
        }

        // --- Commands ---
        public RelayCommand ScheduleRoomsCommand { get; }
        public RelayCommand RefreshCommand { get; }

        public RoomViewModel(
            IRoomSchedulerService roomScheduler,
            IShiftSchedulerService shiftScheduler,
            ExamRoomRepository roomRepo)
        {
            _roomScheduler = roomScheduler;
            _shiftScheduler = shiftScheduler;
            _roomRepo = roomRepo;

            ScheduleRoomsCommand = new RelayCommand(_ => ScheduleRoomsAsync());
            RefreshCommand = new RelayCommand(_ => LoadAllRoomsAsync());
        }

        /// <summary>Được gọi từ MainViewModel khi đổi kỳ thi</summary>
        public void SetSession(ExamSession? session)
        {
            _currentSession = session;
            _shiftResult = null;
            SelectedRoom = null;
            ClearAllRooms();
            StatusMessage = "";

            if (session != null)
                _ = LoadAllRoomsAsync();
        }

        // -------------------------------------------------------------------------

        /// <summary>Tải toàn bộ phòng thi từ database</summary>
        private async Task LoadAllRoomsAsync()
        {
            if (_currentSession == null) return;

            IsBusy = true;
            try
            {
                ClearAllRooms();

                var lit = await _roomRepo.GetBySessionAndTypeAsync(
                    _currentSession.Id, RoomType.Literature);
                var math = await _roomRepo.GetBySessionAndTypeAsync(
                    _currentSession.Id, RoomType.Math);
                var s1 = await _roomRepo.GetBySessionAndTypeAsync(
                    _currentSession.Id, RoomType.Shift1);
                var s2 = await _roomRepo.GetBySessionAndTypeAsync(
                    _currentSession.Id, RoomType.Shift2);

                foreach (var r in lit) LiteratureRooms.Add(r);
                foreach (var r in math) MathRooms.Add(r);
                foreach (var r in s1) Shift1Rooms.Add(r);
                foreach (var r in s2) Shift2Rooms.Add(r);

                NotifyAllSummaries();

                int total = LiteratureRooms.Count + MathRooms.Count
                          + Shift1Rooms.Count + Shift2Rooms.Count;

                if (total == 0)
                {
                    ShowStatus("Chưa có phòng thi. Nhấn 'Xếp phòng thi' để tạo.", isError: false);
                }
                else if (Shift1Rooms.Count == 0 && Shift2Rooms.Count == 0)
                {
                    ShowStatus("Chưa xếp phòng thi cho môn Ca 1 và Ca 2.", isError: false);
                }
                else
                {
                    ShowStatus($"Đã tải {total} phòng thi.", isError: false);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Lỗi tải phòng thi: {ex.Message}", isError: true);
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Xếp phòng thi:
        /// 1. Nếu chưa có kết quả xếp ca → chạy xếp ca trước
        /// 2. Sau đó chạy xếp phòng
        /// </summary>
        private async Task ScheduleRoomsAsync()
        {
            if (_currentSession == null) return;

            var confirm = System.Windows.MessageBox.Show(
                "Xếp phòng thi sẽ xóa toàn bộ phòng cũ và tạo lại.\nTiếp tục?",
                "Xác nhận",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;

            try
            {
                // Bước 1: Xếp ca nếu chưa có
                if (_shiftResult == null)
                {
                    ShowStatus("Đang xếp ca thi...", isError: false);
                    _shiftResult = await _shiftScheduler.ScheduleAsync(_currentSession.Id);
                }

                // Bước 2: Xếp phòng
                ShowStatus("Đang xếp phòng thi...", isError: false);
                await _roomScheduler.ScheduleRoomsAsync(
                    _currentSession.Id,
                    _shiftResult.SubjectShiftMap,
                    _roomCapacity);

                // Bước 3: Reload
                await LoadAllRoomsAsync();

                int total = LiteratureRooms.Count + MathRooms.Count
                          + Shift1Rooms.Count + Shift2Rooms.Count;
                ShowStatus($"Xếp phòng hoàn tất! Tổng cộng {total} phòng thi.", isError: false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Lỗi xếp phòng: {ex.Message}", isError: true);
            }
            finally { IsBusy = false; }
        }


        /// <summary>Load danh sách TS khi chọn phòng</summary>
        private void LoadRoomCandidates(ExamRoom? room)
        {
            RoomCandidates.Clear();
            if (room == null) return;

            var sorted = (room.Assignments ?? new List<RoomAssignment>())
                .OrderBy(a => a.OrderInRoom)
                .ToList();

            foreach (var a in sorted)
                RoomCandidates.Add(a);
        }

        /// <summary>Refresh danh sách phòng của tab hiện tại</summary>
        private void RefreshCurrentTabRooms()
        {
            SelectedRoom = null;
            RoomCandidates.Clear();
        }

        private void ClearAllRooms()
        {
            LiteratureRooms.Clear();
            MathRooms.Clear();
            Shift1Rooms.Clear();
            Shift2Rooms.Clear();
            SelectedRoom = null;
            RoomCandidates.Clear();
        }

        private void NotifyAllSummaries()
        {
            OnPropertyChanged(nameof(LiteratureSummary));
            OnPropertyChanged(nameof(MathSummary));
            OnPropertyChanged(nameof(Shift1Summary));
            OnPropertyChanged(nameof(Shift2Summary));
        }

        /// <summary>Tạo chuỗi tóm tắt: "5 phòng / 118 thí sinh"</summary>
        private static string BuildSummary(ObservableCollection<ExamRoom> rooms)
        {
            int totalTS = rooms.Sum(r => r.Assignments?.Count ?? 0);
            return $"{rooms.Count} phòng / {totalTS} thí sinh";
        }

        private void ShowStatus(string msg, bool isError)
        {
            StatusMessage = msg;
            IsStatusError = isError;
        }
    }
}
