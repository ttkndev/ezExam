using Desktop.Helpers;
using Desktop.Models;
using Desktop.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Desktop.ViewModels
{
    public class KyThiViewModel : BaseViewModel
    {
        private readonly KyThiService _service;

        public ObservableCollection<KyThi> KyThiList { get; set; }
        private KyThi _selectedKyThi;
        public KyThi SelectedKyThi
        {
            get => _selectedKyThi;
            set
            {
                _selectedKyThi = value;
                OnPropertyChanged();
            }
        }

        public ICommand AddKyThiCommand { get; }
        public ICommand EditKyThiCommand { get; }
        public ICommand DeleteKyThiCommand { get; }
        public ICommand SetMacDinhCommand { get; }
        public ICommand SaveKyThiCommand { get; }

        public KyThiViewModel()
        {
            _service = new KyThiService();
            LoadKyThi();

            AddKyThiCommand = new RelayCommand(o => AddKyThi());
            EditKyThiCommand = new RelayCommand(o => EditKyThi(), o => SelectedKyThi != null);
            DeleteKyThiCommand = new RelayCommand(o => DeleteKyThi(), o => SelectedKyThi != null);
            SetMacDinhCommand = new RelayCommand(o => SetMacDinh(), o => SelectedKyThi != null);
            SaveKyThiCommand = new RelayCommand(o => SaveAllKyThi(), o => KyThiList != null && KyThiList.Count > 0);
        }

        private void LoadKyThi()
        {
            KyThiList = new ObservableCollection<KyThi>(_service.GetKyThi());
            OnPropertyChanged(nameof(KyThiList));
        }

        private void AddKyThi()
        {
            var kt = new KyThi
            {
                TenKyThi = "Kỳ thi mới",
                NgayThi = "2026-06-01",
                MacDinh = 0
            };
            _service.AddKyThi(kt);
            LoadKyThi();
        }

        private void EditKyThi()
        {
            if (SelectedKyThi != null)
            {
                _service.UpdateKyThi(SelectedKyThi);
                LoadKyThi();
            }
        }

        private void DeleteKyThi()
        {
            if (SelectedKyThi != null)
            {
                _service.DeleteKyThi(SelectedKyThi.Id);
                LoadKyThi();
            }
        }

        private void SaveAllKyThi()
        {
            foreach (var kyThi in KyThiList)
            {
                _service.UpdateKyThi(kyThi);
            }
            LoadKyThi();
        }

        private void SetMacDinh()
        {
            if (SelectedKyThi != null)
            {
                _service.SetMacDinh(SelectedKyThi.Id);
                LoadKyThi();
            }
        }
    }
}
