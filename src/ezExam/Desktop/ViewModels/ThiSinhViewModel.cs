using Desktop.Models;
using Desktop.Services;
using Desktop.Helpers;
using Microsoft.Win32;
using System;
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
        public ICommand SaveAllThiSinhCommand { get; }
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
            SaveAllThiSinhCommand = new RelayCommand(o => SaveAllThiSinh(), o => ThiSinhList != null && ThiSinhList.Any());
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

            var ts = new ThiSinh { HoTen = "Thí sinh mới", MonVan = true, MonToan = true, KyThiId = kyThi.Id };
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

        private void SaveAllThiSinh()
        {
            foreach (var ts in ThiSinhList)
            {
                _thiSinhService.UpdateThiSinh(ts);
            }
            LoadThiSinh();
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
                SelectedSheet = SheetNames.FirstOrDefault();
                OnPropertyChanged(nameof(SelectedFile));
                OnPropertyChanged(nameof(SheetNames));
                OnPropertyChanged(nameof(SelectedSheet));
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

        private List<string> GetElectiveSubjects(ThiSinh ts) => ts.CacMonThi?.Split(',')
            .Select(m => m.Trim())
            .Where(m => !string.IsNullOrWhiteSpace(m) && m != "Ngữ văn" && m != "Toán")
            .Distinct()
            .ToList() ?? new List<string>();

        private void UpdatePairStats()
        {
            var stats = new Dictionary<(string, string), int>();

            foreach (var ts in ThiSinhList)
            {
                var monList = GetElectiveSubjects(ts);

                if (monList.Count == 2)
                {
                    var a = monList[0];
                    var b = monList[1];
                    var key = string.Compare(a, b, StringComparison.Ordinal) < 0 ? (a, b) : (b, a);

                    if (!stats.ContainsKey(key)) stats[key] = 0;
                    stats[key]++;
                }
            }

            PairStats = new ObservableCollection<PairStat>(
                stats.OrderByDescending(kvp => kvp.Value)
                    .Select(kvp => new PairStat { MonA = kvp.Key.Item1, MonB = kvp.Key.Item2, Count = kvp.Value })
            );
            OnPropertyChanged(nameof(PairStats));
        }

        private void AssignCaThi()
        {
            UpdatePairStats();

            var subjects = ThiSinhList.SelectMany(GetElectiveSubjects).Distinct().ToList();
            var partition = subjects.ToDictionary(s => s, _ => false);
            var pairWeights = PairStats.ToDictionary(p => (p.MonA, p.MonB), p => p.Count);

            var improved = true;
            while (improved)
            {
                improved = false;
                foreach (var subject in subjects)
                {
                    var gain = 0;
                    foreach (var other in subjects.Where(s => s != subject))
                    {
                        var key = string.Compare(subject, other, StringComparison.Ordinal) < 0 ? (subject, other) : (other, subject);
                        if (!pairWeights.TryGetValue(key, out var w)) continue;

                        var currentlySplit = partition[subject] != partition[other];
                        gain += currentlySplit ? -w : w;
                    }

                    if (gain > 0)
                    {
                        partition[subject] = !partition[subject];
                        improved = true;
                    }
                }
            }

            foreach (var ts in ThiSinhList)
            {
                var electives = GetElectiveSubjects(ts);
                ts.MonCa1 = "";
                ts.MonCa2 = "";

                if (electives.Count == 0) continue;
                if (electives.Count == 1)
                {
                    ts.MonCa1 = electives[0];
                    continue;
                }

                var ca1 = electives.FirstOrDefault(s => partition.GetValueOrDefault(s, false));
                var ca2 = electives.FirstOrDefault(s => !partition.GetValueOrDefault(s, false));

                ts.MonCa1 = ca1 ?? electives[0];
                ts.MonCa2 = ca2 ?? electives.Skip(1).FirstOrDefault() ?? electives[0];
            }

            SaveAllThiSinh();
        }
    }
}
