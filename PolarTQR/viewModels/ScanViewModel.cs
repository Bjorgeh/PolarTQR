using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage; // For MediaPicker

namespace PolarTQR.ViewModels
{
    public class ScanViewModel : BindableObject
    {
        private ImageSource _photoPath;
        private bool _isPhotoVisible;
        private string _statusMessage;

        public ICommand OpenCameraCommand { get; }

        public ImageSource PhotoPath
        {
            get => _photoPath;
            set
            {
                _photoPath = value;
                OnPropertyChanged();
            }
        }

        public bool IsPhotoVisible
        {
            get => _isPhotoVisible;
            set
            {
                _isPhotoVisible = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public ScanViewModel()
        {
            OpenCameraCommand = new Command(async () => await OpenCamera());
            IsPhotoVisible = false;
            StatusMessage = "Trykk for å åpne kamera.";
        }

        private async Task OpenCamera()
        {
            try
            {
                StatusMessage = "Åpner kamera...";
                System.Diagnostics.Debug.WriteLine("OpenCamera: Start");

                FileResult photo = await MediaPicker.CapturePhotoAsync();

                if (photo != null)
                {
                    System.Diagnostics.Debug.WriteLine("OpenCamera: Photo captured");

                    var stream = await photo.OpenReadAsync();
                    PhotoPath = ImageSource.FromStream(() => stream);
                    IsPhotoVisible = true;
                    StatusMessage = "Bilde klart!";
                }
                else
                {
                    StatusMessage = "Ingen bilde tatt.";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Feil ved åpning av kamera: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Feil", $"Kunne ikke åpne kamera: {ex.Message}", "OK");
                StatusMessage = "Kamerafeil.";
            }
        }
    }
}
