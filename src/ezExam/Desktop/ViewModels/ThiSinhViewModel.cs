using Desktop.Models;
using Desktop.Services;
using Desktop.Helpers;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Linq;
using System.Collections.Generic;

namespace Desktop.ViewModels
{
    public class ThiSinhViewModel : BaseViewModel
    {
        private readonly ThiSinhService _thiSinhService;
        private readonly KyThiService _kyThiService;
        private readonly ExcelImporter _excelImporter;

        public ObservableCollection<ThiSinh> ThiSinhList { get; set; }
        public ObservableCollection<PairStat> PairStats { get; set; }

        private ThiSinh _selectedThiSinh;
        public ThiSinh SelectedThiSinh
        {
            get => _selectedThiSinh;
            set { _selectedThiSinh = value; OnPropertyChanged(); }
        }

        public string KyThiMacDinhText { get; set; }
        public string SelectedFile { get; set; }
        public string[] SheetNames { get; set; }
        public string SelectedSheet { get; set; }

        public ICommand AddThiSinhCommand { get; }
        public ICommand EditThiSinhCommand { get; }
        public ICommand DeleteThiSinhCommand { get; }
        public ICommand DeleteAllThiSinhCommand { get; }
        public ICommand ImportExcelCommand { get; }
        public ICommand ChooseFileCommand { get; }
        public ICommand AssignCaThiCommand { get; }

        public ThiSinhViewModel()
        {
            _thiSinhService = new ThiSinhService();
            _kyThiService = new KyThiService();
            _excelImporter = new ExcelImporter();

            LoadThiSinh();

            AddThiSinhCommand = new RelayCommand(o => AddThiSinh());
            EditThiSinhCommand = new RelayCommand(o => EditThiSinh(), o => SelectedThiSinh != null);
            DeleteThiSinhCommand = new RelayCommand(o => DeleteThiSinh(), o => SelectedThiSinh != null);
            DeleteAllThiSinhCommand = new RelayCommand(o => DeleteAllThiSinh());
            ChooseFileCommand = new RelayCommand(o => ChooseFile());
            ImportExcelCommand = new RelayCommand(o => ImportExcel(), o => !string.IsNullOrEmpty(SelectedFile) && !string.IsNullOrEmpty(SelectedSheet));
            AssignCaThiCommand = new RelayCommand(o => AssignCaThi(), o => ThiSinhList.Any());
        }

        private void LoadThiSinh()
        {
            var kyThi = _kyThiService.GetKyThiMacDinh();
            if (kyThi != null)
            {
                KyThiMacDinhText = $"Kỳ thi mặc định: {kyThi.TenKyThi} ({kyThi.NgayThi})";
                ThiSinhList = new ObservableCollection<ThiSinh>(_thiSinhService.GetThiSinhByKyThi(kyThi.Id));
            }
            else
            {
                KyThiMacDinhText = "Chưa có kỳ thi mặc định!";
                ThiSinhList = new ObservableCollection<ThiSinh>();
            }
            OnPropertyChanged(nameof(KyThiMacDinhText));
            OnPropertyChanged(nameof(ThiSinhList));

            UpdatePairStats();
        }

        private void AddThiSinh()
        {
            var kyThi = _kyThiService.GetKyThiMacDinh();
            if (kyThi == null) return;

            var ts = new ThiSinh { HoTen = "Thí sinh mới", KyThiId = kyThi.Id };
            _thiSinhService.AddThiSinh(ts, kyThi.Id);
            LoadThiSinh();
        }

        private void EditThiSinh()
        {
            if (SelectedThiSinh != null)
            {
                _thiSinhService.UpdateThiSinh(SelectedThiSinh);
                LoadThiSinh();
            }
        }

        private void DeleteThiSinh()
        {
            if (SelectedThiSinh != null)
            {
                _thiSinhService.DeleteThiSinh(SelectedThiSinh.Id);
                LoadThiSinh();
            }
        }

        private void DeleteAllThiSinh()
        {
            var kyThi = _kyThiService.GetKyThiMacDinh();
            if (kyThi != null)
            {
                _thiSinhService.DeleteAllThiSinhByKyThi(kyThi.Id);
                LoadThiSinh();
            }
        }

        private void ChooseFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx;*.xls",
                Title = "Chọn file Excel"
            };
            if (dialog.ShowDialog() == true)
            {
                SelectedFile = dialog.FileName;
                SheetNames = _excelImporter.GetSheetNames(SelectedFile);
                OnPropertyChanged(nameof(SelectedFile));
                OnPropertyChanged(nameof(SheetNames));
            }
        }

        private void ImportExcel()
        {
            if (!string.IsNullOrEmpty(SelectedFile) && !string.IsNullOrEmpty(SelectedSheet))
            {
                _excelImporter.Import(SelectedFile, SelectedSheet);
                LoadThiSinh();
            }
        }

        private void UpdatePairStats()
        {
            var stats = new Dictionary<(string, string), int>();

            foreach (var ts in ThiSinhList)
            {
                var monList = ts.CacMonThi?.Split(',')
                                          .Select(m => m.Trim())
                                          .Where(m => m != "Ngữ văn" && m != "Toán")
                                          .ToList() ?? new List<string>();

                if (monList.Count == 2)
                {
                    var a = monList[0];
                    var b = monList[1];
                    var key = string.Compare(a, b) < 0 ? (a, b) : (b, a);

                    if (!stats.ContainsKey(key)) stats[key] = 0;
                    stats[key]++;
                }
            }

            PairStats = new ObservableCollection<PairStat>(
                stats.Select(kvp => new PairStat { MonA = kvp.Key.Item1, MonB = kvp.Key.Item2, Count = kvp.Value })
            );
            OnPropertyChanged(nameof(PairStats));
        }

        private void AssignCaThi()
        {
            foreach (var ts in ThiSinhList)
            {
                var monList = ts.CacMonThi?.Split(',')
                                          .Select(m => m.Trim())
                                          .Where(m => m != "Ngữ văn" && m != "Toán")
                                          .ToList() ?? new List<string>();

                if (monList.Count == 2)
                {
                    ts.MonCa1 = monList[0];
                    ts.MonCa2 = monList[1];
                }
            }
            OnPropertyChanged(nameof(ThiSinhList));
        }
    }
}
