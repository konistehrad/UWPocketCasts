using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UWPocketCasts
{
    public sealed partial class PocketCastsView : UserControl
    {
        private const string InjectedJS = @"
(function() {
var foundAudio;

function unregisterCallbacks()
{
}

function jsWatch()
{
  
}

})();
";

        public const string PocketCastsBaseURL = "https://playbeta.pocketcasts.com/web/";
        // <WebView x:Name="MainWebView" Source="https://playbeta.pocketcasts.com/web/"/>
        private WebView webView;

        public PocketCastsView()
        {
            this.InitializeComponent();

            webView = new WebView(WebViewExecutionMode.SeparateThread);
            WebViewBorder.Child = webView;
            webView.ScriptNotify += WebView_ScriptNotify;
            webView.Navigate(new Uri(PocketCastsBaseURL));
        }

        private void WebViewBorder_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private async void WatchForPlay()
        {

        }


        private void WebView_ScriptNotify(object sender, NotifyEventArgs e)
        {
            // Respond to the script notification.
        }
    }
}
