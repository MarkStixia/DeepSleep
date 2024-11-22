using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DeepSleep
{
    public partial class NotificationWindow : Window
    {
        public NotificationWindow(string Text)
        {
            InitializeComponent();
            Loaded += Window_Loaded;
            NotificationText.Text = Text;
            DelayClose();
            if (MainWindow.settings["Sounds"] == "On")
            {
                using (SoundPlayer player = new SoundPlayer(Properties.Resources.NotificationSound))
                {
                    player.Play();
                }
            }
        }
        private async void DelayClose()
        {
            await Task.Delay(10000);
            Close();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            Left = screenWidth - this.Width;
            Top = screenHeight - this.Height - 50; 
        }

        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Close();
        }
    }
}
