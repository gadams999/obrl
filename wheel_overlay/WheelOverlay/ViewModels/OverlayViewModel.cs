using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using WheelOverlay.Models;

namespace WheelOverlay.ViewModels
{
    public class OverlayViewModel : INotifyPropertyChanged
    {
        private int _currentPosition;
        private AppSettings _settings;
        private bool _isDeviceNotFound;

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

        public string[] DisplayItems => _settings.ActiveProfile?.TextLabels?.ToArray() ?? new string[0];

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

        public bool IsDeviceNotFound
        {
            get => _isDeviceNotFound;
            set
            {
                if (_isDeviceNotFound != value)
                {
                    _isDeviceNotFound = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayMessage));
                }
            }
        }

        public string DisplayMessage => IsDeviceNotFound ? "🚨 Not Found! 🚨" : CurrentItem;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
