using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Desktop.Helpers;
using Desktop.Views;

namespace Desktop.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand ShowKyThiCommand { get; }
        public ICommand ShowThiSinhCommand { get; }
        public ICommand ShowCaThiCommand { get; }
        public ICommand ShowPhongThiCommand { get; }
        public ICommand ShowExportCommand { get; }
        public ICommand ShowAboutCommand { get; }

        public MainViewModel()
        {
            // Khởi tạo View mặc định
            CurrentView = new KyThiView();

            // Gán command
            ShowKyThiCommand = new RelayCommand(o => CurrentView = new KyThiView());
            ShowThiSinhCommand = new RelayCommand(o => CurrentView = new ThiSinhView());
            ShowCaThiCommand = new RelayCommand(o => CurrentView = new CaThiView());
            ShowPhongThiCommand = new RelayCommand(o => CurrentView = new PhongThiView());
            ShowExportCommand = new RelayCommand(o => CurrentView = new ExportView());
            ShowAboutCommand = new RelayCommand(o => CurrentView = new AboutView());
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
