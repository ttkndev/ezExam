using Desktop.ViewModels;
using System.Windows.Controls;

namespace Desktop.Views
{
    public partial class PhongThiView : UserControl
    {
        public PhongThiView()
        {
            InitializeComponent();
            DataContext = new PhongThiViewModel();
        }
    }
}
