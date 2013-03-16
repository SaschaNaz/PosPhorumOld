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


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Posroid
{
    public partial class PrivacyPolicy : UserControl
    {
        public String Title
        {
            get { return GetValue(TitleProperty) as String; }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(String),
            typeof(PrivacyPolicy), null);

        public PrivacyPolicy()
        {
            this.InitializeComponent();

            this.DataContext = this;
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            Title = loader.GetString("PrivacyPolicyTitle");
        }

        private void MySettingsBackClicked(object sender, RoutedEventArgs e)
        {
            if (this.Parent.GetType() == typeof(Popup))
            {
                ((Popup)this.Parent).IsOpen = false;
            }
            SettingsPane.Show();
        }
    }
}
