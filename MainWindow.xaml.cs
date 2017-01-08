using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CoreTweet;
using CoreTweet.Streaming;
using MouseKeyboardActivityMonitor;
using MouseKeyboardActivityMonitor.WinApi;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;

namespace SCNTwitter
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private KeyboardHookListener keyboardHookListener = new KeyboardHookListener(new GlobalHooker());
        private ObservableCollection<TweetModel> messages = new ObservableCollection<TweetModel>();
        private Twitter twitter;

        public MainWindow()
        {
            InitializeComponent();
            BindingOperations.EnableCollectionSynchronization(messages, new object());
            DataContext = messages;
            twitter = Twitter.CreateFromConfig("Resources\\account.config");
            twitter.GetTokens().Streaming.UserAsObservable()
                .Where((StreamingMessage m) => m.Type == MessageType.Create)
                .Cast<StatusMessage>()
                .Subscribe(m => messages.Insert(0, new TweetModel()
                {
                    UserName = m.Status.User.Name,
                    UserId = "@" + m.Status.User.ScreenName,
                    Text = m.Status.Text,
                    UserIconUrl = m.Status.User.ProfileImageUrlHttps,
                    Content = m.Status
                }));
            keyboardHookListener.Enabled = true;
            keyboardHookListener.KeyDown += Window_Active;
        }

        public void Window_Active(object sender, KeyEventArgs args)
        {
            if (!args.Control || !args.Alt) return;
            if (args.KeyValue == 'P')
            {
                this.Topmost = true;
                this.Topmost = false;
                this.Activate();
                listBox.Focus();
                listBox.SelectedIndex = 0;
            }
            if (args.KeyValue == 'L' || args.KeyValue == 'R')
            {
                if (!this.IsActive)
                {
                    listBox.SelectedIndex = 0;
                }
                TweetModel tweetModel = (TweetModel)listBox.SelectedValue;
                if (args.KeyValue == 'L')
                {
                    twitter.Send_Like(tweetModel.Content.Id);
                }
                else
                {
                    twitter.Send_Retweet(tweetModel.Content.Id);
                }
                
            }
        }

        public void Button_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            twitter.Tweet(textBox.Text);
        }

        public void Like_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            TweetModel tweetModel = (TweetModel) (sender as Button).DataContext;
            twitter.Send_Like(tweetModel.Content.Id);
        }

        public void Retweet_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            TweetModel tweetModel = (TweetModel)(sender as Button).DataContext;
            twitter.Send_Retweet(tweetModel.Content.Id);
        }
    }

    public struct TweetModel
    {
        public string UserName { get; set; }
        public string UserId { get; set; }
        public string Text { get; set; }
        public string UserIconUrl { get; set; }
        public Status Content { get; set; }
    }

    public class Twitter
    {
        private Tokens tokens;

        public Twitter(string ck, string cs, string at, string ats)
        {
            tokens = CoreTweet.Tokens.Create(ck, cs, at, ats);
        }

        public void Tweet(string text)
        {
            tokens.Statuses.Update(new { status = text });
        }

        public void Send_Like(long id)
        {
            tokens.Favorites.Create(new {id = id});
        }

        public void Send_Retweet(long id)
        {
            tokens.Statuses.Retweet(new {id = id});
        }

        public Tokens GetTokens()
        {
            return tokens;
        }

        public static Twitter CreateFromConfig(string filename)
        {
            var configFileMap = new ExeConfigurationFileMap { ExeConfigFilename = @filename };
            var openMappedExeConfiguration = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
            var config = openMappedExeConfiguration.AppSettings.Settings;
            return new Twitter(config["ConsumerKey"].Value, config["ConsumerSecret"].Value,
                config["AccessToken"].Value, config["AccessTokenSecret"].Value);
        }
    
    }
}
