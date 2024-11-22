using System.Windows;
using System.Windows.Input;

namespace DeepSleep
{
    public partial class CustomMessageBox : Window
    {
        public CustomMessageBox()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter || e.Key == Key.Space || e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}
