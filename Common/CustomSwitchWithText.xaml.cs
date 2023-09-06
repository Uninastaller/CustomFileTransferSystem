using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Common
{
   /// <summary>
   /// Interaction logic for CustomSwitchWithText.xaml
   /// </summary>
   public partial class CustomSwitchWithText : UserControl
   {
      bool isOn = false;

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
         // Create a ThicknessAnimation
         ThicknessAnimation animation = new ThicknessAnimation
         {
            Duration = new Duration(TimeSpan.FromSeconds(0.2))
         };

         // Determine the direction of the animation based on the current state
         if (isOn)
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
         ThumbEllipse.BeginAnimation(MarginProperty, animation);

         // Toggle the state
         isOn = !isOn;
      }

      public CustomSwitchWithText()
      {
         InitializeComponent();
      }
   }
}
