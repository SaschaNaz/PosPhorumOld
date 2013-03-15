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
using Windows.UI.ViewManagement;
using Windows.UI.ApplicationSettings;


// The Grouped Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234231

namespace Posroid
{
    /// <summary>
    /// A page that displays a grouped collection of items.
    /// </summary>
    public sealed partial class DietGroupedPage : Posroid.Common.LayoutAwarePage
    {
        Popup _settingsPopup;

        public DietGroupedPage()
        {
            this.InitializeComponent();
            //preferedLanguage = "ko";
        }

        void DietGroupedPage_CommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            SettingsCommand cmd = new SettingsCommand("sample", "Language", (x) =>
                {
                    Int32 _settingsWidth = 370;
                    Rect _windowBounds = Window.Current.Bounds;
                    _settingsPopup = new Popup();
                    _settingsPopup.Closed += OnPopupClosed;
                    Window.Current.Activated += OnWindowActivated;
                    _settingsPopup.IsLightDismissEnabled = true;
                    _settingsPopup.Width = _settingsWidth;
                    _settingsPopup.Height = _windowBounds.Height;

                    SimpleSettingsNarrow mypane = new SimpleSettingsNarrow()
                    {
                        Width = _settingsWidth,
                        Height = _windowBounds.Height
                    };
                    mypane.SettingChanged += delegate(object sender2, GlobalSettingChangedEventArgs e)
                    {
                        ApplicationData.Current.LocalSettings.Values["ForceKorean"] = e.Value;
                        Application.Current.Resources["ForceKorean"] = e.Value;
                        LanguageOptionUpdate();
                        //await new Windows.UI.Popups.MessageDialog(String.Format("Setting Changed: {0} / {1}", e.WhatSetting.ToString(), e.Value.ToString())).ShowAsync();
                    };                    

                    _settingsPopup.Child = mypane;
                    _settingsPopup.SetValue(Canvas.LeftProperty, _windowBounds.Width - _settingsWidth);
                    _settingsPopup.SetValue(Canvas.TopProperty, 0);
                    _settingsPopup.IsOpen = true;
                });

            args.Request.ApplicationCommands.Add(cmd);
        }

