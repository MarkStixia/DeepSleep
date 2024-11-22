using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using System.ComponentModel;
using System.Timers;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Text;
using Microsoft.Win32;
using System.Reflection;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Text.RegularExpressions;

namespace DeepSleep
{
    public partial class MainWindow : Window
    {
        //Settings and temp variables
        private List<string> poweroffvariants = new List<string> { "Hybernation", "Shutdown" };
        private int selectedPowerOffMode = 0;
        private string lastTime = "";
        private string settingsPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}/Deep Sleep/config.cfg";
        public static Dictionary<string, string> settings = new Dictionary<string, string>();
        private WindowState m_storedWindowState = WindowState.Normal;
        private bool isDelayedTimerStart = false;
        private bool isDelayedTimerPaused = false;
        private int timerSecondLeft = 0;
        private int selectedTimerMode = 0;

        //Alerts and shutdown bools
        private bool is30mAlertShown = false;
        private bool is5mAlertShown = false;
        private bool is1mAlertShown = false;
        private bool isShutdowned = false;

        //Initializing variables
        NotificationWindow notification = null;
        private NotifyIcon m_notifyIcon;
        private System.Timers.Timer mainLogicTimer;
        private System.Timers.Timer delayedTimer;

        #region Main Logic
        public MainWindow()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            m_notifyIcon = new NotifyIcon();
            settings = LoadSettings();
            SetSettings(settings);
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            Left = (screenWidth - Width) / 2;
            Top = (screenHeight - Height) / 2;
            ToolTip toolTip = new ToolTip();
            m_notifyIcon.Text = "Deep Sleep";

