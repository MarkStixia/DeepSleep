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
        private int windowCloseTime;

        #region Main logic
        /// <param name="buttons">1 - кнопки добавления времени, 2 - да/нет, 3 - ок</param>
        /// <param name="closeTime">Время через которое закроется окно (сек)</param>
        public NotificationWindow(string title, int closeTime, byte buttons, MainWindow main)
        {
            InitializeComponent();
            Loaded += Window_Loaded;
            NotificationText.Text = title;
            windowCloseTime = closeTime;
            CloseWindowText.Text = $"Окно закроется через {closeTime} сек";
            mainWindow = main;
            DelayClose();
            switch (buttons)
            {
                case 1:
                    TimeAdditionPanel.Visibility = Visibility.Visible;
                    break;
                    case 2:
                    YesNoPanel.Visibility = Visibility.Visible;
                    break;
                    case 3:
                    OKPanel.Visibility = Visibility.Visible;
                    break;
            }
            if (MainWindow.settings["Sounds"] == "On" && buttons == 1)
            {
                using (SoundPlayer player = new SoundPlayer(Properties.Resources.NotificationSound))
                {
                    player.Play();
                }
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FadeAnimation(true);
        }
        #endregion
        #region Animations
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
        private async void DelayClose()
        {
            await Task.Delay((int)(windowCloseTime * 1000 * 0.93f));
            FadeClose();
        }
        private async void FadeClose()
        {
            FadeAnimation(false);
            await Task.Delay(700);
            Close();
        }

        #endregion
        #region Buttons Logic
        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            FadeClose();
        }
        private void AddTimeButton_Click(object sender, RoutedEventArgs e)
        {
            string uId = ((Button)sender).Uid;
            int Time = int.Parse(uId.Split('_')[0]);
            mainWindow.AddTime(Time);
            FadeClose();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            FadeClose();
        }
        private void YesNoClick(object sender, RoutedEventArgs e)
        {
            string uId = ((Button)sender).Uid;

            if (uId == "True")
            {
                DialogResult = true;
                FadeClose();
            }
            else
            {
                DialogResult = false;
                FadeClose();
            }
        }

        #endregion
    }
}
