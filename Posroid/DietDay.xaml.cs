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
using Windows.Globalization.DateTimeFormatting;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Posroid
{
    public sealed partial class DietDay : UserControl
    {
        public DietDay()
        {
            this.InitializeComponent();
        }
    }


    public class DateConverter : Windows.UI.Xaml.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            if (value == null)
                throw new ArgumentNullException("value", "Value cannot be null.");

            if (!typeof(DateTime).Equals(value.GetType()))
                throw new ArgumentException("Value must be of type DateTime.", "value");

            DateTime dt = (DateTime)value;

            if (parameter == null)
            {
                // Date "7/27/2011 9:30:59 AM" returns "7/27/2011"
                return DateTimeFormatter.ShortDate.Format(dt);
            }
            else if ((string)parameter == "day")
            {
                // Date "7/27/2011 9:30:59 AM" returns "27"
                DateTimeFormatter dateFormatter = new DateTimeFormatter("{day.integer(2)}");
                return dateFormatter.Format(dt);
            }
            else if ((string)parameter == "month")
            {
                // Date "7/27/2011 9:30:59 AM" returns "JUL"
                DateTimeFormatter dateFormatter = new DateTimeFormatter("{month.integer(2)}");
                return dateFormatter.Format(dt);
            }
            else if ((string)parameter == "MM/DD")
            {
                // Date "7/27/2011 9:30:59 AM" returns "JUL"
                DateTimeFormatter dateFormatter1 = new DateTimeFormatter("{month.integer(2)}");
                DateTimeFormatter dateFormatter2 = new DateTimeFormatter("{day.integer(2)}");
                return String.Format("{0}/{1}",dateFormatter1.Format(dt), dateFormatter2.Format(dt));
            }
            else if ((string)parameter == "MM/DD-dayofweek")
            {

                DateTimeFormatter dateFormatter1 = new DateTimeFormatter("{month.integer(2)}");
                DateTimeFormatter dateFormatter2 = new DateTimeFormatter("{day.integer(2)}");
                DateTimeFormatter dateFormatter3 = new DateTimeFormatter("{dayofweek.full}");
                return String.Format("{0}/{1}\n{2}", dateFormatter1.Format(dt), dateFormatter2.Format(dt), dateFormatter3.Format(dt));
            }
            else if ((string)parameter == "dayofweek")
            {
                DateTimeFormatter dateFormatter = new DateTimeFormatter("{dayofweek.full}");
                return dateFormatter.Format(dt);
            }
            //else if ((string)parameter == "year")
            //{
            //    // Date "7/27/2011 9:30:59 AM" returns "2011"
            //    DateTimeFormatter dateFormatter = new DateTimeFormatter("{year.full}");
            //    return dateFormatter.Format(dt);
            //}
            else
            {
                // Requested format is unknown. Return in the original format.
                return dt.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            string strValue = value as string;
            DateTime resultDateTime;
            if (DateTime.TryParse(strValue, out resultDateTime))
            {
                return resultDateTime;
            }
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }

    public class LanguageConverter : Windows.UI.Xaml.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            if (value == null)
                throw new ArgumentNullException("value", "Value cannot be null.");

            if (!typeof(LangString).Equals(value.GetType()))
                throw new ArgumentException("Value must be of type LangString.", "value");

            LangString ls = (LangString)value;

            if (parameter == null)
            {
                //returns the name for OS default language for default value.
                return ls.NameByLanguage(
                    new Windows.Globalization.Language(Windows.Globalization.ApplicationLanguages.Languages[0]));
            }
            else
            {
                return ls.NameByLanguage(new Windows.Globalization.Language((String)parameter));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DayBrushConverter : Windows.UI.Xaml.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            if (value == null)
                throw new ArgumentNullException("value", "Value cannot be null.");

            if (!typeof(DateTime).Equals(value.GetType()))
                throw new ArgumentException("Value must be of type DateTime.", "value");

            DayOfWeek dayofweek = ((DateTime)value).DayOfWeek;

            if (dayofweek == DayOfWeek.Monday)
            {
                return new SolidColorBrush(new Windows.UI.Color() { A = 0xFF, R = 0xFF, G = 0x66, B = 0x66 });
            }
            else if (dayofweek == DayOfWeek.Tuesday)
            {
                return new SolidColorBrush(new Windows.UI.Color() { A = 0xFF, R = 0xFF, G = 0xC1, B = 0x66 });
            }
            else if (dayofweek == DayOfWeek.Wednesday)
            {
                return new SolidColorBrush(new Windows.UI.Color() { A = 0xFF, R = 0xFF, G = 0xFF, B = 0x66 });
            }
            else if (dayofweek == DayOfWeek.Thursday)
            {
                return new SolidColorBrush(new Windows.UI.Color() { A = 0xFF, R = 0x66, G = 0xFF, B = 0x66 });
            }
            else if (dayofweek == DayOfWeek.Friday)
            {
                return new SolidColorBrush(new Windows.UI.Color() { A = 0xFF, R = 0x66, G = 0x66, B = 0xFF });
            }
            else if (dayofweek == DayOfWeek.Saturday)
            {
                return new SolidColorBrush(new Windows.UI.Color() { A = 0xFF, R = 0xC1, G = 0x66, B = 0xFF });
            }
            else if (dayofweek == DayOfWeek.Sunday)
            {
                return new SolidColorBrush(new Windows.UI.Color() { A = 0xFF, R = 0xFF, G = 0x66, B = 0xFF });
            }
            else
            {
                return new SolidColorBrush(Windows.UI.Colors.Black);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MealFoodConverter : Windows.UI.Xaml.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            if (value == null)
                throw new ArgumentNullException("value", "Value cannot be null.");

            if (!typeof(Time[]).Equals(value.GetType()))
                throw new ArgumentException("Value must be of type Time array.", "value");

            Time[] times = (Time[])value;

            if (times == null)
            {
                return new Time[] { };
            }
            else
            {
                List<MealBlock> infolist = new List<MealBlock>();
                foreach (Time time in times)
                {
                    foreach (FoodsInfo info in time.WhatFoods)
                    {
                        infolist.Add(new MealBlock(info.Type, time.Mealtime, info.Kilocalories) { Margin = new Thickness(20,20,0,0), Width = 120, Height = 120 });
                    }
                }
                return infolist.ToArray();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            throw new NotImplementedException();
        }
    }
}
