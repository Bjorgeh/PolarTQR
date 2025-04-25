using System.Collections.ObjectModel;
using PolarTQR.Models;

namespace PolarTQR.ViewModels
{
    public class HomeViewModel : BindableObject
    {
        private ObservableCollection<Event> _events;
        private Event _selectedEvent;

        public ObservableCollection<Event> Events
        {
            get => _events;
            set
            {
                _events = value;
                OnPropertyChanged();
            }
        }

        public Event SelectedEvent
        {
            get => _selectedEvent;
            set
            {
                _selectedEvent = value;
                OnPropertyChanged();
                OnEventSelected(_selectedEvent);
            }
        }

        public HomeViewModel()
        {
            // Dummy data – kan byttes ut med API-henting senere
            Events = new ObservableCollection<Event>
            {
                new Event { Name = "Konsert Oslo Spektrum" },
                new Event { Name = "Tech Meetup 2025" },
                new Event { Name = "Festival Stavanger" }
            };
        }

        private async void OnEventSelected(Event selectedEvent)
        {
            if (selectedEvent != null)
            {
                // Naviger til ScanPage med valgt event
                await Application.Current.MainPage.Navigation.PushAsync(new PolarTQR.pages.ScanPage());
            }
        }
    }
}
