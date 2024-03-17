using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System.Timers;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.IO;
using System.IO.Ports;
using System.Data;
using System.Drawing;
using static SpotifyAPI.Web.Scopes;

namespace WpfApp1
{

    public partial class MainWindow : Window
    {
        //Arduino Port
        private static SerialPort port = new SerialPort("COM3", 9600);
        //Set info for my account
        private static readonly string clientId = "7a3be16d49114bcb8317330636aa2647";
        private static readonly string clientSecret = "dcf8b5b397b147579986ac718e0b0b6b";
        //set data
        private static SpotifyClient _spotify;
        private static readonly EmbedIOAuthServer _server = new EmbedIOAuthServer(new Uri("http://localhost:5000/callback"), 5000);
        DispatcherTimer time = new DispatcherTimer();
        Settings set = new Settings();
        IPlayableItem currently;
        Window1 lightset;

        public MainWindow()
        {

            InitializeComponent();
            //Get Authorization for spotify API
            getauth();
            
            while (_spotify == null)
            {
                //do nothing until spotify api is authorized
            }
            //set the light settings window with the spotifyclient information
             lightset = new Window1(_spotify);
            //start update timer
            time.Interval =  new TimeSpan(0, 0, 0,0, 500);
            time.Start();
            time.Tick += new EventHandler(dispatcherTimer_Tick);
        }
        //Authorization (dont totally understand, found from spotify API documentation)
        private void getauth()
        {
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new NullReferenceException(
                  "Please set SPOTIFY_CLIENT_ID and SPOTIFY_CLIENT_SECRET via environment variables before starting the program");
            }
            _server.Start();
            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
            var request = new LoginRequest(_server.BaseUri, clientId, LoginRequest.ResponseType.Code)
            {
                Scope = new List<string> { UserReadEmail, AppRemoteControl, UserReadPlaybackState, UserModifyPlaybackState }
            };
            Uri uri = request.ToUri();
            BrowserUtil.Open(uri);
        }
        public static async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            await _server.Stop();
            AuthorizationCodeTokenResponse token = await new OAuthClient().RequestToken(
              new AuthorizationCodeTokenRequest(clientId, clientSecret, response.Code, _server.BaseUri)
            );
            var config = SpotifyClientConfig.CreateDefault().WithToken(token.AccessToken, token.TokenType);
            _spotify = new SpotifyClient(config);
            Console.Clear();
        }
        //updates every few milliseconds
        private async void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            //if spotifyclient exists
            if (_spotify != null)
            {
                //sets a temporary variable (current) to a current track request
                var current = new PlayerCurrentlyPlayingRequest(PlayerCurrentlyPlayingRequest.AdditionalTypes.Track);
                //if there is a song playing
                if (current != null)
                {
                    //current variable gets set to the currently playing track
                    var currentlyplaying = await _spotify.Player.GetCurrentlyPlaying(current);

                    //if there is a currently playing track
                    if (currentlyplaying != null)
                    {
                        //Playable Item gets set to the item of the currently playing track (weird spotify api datatype requires this to see more info about it)
                         currently = currentlyplaying.Item;
                        
                        //if the currently playing track is indeed a track with info 
                        if (currently is FullTrack track1)
                        {
                            //track features variable gets the audiofeatures of the currently playing track
                            try
                            {
                                var trackfeat = await _spotify.Tracks.GetAudioFeatures(track1.Id);
                                Stats_Label.Content = "Popularity: " + track1.Popularity + "\nTempo: " + trackfeat.Tempo + "\nDanceability: " + trackfeat.Danceability;

                            }
                            catch
                            {

                            }
                            //progress bar at the bottom of the main window is updated to represent how far a song has played throygh
                            progress.Value = Convert.ToDouble(100 * (Convert.ToDouble(currentlyplaying.ProgressMs) / Convert.ToDouble(track1.DurationMs)));

                            //gets some cool track info from the Fulltrack info
                            if (track1.Album.Name.Length >= 23)
                            {
                                //adds an extra line if needed
                                Label.Content = (track1.Name + "\nby: " + track1.Artists[0].Name + "\nAlbum:\n" + track1.Album.Name);

                            }
                            else
                            {
                                Label.Content = (track1.Name + "\nby: " + track1.Artists[0].Name + "\nAlbum: " + track1.Album.Name);

                            }
                        }
                    }
                    else if (currentlyplaying == null)
                    {
                        //tell the user if the spotifyclient hasnt detected a currentlyplaying track
                        Label.Content = "No Song Currently Playing";
                    }
                }
            }
            else if (_spotify == null)
            {
                //tells the user if there wasnt a spotifyclient found
                Label.Content = "No Device Found";
            }
        }
        private async void GetSong_Click(object sender, RoutedEventArgs e)
        {
            //sets a temporary string variable to what song the user has requested
            var title = songbox.Text;

            //creates a searchrequest variable and sets the parameters to search for a track with a query of the users choice
            SearchRequest tracksearch = new SearchRequest(SearchRequest.Types.Track, title);

            //search response variable set to search for Tracks with the query keyword in them
            SearchResponse searched_track = await _spotify.Search.Item(tracksearch);

            //Fulltrack variable made to be set to the most popular track from the search response above
            var selectedTrack = await _spotify.Tracks.Get(searched_track.Tracks.Items[0].Id);

            //new spotify addtoqueue request made with the selected track Uri
            var addtrack = new PlayerAddToQueueRequest(selectedTrack.Uri);
            if (addtrack != null)
            {
                //If the desired track meets the prerequesites made by the user
                if (selectedTrack.Popularity > set.poplevel)
                {
                    //adds the track to queue and skips to the next track || may change the skip track feature later as
                    await _spotify.Player.AddToQueue(addtrack);
                    var nexttrack = new PlayerSkipNextRequest();
                    await _spotify.Player.SkipNext(nexttrack); 
                    //this skip feature was added because the spotify API offers not way to seek the track out of the queue, or (from my knowlege) play a track immediatly without adding it to the queue
                }
                else
                {
                    //error message informing the user that the popularity of the track doesnt meet the minumum level set
                    MessageBox.Show(selectedTrack.Name + " does not meet the minimum popularity level of " + set.poplevel);
                }
            }
        }
        private void songbox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //opens settings window
                set.Show();          
        } 
        private void Light_settings_click(object sender, RoutedEventArgs e)
        {      
            //opens light settings window for arduino support
            lightset.Show();
        }
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            //skips to the nect song in queue
            var nexttrack = new PlayerSkipNextRequest();

            await _spotify.Player.SkipNext(nexttrack);
        }
    }
}
