using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Common
{
    /// <summary>
    /// Interaction logic for CustomSwitchWithText.xaml
    /// </summary>
    public partial class CustomSwitchWithText : UserControl
    {
        private bool _isOnLeft = true;
        public bool IsInProgress { get; private set; } = false;
        public double ProgressTime { get; set; } = 1;

        public bool IsOnLeft
        {
            get { return _isOnLeft; }
            set
            {
                if (_isOnLeft != value && !IsInProgress)
                {
                    IsInProgress = true;
                    _isOnLeft = value;
                    ThicknessAnimation animation = new ThicknessAnimation
                    {
                        Duration = new Duration(TimeSpan.FromSeconds(ProgressTime)),
                    };

                    Storyboard storyboard = new Storyboard();
                    storyboard.Children.Add(animation);
                    Storyboard.SetTarget(animation, ThumbEllipse);
                    Storyboard.SetTargetProperty(animation, new PropertyPath("Margin"));
                    storyboard.Completed += (s, e) => IsInProgress = false;  // Reset the flag when the progress is completed

                    // Determine the direction of the animation based on the current state
                    if (value)
                    {
                        animation.From = new Thickness(60, 0, 0, 0);
                        animation.To = new Thickness(0, 0, 0, 0);
                    }
                    else
                    {
                        animation.From = new Thickness(0, 0, 0, 0);
                        animation.To = new Thickness(60, 0, 0, 0);
                    }

                    // Begin the animation
                    storyboard.Begin();
                }
            }
        }

        public string LeftText
        {
            get { return (string)GetValue(LeftTextProperty); }
            set { SetValue(LeftTextProperty, value); }
        }

        public static readonly DependencyProperty LeftTextProperty =
            DependencyProperty.Register("LeftText", typeof(string), typeof(CustomSwitchWithText), new PropertyMetadata(""));

        public string RightText
        {
            get { return (string)GetValue(RightTextProperty); }
            set { SetValue(RightTextProperty, value); }
        }

        public static readonly DependencyProperty RightTextProperty =
            DependencyProperty.Register("RightText", typeof(string), typeof(CustomSwitchWithText), new PropertyMetadata(""));

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            IsOnLeft = !IsOnLeft;
            OnSwitched();
        }

        public CustomSwitchWithText()
        {
            InitializeComponent();
        }

        public delegate void SwitchedEventHandler(object sender, EventArgs e);
        public event SwitchedEventHandler? Switched;

        protected virtual void OnSwitched()
        {
            Switched?.Invoke(this, EventArgs.Empty);
        }
    }
}