            m_notifyIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(m_notifyIcon_Click);
            ContextMenu contextMenu = new ContextMenu();
            MenuItem closeMenuItem = new MenuItem("Закрыть", Close);
            contextMenu.MenuItems.Add(closeMenuItem);
            m_notifyIcon.ContextMenu = contextMenu;
            if (settings["TrayStart"] == "On")
            {
                Hide();
                WindowState = WindowState.Minimized;
                if (m_notifyIcon != null)
                    ShowTrayIcon(true);
            }
            mainLogicTimer = new System.Timers.Timer(10000);
            mainLogicTimer.Elapsed += OnTimedEvent;
            mainLogicTimer.AutoReset = true;
            mainLogicTimer.Enabled = true;
        }
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            if (settings["PowerOn"] == "On")
            {
                string timeNow = DateTime.Now.ToString("HH:mm");
                if (timeNow != lastTime)
                {
                    lastTime = "";
                }
                Dispatcher.Invoke(() =>
                {
                    if (timeNow == settings["TimeBox"] && settings["TimeBox"] != lastTime && !isShutdowned)
                    {
                        lastTime = timeNow;
                        switch (poweroffvariants[selectedPowerOffMode])
                        {
                            case "Hybernation":
                                isShutdowned = true;
                                PowerShellCommand("shutdown /h");
                                is30mAlertShown = false;
                                is5mAlertShown = false;
                                is1mAlertShown = false;
                                break;
                            case "Shutdown":
                                isShutdowned = true;
                                PowerShellCommand("shutdown /s /f /t 0");
                                is30mAlertShown = false;
                                is5mAlertShown = false;
                                is1mAlertShown = false;
                                break;
                        }
                    }
                    if (settings["Notifications"] == "On")
                    {
                        DateTime dateTimeNow = DateTime.ParseExact(timeNow, "HH:mm", null);
                        DateTime dateTimeGoal = DateTime.ParseExact(settings["TimeBox"], "HH:mm", null);
                        if (dateTimeGoal < dateTimeNow)
                        {
                            dateTimeGoal = dateTimeGoal.AddDays(1);
                        }

                        TimeSpan difference = dateTimeGoal - dateTimeNow;

                        if (difference.TotalMinutes <= 30 && difference.TotalMinutes > 25 && !is30mAlertShown)
                        {
                            notification = new NotificationWindow("До выключения менее 30 мин");
                            notification.Show();
                            is30mAlertShown = true;
                            is5mAlertShown = false;
                            is1mAlertShown = false;
                        }
                        else if (difference.TotalMinutes <= 5 && difference.TotalMinutes > 2 && !is5mAlertShown)
                        {
                            notification = new NotificationWindow("До выключения менее 5 мин");
                            notification.Show();
                            is5mAlertShown = true;
                            is1mAlertShown = false;
                        }
                        else if (difference.TotalSeconds <= 60 && difference.TotalSeconds > 0 && !is1mAlertShown)
                        {
                            notification = new NotificationWindow("До выключения менее 1 мин");
                            notification.Show();
                            is1mAlertShown = true;
                        }
                        if (difference.TotalMinutes < 0) //TODO: Спорная тема
                        {
                            is30mAlertShown = false;
                            is5mAlertShown = false;
                            is1mAlertShown = false;
                        }
                    }

                });
                if (isShutdowned && timeNow != settings["TimeBox"])
                {
                    isShutdowned = false;
                }
            }
        }
        #endregion

        #region Settings Functions
   
        private void SaveSettings()
        {
            CheckSettingsFile();
            if (TimeBox1.Text.Length == 0)
            {
                TimeBox1.Text = "00";
            }
            if (TimeBox2.Text.Length == 0)
            {
                TimeBox2.Text = "00";
            }
            if (TimeBox1.Text.Length == 1)
            {
                TimeBox1.Text = "0" + TimeBox1.Text;
            }
            if (TimeBox2.Text.Length == 1)
            {
                TimeBox2.Text = "0" + TimeBox2.Text;
            }
            settings["TimeBox"] = TimeBox1.Text + ":" + TimeBox2.Text;
            settings["PowerOffMode"] = selectedPowerOffMode.ToString();
            settings["TrayStart"] = TrayStartToggle.Tag.ToString();
            settings["Notifications"] = NotificationsToggle.Tag.ToString();
            settings["Sounds"] = SoundsToggle.Tag.ToString();
            settings["AutoStart"] = AutoStartToggle.Tag.ToString();
            settings["PowerOn"] = SleepOnToggle.Tag.ToString();
            settings["TimerSeconds"] = (int.Parse(TimerBoxHours.Text) * 3600 + int.Parse(TimerBoxMinutes.Text) * 60 + int.Parse(TimerBoxSeconds.Text)).ToString();
            settings["TimerMode"] = selectedTimerMode.ToString();

            using (FileStream fileStream = new FileStream(settingsPath, FileMode.Create, FileAccess.Write))
            {
                foreach (var kvp in settings)
                {
                    string dataToWrite = $"{kvp.Key}={kvp.Value}\n";
                    byte[] data = Encoding.UTF8.GetBytes(dataToWrite);
                    fileStream.Write(data, 0, data.Length);
                }
            }
        }
        private Dictionary<string, string> LoadSettings()
        {
            CheckSettingsFile();
            Dictionary<string, string> values = new Dictionary<string, string>();

            using (FileStream fileStream = new FileStream(settingsPath, FileMode.Open, FileAccess.Read))
            {
                using (StreamReader reader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] parts = line.Split(new[] { '=' }, 2);
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim();
                            string value = parts[1].Trim();
                            values[key] = value;
                        }
                    }
                }
            }

            return values;
        }
        private void SetSettings(Dictionary<string, string> settingsDictionary)
        {
            if (settingsDictionary.TryGetValue("TimeBox", out string readTimeBox))
            {
                string[] time = readTimeBox.Split(':');
                TimeBox1.Text = time[0];
                TimeBox2.Text = time[1];
            }

            if (settingsDictionary.TryGetValue("PowerOffMode", out string readSelectedPowerOffMode))
            {
                int parsedSelectedPowerOffMode = int.Parse(readSelectedPowerOffMode);
                switch (parsedSelectedPowerOffMode)
                {
                    case 0:
                        HybernationRB.IsChecked = true;
                        ShutdownRB.IsChecked = false;
                        break;
                    case 1:
                        HybernationRB.IsChecked = false;
                        ShutdownRB.IsChecked = true;
                        break;
                }
                selectedPowerOffMode = parsedSelectedPowerOffMode;
            }
            if (settingsDictionary.TryGetValue("TrayStart", out string readTrayStart))
            {
                TrayStartToggle.Tag = readTrayStart;
                switch (TrayStartToggle.Tag)
                {
                    case "Off":
                        TrayStartToggle.Source = new BitmapImage(new Uri("/Icons/ToggleOff.png", UriKind.Relative));
                        TrayStartToggleText.Text = "Выкл";
                        TrayStartToggle.Tag = "Off";
                        break;
                    case "On":
                        TrayStartToggle.Source = new BitmapImage(new Uri("/Icons/ToggleOn.png", UriKind.Relative));
                        TrayStartToggleText.Text = "Вкл";
                        TrayStartToggle.Tag = "On";
                        break;
                }
            }
            if (settingsDictionary.TryGetValue("Notifications", out string readNotifications))
            {
                NotificationsToggle.Tag = readNotifications;
                switch (NotificationsToggle.Tag)
                {
                    case "Off":
                        NotificationsToggle.Source = new BitmapImage(new Uri("/Icons/ToggleOff.png", UriKind.Relative));
                        NotificationsToggleText.Text = "Выкл";
                        NotificationsToggle.Tag = "Off";
                        break;
                    case "On":
                        NotificationsToggle.Source = new BitmapImage(new Uri("/Icons/ToggleOn.png", UriKind.Relative));
                        NotificationsToggleText.Text = "Вкл";
                        NotificationsToggle.Tag = "On";
                        break;
                }
            }

            if (settingsDictionary.TryGetValue("Sounds", out string readSounds))
            {
                SoundsToggle.Tag = readSounds;
                switch (SoundsToggle.Tag)
                {
                    case "Off":
                        SoundsToggle.Source = new BitmapImage(new Uri("/Icons/ToggleOff.png", UriKind.Relative));
                        SoundsToggleText.Text = "Выкл";
                        SoundsToggle.Tag = "Off";
                        break;
                    case "On":
                        SoundsToggle.Source = new BitmapImage(new Uri("/Icons/ToggleOn.png", UriKind.Relative));
                        SoundsToggleText.Text = "Вкл";
                        SoundsToggle.Tag = "On";
                        break;
                }
            }
            if (settingsDictionary.TryGetValue("AutoStart", out string readAutoStart))
            {
                AutoStartToggle.Tag = readAutoStart;
                if (readAutoStart == "On" && !IsAutoStartEnabled())
                {
                    AddToStartup();
                }
                switch (AutoStartToggle.Tag)
                {
                    case "Off":
                        AutoStartToggle.Source = new BitmapImage(new Uri("/Icons/ToggleOff.png", UriKind.Relative));
                        AutoStartToggleText.Text = "Выкл";
                        AutoStartToggle.Tag = "Off";
                        break;
                    case "On":
                        AutoStartToggle.Source = new BitmapImage(new Uri("/Icons/ToggleOn.png", UriKind.Relative));
                        AutoStartToggleText.Text = "Вкл";
                        AutoStartToggle.Tag = "On";
                        break;
                }
            }
            if (settingsDictionary.TryGetValue("PowerOn", out string readPowerOn))
            {
                SleepOnToggle.Tag = readPowerOn;
                switch (SleepOnToggle.Tag)
                {
                    case "On":
                        SleepOnToggle.Source = new BitmapImage(new Uri("/Icons/ToggleOn.png", UriKind.Relative));
                        TurnOnImage.Source = new BitmapImage(new Uri("/Resources/AppIcon.ico", UriKind.Relative));
                        AppIconImage.Source = new BitmapImage(new Uri("/Resources/AppIcon.ico", UriKind.Relative));
                        var urion = new Uri("pack://application:,,,/Resources/AppIcon.ico");
                        using (var stream = System.Windows.Application.GetResourceStream(urion).Stream)
                        {
                            m_notifyIcon.Icon = new Icon(stream);
                        }
                        SleepOnToggleText.Text = "Вкл";
                        break;
                    case "Off":
                        SleepOnToggle.Source = new BitmapImage(new Uri("/Icons/ToggleOff.png", UriKind.Relative));
                        SleepOnToggleText.Text = "Выкл";
                        TurnOnImage.Source = new BitmapImage(new Uri("/Resources/AppIconOff.ico", UriKind.Relative));
                        AppIconImage.Source = new BitmapImage(new Uri("/Resources/AppIconOff.ico", UriKind.Relative));
                        var urioff = new Uri("pack://application:,,,/Resources/AppIconoff.ico");
                        using (var stream = System.Windows.Application.GetResourceStream(urioff).Stream)
                        {
                            m_notifyIcon.Icon = new Icon(stream);
                        }
                        break;
                }

            }
            if(settingsDictionary.TryGetValue("TimerSeconds", out string readTimerSeconds))
            {
                int totalSeconds = int.Parse(readTimerSeconds);
                int hours = totalSeconds / 3600;
                int minutes = (totalSeconds - (hours * 3600)) / 60;
                int seconds = totalSeconds - (hours * 3600) - (minutes * 60);
                TimerBoxHours.Text = TimeToString(hours);
                TimerBoxMinutes.Text = TimeToString(minutes);
                TimerBoxSeconds.Text = TimeToString(seconds);
            }
            if (settingsDictionary.TryGetValue("TimerMode", out string readTimerMode))
            {
                HybernationTimerRB.IsChecked = false;
                ShutdownTimerRB.IsChecked = false;
                if (readTimerMode == "0")
                {
                    HybernationTimerRB.IsChecked = true;
                }
                else
                {
                    ShutdownTimerRB.IsChecked = true;
                }
            }
            SaveSettings();
        }
        private void CheckSettingsFile()
        {
            string directoryPath = System.IO.Path.GetDirectoryName(settingsPath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            if (!File.Exists(settingsPath))
            {
                var create = File.Create(settingsPath);
                create.Close();
            }
        }
        #endregion

        #region System Commands
        private void PowerShellCommand(string command)
        {
            Process process = new Process();
            process.StartInfo.FileName = "powershell.exe";
            process.StartInfo.Arguments = $"-Command \"{command}\"";
            process.StartInfo.CreateNoWindow = true;
            process.Start();
        }
        private void AddToStartup()
        {
            string exePath = Assembly.GetExecutingAssembly().Location;

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (key != null)
                {
                    key.SetValue("DeepSleep", exePath);
                }
            }
        }
        public static void RemoveFromStartup()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (key != null)
                {
                    if (key.GetValue("DeepSleep") != null)
                    {
                        key.DeleteValue("DeepSleep");
                    }
                }
            }
        }
        private bool IsAutoStartEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run"))
            {
                if (key != null)
                {
                    return key.GetValue("DeepSleep") != null;
                }
            }
            return false;
        }
        #endregion

        #region Tray Logic
        void OnClose(object sender, CancelEventArgs args)
        {
            m_notifyIcon.Dispose();
            m_notifyIcon = null;
        }
        private void Close(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
        void m_notifyIcon_Click(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Show();
                WindowState = m_storedWindowState;
            }
        }
        void CheckTrayIcon()
        {
            ShowTrayIcon(!IsVisible);
        }

        void ShowTrayIcon(bool show)
        {
            if (m_notifyIcon != null)
                m_notifyIcon.Visible = show;
        }
        void OnStateChanged(object sender, EventArgs args)
        {
            if (WindowState == WindowState.Minimized)
            {
                if (notification != null)
                    notification.Owner = null;
                Hide();
            }
            else
                m_storedWindowState = WindowState;
        }
        void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            CheckTrayIcon();
        }




        #endregion

        #region Buttons Logic
        private void StopTimerButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            System.Windows.Controls.Image image = FindVisualChild<System.Windows.Controls.Image>(StartTimerButton);
            StopTimerButton.IsEnabled = false;
            TimerChoosePanel.Visibility = Visibility.Visible;
            TimerCountdownPanel.Visibility = Visibility.Hidden;
            image.Source = new BitmapImage(new Uri("/Icons/PlayArrow.png", UriKind.Relative));
            delayedTimer.Enabled = false;
            isDelayedTimerStart = false;
            isDelayedTimerPaused = false;
        }

        private void HybernationTimerRB_Checked(object sender, RoutedEventArgs e)
        {
            if (settings.Count > 0)
            {
                int uid = int.Parse(((System.Windows.Controls.RadioButton)sender).Uid);
                selectedTimerMode = uid;
                if (settings["TimerMode"] != uid.ToString())
                    SaveSettings();
            }
        }
        private void StartTimerButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button button = (System.Windows.Controls.Button)sender;
            System.Windows.Controls.Image image = FindVisualChild<System.Windows.Controls.Image>(button);
            if (!isDelayedTimerPaused && !isDelayedTimerStart || (isDelayedTimerPaused && isDelayedTimerStart))
            {
                if (!isDelayedTimerPaused && !isDelayedTimerStart)
                {
                    StopTimerButton.IsEnabled = true;
                    TimerChoosePanel.Visibility = Visibility.Hidden;
                    TimerCountdownPanel.Visibility = Visibility.Visible;
                    timerSecondLeft = int.Parse(TimerBoxHours.Text) * 3600 + int.Parse(TimerBoxMinutes.Text) * 60 + int.Parse(TimerBoxSeconds.Text);
                    UpdateTimerTime();
                }
                image.Source = new BitmapImage(new Uri("/Icons/Pause.png", UriKind.Relative));
                delayedTimer = new System.Timers.Timer(1000);
                delayedTimer.Elapsed += DelayedTimerTick;
                delayedTimer.AutoReset = true;
                delayedTimer.Enabled = true;
                isDelayedTimerStart = true;
                isDelayedTimerPaused = false;
            }
            else
            {
                image.Source = new BitmapImage(new Uri("/Icons/PlayArrow.png", UriKind.Relative));
                isDelayedTimerPaused = true;
                delayedTimer.Enabled = false;
            }
        }
        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void _maskedTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            System.Windows.Controls.TextBox tb = ((System.Windows.Controls.TextBox)sender);
            if (tb.Text.Length == 2)
            {
                if (tb.Uid == "hours")
                {
                    if (int.Parse(tb.Text) >= 24 || int.Parse(tb.Text) < 0)
                    {
                        tb.Text = "00";
                    }
                }
                else
                {
                    if (int.Parse(tb.Text) >= 60 || int.Parse(tb.Text) < 0)
                    {
                        tb.Text = "00";
                    }
                }
            }
        }

        private void HybernationRB_Checked(object sender, RoutedEventArgs e)
        {
            if (settings.Count > 0)
            {
                int uid = int.Parse(((System.Windows.Controls.RadioButton)sender).Uid);
                selectedPowerOffMode = uid;
                if (settings["PowerOffMode"] != uid.ToString())
                    SaveSettings();
            }
        }
        private void OnlyDigitsProcedure(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void NoInsertProcedure(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
            }
        }
        private void Pages_Click(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Controls.Image image = sender as System.Windows.Controls.Image;
            HomePage.Visibility = Visibility.Hidden;
            NotificationsPage.Visibility = Visibility.Hidden;
            HomeUnderscoore.Visibility = Visibility.Hidden;
            NotificationsUnderscoore.Visibility = Visibility.Hidden;
            TimerUnderscoore.Visibility = Visibility.Hidden;
            SettingsUnderscoore.Visibility = Visibility.Hidden;
            SettingsPage.Visibility = Visibility.Hidden;
            TimerPage.Visibility = Visibility.Hidden;
            switch (image.Uid)
            {
                case "0":
                    HomePage.Visibility = Visibility.Visible;
                    HomeUnderscoore.Visibility = Visibility.Visible;
                    break;
                case "1":
                    NotificationsPage.Visibility = Visibility.Visible;
                    NotificationsUnderscoore.Visibility = Visibility.Visible;
                    break;
                case "2":
                    TimerUnderscoore.Visibility = Visibility.Visible;
                    TimerPage.Visibility = Visibility.Visible;
                    break;
                case "3":
                    SettingsUnderscoore.Visibility = Visibility.Visible;
                    SettingsPage.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void SleepOnToggle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Controls.Image toggleImage = (System.Windows.Controls.Image)sender;
            toggleImage.Tag = toggleImage.Tag.ToString() == "On" ? "Off" : "On";
            settings["PowerOn"] = toggleImage.Tag.ToString() == "On" ? "On" : "Off";
            SaveSettings();
            SetSettings(settings);
        }

        private void TimeButton_Click(object sender, MouseButtonEventArgs e)
        {
            CustomMessageBox customMessageBox = new CustomMessageBox();
            customMessageBox.Owner = this;
            customMessageBox.ShowDialog();
            is30mAlertShown = false;
            is5mAlertShown = false;
            is1mAlertShown = false;
            SaveSettings();
        }
        #endregion

        #region Toggles
        private void NotificationsToggle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Controls.Image toggleImage = (System.Windows.Controls.Image)sender;
            toggleImage.Tag = toggleImage.Tag.ToString() == "On" ? "Off" : "On";
            settings["Notifications"] = toggleImage.Tag.ToString() == "On" ? "On" : "Off";
            SaveSettings();
            SetSettings(settings);
        }

        private void SoundToggle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Controls.Image toggleImage = (System.Windows.Controls.Image)sender;
            toggleImage.Tag = toggleImage.Tag.ToString() == "On" ? "Off" : "On";
            settings["Sounds"] = toggleImage.Tag.ToString() == "On" ? "On" : "Off";
            SaveSettings();
            SetSettings(settings);
        }

        private void AutoStartToggle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Controls.Image toggleImage = (System.Windows.Controls.Image)sender;
            toggleImage.Tag = toggleImage.Tag.ToString() == "On" ? "Off" : "On";
            settings["AutoStart"] = toggleImage.Tag.ToString() == "On" ? "On" : "Off";
            SaveSettings();
            SetSettings(settings);
            if (AutoStartToggle.Tag.ToString() == "On" && !IsAutoStartEnabled())
            {
                AddToStartup();
            }
            else if (AutoStartToggle.Tag.ToString() == "Off" && IsAutoStartEnabled())
            {
                RemoveFromStartup();
            }
        }

        private void TrayStartToggle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Controls.Image toggleImage = (System.Windows.Controls.Image)sender;
            toggleImage.Tag = toggleImage.Tag.ToString() == "On" ? "Off" : "On";
            settings["TrayStart"] = toggleImage.Tag.ToString() == "On" ? "On" : "Off";
            SaveSettings();
            SetSettings(settings);
        }
        #endregion

        #region Timer Logic
        private void DelayedTimerTick(Object source, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (timerSecondLeft > 0)
                {
                    UpdateTimerTime();
                    timerSecondLeft--;
                }
                else if(timerSecondLeft == 0 && !isShutdowned)
                {
                    switch (poweroffvariants[selectedTimerMode])
                    {
                        case "Hybernation":
                             isShutdowned = true;
                             PowerShellCommand("shutdown /h");
                            break;
                        case "Shutdown":
                             isShutdowned = true;
                             PowerShellCommand("shutdown /s /f /t 0");
                            break;
                    }
                    SaveSettings();
                    System.Windows.Controls.Image image = FindVisualChild<System.Windows.Controls.Image>(StartTimerButton);
                    StopTimerButton.IsEnabled = false;
                    TimerChoosePanel.Visibility = Visibility.Visible;
                    TimerCountdownPanel.Visibility = Visibility.Hidden;
                    image.Source = new BitmapImage(new Uri("/Icons/PlayArrow.png", UriKind.Relative));
                    delayedTimer.Enabled = false;
                    isDelayedTimerStart = false;
                    isDelayedTimerPaused = false;
                }
            });
        }
        private void UpdateTimerTime()
        {
            int hoursLeft = timerSecondLeft / 3600;
            int minutesLeft = (timerSecondLeft - (hoursLeft * 3600)) / 60;
            int secondsLeft = timerSecondLeft - (hoursLeft * 3600) - (minutesLeft * 60);
            TimerCountdownBoxHours.Text = TimeToString(hoursLeft);
            TimerCountdownBoxMinutes.Text = TimeToString(minutesLeft);
            TimerCountdownBoxSeconds.Text = TimeToString(secondsLeft);
        }
        private string TimeToString(int time)
        {
            string result;
            if (time.ToString().Length == 1)
            {
                result = "0" + time.ToString();
            }
            else
            {
                result = time.ToString();
            }
            return result;
        }
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is T)
                {
                    return (T)child;
                }

                T childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }


        #endregion

        private void NormalizeTimeFormat(object sender, KeyboardFocusChangedEventArgs e)
        {
            System.Windows.Controls.TextBox tb = ((System.Windows.Controls.TextBox)sender);
            if (tb.Text.Length == 1)
            {
             tb.Text = "0" + tb.Text;
            }
        }
    }
}

