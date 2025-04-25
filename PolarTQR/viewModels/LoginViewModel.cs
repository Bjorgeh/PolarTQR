using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace PolarTQR.ViewModels
{
    public class LoginViewModel : BindableObject
    {
        private string _username;
        private string _password;
        private string _errorMessage;

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new Command(OnLogin);
            ErrorMessage = string.Empty;

            // Pre-fill dummy data (valgfritt)
            Username = "admin";
            Password = "1234";
        }

        private async void OnLogin()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Fyll inn både brukernavn og passord.";
                return;
            }

            // Dummy login-sjekk
            if (Username == "admin" && Password == "1234")
            {
                ErrorMessage = string.Empty;
                await Application.Current.MainPage.Navigation.PushAsync(new PolarTQR.pages.HomePage());
            }
            else
            {
                ErrorMessage = "Feil brukernavn eller passord.";
            }
        }
    }
}
