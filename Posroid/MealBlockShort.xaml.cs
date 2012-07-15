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
    partial class MealBlockShort : UserControl, IMealBlock
    {
        public MealBlockShort()
        {
            this.InitializeComponent();
        }

        //아래 속성들은 다 사라지고 MealData 하나로 대체하겠습니다^-^
        //public SolidColorBrush BackgroundBrush { get; private set; }//MealData.Mealtime에서 바로 BackgroundBrush 컨버팅해 나올 수 있도록 컨버터 만듦
        //public String MealtimeString { get; private set; }//MealData.Mealtime으로 바인딩
        //public String TypeString { get; private set; }//MealData.FoodInformations.Type으로 바인딩
        //public String CaloriesString { get; private set; }//MealData.FoodInformations.Kilocalories에서 컨버팅
        //FoodsInfo FoodInformations { get;  set; }
        public MealData InternalData { get; private set; }

        public MealBlockShort(MealData data)
        {
            this.InitializeComponent();
            InternalData = data;
            //FoodInformations = info;
            //switch (mealtime)
            //{
            //    case When.Breakfast:
            //        BackgroundBrush = new SolidColorBrush(new Windows.UI.Color() { A = 0xFF, R = 0x0E, G = 0x9C, B = 0x00 });//<!--R:FFFF5FBE B:FF5F8BFF Y:FFF8FF5F G:FF6EFF5F-->
            //        MealtimeString = "Breakfast";
            //        break;
            //    case When.Lunch:
            //        BackgroundBrush = new SolidColorBrush(new Windows.UI.Color() { A = 0xFF, R = 0x00, G = 0x63, B = 0x9C });
            //        MealtimeString = "Lunch";
            //        break;
            //    case When.Dinner:
            //        BackgroundBrush = new SolidColorBrush(new Windows.UI.Color() { A = 0xFF, R = 0xE0, G = 0x00, B = 0x85 });
            //        MealtimeString = "Dinner";
            //        break;
            //}
            //TypeString = info.Type;
            //CaloriesString = String.Format("{0}kcal", info.Kilocalories);
            
        }
    }
}
