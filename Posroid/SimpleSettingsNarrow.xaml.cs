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
    public delegate void GlobalSettingChangedEventHandler(object sender, GlobalSettingChangedEventArgs e);
    public class GlobalSettingChangedEventArgs : EventArgs
    {
        public SettingType WhatSetting;
        public Object Value;
    }
    public enum SettingType
    {
        ForceKorean
    }

    public partial class SimpleSettingsNarrow : UserControl
    {
        Boolean loadCompleted;
        public SimpleSettingsNarrow()
        {
            this.InitializeComponent();
            ForceKoreanToggle.IsOn = (Boolean)Application.Current.Resources["ForceKorean"];
            this.loadCompleted = true;
        }

        private void MySettingsBackClicked(object sender, RoutedEventArgs e)
        {
            if (this.Parent.GetType() == typeof(Popup))
            {
                ((Popup)this.Parent).IsOpen = false;
            }
            SettingsPane.Show();
        }

        public event GlobalSettingChangedEventHandler SettingChanged;

        protected virtual void OnSettingChanged(GlobalSettingChangedEventArgs e)
        {
            if (SettingChanged != null)
            {
                SettingChanged(this, e);
            }
        }

        private void ToggleSwitch_Toggled_1(object sender, RoutedEventArgs e)
        {
            if (loadCompleted)
                OnSettingChanged(new GlobalSettingChangedEventArgs() { WhatSetting = SettingType.ForceKorean, Value = ForceKoreanToggle.IsOn });
        }
    }
}
