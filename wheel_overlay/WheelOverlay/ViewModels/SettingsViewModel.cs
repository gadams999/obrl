using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WheelOverlay.Models;

namespace WheelOverlay.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private Profile _selectedProfile = new Profile();
        
        public Profile SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                _selectedProfile = value;
                OnPropertyChanged();
                RefreshTextLabelInputs();
                RefreshGridPreview();
                RefreshSuggestedDimensions();
                OnPropertyChanged(nameof(GridCapacityDisplay));
            }
        }
        
        public List<int> AvailablePositionCounts { get; } = 
            Enumerable.Range(2, 19).ToList(); // 2-20
        
        public List<int> AvailableRows { get; } = 
            Enumerable.Range(1, 10).ToList(); // 1-10
        
        public List<int> AvailableColumns { get; } = 
            Enumerable.Range(1, 10).ToList(); // 1-10
        
        private ObservableCollection<TextLabelInput> _textLabelInputs = new ObservableCollection<TextLabelInput>();
        public ObservableCollection<TextLabelInput> TextLabelInputs
        {
            get => _textLabelInputs;
            set { _textLabelInputs = value; OnPropertyChanged(); }
        }
        
        private ObservableCollection<string> _gridPreviewCells = new ObservableCollection<string>();
        public ObservableCollection<string> GridPreviewCells
        {
            get => _gridPreviewCells;
            set { _gridPreviewCells = value; OnPropertyChanged(); }
        }
        
        private ObservableCollection<SuggestedDimension> _suggestedDimensions = new ObservableCollection<SuggestedDimension>();
        public ObservableCollection<SuggestedDimension> SuggestedDimensions
        {
            get => _suggestedDimensions;
            set { _suggestedDimensions = value; OnPropertyChanged(); }
        }
        
        public string GridCapacityDisplay
        {
            get
            {
                if (SelectedProfile == null) return "";
                int capacity = SelectedProfile.GridRows * SelectedProfile.GridColumns;
                return $"Grid Capacity: {capacity} (Position Count: {SelectedProfile.PositionCount})";
            }
        }
        
        public ICommand ApplySuggestedDimensionCommand { get; }
        
        public SettingsViewModel()
        {
            ApplySuggestedDimensionCommand = new RelayCommand<SuggestedDimension>(ApplySuggestedDimension);
        }
        
        public void UpdatePositionCount(int newCount)
        {
            if (SelectedProfile == null) return;
            
            int oldCount = SelectedProfile.TextLabels.Count;
            
            if (newCount > oldCount)
            {
                // Add empty labels
                for (int i = oldCount; i < newCount; i++)
                {
                    SelectedProfile.TextLabels.Add("");
                }
            }
            else if (newCount < oldCount)
            {
                // Remove labels
                SelectedProfile.TextLabels.RemoveRange(newCount, oldCount - newCount);
            }
            
            SelectedProfile.PositionCount = newCount;
            RefreshTextLabelInputs();
            RefreshSuggestedDimensions();
            OnPropertyChanged(nameof(GridCapacityDisplay));
        }
        
        public void RefreshTextLabelInputs()
        {
            if (SelectedProfile == null) return;
            
            TextLabelInputs = new ObservableCollection<TextLabelInput>(
                SelectedProfile.TextLabels.Select((label, index) => 
                    new TextLabelInput
                    {
                        PositionNumber = $"Position {index + 1}:",
                        Label = label,
                        Index = index
                    }));
        }
        
        public void RefreshGridPreview()
        {
            if (SelectedProfile == null) return;
            
            var cells = new List<string>();
            int totalCells = SelectedProfile.GridRows * SelectedProfile.GridColumns;
            
            for (int i = 0; i < totalCells; i++)
            {
                if (i < SelectedProfile.PositionCount)
                {
                    cells.Add((i + 1).ToString());
                }
                else
                {
                    cells.Add(""); // Empty cell
                }
            }
            
            GridPreviewCells = new ObservableCollection<string>(cells);
        }
        
        public void RefreshSuggestedDimensions()
        {
            if (SelectedProfile == null) return;
            
            var suggestions = ProfileValidator.GetSuggestedDimensions(SelectedProfile.PositionCount);
            SuggestedDimensions = new ObservableCollection<SuggestedDimension>(
                suggestions.Select(d => new SuggestedDimension
                {
                    Rows = d.Rows,
                    Columns = d.Columns,
                    DisplayText = $"{d.Rows}×{d.Columns}"
                }));
        }
        
        private void ApplySuggestedDimension(SuggestedDimension dimension)
        {
            if (SelectedProfile == null) return;
            
            SelectedProfile.GridRows = dimension.Rows;
            SelectedProfile.GridColumns = dimension.Columns;
            OnPropertyChanged(nameof(SelectedProfile));
            RefreshGridPreview();
            OnPropertyChanged(nameof(GridCapacityDisplay));
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    public class TextLabelInput : INotifyPropertyChanged
    {
        public string PositionNumber { get; set; } = string.Empty;
        
        private string _label = string.Empty;
        public string Label
        {
            get => _label;
            set { _label = value; OnPropertyChanged(); }
        }
        
        public int Index { get; set; }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    public class SuggestedDimension
    {
        public int Rows { get; set; }
        public int Columns { get; set; }
        public string DisplayText { get; set; } = string.Empty;
    }
    
    // Simple RelayCommand implementation for the ViewModel
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool>? _canExecute;
        
        public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        
        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute((T)parameter!);
        }
        
        public void Execute(object? parameter)
        {
            _execute((T)parameter!);
        }
        
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
