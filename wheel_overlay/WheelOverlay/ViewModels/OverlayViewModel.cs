using System.ComponentModel;
using System.Runtime.CompilerServices;
using WheelOverlay.Models;

namespace WheelOverlay.ViewModels
{
    public class OverlayViewModel : INotifyPropertyChanged
    {
        private int _currentPosition;
        private AppSettings _settings;

        public OverlayViewModel(AppSettings settings)
        {
            _settings = settings;
        }

        public AppSettings Settings
        {
            get => _settings;
            set
            {
                _settings = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayItems));
                OnPropertyChanged(nameof(CurrentItem));
            }
        }

        public string[] DisplayItems => _settings.TextLabels;

        public int CurrentPosition
        {
            get => _currentPosition;
            set
            {
                if (_currentPosition != value)
                {
                    _currentPosition = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentItem));
                }
            }
        }

        public string CurrentItem
        {
            get
            {
                if (_currentPosition >= 0 && _currentPosition < DisplayItems.Length)
                {
                    return DisplayItems[_currentPosition];
                }
                return string.Empty;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
