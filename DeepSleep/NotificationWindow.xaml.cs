using System;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace DeepSleep
{
    public partial class NotificationWindow : Window
    {
        private MainWindow mainWindow;
        public NotificationWindow(string Text, MainWindow main)
        {
            InitializeComponent();
            Loaded += Window_Loaded;
            NotificationText.Text = Text;
            mainWindow = main;
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
            await Task.Delay(9300);
            FadeAnimation(false);
            await Task.Delay(700);
            Close();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FadeAnimation(true);
        }
        private void FadeAnimation(bool show)
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            Left = (screenWidth - Width) / 2;
            double TopPos = screenHeight * 0.01;
            Top = TopPos - 200;
            Opacity = 0;
            double FromDir;
            double ToDir;
            if (show)
            {
                FromDir = Top;
                ToDir = TopPos;
            }
            else
            {
                FromDir = TopPos;
                ToDir = -200;
            }
            var animY = new DoubleAnimation
            {
                From = FromDir,
                To = ToDir,
                Duration = TimeSpan.FromMilliseconds(700),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var animOpacity = new DoubleAnimation
            {
                From = 1 * (show ? 0 : 1),
                To = show ? 1 : 0,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = TimeSpan.FromMilliseconds(0)
            };

            BeginAnimation(Window.TopProperty, animY);
            BeginAnimation(Window.OpacityProperty, animOpacity);
        }
        private async void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            FadeAnimation(false);
            await Task.Delay(700);
            Close();
        }

        private void AddTimeButton_Click(object sender, RoutedEventArgs e)
        {
            string uId = ((Button)sender).Uid;
            int Time = int.Parse(uId.Split('_')[0]);
            mainWindow.AddTime(Time);
            Close();
        }
    }
}
