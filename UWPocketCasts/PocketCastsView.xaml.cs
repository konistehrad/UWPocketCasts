using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Data.Json;
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
        public const string PocketCastsBaseURL = "https://playbeta.pocketcasts.com/web/";
        // <WebView x:Name="MainWebView" Source="https://playbeta.pocketcasts.com/web/"/>
        private WebView webView;
        private bool kickedOffWatch;

        public bool Playing { get; private set; }
        public double DurationInSeconds { get; private set; }

        private double positionInSeconds;
        public double PositionInSeconds
        {
            get { return positionInSeconds; }
            set
            {
                // dispatch async call to update scrub position
            }
        }

        public PocketCastsView()
        {
            this.InitializeComponent();

            webView = new WebView(WebViewExecutionMode.SeparateThread);
            WebViewBorder.Child = webView;
            webView.ScriptNotify += WebView_ScriptNotify;
            webView.NavigationStarting += WebView_NavigationStarting;
            webView.NavigationCompleted += WebView_NavigationCompleted;
            webView.Navigate(new Uri(PocketCastsBaseURL));
        }

        private void WebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            // reject navigation to non pocketcasts sites ...
            Debug.WriteLine("WebView_NavigationStarting: " + args.Uri.Host);
            if (!args.Uri.Host.Contains("pocketcasts.com", StringComparison.InvariantCultureIgnoreCase))
            {
                args.Cancel = true;
            }
        }

        private async Task<bool> InjectBridgeJS()
        {
            try
            {
                StreamReader bridgeStream = new StreamReader(
                    Assembly.GetExecutingAssembly().GetManifestResourceStream("UWPocketCasts.PocketCastsBridge.js")
                );

                string contents = await bridgeStream.ReadToEndAsync();
                string[] evalArgs = new string[1];

                evalArgs[0] = contents;
                bool.Parse(await webView.InvokeScriptAsync("eval", evalArgs));

                await SyncPlayerState();

                return true;
            }
            catch(Exception e)
            {
                return false;
            }
        }

        private async Task SyncPlayerState()
        {
            string[] evalArgs = new string[1];

            evalArgs[0] = "window.pocketCastBridge.jsonPlayerState";
            string jsonResult = await webView.InvokeScriptAsync("eval", evalArgs);
            JsonObject obj = JsonValue.Parse(jsonResult).GetObject();
            bool playing = obj.GetNamedBoolean("isPlaying");
            double duration = obj.GetNamedNumber("durationInSeconds");
            double position = obj.GetNamedNumber("positionInSeconds");

            this.Playing = playing;
            this.DurationInSeconds = duration;
            this.positionInSeconds = position; // use field, not prop here!!!
        }

        private void WebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            Debug.WriteLine(string.Format("Nav complete {1}: {0}", args.Uri, args.IsSuccess));
            if(!kickedOffWatch)
            {
                kickedOffWatch = true;
                InjectBridgeJS();
            }
        }

        private void WebViewBorder_Loaded(object sender, RoutedEventArgs e) { }
        
        private void WebView_ScriptNotify(object sender, NotifyEventArgs e)
        {
            Debug.WriteLine(string.Format("ScriptNotify: {0}", e.Value));

            string value = e.Value;

            if (value == "playing")
            {
                Playing = true;
            }
            else if(value == "pause")
            {
                Playing = false;
            }
            else if(value == "ended")
            {
                Playing = false;
            }
            else if(value == "audioFound")
            {

            }
            else if(value == "audioLost")
            {
                Playing = false;
            }
        }
    }
}
