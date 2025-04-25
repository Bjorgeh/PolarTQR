using PolarTQR.ViewModels;

namespace PolarTQR.pages
{
    public partial class HomePage : ContentPage

    {
        public HomePage()
        {
            InitializeComponent(); // Denne må matches
            BindingContext = new HomeViewModel();
        }
    }
}