        private void OnWindowActivated(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.Deactivated)
            {
                _settingsPopup.IsOpen = false;
            }
        }

        void OnPopupClosed(object sender, object e)
        {
            Window.Current.Activated -= OnWindowActivated;
        }

        void LanguageOptionUpdate()
        {
            //Application.Current.Resources["preferedLanguage"] = "en-US";
            List<object> templist = textList.Items.ToList();
            textList.Items.Clear();
            foreach (Object o in templist)
                textList.Items.Add(o);
            //await Refresh();
        }

        //public String preferedLanguage
        //{
        //    //get
        //    //{
        //    //    if ((Boolean)Application.Current.Resources["ForceKorean"])
        //    //        return "ko";
        //    //    else
        //    //    {
        //    //        return null;
        //    //    }
        //    //}
        //    get;
        //    set;
        //}
        

        ApplicationViewState previousViewState;
        void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            ApplicationViewState myViewState = ApplicationView.Value;

            IsSwitching = true;
            switch (myViewState)
            {
                case ApplicationViewState.Snapped:
                    {
                        itemListView.SelectedItems.Clear();
                        foreach (Object o in textList.Items)
                        {
                            itemListView.SelectedItems.Add(o);
                        }
                        break;
                    }
                default:
                    {
                        if (previousViewState == ApplicationViewState.Snapped)
                        {
                            itemGridView.SelectedItems.Clear();
                            foreach (Object o in textList.Items)
                            {
                                itemGridView.SelectedItems.Add(o);
                            }
                        }
                        break;
                    }
            }
            previousViewState = myViewState;
            IsSwitching = false;
        }

        async Task SetData(Boolean ForceDataReload, Double scrollOffset, Char offsetDirection)
        {
            semanticView.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            Boolean IsNewDataNeeded = false;

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            if (!ForceDataReload)
            {
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
                        {
                            this.DefaultViewModel["Groups"] = dietmenu.Days;
                            itemGridViewZoomedOut.ItemsSource = groupedItemsViewSource.View.CollectionGroups;
                        }
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
            }
            else
                IsNewDataNeeded = true;

            String message = null;
            if (IsNewDataNeeded)
            {
                try
                {
                    XDocument parsedMenu = await ParseDietmenu();
                    DietMenu dietMenu = new DietMenu(parsedMenu);
                    this.DefaultViewModel["Groups"] = dietMenu.Days;
                    itemGridViewZoomedOut.ItemsSource = groupedItemsViewSource.View.CollectionGroups;

                    if (new TimeSpan(DateTime.Now.Ticks - dietMenu.Days[0].ServedDate.Ticks).Days < 7)
                    {
                        StorageFile file = await localFolder.CreateFileAsync("dietMenuData.xml", CreationCollisionOption.ReplaceExisting);
                        await FileIO.WriteTextAsync(file, parsedMenu.ToString());
                    }
                    else
                    {
                        if (!Application.Current.Resources.ContainsKey("NewDataChecked"))
                        {
                            message = "이번 주 새 식단표가 아직 올라오지 않았습니다. 몇 시간 뒤에 리프레시해서 다시 확인해 주세요.";
                            Application.Current.Resources["NewDataChecked"] = true;
                        }
                    }
                }
                catch (HttpRequestException)
                {
                    message = "서버에 문제가 있거나 인터넷 연결이 원활하지 않습니다. 상태를 확인해 주신 후 앺 바에서 리프레시 버튼을 눌러 시간표를 다시 읽어 주시기 바랍니다.";
                }
                //localSettings.Values["dietMenuData"] = dietMenu.Stringify();
            }

            RoutedEventHandler handler = null;
            handler = delegate
            {
                setOffset(scrollOffset, offsetDirection);
                itemGridView.Loaded -= handler;
            };
            itemGridView.Loaded += handler;
            semanticView.Visibility = Windows.UI.Xaml.Visibility.Visible;

            if (message != null)
                await new Windows.UI.Popups.MessageDialog(message).ShowAsync();
        }

        void setOffset(Double scrollOffset, Char offsetDirection)
        {
            if (ApplicationView.Value != ApplicationViewState.Snapped)
            {
                if (scrollOffset >= 0 && offsetDirection == 'H')
                    GetVisualChild<ScrollViewer>(itemGridView).ScrollToHorizontalOffset(scrollOffset);
                else
                {
                    foreach (object o in itemGridView.Items)
                    {
                        if ((o as MealData).ServedDate.Day == DateTime.Now.Day)
                        {
                            itemGridView.ScrollIntoView(o, ScrollIntoViewAlignment.Leading);
                            break;
                        }
                    }
                }
            }
            else
            {
                if (scrollOffset >= 0 && offsetDirection == 'V')
                    GetVisualChild<ScrollViewer>(itemListView).ScrollToVerticalOffset(scrollOffset);
                else
                {
                    foreach (object o in itemListView.Items)
                    {
                        if ((o as MealData).ServedDate.Day == DateTime.Now.Day)
                        {
                            itemListView.ScrollIntoView(o, ScrollIntoViewAlignment.Leading);
                            break;
                        }
                    }
                }
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
                    XElement[] tablerows;
                    {
                        XElement body = xelm.Element("body");
                        XElement table = null;
                        foreach(XElement xp in body.Elements("p"))
                        {
                            table = xp.Element("table");
                            if (table != null)
                                break;
                        }//contains table
                        tablerows = table.Elements().ToArray();//tablerows
                    }

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
                            String[] monthday = SplitIntoTwoBySlash(daydayofweek[0]);
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
            List<String> strfood = new List<String>();
            {
                XElement[] foodcode = column
                    .Elements().ToArray();//p
                foreach (XElement code in foodcode)
                {
                    String str = "";
                    foreach (XElement fontcode in code.Elements())//font 태그는 한 개만 있을 거 같죠? ㅎㅎㅎ... 내용 빈 font 태그 한번 보셔야...
                    {
                        str += fontcode.Value;
                    }
                    if (str.Length > 0)
                        strfood.Add(str);
                }
            }

            if (strfood.Count > 1)
            {
                String calint = "";
                {
                    XElement[] foodcode = column2
                        .Elements().ToArray();//p
                    foreach (XElement code in foodcode)
                    {
                        foreach (XElement fontcode in code.Elements())//font 태그는 한 개만 있을 거 같죠? ㅎㅎㅎ... 내용 빈 font 태그 한번 보셔야...
                        {
                            calint += fontcode.Value;
                        }
                    }
                }
                return ParseSingleFoodData(strfood, calint, type);
            }
            else
                return null;
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
            List<String> strfood = new List<String>();
            {
                XElement[] foodcode = column
                    .Elements().ToArray();//p
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
            }

            Boolean IsThereNoType1 = false, IsThereNoType2 = false;//C, D는 항상 함께 있을 거 같죠? ㅎㅎㅎㅎㅎㅎㅎ.... 'D코너' 한줄 추가되어 있고 C코너 사라진 꼴을 보셔야 ㅎㅎㅎ

            if (strfood.Count > 1)//코너 하나만 있는지 모두 있는지 판별. 하나만 있는 경우는 최상단에 해당 코너 이름을 써 주는 관습이 있습니다.
            {
                if (strfood.First().Contains(String.Format("{0}코너", type1)))//왜 StartsWith 안쓰고 Contains 쓰냐면... 어떤주엔 'D코너' 써놓고 어떤주엔 '<D코너>' 써놓거든요. 제발 <D>는 안 썼으면...
                {
                    IsThereNoType2 = true;
                    strfood.Remove(strfood.First());
                }
                else if (strfood.First().Contains(String.Format("{0}코너", type2)))
                {
                    IsThereNoType1 = true;
                    strfood.Remove(strfood.First());
                }//첫번째 줄을 무조건 지우면 안 돼요... 홀수인 이유가 코너이름 적으려고도 있겠지만 엔터치고 괄호안에 원산지 등 추가정보 적으면서 저렇게 되는 경우가 있어서...
            }

            if (strfood.Count > 1)//<행사이름> 써 놓고 그 이상 아무것도 없을 때 오류 방지
            {
                String calint = "";
                {
                    XElement[] foodcode = column2
                        .Elements().ToArray();//p
                    foreach (XElement code in foodcode)
                    {
                        foreach (XElement fontcode in code.Elements())//font 태그는 한 개만 있을 거 같죠? ㅎㅎㅎ... 내용 빈 font 태그 한번 보셔야...
                        {
                            calint += fontcode.Value;
                        }
                    }
                }

                if (!IsThereNoType1 && !IsThereNoType2)
                {
                    for (Int32 i = 1; i < strfood.Count; i++)//기본적으론 C와 D는 슬래시로만 구분되지만, 슬래시 앞에 엔터로 또 구분되어 있는 경우가 있다
                    {
                        if (strfood[i][0] == '/')
                        {
                            strfood[i - 1] += strfood[i];
                            strfood.Remove(strfood[i]);
                            i--;
                        }
                    }

                    XElement xfoods1 = new XElement("Foods", new XAttribute("Type", type1));
                    XElement xfoods2 = new XElement("Foods", new XAttribute("Type", type2));
                    if (calint.Length > 0)
                    {
                        String[] splittedcalories = SplitIntoTwoBySlash(calint);
                        if (splittedcalories.Length > 1)
                        {
                            xfoods1.Add(new XAttribute("Calories", splittedcalories[0]));
                            xfoods2.Add(new XAttribute("Calories", splittedcalories[1]));
                        }
                        else //이건 중간에 슬래시를 빼 먹은 것
                        {
                            Int32 length = splittedcalories[0].Length;
                            if (length > 4)
                            {
                                if (length % 2 == 1 && splittedcalories[0][0] < splittedcalories[0][length / 2])//길이가 같거나, 앞 식단 자리수가 뒷 식단 자리수보다 큼
                                {
                                    xfoods1.Add(new XAttribute("Calories", splittedcalories[0].Substring(0, length / 2 + 1)));
                                    xfoods2.Add(new XAttribute("Calories", splittedcalories[0].Substring(length / 2 + 1)));
                                }
                                else
                                {
                                    xfoods1.Add(new XAttribute("Calories", splittedcalories[0].Substring(0, length / 2)));
                                    xfoods2.Add(new XAttribute("Calories", splittedcalories[0].Substring(length / 2)));
                                }
                            }
                            else
                            {
                                xfoods1.Add(new XAttribute("Calories", splittedcalories[0]));
                                xfoods2.Add(new XAttribute("Calories", -1));
                            }
                        }
                    }
                    else
                    {
                        xfoods1.Add(new XAttribute("Calories", -1));
                        xfoods2.Add(new XAttribute("Calories", -1));
                    }

                    Char FormerLineLanguage = 'E';//K for Korean, E for English
                    XElement xfood1 = null;
                    XElement xfood2 = null;
                    foreach (String str in strfood)
                    {
                        Nullable<BracketData> bdata = ExtractBracket(str);
                        String processed = String.Empty;
                        if (bdata.HasValue)
                        {
                            processed = bdata.Value.AfterExtracted;
                            if (processed.Length == 0)
                            {
                                //마지막 코드에 괄호 추가, 더 할 일 없음. 바로 브레이크
                                XElement langToBeFixed = xfood2.Elements().Last();
                                XAttribute fixstr = langToBeFixed.Attribute("Value");
                                fixstr.Remove();
                                langToBeFixed.Add(new XAttribute("Value", (String)fixstr + bdata.Value.ExtractedString));
                                break;
                            }
                        }

                        //문자열 처리부분
                        String[] splitted = SplitIntoTwoBySlash(str);
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
                                        {
                                            xfoods1.Add(xfood1);
                                            xfoods2.Add(xfood2);
                                            xfood1 = new XElement("Food");
                                            xfood2 = new XElement("Food");
                                            xfood1.Add(new XElement("Name", new XAttribute("language", "ko"), new XAttribute("Value", "[이름 미등록]")));
                                            xfood2.Add(new XElement("Name", new XAttribute("language", "ko"), new XAttribute("Value", "[이름 미등록]")));
                                            xfood1.Add(new XElement("Name", new XAttribute("language", "en-US"), new XAttribute("Value", splitted[0])));
                                            if (splitted.Length > 1)
                                                xfood2.Add(new XElement("Name", new XAttribute("language", "en-US"), new XAttribute("Value", splitted[1])));
                                            else
                                                xfood2.Add(new XElement("Name", new XAttribute("language", "en-US"), new XAttribute("Value", "[name unregistered]")));
                                            break;
                                        }
                                    case 'K':
                                        {
                                            xfoods1.Add(xfood1);
                                            xfoods2.Add(xfood2);
                                            xfood1 = new XElement("Food");
                                            xfood2 = new XElement("Food");
                                            xfood1.Add(new XElement("Name", new XAttribute("language", "ko"), new XAttribute("Value", splitted[0])));
                                            if (splitted.Length > 1)
                                                xfood2.Add(new XElement("Name", new XAttribute("language", "ko"), new XAttribute("Value", splitted[1])));
                                            else
                                                xfood2.Add(new XElement("Name", new XAttribute("language", "ko"), new XAttribute("Value", "[이름 미등록]")));
                                            break;
                                        }
                                }
                                break;
                            case 'K':
                                switch (PresentLineLanguage)
                                {
                                    case 'E':
                                        {
                                            xfood1.Add(new XElement("Name", new XAttribute("language", "en-US"), new XAttribute("Value", splitted[0])));
                                            if (splitted.Length > 1)
                                                xfood2.Add(new XElement("Name", new XAttribute("language", "en-US"), new XAttribute("Value", splitted[1])));
                                            else
                                                xfood2.Add(new XElement("Name", new XAttribute("language", "en-US"), new XAttribute("Value", "[name unregistered]")));
                                            //xfoods.Add(xfood);
                                            //xfood = new XElement("Food");
                                            break;
                                        }
                                    case 'K':
                                        {
                                            xfood1.Add(new XElement("Name", new XAttribute("language", "en-US"), new XAttribute("Value", "[name unregistered]")));
                                            xfood2.Add(new XElement("Name", new XAttribute("language", "en-US"), new XAttribute("Value", "[name unregistered]")));
                                            xfoods1.Add(xfood1);
                                            xfoods2.Add(xfood2);
                                            xfood1 = new XElement("Food");
                                            xfood2 = new XElement("Food");
                                            xfood1.Add(new XElement("Name", new XAttribute("language", "ko"), new XAttribute("Value", splitted[0])));
                                            if (splitted.Length > 1)
                                                xfood2.Add(new XElement("Name", new XAttribute("language", "ko"), new XAttribute("Value", splitted[1])));
                                            else
                                                xfood2.Add(new XElement("Name", new XAttribute("language", "ko"), new XAttribute("Value", "[이름 미등록]")));
                                            break;
                                        }
                                }
                                break;
                        }
                        FormerLineLanguage = PresentLineLanguage;

                        if (processed.Length > 0)
                        {
                            //후처리부
                            Int32 splitpoint = str.IndexOf('/');
                            XElement langToBeFixed;
                            if (splitpoint < bdata.Value.StartPoint)
                            {
                                langToBeFixed = xfood1.Elements().Last();
                                XAttribute fixstr = langToBeFixed.Attribute("Value");
                                fixstr.Remove();
                                langToBeFixed.Add(new XAttribute("Value", (String)fixstr + bdata.Value.ExtractedString));
                            }
                            else
                            {
                                langToBeFixed = xfood2.Elements().Last();
                                XAttribute fixstr = langToBeFixed.Attribute("Value");
                                fixstr.Remove();
                                langToBeFixed.Add(new XAttribute("Value", (String)fixstr + bdata.Value.ExtractedString));
                            }
                        }
                    }

                    if (xfood1.HasElements)
                        xfoods1.Add(xfood1);
                    if (xfood2.HasElements)
                        xfoods2.Add(xfood2);

                    return new XElement[] { xfoods1, xfoods2 };
                }
                else if (IsThereNoType1)
                    return new XElement[] { ParseSingleFoodData(strfood, calint, type2) };
                else
                    return new XElement[] { ParseSingleFoodData(strfood, calint, type1) };
            }
            else
                return new XElement[0];
        }

        XElement ParseSingleFoodData(List<String> strfood, String calstr, String type)
        {
            XElement xfoods = new XElement("Foods", new XAttribute("Type", type));
            if (calstr.Length > 0)
                xfoods.Add(new XAttribute("Calories", calstr));
            else
                xfoods.Add(new XAttribute("Calories", -1));

            Char FormerLineLanguage = 'E';//K for Korean, E for English
            XElement xfood = null;//new XElement("Food");
            foreach (String str in strfood)
            {
                Nullable<BracketData> bdata = ExtractBracket(str);
                String processed = String.Empty;
                if (bdata.HasValue)
                {
                    processed = bdata.Value.AfterExtracted;
                    if (processed.Length == 0)
                    {
                        //마지막 코드에 괄호 추가, 더 할 일 없음. 바로 브레이크
                        XElement langToBeFixed = xfood.Elements().Last();
                        XAttribute fixstr = langToBeFixed.Attribute("Value");
                        fixstr.Remove();
                        langToBeFixed.Add(new XAttribute("Value", (String)fixstr + bdata.Value.ExtractedString));
                        break;
                    }
                }


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
                                xfoods.Add(xfood);
                                xfood = new XElement("Food");
                                xfood.Add(new XElement("Name", new XAttribute("language", "ko"), new XAttribute("Value", str)));
                                break;
                        }
                        break;
                    case 'K':
                        switch (PresentLineLanguage)
                        {
                            case 'E':
                                xfood.Add(new XElement("Name", new XAttribute("language", "en-US"), new XAttribute("Value", str)));
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

                if (processed.Length > 0)
                {
                    XElement langToBeFixed = xfood.Elements().Last();
                    XAttribute fixstr = langToBeFixed.Attribute("Value");
                    fixstr.Remove();
                    langToBeFixed.Add(new XAttribute("Value", (String)fixstr + bdata.Value.ExtractedString));
                }
            }

            if (xfood.HasElements)
                xfoods.Add(xfood);

            return xfoods;
        }

        struct BracketData
        {
            public Int32 StartPoint;
            public String AfterExtracted;
            public String ExtractedString;
        }

        /// <summary>
        /// String 안에 괄호가 있는지 검사하여 있으면 그에 대한 정보가 반환되며, 없을 경우 null이 반환됩니다.
        /// </summary>
        /// <param name="input">검사할 String</param>
        /// <returns>괄호의 시작점, 괄호를 포함해 괄호 안의 문자열, 또 그것을 제외한 문자열을 반환합니다.</returns>
        Nullable<BracketData> ExtractBracket(String input)
        {
            Int32 bracketStartPoint = input.IndexOf('(');
            if (bracketStartPoint != -1)
            {
                Int32 bracketEndPoint = input.LastIndexOf(')');
                Int32 Length = bracketEndPoint - bracketStartPoint + 1;
                if (Length > 0)
                    return new BracketData()
                    {
                        StartPoint = bracketStartPoint,
                        AfterExtracted = input.Remove(bracketStartPoint, Length),
                        ExtractedString = input.Substring(bracketStartPoint, Length)
                    };
            }
            return null;
        }

        Boolean IsEnglish(String str)
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

        String[] SplitIntoTwoBySlash(String str)
        {
            return str.Split(new Char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
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
        protected override async void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            // TODO: Assign a collection of bindable groups to this.DefaultViewModel["Groups"]
            Double previousScrollOffset = -1;
            Char previousDirection = 'H';
            if (pageState != null)
            {
                if (pageState.ContainsKey("ScrollOffset"))
                    previousScrollOffset = (Double)pageState["ScrollOffset"];
                if (pageState.ContainsKey("PageViewState"))
                    previousDirection = (Char)pageState["PageViewState"];
            }
            await SetData(false, previousScrollOffset, previousDirection);
            SettingsPane.GetForCurrentView().CommandsRequested += DietGroupedPage_CommandsRequested;
            Window.Current.SizeChanged += Current_SizeChanged;
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
            Char direction;
            if (ApplicationView.Value == ApplicationViewState.Snapped)
                direction = 'V';
            else
                direction = 'H';
            pageState["PageViewState"] = direction;
            switch(direction)
            {
                case 'H':
                    {
                        pageState["ScrollOffset"] = GetVisualChild<ScrollViewer>(itemGridView).HorizontalOffset;
                        break;
                    }
                case 'V':
                    {
                        pageState["ScrollOffset"] = GetVisualChild<ScrollViewer>(itemListView).VerticalOffset;
                        break;
                    }
            }
            SettingsPane.GetForCurrentView().CommandsRequested -= DietGroupedPage_CommandsRequested;
            Window.Current.SizeChanged -= Current_SizeChanged;
        }

        public T GetVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                DependencyObject v = (DependencyObject)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                    child = GetVisualChild<T>(v);
                if (child != null)
                    break;
            }
            return child;
        }

        Boolean IsSwitching = false;

        private void itemGridView_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (!IsSwitching)
            {
                if ((sender as ListViewBase).SelectedItems.Count == 0)
                {
                    TextListContainer.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    bottomAppBar.IsOpen = false;
                    //bottomAppBar.IsSticky = false;
                }
                else
                {
                    TextListContainer.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    if (!bottomAppBar.IsOpen)
                    {
                        bottomAppBar.IsOpen = true;
                        //bottomAppBar.IsSticky = true;
                    }
                }

                foreach (object o in e.AddedItems)
                {
                    textList.Items.Insert(0, o);
                }
                foreach (object o in e.RemovedItems)
                {
                    textList.Items.Remove(o);
                }
            }
        }

        private void ClearButtonClicked(object sender, RoutedEventArgs e)
        {
            itemGridView.SelectedItems.Clear();
            itemListView.SelectedItems.Clear();
            bottomAppBar.IsOpen = false;
        }

        private void ItemClicked(object sender, ItemClickEventArgs e)
        {
            ListViewBase gridView = sender as ListViewBase;
            if (gridView.SelectedItems.Contains(e.ClickedItem))
            {
                if (gridView.SelectedItems.Count > 1)
                {
                    gridView.SelectedItem = e.ClickedItem;
                }
                else
                {
                    gridView.SelectedItems.Remove(e.ClickedItem);
                }
            }
            else
            {
                gridView.SelectedItem = e.ClickedItem;
            }
        }

        private async void RefreshButtonClicked(object sender, RoutedEventArgs e)
        {
            await Refresh();
        }

        async Task Refresh()
        {
            Application.Current.Resources.Remove("NewDataChecked");
            Char direction;
            if (ApplicationView.Value != ApplicationViewState.Snapped)
                direction = 'H';
            else
                direction = 'V';
            Double offset;
            ScrollViewer viewer = GetVisualChild<ScrollViewer>(itemGridView);
            if (viewer != null)
                if (direction == 'V')
                    offset = viewer.VerticalOffset;
                else//'H'
                    offset = viewer.HorizontalOffset;
            else
                offset = 0;
            await SetData(true, offset, direction);
        }

        private void NavigateClicked(object sender, RoutedEventArgs e)
        {
            Day context = (sender as Button).DataContext as Day;
            Frame.Navigate(typeof(DayDetailPage), context);
        }

        private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            switch (e.AddedItems[0] as String)
            {
                case "Korean":
                    break;
                case "English-US":
                    break;
            }
        }
    }

    public class MealBlockDataTemplateSelector : DataTemplateSelector
    {
        protected override DataTemplate
            SelectTemplateCore(object item, DependencyObject container)
        {
            if (container != null && item != null && item is MealData)
            {
                var currentFrame = Window.Current.Content as Frame;
                var currentPage = currentFrame.Content as Page;

                MealData data = item as MealData;

                if (data != null)
                {
                    if (!data.HighestCalories)
                        return currentPage.Resources["mealBlockShortTemplate"] as DataTemplate;
                    else
                        return currentPage.Resources["mealBlockTallTemplate"] as DataTemplate;
                }
            }

            return null;
        }
    }
}
