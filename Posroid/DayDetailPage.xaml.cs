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
using Windows.UI.ApplicationSettings;
using Windows.Storage;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Posroid
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class DayDetailPage : Posroid.Common.LayoutAwarePage
    {
        public DayDetailPage()
        {
            this.InitializeComponent();
        }

        Popup _settingsPopup;
        void DietGroupedPage_CommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            var languageTitle = loader.GetString("LanguageTitle");
            SettingsCommand cmd = new SettingsCommand("lang", languageTitle, (x) =>
            {
                Int32 _settingsWidth = 370;
                Rect _windowBounds = Window.Current.Bounds;
                _settingsPopup = new Popup();
                _settingsPopup.Closed += OnPopupClosed;
                Window.Current.Activated += OnWindowActivated;
                _settingsPopup.IsLightDismissEnabled = true;
                _settingsPopup.Width = _settingsWidth;
                _settingsPopup.Height = _windowBounds.Height;

                LanguageControl control = new LanguageControl();
                SettingsFlyout mypane = new SettingsFlyout(languageTitle, control)
                {
                    Width = _settingsWidth,
                    Height = _windowBounds.Height
                };
                control.SettingChanged += delegate(object sender2, GlobalSettingChangedEventArgs e)
                {

                    ApplicationData.Current.LocalSettings.Values["ForceKorean"] = e.Value;
                    Time[] abc = this.DefaultViewModel["MealTimes"] as Time[];
                    this.DefaultViewModel["MealTimes"] = null;
                    this.DefaultViewModel["MealTimes"] = abc;
                };

                _settingsPopup.Child = mypane;
                _settingsPopup.SetValue(Canvas.LeftProperty, _windowBounds.Width - _settingsWidth);
                _settingsPopup.SetValue(Canvas.TopProperty, 0);
                _settingsPopup.IsOpen = true;
            });

            args.Request.ApplicationCommands.Add(cmd);

            var ppolicyTitle = loader.GetString("PrivacyPolicyTitle");
            cmd = new SettingsCommand("ppolicy", ppolicyTitle, (x) =>
            {
                Int32 _settingsWidth = 370;
                Rect _windowBounds = Window.Current.Bounds;
                _settingsPopup = new Popup();
                _settingsPopup.Closed += OnPopupClosed;
                Window.Current.Activated += OnWindowActivated;
                _settingsPopup.IsLightDismissEnabled = true;
                _settingsPopup.Width = _settingsWidth;
                _settingsPopup.Height = _windowBounds.Height;

                SettingsFlyout mypane = new SettingsFlyout(ppolicyTitle, new TextBlock() { Text = loader.GetString("PrivacyPolicyContent"), TextWrapping = TextWrapping.Wrap, FontSize = 15 })
                {
                    Width = _settingsWidth,
                    Height = _windowBounds.Height
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
            if (navigationParameter != null)
                this.DefaultViewModel["MealTimes"] = (navigationParameter as Day).Times;
            this.DefaultViewModel["ServedDate"] = (navigationParameter as Day).ServedDate;
            SettingsPane.GetForCurrentView().CommandsRequested += DietGroupedPage_CommandsRequested;
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
            SettingsPane.GetForCurrentView().CommandsRequested -= DietGroupedPage_CommandsRequested;
        }
    }
}
