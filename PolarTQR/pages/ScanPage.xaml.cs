using PolarTQR.ViewModels;

namespace PolarTQR.pages
{
    public partial class ScanPage : ContentPage
    {
        public ScanPage()
        {
            InitializeComponent();
            BindingContext = new ScanViewModel();
        }
    }
}
