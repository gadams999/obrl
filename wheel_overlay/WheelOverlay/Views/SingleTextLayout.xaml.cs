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
        private bool _isInitialized = false;
        
        // Animation queue management
        private Queue<PositionChange> _animationQueue = new Queue<PositionChange>();
        private Stopwatch _lagTimer = new Stopwatch();
        private int _targetPosition = -1;
        
        private struct PositionChange
        {
            public int NewPosition { get; set; }
            public int OldPosition { get; set; }
            public int PositionCount { get; set; }
            public DateTime Timestamp { get; set; }
        }
        
        public SingleTextLayout()
        {
            InitializeComponent();
            Loaded += (s, e) => _isInitialized = true;
        }
        
        public void OnPositionChanged(int newPosition, int oldPosition, int positionCount)
        {
            // Don't animate on first load or if not initialized
            if (!_isInitialized || oldPosition == -1)
            {
                _currentPosition = newPosition;
                _targetPosition = newPosition;
                return;
            }
            
            // Check if animations are disabled
            var viewModel = DataContext as OverlayViewModel;
            if (viewModel?.Settings?.EnableAnimations == false)
            {
                // Skip animation, just update the text immediately
                _currentPosition = newPosition;
                _targetPosition = newPosition;
                CurrentText.Text = viewModel.DisplayedText;
                return;
            }
            
            // Update target position
            _targetPosition = newPosition;
            
            // Add to queue
            _animationQueue.Enqueue(new PositionChange
            {
                NewPosition = newPosition,
                OldPosition = oldPosition,
                PositionCount = positionCount,
                Timestamp = DateTime.Now
            });
            
            // Start lag timer if not running
            if (!_lagTimer.IsRunning)
            {
                _lagTimer.Restart();
            }
            
            // Check for lag
            double lagMs = _lagTimer.Elapsed.TotalMilliseconds;
            
            // If we're lagging behind by more than MAX_LAG_MS, skip to the target position
            if (lagMs > MAX_LAG_MS && _animationQueue.Count > 1)
            {
                // Clear the queue except for the last position
                while (_animationQueue.Count > 1)
                {
                    _animationQueue.Dequeue();
                }
                
                // Stop current animation and jump to target
                if (_isAnimating)
                {
                    StopCurrentAnimation();
                }
                
                // Reset lag timer
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
            
            // Determine direction
            bool isForward = IsForwardTransition(change.OldPosition, change.NewPosition, change.PositionCount);
            
            // Start animation
            await AnimateTransitionAsync(change.NewPosition, isForward);
            
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
        
        private async Task AnimateTransitionAsync(int newPosition, bool isForward)
        {
            _isAnimating = true;
            
            // Get the ViewModel to access the text for the new position
            var viewModel = DataContext as OverlayViewModel;
            if (viewModel == null)
            {
                _isAnimating = false;
                return;
            }
            
            // Set up next text
            NextText.Text = viewModel.DisplayedText;
            
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
            
            // Swap texts
            CurrentText.Text = NextText.Text;
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
