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
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage.Streams;
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

        private WebView webView;
        private bool kickedOffWatch;

        public bool Playing { get; private set; }
        public double DurationInSeconds { get; private set; }
        public string PodcastImageURL { get; private set; } = string.Empty;
        public string PodcastTitle { get; private set; } = string.Empty;
        public string EpisodeTitle { get; private set; } = string.Empty;

        private double positionInSeconds;
        public double PositionInSeconds
        {
            get { return positionInSeconds; }
            set
            {
                // dispatch async call to update scrub position
                UpdatePosition(value);
            }
        }

        private string lastPodcastImageURL = string.Empty;
        private bool hasAudio;
        private SystemMediaTransportControls systemMediaTransportControls;

        public PocketCastsView()
        {
            this.InitializeComponent();

            webView = new WebView(WebViewExecutionMode.SeparateThread);
            WebViewBorder.Child = webView;
            webView.ScriptNotify += WebView_ScriptNotify;
            webView.NavigationStarting += WebView_NavigationStarting;
            webView.NavigationCompleted += WebView_NavigationCompleted;
            webView.Navigate(new Uri(PocketCastsBaseURL));
            
            systemMediaTransportControls = SystemMediaTransportControls.GetForCurrentView();
            systemMediaTransportControls.ButtonPressed += SystemControls_ButtonPressed;
        }

        private async Task<double> UpdatePosition(double pos)
        {
            await Eval("window.pocketCastBridge.positionInSeconds = " + pos);
            await SyncPlayerState();
            return positionInSeconds;
        }

        private async Task<bool> InjectBridgeJS()
        {
            try
            {
                StreamReader bridgeStream = new StreamReader(
                    Assembly.GetExecutingAssembly().GetManifestResourceStream("UWPocketCasts.PocketCastsBridge.js")
                );

                string contents = await bridgeStream.ReadToEndAsync();
                string injectResult = await Eval(contents);
                if (injectResult != "loaded") return false;

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
            string jsonResult = await Eval("window.pocketCastBridge.jsonPlayerState");

            JsonObject obj = JsonValue.Parse(jsonResult).GetObject();
            bool playing = obj.GetNamedBoolean("isPlaying");
            bool hasAudio = obj.GetNamedBoolean("hasAudio");
            double duration = obj.GetNamedNumber("durationInSeconds");
            double position = obj.GetNamedNumber("positionInSeconds");
            string episodeTitle = obj.GetNamedString("episodeTitle");
            string podcastTitle = obj.GetNamedString("podcastTitle");
            string podcastImageURL = obj.GetNamedString("podcastImageURL");

            this.hasAudio = hasAudio;
            this.Playing = playing;
            this.DurationInSeconds = duration;
            this.positionInSeconds = position; // use field, not prop here!!!
            this.PodcastImageURL = podcastImageURL;
            this.PodcastTitle = podcastTitle;
            this.EpisodeTitle = episodeTitle;

            SyncMediaTransportControlsToPlayerState();
            // Debug.WriteLine("Playing? {0} Podcast? {1} : {4} Position? {2:0.00}/{3:0.00}", this.Playing, this.PodcastTitle, this.positionInSeconds, this.DurationInSeconds, this.EpisodeTitle);
        }

        private void SyncMediaTransportControlsToPlayerState()
        {
            SystemMediaTransportControlsDisplayUpdater updater = systemMediaTransportControls.DisplayUpdater;
            updater.Type = MediaPlaybackType.Music;
            
            if (!this.hasAudio)
            {
                // we don't have nothin, sorry!
                systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Closed;
                systemMediaTransportControls.IsPlayEnabled = false;
                systemMediaTransportControls.IsPauseEnabled = false;
                systemMediaTransportControls.IsRewindEnabled = false;
                systemMediaTransportControls.IsFastForwardEnabled = false;


                updater.MusicProperties.Artist = string.Empty;
                updater.MusicProperties.Title = string.Empty;

                lastPodcastImageURL = string.Empty;
                updater.Thumbnail = null;
            }
            else
            {
                var timelineProperties = new SystemMediaTransportControlsTimelineProperties();

                systemMediaTransportControls.IsPlayEnabled = true;
                systemMediaTransportControls.IsPauseEnabled = true;
                systemMediaTransportControls.IsRewindEnabled = true;
                systemMediaTransportControls.IsFastForwardEnabled = true;

                systemMediaTransportControls.PlaybackStatus =
                    this.Playing ?
                        MediaPlaybackStatus.Playing :
                        MediaPlaybackStatus.Paused;

                updater.MusicProperties.Artist = this.PodcastTitle;
                updater.MusicProperties.Title = this.EpisodeTitle;

                timelineProperties.StartTime = TimeSpan.FromSeconds(0);
                timelineProperties.MinSeekTime = TimeSpan.FromSeconds(0);
                timelineProperties.Position = TimeSpan.FromSeconds(this.positionInSeconds);
                timelineProperties.MaxSeekTime = TimeSpan.FromSeconds(this.DurationInSeconds);
                timelineProperties.EndTime = TimeSpan.FromSeconds(this.DurationInSeconds);
                
                if(!lastPodcastImageURL.Equals(this.PodcastImageURL))
                {
                    lastPodcastImageURL = this.PodcastImageURL;
                    if(!string.IsNullOrEmpty(PodcastImageURL))
                    {
                        updater.Thumbnail = 
                            RandomAccessStreamReference.CreateFromUri(new Uri(PodcastImageURL));
                    }
                    else
                    {
                        updater.Thumbnail = null;
                    }
                }

            }

            updater.Update();
        }
        
        private IAsyncOperation<string> Eval(string arg)
        {
            string[] evalArgs = new string[1];
            evalArgs[0] = arg;
            return webView.InvokeScriptAsync("eval", evalArgs);
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

        private async void SystemControls_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        await Eval("window.pocketCastBridge.play()");
                    });
                    break;
                case SystemMediaTransportControlsButton.FastForward:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        await Eval("window.pocketCastBridge.fastForward()");
                    });
                    break;
                case SystemMediaTransportControlsButton.Rewind:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        await Eval("window.pocketCastBridge.rewind()");
                    });
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        await Eval("window.pocketCastBridge.pause()");
                    });
                    break;
                default:
                    break;
            }
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

        private void WebViewBorder_Loaded(object sender, RoutedEventArgs e) { }
        
        private void WebView_ScriptNotify(object sender, NotifyEventArgs e)
        {
            // Debug.WriteLine(string.Format("ScriptNotify: {0}", e.Value));

            string value = e.Value;

            if (value == "playing")
            {
            }
            else if(value == "pause")
            {
            }
            else if(value == "ended")
            {
            }
            else if(value == "audioFound")
            {
            }
            else if(value == "audioLost")
            {
            }
            else if(value =="tick")
            {
            }

            // no matter what, we sync here!
            SyncPlayerState();
        }
    }
}
