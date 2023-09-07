using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
                if (_isOnLeft != value)
                {
                    IsInProgress = true;
                    _isOnLeft = value;

                    // Move animation
                    ThicknessAnimation moveAnimation = new ThicknessAnimation
                    {
                        Duration = new Duration(TimeSpan.FromSeconds(ProgressTime)),
                    };

                    Storyboard storyboard = new Storyboard();
                    storyboard.Children.Add(moveAnimation);
                    Storyboard.SetTarget(moveAnimation, ThumbEllipse);
                    Storyboard.SetTargetProperty(moveAnimation, new PropertyPath("Margin"));
                    storyboard.Completed += (s, e) => IsInProgress = false;  // Reset the flag when the progress is completed

                    // Check if the colors are different before adding the color animation
                    if (ColorLeft != ColorRight)
                    {
                        // Color animation
                        ColorAnimation colorAnimation = new ColorAnimation
                        {
                            Duration = new Duration(TimeSpan.FromSeconds(ProgressTime))
                        };

                        storyboard.Children.Add(colorAnimation);
                        Storyboard.SetTarget(colorAnimation, ThumbEllipse);
                        Storyboard.SetTargetProperty(colorAnimation, new PropertyPath("(Shape.Fill).(SolidColorBrush.Color)"));

                        // Determine the direction of the animation based on the current state
                        if (value)
                        {
                            colorAnimation.From = ColorRight;
                            colorAnimation.To = ColorLeft;
                        }
                        else
                        {
                            colorAnimation.From = ColorLeft;
                            colorAnimation.To = ColorRight;
                        }
                    }

                    // Determine the direction of the move animation based on the current state
                    if (value)
                    {
                        moveAnimation.From = new Thickness(60, 0, 0, 0);
                        moveAnimation.To = new Thickness(0, 0, 0, 0);
                    }
                    else
                    {
                        moveAnimation.From = new Thickness(0, 0, 0, 0);
                        moveAnimation.To = new Thickness(60, 0, 0, 0);
                    }

                    // Begin the animation
                    storyboard.Begin();
                }
                else
                {
                    IsInProgress = false;
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

        public Color ColorLeft
        {
            get { return (Color)GetValue(ColorLeftProperty); }
            set { SetValue(ColorLeftProperty, value); }
        }

        public Color ColorRight
        {
            get { return (Color)GetValue(ColorRightProperty); }
            set { SetValue(ColorRightProperty, value); }
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsInProgress)
            {
                IsInProgress = true;
                OnSwitched();
            }
        }

        public CustomSwitchWithText()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ColorLeftProperty =
            DependencyProperty.Register("ColorLeft", typeof(Color), typeof(CustomSwitchWithText), new PropertyMetadata(Colors.Transparent, OnColorLeftChanged));

        public static readonly DependencyProperty ColorRightProperty =
            DependencyProperty.Register("ColorRight", typeof(Color), typeof(CustomSwitchWithText), new PropertyMetadata(Colors.Transparent, OnColorRightChanged));

        private static void OnColorLeftChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CustomSwitchWithText customSwitch = (CustomSwitchWithText)d;
            customSwitch.UpdateThumbEllipseFill();
        }

        private static void OnColorRightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CustomSwitchWithText customSwitch = (CustomSwitchWithText)d;
            customSwitch.UpdateThumbEllipseFill();
        }

        private void UpdateThumbEllipseFill()
        {
            if (ThumbEllipse != null)  // Check if the element is initialized
            {
                ThumbEllipse.Fill = new SolidColorBrush(IsOnLeft ? ColorLeft : ColorRight);
            }
        }

        public delegate void SwitchedEventHandler(object sender, EventArgs e);
        public event SwitchedEventHandler? Switched;

        protected virtual void OnSwitched()
        {
            Switched?.Invoke(this, EventArgs.Empty);
        }
    }
}
