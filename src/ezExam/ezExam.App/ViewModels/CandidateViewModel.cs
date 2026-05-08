using ezExam.App.ViewModels.Base;
using ezExam.Core.Models;
using ezExam.Core.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ezExam.App.ViewModels
{
    /// <summary>
    /// ViewModel quản lý thí sinh:
    /// - Import Excel
    /// - Thống kê sau import
    /// - Sắp xếp và đánh SBD
    /// - Xếp ca thi
    /// </summary>
    public class CandidateViewModel : BaseViewModel
    {
        private readonly ICandidateService _candidateService;
        private readonly IShiftSchedulerService _shiftScheduler;

        // Kỳ thi hiện tại (set từ MainViewModel)
        private ExamSession? _currentSession;

        // --- Danh sách thí sinh ---
        public ObservableCollection<Candidate> Candidates { get; } = new();

        // --- Thống kê ---
        private CandidateStatistics? _statistics;
        public CandidateStatistics? Statistics
        {
            get => _statistics;
            set
            {
                SetProperty(ref _statistics, value);
                OnPropertyChanged(nameof(HasStatistics));
                OnPropertyChanged(nameof(SubjectStatisticsLine));
                OnPropertyChanged(nameof(SubjectPairStatisticsLine));
                OnPropertyChanged(nameof(SubjectStatisticTags));
                OnPropertyChanged(nameof(SubjectPairStatisticTags));
            }
        }
        public bool HasStatistics => _statistics != null;

        public string SubjectStatisticsLine => Statistics == null
            ? ""
            : $"Số lượng đăng ký môn: {string.Join(" | ", Statistics.CandidatesBySubject.Select(kv => $"{kv.Key}: {kv.Value}"))}";

        public string SubjectPairStatisticsLine => Statistics == null
            ? ""
            : $"Số lượng đăng ký cặp môn tự chọn: {string.Join(" | ", Statistics.CandidatesBySubjectPair.Select(kv => $"({kv.Key}): {kv.Value}"))}";

        public IEnumerable<string> SubjectStatisticTags =>
            Statistics?.CandidatesBySubject
                .Select(kv => $"{kv.Key}: {kv.Value}")
            ?? Enumerable.Empty<string>();

        public IEnumerable<string> SubjectPairStatisticTags =>
            Statistics?.CandidatesBySubjectPair
                .Select(kv => $"{kv.Key}: {kv.Value}")
            ?? Enumerable.Empty<string>();

        // --- Kết quả xếp ca ---
        private ShiftScheduleResult? _shiftResult;
        public ShiftScheduleResult? ShiftResult
        {
            get => _shiftResult;
            set
            {
                SetProperty(ref _shiftResult, value);
                OnPropertyChanged(nameof(HasShiftResult));
            }
        }
        public bool HasShiftResult => _shiftResult != null;

        // --- Tab đang hiển thị: 0=Danh sách, 1=Thống kê ---
        private int _selectedTab;
        public int SelectedTab
        {
            get => _selectedTab;
            set => SetProperty(ref _selectedTab, value);
        }

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

        // --- Số SBD bắt đầu ---
        private int _startNumber = 1;
        public int StartNumber
        {
            get => _startNumber;
            set
            {
                if (SetProperty(ref _startNumber, value))
                    OnPropertyChanged(nameof(PreviewStartCandidateNumber));
            }
        }

        // --- Format SBD (ví dụ: TBA{d:3} => TBA001) ---
        private string _candidateNumberFormat = "TBA{d:3}";
        public string CandidateNumberFormat
        {
            get => _candidateNumberFormat;
            set
            {
                if (SetProperty(ref _candidateNumberFormat, value))
                    OnPropertyChanged(nameof(PreviewStartCandidateNumber));
            }
        }

        public string PreviewStartCandidateNumber => FormatCandidateNumber(StartNumber, CandidateNumberFormat);

        // --- Commands ---
        public RelayCommand ImportCommand { get; }
        public RelayCommand SortAssignCommand { get; }
        public RelayCommand ScheduleShiftCommand { get; }
        public RelayCommand RefreshCommand { get; }

        public CandidateViewModel(
            ICandidateService candidateService,
            IShiftSchedulerService shiftScheduler)
        {
            _candidateService = candidateService;
            _shiftScheduler = shiftScheduler;

            ImportCommand = new RelayCommand(_ => ImportAsync());
            SortAssignCommand = new RelayCommand(_ => SortAndAssignAsync(),
                                      _ => Candidates.Any());
            ScheduleShiftCommand = new RelayCommand(_ => ScheduleShiftAsync(),
                                      _ => Candidates.Any());
            RefreshCommand = new RelayCommand(_ => LoadCandidatesAsync());
        }

        /// <summary>Được gọi từ MainViewModel khi đổi kỳ thi</summary>
        public void SetSession(ExamSession? session)
        {
            _currentSession = session;
            Candidates.Clear();
            Statistics = null;
            ShiftResult = null;
            StatusMessage = "";

            if (session != null)
                _ = LoadCandidatesAsync();
        }

        // -------------------------------------------------------------------------

        /// <summary>Tải danh sách thí sinh từ database</summary>
        private async Task LoadCandidatesAsync()
        {
            if (_currentSession == null) return;

            IsBusy = true;
            try
            {
                var list = await _candidateService.GetBySessionAsync(_currentSession.Id);
                Candidates.Clear();
                foreach (var c in list) Candidates.Add(c);

                // Tự động load thống kê nếu có dữ liệu
                if (Candidates.Any())
                    Statistics = await _candidateService.GetStatisticsAsync(_currentSession.Id);

                ShowStatus($"Đã tải {Candidates.Count} thí sinh.", isError: false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Lỗi tải dữ liệu: {ex.Message}", isError: true);
            }
            finally { IsBusy = false; }
        }

        /// <summary>Import danh sách thí sinh từ file Excel</summary>
        private async Task ImportAsync()
        {
            if (_currentSession == null) return;

            // Mở hộp thoại chọn file
            var dialog = new OpenFileDialog
            {
                Title = "Chọn file danh sách thí sinh",
                Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true) return;

            IsBusy = true;
            ShowStatus("Đang import dữ liệu...", isError: false);

            try
            {
                await _candidateService.ImportFromExcelAsync(
                    dialog.FileName, _currentSession.Id);

                // Reload và thống kê
                var list = await _candidateService.GetBySessionAsync(_currentSession.Id);
                Candidates.Clear();
                foreach (var c in list) Candidates.Add(c);

                Statistics = await _candidateService.GetStatisticsAsync(_currentSession.Id);

                ShowStatus($"Import thành công {Candidates.Count} thí sinh!", isError: false);

                // Tự chuyển sang tab thống kê
                SelectedTab = 1;
            }
            catch (Exception ex)
            {
                ShowStatus($"Lỗi import: {ex.Message}", isError: true);
            }
            finally { IsBusy = false; }
        }

        /// <summary>Sắp xếp theo tên và đánh SBD</summary>
        private async Task SortAndAssignAsync()
        {
            if (_currentSession == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Sắp xếp thí sinh theo tên và đánh SBD bắt đầu từ {StartNumber:D6}?\n" +
                "Thao tác này sẽ ghi đè SBD cũ.",
                "Xác nhận",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            ShowStatus("Đang sắp xếp và đánh SBD...", isError: false);

            try
            {
                await _candidateService.SortAndAssignNumbersAsync(
                    _currentSession.Id, StartNumber, CandidateNumberFormat);

                await LoadCandidatesAsync();
                ShowStatus("Đã sắp xếp và đánh SBD thành công!", isError: false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Lỗi: {ex.Message}", isError: true);
            }
            finally { IsBusy = false; }
        }

        /// <summary>Chạy thuật toán xếp ca thi</summary>
        private async Task ScheduleShiftAsync()
        {
            if (_currentSession == null) return;

            IsBusy = true;
            ShowStatus("Đang xếp ca thi...", isError: false);

            try
            {
                ShiftResult = await _shiftScheduler.ScheduleAsync(_currentSession.Id);

                // Hiển thị kết quả
                var msg = ShiftResult.IsPerfect
                    ? "✅ Xếp ca hoàn hảo, không có xung đột!"
                    : $"⚠️ {ShiftResult.Message}";

                ShowStatus(msg, isError: !ShiftResult.IsPerfect);

                // Ở lại tab danh sách để xem kết quả trực tiếp trên cột Môn ca 1/2
                SelectedTab = 0;
                await LoadCandidatesAsync();
            }
            catch (Exception ex)
            {
                ShowStatus($"Lỗi xếp ca: {ex.Message}", isError: true);
            }
            finally { IsBusy = false; }
        }

        private static string FormatCandidateNumber(int number, string? format)
        {
            if (string.IsNullOrWhiteSpace(format))
                return number.ToString();

            try
            {
                var parsed = ParseNumberFormat(format);
                return $"{parsed.Prefix}{number.ToString($"D{parsed.Digits}")}";
            }
            catch
            {
                return number.ToString();
            }
        }

        private static (string Prefix, int Digits) ParseNumberFormat(string format)
        {
            var open = format.IndexOf("{d:", StringComparison.OrdinalIgnoreCase);
            if (open < 0)
                throw new FormatException("Thiếu phần định dạng {d:n}.");

            var close = format.IndexOf('}', open);
            if (close < 0)
                throw new FormatException("Thiếu dấu } trong định dạng SBD.");

            var digitsText = format.Substring(open + 3, close - open - 3);
            if (!int.TryParse(digitsText, out var digits) || digits <= 0)
                throw new FormatException("Số chữ số trong định dạng SBD không hợp lệ.");

            var prefix = format.Substring(0, open);
            return (prefix, digits);
        }

        private void ShowStatus(string msg, bool isError)
        {
            StatusMessage = msg;
            IsStatusError = isError;
        }
    }

}
