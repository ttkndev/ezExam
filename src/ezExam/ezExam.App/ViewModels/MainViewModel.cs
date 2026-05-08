using ezExam.App.ViewModels.Base;
using ezExam.Core.Models;
using ezExam.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace ezExam.App.ViewModels
{
    /// <summary>
    /// ViewModel chính: quản lý điều hướng giữa các màn hình,
    /// hiển thị kỳ thi đang chọn ở thanh tiêu đề
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        private readonly IExamSessionService _sessionService;

        // --- Navigation ---
        private BaseViewModel _currentView = null!;
        public BaseViewModel CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        // --- Kỳ thi đang chọn (hiển thị trên header) ---
        private ExamSession? _currentSession;
        public ExamSession? CurrentSession
        {
            get => _currentSession;
            set
            {
                SetProperty(ref _currentSession, value);
                OnPropertyChanged(nameof(SessionTitle));
            }
        }

        public string SessionTitle => _currentSession == null
            ? "ezExam — Chưa chọn kỳ thi"
            : $"ezExam — {_currentSession.Name}";

        // --- Loading overlay ---
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private string _busyMessage = "";
        public string BusyMessage
        {
            get => _busyMessage;
            set => SetProperty(ref _busyMessage, value);
        }

        // --- Commands điều hướng ---
        public RelayCommand NavSessionCommand { get; }
        public RelayCommand NavCandidateCommand { get; }
        public RelayCommand NavRoomCommand { get; }

        // --- Sub ViewModels ---
        private readonly ExamSessionViewModel _examSessionVM;
        private readonly CandidateViewModel _candidateVM;
        private readonly RoomViewModel _roomVM;

        public MainViewModel(
            IExamSessionService sessionService,
            ExamSessionViewModel examSessionVM,
            CandidateViewModel candidateVM,
            RoomViewModel roomVM)
        {
            _sessionService = sessionService;
            _examSessionVM = examSessionVM;
            _candidateVM = candidateVM;
            _roomVM = roomVM;

            // Khi sub-vm báo session thay đổi → cập nhật header
            _examSessionVM.SessionChanged += OnSessionChanged;

            NavSessionCommand = new RelayCommand(_ => CurrentView = _examSessionVM);
            NavCandidateCommand = new RelayCommand(_ => NavigateCandidate());
            NavRoomCommand = new RelayCommand(_ => NavigateRoom());

            // Mặc định hiển thị màn hình Kỳ thi
            CurrentView = _examSessionVM;

            // Load kỳ thi mặc định
            _ = LoadDefaultSessionAsync();
        }

        private async Task LoadDefaultSessionAsync()
        {
            var defaultSession = await _sessionService.GetDefaultAsync();
            CurrentSession = defaultSession;
            _candidateVM.SetSession(defaultSession);
            _roomVM.SetSession(defaultSession);
        }

        private void OnSessionChanged(ExamSession? session)
        {
            CurrentSession = session;
            // Thông báo cho các VM con biết session đổi
            _candidateVM.SetSession(session);
            _roomVM.SetSession(session);
        }

        private void NavigateCandidate()
        {
            if (_currentSession == null)
            {
                System.Windows.MessageBox.Show(
                    "Vui lòng chọn kỳ thi trước!",
                    "Thông báo", System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }
            CurrentView = _candidateVM;
        }

        private void NavigateRoom()
        {
            if (_currentSession == null)
            {
                System.Windows.MessageBox.Show(
                    "Vui lòng chọn kỳ thi trước!",
                    "Thông báo", System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }
            CurrentView = _roomVM;
        }
    }
}
