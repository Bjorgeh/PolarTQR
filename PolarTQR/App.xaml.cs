namespace PolarTQR
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new PolarTQR.pages.LoginPage());
        }
    }
}