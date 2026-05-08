using ezExam.App.ViewModels.Base;
using ezExam.Core.Models;
using ezExam.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ezExam.App.ViewModels
{
    /// <summary>
    /// ViewModel quản lý kỳ thi:
    /// - Hiển thị danh sách kỳ thi
    /// - Thêm / sửa / xóa kỳ thi
    /// - Đặt kỳ thi mặc định (đang thao tác)
    /// </summary>
    public class ExamSessionViewModel : BaseViewModel
    {
        private readonly IExamSessionService _service;

        // Event thông báo cho MainViewModel khi session thay đổi
        public event Action<ExamSession?>? SessionChanged;

        // --- Danh sách kỳ thi ---
        public ObservableCollection<ExamSession> Sessions { get; } = new();

        // --- Kỳ thi đang chọn trong danh sách ---
        private ExamSession? _selectedSession;
        public ExamSession? SelectedSession
        {
            get => _selectedSession;
            set
            {
                SetProperty(ref _selectedSession, value);
                // Khi chọn dòng → điền vào form
                if (value != null) LoadFormFromSession(value);
                OnPropertyChanged(nameof(HasSelection));
            }
        }

        public bool HasSelection => _selectedSession != null;

        // --- Form nhập liệu ---
        private string _formName = "";
        public string FormName
        {
            get => _formName;
            set => SetProperty(ref _formName, value);
        }

        private DateTime _formDate = DateTime.Today;
        public DateTime FormDate
        {
            get => _formDate;
            set => SetProperty(ref _formDate, value);
        }

        // --- Trạng thái form: đang thêm mới hay đang sửa ---
        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                SetProperty(ref _isEditing, value);
                OnPropertyChanged(nameof(FormTitle));
            }
        }

        public string FormTitle => _isEditing ? "Cập nhật kỳ thi" : "Thêm kỳ thi mới";

        // --- Thông báo lỗi / thành công ---
        private string _message = "";
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        private bool _isError;
        public bool IsError
        {
            get => _isError;
            set => SetProperty(ref _isError, value);
        }

        // --- Commands ---
        public RelayCommand LoadCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand SetDefaultCommand { get; }
        public RelayCommand NewCommand { get; }
        public RelayCommand CancelCommand { get; }

        public ExamSessionViewModel(IExamSessionService service)
        {
            _service = service;

            LoadCommand = new RelayCommand(_ => LoadAsync());
            SaveCommand = new RelayCommand(_ => SaveAsync());
            DeleteCommand = new RelayCommand(_ => DeleteAsync(),
                                   _ => HasSelection);
            SetDefaultCommand = new RelayCommand(_ => SetDefaultAsync(),
                                   _ => HasSelection);
            NewCommand = new RelayCommand(_ => PrepareNew());
            CancelCommand = new RelayCommand(_ => CancelEdit());

            _ = LoadAsync();
        }

        // -------------------------------------------------------------------------

        /// <summary>Tải danh sách kỳ thi từ database</summary>
        private async Task LoadAsync()
        {
            var list = await _service.GetAllAsync();
            Sessions.Clear();
            foreach (var s in list) Sessions.Add(s);

            // Tự động chọn kỳ thi mặc định
            var def = Sessions.FirstOrDefault(s => s.IsDefault);
            if (def != null) SelectedSession = def;

            // Thông báo cho MainViewModel
            SessionChanged?.Invoke(def);
        }

        /// <summary>Chuẩn bị form thêm mới</summary>
        private void PrepareNew()
        {
            SelectedSession = null;
            FormName = "";
            FormDate = DateTime.Today;
            IsEditing = false;
            Message = "";
        }

        /// <summary>Hủy chỉnh sửa, quay về trạng thái ban đầu</summary>
        private void CancelEdit()
        {
            if (_selectedSession != null)
                LoadFormFromSession(_selectedSession);
            else
                PrepareNew();
            Message = "";
        }

        /// <summary>Điền dữ liệu kỳ thi vào form để sửa</summary>
        private void LoadFormFromSession(ExamSession session)
        {
            FormName = session.Name;
            FormDate = session.ExamDate;
            IsEditing = true;
            Message = "";
        }

        /// <summary>Lưu kỳ thi (thêm mới hoặc cập nhật)</summary>
        private async Task SaveAsync()
        {
            // Validate
            if (string.IsNullOrWhiteSpace(FormName))
            {
                ShowMessage("Vui lòng nhập tên kỳ thi!", isError: true);
                return;
            }

            if (_isEditing && _selectedSession != null)
            {
                // Cập nhật
                _selectedSession.Name = FormName.Trim();
                _selectedSession.ExamDate = FormDate;
                await _service.UpdateAsync(_selectedSession);
                ShowMessage("Cập nhật kỳ thi thành công!", isError: false);
            }
            else
            {
                // Thêm mới
                var session = new ExamSession
                {
                    Name = FormName.Trim(),
                    ExamDate = FormDate
                };
                await _service.AddAsync(session);
                ShowMessage("Thêm kỳ thi thành công!", isError: false);
            }

            await LoadAsync();
        }

        /// <summary>Xóa kỳ thi đang chọn</summary>
        private async Task DeleteAsync()
        {
            if (_selectedSession == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Xóa kỳ thi \"{_selectedSession.Name}\"?\nToàn bộ thí sinh và phòng thi sẽ bị xóa theo!",
                "Xác nhận xóa",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes) return;

            await _service.DeleteAsync(_selectedSession.Id);
            ShowMessage("Đã xóa kỳ thi!", isError: false);
            PrepareNew();
            await LoadAsync();
        }

        /// <summary>Đặt kỳ thi đang chọn làm kỳ thi mặc định</summary>
        private async Task SetDefaultAsync()
        {
            if (_selectedSession == null) return;

            await _service.SetDefaultAsync(_selectedSession.Id);
            await LoadAsync();
            ShowMessage($"Đã chọn \"{_selectedSession?.Name}\" làm kỳ thi đang thao tác!", isError: false);
        }

        private void ShowMessage(string msg, bool isError)
        {
            Message = msg;
            IsError = isError;
        }
    }
}
