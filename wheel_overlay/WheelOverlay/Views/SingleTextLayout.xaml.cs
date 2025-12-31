using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WheelOverlay.ViewModels;

namespace WheelOverlay.Views
{
    public partial class SingleTextLayout : System.Windows.Controls.UserControl
    {
        private const double ANIMATION_DURATION_MS = 250;
        private const double ROTATION_ANGLE = 15; // degrees
        private const double TRANSLATION_DISTANCE = 50; // pixels
        private const double MAX_LAG_MS = 100; // Maximum acceptable lag before skipping animations
        
        private int _currentPosition = -1;
        private bool _isAnimating = false;
        private DateTime _lastPositionChangeTime = DateTime.MinValue;
        
        // Animation queue management
        private Queue<PositionChange> _animationQueue = new Queue<PositionChange>();
        private Stopwatch _lagTimer = new Stopwatch();
        private int _targetPosition = -1;
        
        private struct PositionChange
        {
            public int NewPosition { get; set; }
            public int OldPosition { get; set; }
            public OverlayViewModel? ViewModel { get; set; }
            public DateTime Timestamp { get; set; }
        }
        
        public SingleTextLayout()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }
        
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Initialize the current position from the DataContext (ViewModel) if available
            if (DataContext is OverlayViewModel viewModel)
            {
                _currentPosition = viewModel.CurrentPosition;
                _targetPosition = viewModel.CurrentPosition;
                CurrentText.Text = viewModel.GetTextForPosition(viewModel.CurrentPosition);
            }
        }
        
        public void OnPositionChanged(int newPosition, OverlayViewModel? viewModel)
        {
            // Validate viewModel and settings
            if (viewModel?.Settings?.ActiveProfile == null)
                return;
            
            // At this point, viewModel is guaranteed to be non-null
            var vm = viewModel;
            
            // Use _currentPosition as oldPosition (not external parameter)
            int oldPosition = _currentPosition;
            int positionCount = vm.Settings.ActiveProfile.PositionCount;
            
            // If this is the first call (_currentPosition == -1), initialize from ViewModel
            // This handles the case where the ViewModel already has a position set before the first change
            if (oldPosition == -1)
            {
                // Check if the ViewModel already has a position set (not the default 0)
                // If newPosition matches ViewModel's current position, this is just initialization
                if (newPosition == vm.CurrentPosition)
                {
                    // This is the initial sync - just set the position without animation
                    _currentPosition = newPosition;
                    _targetPosition = newPosition;
                    CurrentText.Text = vm.GetTextForPosition(newPosition);
                    return;
                }
                else
                {
                    // This is the first actual position change
                    // Initialize oldPosition from ViewModel's current position
                    oldPosition = vm.CurrentPosition;
                    _currentPosition = oldPosition;
                    // Don't return - continue to animate from oldPosition to newPosition
                }
            }
            
            // Ignore duplicate position changes (already at this position or already targeting it)
            if (newPosition == _currentPosition || newPosition == _targetPosition)
            {
                return;
            }
            
            // If the position hasn't actually changed, ignore this call
            // This prevents re-processing the same position multiple times
            if (newPosition == _currentPosition && newPosition == _targetPosition)
            {
                return;
            }
            
            // Check if animations are disabled
            if (vm.Settings?.EnableAnimations == false)
            {
                // Skip animation, just update the text immediately
                _currentPosition = newPosition;
                _targetPosition = newPosition;
                CurrentText.Text = vm.DisplayedText;
                return;
            }
            
            // Update target position
            _targetPosition = newPosition;
            
            // Check if we're receiving rapid position changes (rotating the wheel)
            // If a new position change comes in less than ANIMATION_DURATION_MS after the last one,
            // it's rapid input and we should skip animation
            DateTime now = DateTime.Now;
            double timeSinceLastChange = (now - _lastPositionChangeTime).TotalMilliseconds;
            _lastPositionChangeTime = now;
            
            bool isRapidInput = timeSinceLastChange < ANIMATION_DURATION_MS && timeSinceLastChange > 0;
            
            if (isRapidInput)
            {
                // During rapid rotation, skip animation and jump directly to the new position
                // Clear the queue since we're jumping
                _animationQueue.Clear();
                
                // Stop any current animation
                if (_isAnimating)
                {
                    StopCurrentAnimation();
                }
                
                // Jump directly to the new position
                _currentPosition = newPosition;
                CurrentText.Text = vm.GetTextForPosition(newPosition);
                return;
            }
            
            // Single position change - queue for animation
            _animationQueue.Enqueue(new PositionChange
            {
                NewPosition = newPosition,
                OldPosition = oldPosition,
                ViewModel = vm,
                Timestamp = DateTime.Now
            });
            
            // Start lag timer if not running
            if (!_lagTimer.IsRunning)
            {
                _lagTimer.Restart();
            }
            
            // Process queue if not currently animating
            if (!_isAnimating && _animationQueue.Count > 0)
            {
                ProcessNextAnimation();
            }
        }
        
        private async void ProcessNextAnimation()
        {
            if (_animationQueue.Count == 0 || _isAnimating)
                return;
            
            var change = _animationQueue.Dequeue();
            
            // Skip if ViewModel is null
            if (change.ViewModel == null)
                return;
            
            // Extract positionCount from ViewModel
            int positionCount = change.ViewModel.Settings?.ActiveProfile?.PositionCount ?? 0;
            
            // Determine direction
            bool isForward = IsForwardTransition(change.OldPosition, change.NewPosition, positionCount);
            
            // Start animation with oldPosition and viewModel
            await AnimateTransitionAsync(change.NewPosition, change.OldPosition, change.ViewModel, isForward);
            
            // Reset lag timer after animation completes
            _lagTimer.Restart();
            
            // Process next animation if queue is not empty
            if (_animationQueue.Count > 0)
            {
                ProcessNextAnimation();
            }
            else
            {
                // Stop lag timer when queue is empty
                _lagTimer.Stop();
            }
        }
        
        public bool IsForwardTransition(int oldPos, int newPos, int positionCount)
        {
            // Handle wrap-around
            if (oldPos == positionCount - 1 && newPos == 0)
                return true; // Wrapping forward
            if (oldPos == 0 && newPos == positionCount - 1)
                return false; // Wrapping backward
            
            return newPos > oldPos;
        }
        
        private async Task AnimateTransitionAsync(int newPosition, int oldPosition, OverlayViewModel viewModel, bool isForward)
        {
            _isAnimating = true;
            
            // Validate viewModel
            if (viewModel == null)
            {
                _isAnimating = false;
                return;
            }
            
            // Fetch text for specific positions (not current ViewModel state)
            string oldText = viewModel.GetTextForPosition(oldPosition);
            string newText = viewModel.GetTextForPosition(newPosition);
            
            // Set up current and next text correctly
            CurrentText.Text = oldText;  // OLD position text
            NextText.Text = newText;     // NEW position text
            
            // Create animations
            var duration = TimeSpan.FromMilliseconds(ANIMATION_DURATION_MS);
            
            // Current text: rotate and fade out
            var currentRotateAnim = new DoubleAnimation
            {
                To = isForward ? -ROTATION_ANGLE : ROTATION_ANGLE,
                Duration = duration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            
            var currentTranslateAnim = new DoubleAnimation
            {
                To = isForward ? -TRANSLATION_DISTANCE : TRANSLATION_DISTANCE,
                Duration = duration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            
            var currentOpacityAnim = new DoubleAnimation
            {
                To = 0,
                Duration = duration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            
            // Next text: rotate and fade in
            var nextRotateAnim = new DoubleAnimation
            {
                From = isForward ? ROTATION_ANGLE : -ROTATION_ANGLE,
                To = 0,
                Duration = duration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            var nextTranslateAnim = new DoubleAnimation
            {
                From = isForward ? TRANSLATION_DISTANCE : -TRANSLATION_DISTANCE,
                To = 0,
                Duration = duration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            var nextOpacityAnim = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = duration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            // Apply animations
            CurrentRotate.BeginAnimation(RotateTransform.AngleProperty, currentRotateAnim);
            CurrentTranslate.BeginAnimation(TranslateTransform.YProperty, currentTranslateAnim);
            CurrentText.BeginAnimation(OpacityProperty, currentOpacityAnim);
            
            NextRotate.BeginAnimation(RotateTransform.AngleProperty, nextRotateAnim);
            NextTranslate.BeginAnimation(TranslateTransform.YProperty, nextTranslateAnim);
            NextText.BeginAnimation(OpacityProperty, nextOpacityAnim);
            
            // Wait for animation to complete
            await Task.Delay((int)ANIMATION_DURATION_MS);
            
            // Swap texts - use newText (not NextText.Text in case it changed)
            CurrentText.Text = newText;
            CurrentText.Opacity = 1;
            CurrentRotate.Angle = 0;
            CurrentTranslate.Y = 0;
            
            NextText.Opacity = 0;
            NextRotate.Angle = 0;
            NextTranslate.Y = 0;
            
            _currentPosition = newPosition;
            _isAnimating = false;
        }
        
        public void StopCurrentAnimation()
        {
            // Stop all animations
            CurrentRotate.BeginAnimation(RotateTransform.AngleProperty, null);
            CurrentTranslate.BeginAnimation(TranslateTransform.YProperty, null);
            CurrentText.BeginAnimation(OpacityProperty, null);
            
            NextRotate.BeginAnimation(RotateTransform.AngleProperty, null);
            NextTranslate.BeginAnimation(TranslateTransform.YProperty, null);
            NextText.BeginAnimation(OpacityProperty, null);
            
            // Reset to stable state
            CurrentText.Opacity = 1;
            CurrentRotate.Angle = 0;
            CurrentTranslate.Y = 0;
            
            NextText.Opacity = 0;
            NextRotate.Angle = 0;
            NextTranslate.Y = 0;
            
            _isAnimating = false;
        }
        
        // Public method to check if lagging (for testing)
        public bool IsLagging()
        {
            return _lagTimer.IsRunning && _lagTimer.Elapsed.TotalMilliseconds > MAX_LAG_MS;
        }
        
        // Public method to get queue count (for testing)
        public int GetQueueCount()
        {
            return _animationQueue.Count;
        }
    }
}
