using PolarTQR.ViewModels;

namespace PolarTQR.pages
{
    public partial class HomePage : ContentPage

    {
        public HomePage()
        {
            InitializeComponent(); // Denne m� matches
            BindingContext = new HomeViewModel();
        }
    }
}
