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

            var graph = new Dictionary<string, HashSet<string>>();
            foreach (var ts in ThiSinhList)
            {
                var electives = GetElectiveSubjects(ts);
                if (electives.Count != 2) continue;

                var a = electives[0];
                var b = electives[1];
                if (a == b) continue;

                if (!graph.ContainsKey(a)) graph[a] = new HashSet<string>();
                if (!graph.ContainsKey(b)) graph[b] = new HashSet<string>();
                graph[a].Add(b);
                graph[b].Add(a);
            }

            var partition = new Dictionary<string, bool>();
            var queue = new Queue<string>();

            foreach (var root in graph.Keys)
            {
                if (partition.ContainsKey(root)) continue;

                partition[root] = false;
                queue.Enqueue(root);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    foreach (var neighbor in graph[current])
                    {
                        if (!partition.ContainsKey(neighbor))
                        {
                            partition[neighbor] = !partition[current];
                            queue.Enqueue(neighbor);
                        }
                        // Nếu đồ thị có chu trình lẻ thì sẽ phát sinh cạnh cùng màu.
                        // Trường hợp này vẫn gán ca theo màu hiện có để giảm xung đột.
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

                var ca1 = electives.FirstOrDefault(s => !partition.GetValueOrDefault(s, false));
                var ca2 = electives.FirstOrDefault(s => partition.GetValueOrDefault(s, false));

                if (ca1 == null || ca2 == null)
                {
                    ts.MonCa1 = electives[0];
                    ts.MonCa2 = electives[1];
                    continue;
                }

                ts.MonCa1 = ca1;
                ts.MonCa2 = ca2;
            }

            SaveAllThiSinh();
        }
    }
}
