using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Posroid
{
    sealed partial class MealBlock : UserControl
    {
        public MealBlock()
        {
            this.InitializeComponent();
        }

        public MealBlock(String Type, When when, Int32 kcal)
        {
            this.InitializeComponent();
            switch (when)
            {
                case When.Breakfast:
                    LayoutRoot.Background = new SolidColorBrush(new Windows.UI.Color() { A = 0xFF, R = 0x0E, G = 0x9C, B = 0x00 });//<!--R:FFFF5FBE B:FF5F8BFF Y:FFF8FF5F G:FF6EFF5F-->
                    mealtimeText.Text = "Breakfast";
                    break;
                case When.Lunch:
                    LayoutRoot.Background = new SolidColorBrush(new Windows.UI.Color() { A = 0xFF, R = 0x00, G = 0x63, B = 0x9C });
                    mealtimeText.Text = "Lunch";
                    break;
                case When.Dinner:
                    LayoutRoot.Background = new SolidColorBrush(new Windows.UI.Color() { A = 0xFF, R = 0xE0, G = 0x00, B = 0x85 });
                    mealtimeText.Text = "Dinner";
                    break;
            }
            typeText.Text = Type;
            caloriesText.Text = String.Format("{0}kcal", kcal);
        }
    }
}
