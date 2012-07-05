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
using System.Xml.Linq;
using System.Net.Http;
using Windows.Storage;
using System.Threading.Tasks;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Posroid
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class CustomGroupedPage : Posroid.Common.LayoutAwarePage
    {
        public CustomGroupedPage()
        {
            this.InitializeComponent();
            SetData();
        }

        async void SetData()
        {
            Boolean IsNewDataNeeded = false;

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            try
            {
                StorageFile file = await localFolder.GetFileAsync("dietMenuData.xml");
                try
                {
                    String str = await FileIO.ReadTextAsync(file);
                    DietMenu dietmenu = new DietMenu(XDocument.Parse(str));
                    if (new TimeSpan(DateTime.Now.Ticks - dietmenu.Days[0].ServedDate.Ticks).Days >= 7)
                        IsNewDataNeeded = true;
                    else
                        this.DefaultViewModel["Groups"] = dietmenu.Days;
                }
                catch
                {
                    IsNewDataNeeded = true;
                }
            }
            catch
            {
                IsNewDataNeeded = true;
            }

            if (IsNewDataNeeded)
            {
                String errorMessage = null;
                try
                {
                    XDocument parsedMenu = await ParseDietmenu();
                    DietMenu dietMenu = new DietMenu(parsedMenu);
                    this.DefaultViewModel["Groups"] = dietMenu.Days;

                    if (new TimeSpan(DateTime.Now.Ticks - dietMenu.Days[0].ServedDate.Ticks).Days < 7)
                    {
                        StorageFile file = await localFolder.CreateFileAsync("dietMenuData.xml", CreationCollisionOption.ReplaceExisting);
                        await FileIO.WriteTextAsync(file, parsedMenu.ToString());
                    }
                    else
                    {
                        await new Windows.UI.Popups.MessageDialog("이번 주 새 식단표가 아직 올라오지 않았습니다.").ShowAsync();
                    }
                }
                catch (HttpRequestException)
                {
                    errorMessage = "서버에 문제가 있거나 인터넷 연결이 원활하지 않습니다. 상태를 확인해 주신 후 앺 바에서 리프레시 버튼을 눌러 시간표를 다시 읽어 주시기 바랍니다.";
                }

                if (errorMessage != null)
                    await new Windows.UI.Popups.MessageDialog(errorMessage).ShowAsync();
                //localSettings.Values["dietMenuData"] = dietMenu.Stringify();
            }
        }

        async Task<XDocument> ParseDietmenu()
        {
            /*
             * 자 전체 구조는 이런거죠
             * Dietmenu 안에
             * Day가 들어 있고
             * Day 안에
             * Time이 들어 있고 - 이때 속성으로 아침점심저녁
             * Time 안에 각 Foods가 들어 있죠 그런거죠! - 이때 속성으로 ABCDEFG (?
             * 
             */

            XDocument xdietmenu = new XDocument();
            XElement dietofweek = new XElement("DietofWeek");
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(new Uri("http://page.postech.ac.kr/res1/")))
                {
                    String str = await response.Content.ReadAsStringAsync();
                    str = str.Remove(200, 126);
                    str = str.Replace("&nbsp;", " ");
                    str = str.Replace("<br>", "<br />");
                    str = str.Remove(str.IndexOf("<![if !supportMisalignedRows]>"), str.IndexOf("<![endif]>") - str.IndexOf("<![if !supportMisalignedRows]>") + 10);
                    str = str.Replace("</html>", "</body></html>");
                    XElement xelm = XElement.Parse(str);
                    XElement[] tablerows = xelm.Elements("body").First()
                        .Elements("p").ElementAt(6)//contains table
                        .Elements().First()//table
                        .Elements().ToArray();//tablerows

                    for (Int32 i = 3; i < tablerows.Length - 1; i += 2)//each row is for a day, 기둥 뒤에 공간 있...는 게 아니라 표 끝에 빈 줄 하나 있습니다. 진짜예요 height=0으로..;;;
                    {
                        XElement xday = new XElement("Day");
                        XElement[] columns = tablerows[i].Elements().ToArray();
                        XElement[] columnscal = tablerows[i + 1].Elements().ToArray();

                        #region 날짜
                        {
                            String[] daydayofweek = columns[0]
                                .Elements().First()//p
                                .Elements().First()//font
                                .Value.Split('\n');//날짜 및 요일
                            String[] monthday = daydayofweek[0].Split('/');
                            xday.Add(
                                new XAttribute("Month", Convert.ToInt32(monthday[0])),
                                new XAttribute("Day", Convert.ToInt32(monthday[1])));
                            //new XAttribute("DayofWeek", daydayofweek[1][1])); DateTime 클래스는 이미 DayOfWeek 프로퍼티를 갖고 있다. 따라서 또 넣을 필요는 없음.
                        }
                        #endregion

                        //아침
                        xday.Add(
                            new XElement("Time",
                                new XAttribute("When", "Breakfast"),
                                MakeFoodsElement(columns[1], columnscal[1], "B"),
                                MakeFoodsElement(columns[2], columnscal[2], "C")));

                        //점심
                        xday.Add(
                            new XElement("Time",
                                new XAttribute("When", "Lunch"),
                                MakeFoodsElement(columns[3], columnscal[3], "B"),
                                MakeDualFoodsElement(columns[5], columnscal[5], "C", "D")));

                        //저녁
                        xday.Add(
                            new XElement("Time",
                                new XAttribute("When", "Dinner"),
                                MakeFoodsElement(columns[4], columnscal[4], "B"),
                                MakeDualFoodsElement(columns[6], columnscal[6], "C", "D")));

                        dietofweek.Add(xday);
                    }
                    xdietmenu.Add(dietofweek);
                }
            }
            //지금은 시간이랑 타입을 하드코딩해서 넣지만 맨 윗줄에서 읽어들여서 자동화하는 방법을 생각해봅시다

            return xdietmenu;
        }

        //그냥 두 줄씩 떼어서 첫줄은 한국어 둘째줄은 영어 하면 될 것 같죠? ㅎㅎ... 쓰다가 영어 한 줄 빼먹은 걸 보셔야...
        //그러므로, 첫줄 한국어면 다음 영어 찾고, 없고 다음줄이 또 한국어면 그냥 넘기고 다음 Food 작성
        //한국어가 안 들어오고 영어가 먼저 들어왔다면 그냥 넘기고 다음 Food 작성
        /// <summary>
        /// 파싱하고 있는 테이블 중 한 칸 떼어 넘기면 파싱된 결과물을 줍니다!
        /// </summary>
        /// <param name="column">파싱하고 있는 테이블 중 한 칸</param>
        /// <param name="type">어느 코너에 나오는 음식인가요</param>
        /// <returns></returns>
        XElement MakeFoodsElement(XElement column, XElement column2, String type)
        {
            XElement[] foodcode = column
                .Elements().ToArray();//p
            List<String> strfood = new List<String>();
            foreach (XElement code in foodcode)
            {
                String str = "";
                foreach (XElement fontcode in code.Elements())//font 태그는 한 개만 있을 거 같죠? ㅎㅎㅎ... 내용 빈 font 태그 한번 보셔야...
                {
                    str += fontcode.Value;
                }
                if (str.Length > 0)
                    strfood.Add(code.Elements().First().Value);
            }

            XElement xfoods = new XElement("Foods", new XAttribute("Type", type), new XAttribute("Calories", column2.Elements().First().Elements().First().Value));

            Char FormerLineLanguage = 'E';//K for Korean, E for English
            XElement xfood = new XElement("Food");
            foreach (String str in strfood)
            {
                Char PresentLineLanguage;
                if (IsEnglish(str))
                    PresentLineLanguage = 'E';
                else
                    PresentLineLanguage = 'K';
                switch (FormerLineLanguage)
                {
                    case 'E':
                        switch (PresentLineLanguage)
                        {
                            case 'E':
                                xfoods.Add(xfood);
                                xfood = new XElement("Food");
                                xfood.Add(new XElement("Name", new XAttribute("language", "en-US"), new XAttribute("Value", str)));
                                break;
                            case 'K':
                                xfood.Add(new XElement("Name", new XAttribute("language", "ko"), new XAttribute("Value", str)));
                                break;
                        }
                        break;
                    case 'K':
                        switch (PresentLineLanguage)
                        {
                            case 'E':
                                xfood.Add(new XElement("Name", new XAttribute("language", "en-US"), new XAttribute("Value", str)));
                                xfoods.Add(xfood);
                                xfood = new XElement("Food");
                                break;
                            case 'K':
                                xfoods.Add(xfood);
                                xfood = new XElement("Food");
                                xfood.Add(new XElement("Name", new XAttribute("language", "ko"), new XAttribute("Value", str)));
                                break;
                        }
                        break;
                }
                FormerLineLanguage = PresentLineLanguage;
            }

            if (xfood.HasElements)
                xfoods.Add(xfood);
            //Boolean IsFormerLineRegular = true;
            //for (Int32 i2 = 0; i2 < strfood.Count; i2 += 2)
            //{
            //    XElement xfood = new XElement("Food");

            //    if (IsFormerLineRegular)
            //    {
            //        if (!IsEnglish(strfood[i2]))//이번 라인은 한국어, 한국어입니다. This line is Korean, Korean line. 今回のラインは韓国語、韓国語です。
            //        {
            //            xfood.Add(new XElement("Name", new XAttribute("language", "ko"), new XAttribute("Value", strfood[i2])));
            //            if (IsEnglish(strfood[i2 + 1]))
            //            {
            //                xfood.Add(new XElement("Name", new XAttribute("language", "en-US"), new XAttribute("Value", strfood[i2 + 1])));
            //                IsFormerLineRegular = true;
            //            }
            //            else
            //            {
            //                IsFormerLineRegular = false;
            //                i2--;
            //            }
            //        }
            //        else
            //        {
            //            xfood.Add(new XElement("Name", new XAttribute("language", "en-US"), new XAttribute("Value", strfood[i2])));
            //            IsFormerLineRegular = false;
            //            i2--;
            //        }
            //    }
            //    else
            //    {
            //        xfood.Add(new XElement("Name", new XAttribute("language", "ko"), new XAttribute("Value", strfood[i2])));
            //        if (IsEnglish(strfood[i2 + 1]))
            //        {
            //            xfood.Add(new XElement("Name", new XAttribute("language", "en-US"), new XAttribute("Value", strfood[i2 + 1])));
            //            IsFormerLineRegular = true;
            //        }
            //        else
            //        {
            //            IsFormerLineRegular = false;
            //            i2--;
            //        }
            //    }

            //    xfoods.Add(xfood);
            //}
            return xfoods;
        }

        public Boolean IsEnglish(String str)
        {
            foreach (Char c in str)
            {
                //if (!(c == '\u0020' // space
                //    || (c >= 0x0041 && c <= 0x005A) // english capital letter
                //    || (c >= 0x0061 && c <= 0x007A))) // english small letter
                if (c > 0x007F) // is it out of ASCII code - combines all punctuation marks and English letters.
                    return false;
            }
            return true;
        }


        /// <summary>
        /// 기존 포스로이드 앺과의 호환성 때문에 C/D는 합쳐놓은 듯한데... 그런 거 없고 따로 표시합니다. 그러려고 만들었어요. 역시 파싱하고 있는 테이블 중 한 칸 떼어 넘기면 파싱된 결과물을 줍니다.
        /// </summary>
        /// <param name="column">파싱하고 있는 테이블 중 한 칸</param>
        /// <param name="type1">어느 코너에 나오는 음식인가요1</param>
        /// <param name="type2">어느 코너에 나오는 음식인가요2</param>
        /// <returns></returns>
        XElement[] MakeDualFoodsElement(XElement column, XElement column2, String type1, String type2)
        {
            XElement[] foodcode = column
                .Elements().ToArray();//p
            List<String> strfood = new List<String>();
            foreach (XElement code in foodcode)
            {
                String str = "";
                foreach (XElement fontcode in code.Elements())//font 태그는 한 개만 있을 거 같죠? ㅎㅎㅎ... 내용 빈 font 태그 한번 보셔야...
                {
                    str += fontcode.Value;
                }
                if (str.Length > 0)//빈 문자열은 갖다 버린다!.. 왜 엔터는 쳐서 빈 곳을 만들어요 으아아
                    strfood.Add(str);
            }

            Boolean IsThereNoType1 = false, IsThereNoType2 = false;//C, D는 항상 함께 있을 거 같죠? ㅎㅎㅎㅎㅎㅎㅎ.... 'D코너' 한줄 추가되어 있고 C코너 사라진 꼴을 보셔야 ㅎㅎㅎ
            if (strfood.Count % 2 == 1)
            {
                if (strfood.First().Contains(String.Format("{0}코너", type1)))//왜 StartsWith 안쓰고 Contains 쓰냐면... 어떤주엔 'D코너' 써놓고 어떤주엔 '<D코너>' 써놓거든요. 제발 <D>는 안 썼으면...
                    IsThereNoType2 = true;
                else if (strfood.First().Contains(String.Format("{0}코너", type2)))
                    IsThereNoType1 = true;
                strfood.Remove(strfood.First());
            }
            if (strfood.Count != 0)
            {
                if (!IsThereNoType1 && !IsThereNoType2)
                {
                    XElement xfoods1 = new XElement("Foods", new XAttribute("Type", type1));
                    XElement xfoods2 = new XElement("Foods", new XAttribute("Type", type2));
                    for (Int32 i2 = 0; i2 < strfood.Count; i2 += 2)
                    {
                        XElement xfood1 = new XElement("Food");
                        XElement xfood2 = new XElement("Food");
                        String[] splitted1 = strfood[i2].Split('/');
                        String[] splitted2 = strfood[i2 + 1].Split('/');
                        String[] splittedcalories = column2.Elements().First().Elements().First().Value.Split('/');
                        xfood1.Add(new XElement("Name", new XAttribute("language", "ko"), new XAttribute("Value", splitted1[0])));
                        xfood1.Add(new XElement("Name", new XAttribute("language", "en-US"), new XAttribute("Value", splitted2[0])));
                        xfoods1.Add(new XAttribute("Calories", Convert.ToInt32(splittedcalories[0])));
                        xfoods1.Add(xfood1);

                        xfood2.Add(new XElement("Name", new XAttribute("language", "ko"), new XAttribute("Value", splitted1[1])));
                        xfood2.Add(new XElement("Name", new XAttribute("language", "en-US"), new XAttribute("Value", splitted2[1])));
                        xfoods2.Add(new XAttribute("Calories", Convert.ToInt32(splittedcalories[1])));
                        xfoods2.Add(xfood2);
                    }

                    return new XElement[] { xfoods1, xfoods2 };
                }
                else if (IsThereNoType1)
                {
                    XElement xfoods = new XElement("Foods", new XAttribute("Type", type2), new XAttribute("Calories", column2.Elements().First().Elements().First().Value));
                    for (Int32 i2 = 0; i2 < strfood.Count; i2 += 2)
                    {
                        XElement xfood = new XElement("Food");
                        xfood.Add(new XElement("Name", new XAttribute("language", "ko"), new XAttribute("Value", strfood[i2])));
                        xfood.Add(new XElement("Name", new XAttribute("language", "en-US"), new XAttribute("Value", strfood[i2 + 1])));
                        xfoods.Add(xfood);
                    }
                    return new XElement[] { xfoods };
                }
                else
                {
                    XElement xfoods = new XElement("Foods", new XAttribute("Type", type1), new XAttribute("Calories", column2.Elements().First().Elements().First().Value));
                    for (Int32 i2 = 0; i2 < strfood.Count; i2 += 2)
                    {
                        XElement xfood = new XElement("Food");
                        xfood.Add(new XElement("Name", new XAttribute("language", "ko"), new XAttribute("Value", strfood[i2])));
                        xfood.Add(new XElement("Name", new XAttribute("language", "en-US"), new XAttribute("Value", strfood[i2 + 1])));
                        xfoods.Add(xfood);
                    }
                    return new XElement[] { xfoods };
                }
            }
            else
                return new XElement[0];
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
        }
    }
}
