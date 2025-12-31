using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using WheelOverlay.Models;

namespace WheelOverlay.ViewModels
{
    public class OverlayViewModel : INotifyPropertyChanged
    {
        private int _currentPosition;
        private AppSettings _settings;
        private bool _isDeviceNotFound;
        private List<int> _populatedPositions;
        private bool _isFlashing;
        private CancellationTokenSource? _flashCancellationTokenSource;
        private int _lastPopulatedPosition;
        private bool _isTestMode;
        private ObservableCollection<GridPositionItem> _populatedPositionItems;

        public OverlayViewModel(AppSettings settings)
        {
            _settings = settings;
            _populatedPositions = new List<int>();
            _populatedPositionItems = new ObservableCollection<GridPositionItem>();
            _isFlashing = false;
            _lastPopulatedPosition = 0;
            _isTestMode = false; // Explicitly initialize to false
            UpdatePopulatedPositions();
            InitializeLastPopulatedPosition();
            UpdatePopulatedPositionItems();
        }

        private void InitializeLastPopulatedPosition()
        {
            // Set LastPopulatedPosition to the first populated position, or 0 if none exist
            if (_populatedPositions.Count > 0)
            {
                _lastPopulatedPosition = _populatedPositions[0];
            }
        }

        public AppSettings Settings
        {
            get => _settings;
            set
            {
                _settings = value;
                UpdatePopulatedPositions();
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayItems));
                OnPropertyChanged(nameof(CurrentItem));
                OnPropertyChanged(nameof(PopulatedPositionLabels));
                OnPropertyChanged(nameof(EffectiveGridRows));
                OnPropertyChanged(nameof(EffectiveGridColumns));
                UpdatePopulatedPositionItems();
            }
        }

        public List<int> PopulatedPositions
        {
            get => _populatedPositions;
            private set
            {
                _populatedPositions = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PopulatedPositionLabels));
            }
        }

        public bool IsFlashing
        {
            get => _isFlashing;
            set
            {
                if (_isFlashing != value)
                {
                    _isFlashing = value;
                    OnPropertyChanged();
                }
            }
        }

        public int LastPopulatedPosition
        {
            get => _lastPopulatedPosition;
            private set
            {
                if (_lastPopulatedPosition != value)
                {
                    _lastPopulatedPosition = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsTestMode
        {
            get => _isTestMode;
            set
            {
                if (_isTestMode != value)
                {
                    _isTestMode = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TestModeIndicatorText));
                }
            }
        }

        public string TestModeIndicatorText
        {
            get
            {
                if (!IsTestMode)
                    return string.Empty;
                
                return $"TEST MODE - Position {CurrentPosition + 1}";
            }
        }

        public List<string> PopulatedPositionLabels
        {
            get
            {
                var profile = _settings.ActiveProfile;
                if (profile == null || profile.TextLabels == null)
                    return new List<string>();

                return _populatedPositions
                    .Where(i => i >= 0 && i < profile.TextLabels.Count)
                    .Select(i => profile.TextLabels[i])
                    .ToList();
            }
        }

        public string[] DisplayItems => _settings.ActiveProfile?.TextLabels?.ToArray() ?? new string[0];

        public void UpdatePopulatedPositions()
        {
            var profile = _settings.ActiveProfile;
            if (profile == null || profile.TextLabels == null)
            {
                PopulatedPositions = new List<int>();
                return;
            }

            var populated = new List<int>();
            for (int i = 0; i < profile.TextLabels.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(profile.TextLabels[i]))
                {
                    populated.Add(i);
                }
            }
            PopulatedPositions = populated;
            UpdatePopulatedPositionItems();
        }

        public int CurrentPosition
        {
            get => _currentPosition;
            set
            {
                if (_currentPosition != value)
                {
                    _currentPosition = value;
                    HandlePositionChange(value);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentItem));
                    OnPropertyChanged(nameof(DisplayedText));
                    OnPropertyChanged(nameof(IsDisplayingEmptyPosition));
                    OnPropertyChanged(nameof(TestModeIndicatorText));
                    UpdatePopulatedPositionItems();
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

        // Properties for Single Layout
        public string DisplayedText
        {
            get
            {
                var profile = _settings.ActiveProfile;
                if (profile == null || profile.TextLabels == null)
                    return string.Empty;

                // If device not found, show the device not found message
                if (IsDeviceNotFound)
                    return "🚨 Not Found! 🚨";

                // Determine which position to display
                int displayPosition = _populatedPositions.Contains(_currentPosition)
                    ? _currentPosition
                    : _lastPopulatedPosition;

                // Ensure the position is valid
                if (displayPosition >= 0 && displayPosition < profile.TextLabels.Count)
                {
                    return profile.TextLabels[displayPosition];
                }

                return string.Empty;
            }
        }

        public bool IsDisplayingEmptyPosition
        {
            get
            {
                // If device not found, we're not displaying an empty position
                if (IsDeviceNotFound)
                    return false;

                // Check if current position is empty
                return !_populatedPositions.Contains(_currentPosition);
            }
        }

        private void HandlePositionChange(int newPosition)
        {
            bool isEmptyPosition = !_populatedPositions.Contains(newPosition);

            if (isEmptyPosition)
            {
                TriggerFlashAnimation();
                // Don't update LastPopulatedPosition for empty positions
            }
            else
            {
                StopFlashAnimation();
                LastPopulatedPosition = newPosition;
            }
        }

        public void TriggerFlashAnimation()
        {
            // Cancel any existing flash animation
            _flashCancellationTokenSource?.Cancel();
            _flashCancellationTokenSource = new CancellationTokenSource();

            IsFlashing = true;

            // Start async task to stop flashing after 500ms
            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(500, _flashCancellationTokenSource.Token);
                    IsFlashing = false;
                }
                catch (TaskCanceledException)
                {
                    // Flash was cancelled, which is expected behavior
                }
            });
        }

        public void StopFlashAnimation()
        {
            _flashCancellationTokenSource?.Cancel();
            IsFlashing = false;
        }

        // Grid Layout Properties and Methods

        /// <summary>
        /// Gets the effective number of rows for the grid layout after condensing.
        /// </summary>
        public int EffectiveGridRows
        {
            get
            {
                if (Settings?.ActiveProfile == null) return 2;

                int populatedCount = PopulatedPositions.Count;
                if (populatedCount == 0) return 1;

                var profile = Settings.ActiveProfile;
                int configuredCapacity = profile.GridRows * profile.GridColumns;

                // If all positions are populated, use configured dimensions
                if (populatedCount >= configuredCapacity)
                    return profile.GridRows;

                // Calculate condensed dimensions maintaining aspect ratio
                return CalculateCondensedRows(populatedCount, profile.GridRows, profile.GridColumns);
            }
        }

        /// <summary>
        /// Gets the effective number of columns for the grid layout after condensing.
        /// </summary>
        public int EffectiveGridColumns
        {
            get
            {
                if (Settings?.ActiveProfile == null) return 4;

                int populatedCount = PopulatedPositions.Count;
                if (populatedCount == 0) return 1;

                var profile = Settings.ActiveProfile;
                int configuredCapacity = profile.GridRows * profile.GridColumns;

                // If all positions are populated, use configured dimensions
                if (populatedCount >= configuredCapacity)
                    return profile.GridColumns;

                return CalculateCondensedColumns(populatedCount, profile.GridRows, profile.GridColumns);
            }
        }

        /// <summary>
        /// Gets the collection of populated position items for grid display.
        /// </summary>
        public ObservableCollection<GridPositionItem> PopulatedPositionItems
        {
            get => _populatedPositionItems;
            private set
            {
                _populatedPositionItems = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Calculates the condensed number of rows while maintaining aspect ratio.
        /// </summary>
        private int CalculateCondensedRows(int itemCount, int configuredRows, int configuredColumns)
        {
            if (itemCount == 0) return 1;
            
            // Maintain aspect ratio while condensing
            double aspectRatio = (double)configuredRows / configuredColumns;

            // Try different row counts and pick the one that:
            // 1. Has sufficient capacity (rows * columns >= itemCount)
            // 2. Maintains aspect ratio closest to configured
            // 3. Doesn't exceed configured dimensions
            
            int bestRows = 1;
            double bestAspectRatioDiff = double.MaxValue;
            
            for (int rows = 1; rows <= System.Math.Min(configuredRows, itemCount); rows++)
            {
                int columns = (int)System.Math.Ceiling((double)itemCount / rows);
                
                // Skip if columns exceed configured limit
                if (columns > configuredColumns)
                    continue;
                
                // Calculate aspect ratio for this configuration
                double testAspectRatio = (double)rows / columns;
                double diff = System.Math.Abs(testAspectRatio - aspectRatio);
                
                // If this is closer to the target aspect ratio, use it
                if (diff < bestAspectRatioDiff)
                {
                    bestAspectRatioDiff = diff;
                    bestRows = rows;
                }
            }

            return bestRows;
        }

        /// <summary>
        /// Calculates the condensed number of columns based on item count and rows.
        /// </summary>
        private int CalculateCondensedColumns(int itemCount, int configuredRows, int configuredColumns)
        {
            if (itemCount == 0) return 1;
            
            int rows = CalculateCondensedRows(itemCount, configuredRows, configuredColumns);
            int columns = (int)System.Math.Ceiling((double)itemCount / rows);
            
            // Ensure we don't exceed configured dimensions
            columns = System.Math.Min(columns, configuredColumns);
            
            return columns;
        }

        /// <summary>
        /// Updates the PopulatedPositionItems collection based on current state.
        /// </summary>
        private void UpdatePopulatedPositionItems()
        {
            var items = new ObservableCollection<GridPositionItem>();

            if (Settings?.ActiveProfile == null)
            {
                PopulatedPositionItems = items;
                return;
            }

            var profile = Settings.ActiveProfile;

            foreach (int position in PopulatedPositions)
            {
                bool isSelected = position == CurrentPosition;

                items.Add(new GridPositionItem
                {
                    PositionNumber = $"#{position + 1}",
                    Label = profile.TextLabels[position],
                    IsSelected = isSelected
                });
            }

            PopulatedPositionItems = items;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
