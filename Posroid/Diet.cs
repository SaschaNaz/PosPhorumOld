using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Posroid
{
    class DietMenu
    {
        public Day[] Days { get; private set; }
        public DietMenu(XDocument dietmenu)
        {
            List<Day> _days = new List<Day>();
            foreach (XElement day in dietmenu.Element("DietofWeek").Elements())
            {
                _days.Add(new Day(day));
            }
            Days = _days.ToArray();
        }
    }
    class Day
    {
        public Time[] Times { get; private set; }
        public DateTime ServedDate { get; private set; }
        public Day(XElement day)
        {
            List<Time> _times = new List<Time>();
            foreach (XElement time in day.Elements("Time"))
            {
                _times.Add(new Time(time));
            }
            Times = _times.ToArray();
            Int32 _month = (Int32)day.Attribute("Month");
            Int32 _day = (Int32)day.Attribute("Day");
            ServedDate = new DateTime(DateTime.Now.Year, _month, _day);
        }
    }
    class Time
    {
        public When Mealtime { get; private set; }
        public FoodsInfo[] WhatFoods { get; private set; }
        public Time(XElement time)
        {
            List<FoodsInfo> _foodinfo = new List<FoodsInfo>();
            foreach (XElement foods in time.Elements("Foods"))
            {
                _foodinfo.Add(new FoodsInfo(foods));
            }
            WhatFoods = _foodinfo.ToArray();
            String mealtime = (String)time.Attribute("When");
            switch (mealtime)
            {
                case "Breakfast":
                    Mealtime = When.Breakfast;
                    break;
                case "Lunch":
                    Mealtime = When.Lunch;
                    break;
                case "Dinner":
                    Mealtime = When.Dinner;
                    break;
            }
        }
    }
    enum When
    {
        Breakfast, Lunch, Dinner
    }
    class FoodsInfo
    {
        public Food[] Foods { get; private set; }
        public Int32 Kilocalories { get; private set; }
        public String Type { get; private set; }
        public FoodsInfo(XElement foods)
        {
            List<Food> foodlist = new List<Food>();
            foreach (XElement xfood in foods.Elements())
            {
                foodlist.Add(new Food(xfood));
            }
            Foods = foodlist.ToArray();
            Kilocalories = (Int32)foods.Attribute("Calories");
            Type = (String)foods.Attribute("Type");
        }
    }
    class Food
    {
        public LangString langstr { get; private set; }
        public Food(XElement food)
        {
            List<SingleLangString> slang = new List<SingleLangString>();
            foreach (XElement xname in food.Elements())
            {
                slang.Add(
                    new SingleLangString
                    {
                        Name = (String)xname.Attribute("Value"),
                        Language = new Windows.Globalization.Language((String)xname.Attribute("language"))
                    });
            }
            langstr = new LangString(slang.ToArray());
            //XElement[] xnames = food.Elements().ToArray();

        }
    }
    class LangString
    {
        SingleLangString[] Languages;
        public String NameByLanguage(params Windows.Globalization.Language[] language)
        {
            //http://msdn.microsoft.com/en-us/library/windows/apps/windows.globalization.applicationlanguages.aspx
            //Windows.Globalization.ApplicationLanguages.Languages.
            //new System.Globalization.CultureInfo(System.Globalization.CultureInfo.
            //CultureInfo 사용법...
            Int32 index = language.Length;
            Int32 sindex = 0;
            
            for (Int32 i = 0; i < Languages.Length; i++)
            {
                if (Languages[i].Language.LanguageTag == language[0].LanguageTag)
                {
                    index = 0;
                    sindex = i;
                    break;
                }
                for (Int32 i2 = 1; i2 < language.Length; i2++)
                    if (Languages[i].Language.LanguageTag == language[i2].LanguageTag)
                        if (i2 < index)
                        {
                            index = i2;
                            sindex = i;
                        }
                //if (str.Language == language)
                //{
                //    return str.Name;
                //}
            }
            if (index != language.Length)
                return Languages[sindex].Name;
            else
                return "(unknown)";
                //throw new Exception("There is no name for the language you specified.");
        }
        //public Boolean IsLanguageSupported(Windows.Globalization.Language language)
        //{
        //    foreach (SingleLangString str in Languages)
        //    {
        //        if (str.Language == language)
        //            return true;
        //    }
        //    return false;
        //}

        public LangString(params SingleLangString[] langs)
        {
            Languages = langs;
        }
    }
    class SingleLangString
    {
        public Windows.Globalization.Language Language { get; set; }
        public String Name { get; set; }
    }
    //public enum Languages
    //{
    //    Korean, English
    //}
}
