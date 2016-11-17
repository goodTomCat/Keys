using System;
using System.Collections.Generic;
using System.IO;
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
using KeySenderLib.KeySenderAdvanced;
using Microsoft.Win32;

namespace KeysSendingApplication2
{
    /// <summary>
    /// Логика взаимодействия для MainWindowControl.xaml
    /// </summary>
    public partial class MainWindowControl : UserControl
    {
        protected KeySenderAdvanced SenderF;
        protected IList<KeyToSend> KeysF;
        protected KeysSendingData DataF;
        protected Grid MainGridF;
        protected IDictionary<string, object> DataContextAsDicF;
        protected MediaElement MediaElementF = new MediaElement();


        public MainWindowControl(KeySenderAdvanced sender, KeysSendingData data, Grid mainGrid, 
            IDictionary<string, object> dataContextAsDic)
        {
            SenderF = sender;
            MainGridF = mainGrid;
            DataF = data;
            DataContextAsDicF = dataContextAsDic;            
            InitializeComponent();
            if (DataF.Profiles.Any())
                listBox.SelectedItem = DataF.Profiles.First();
            checkBox.IsChecked = !string.IsNullOrEmpty(data.MediaFilePath);

            MediaElementF.LoadedBehavior = MediaState.Manual;
            MediaElementF.UnloadedBehavior = MediaState.Manual;
            MediaElementF.MediaEnded += (o, args) =>
            {
                Dispatcher.Invoke(() =>
                {
                    MediaElementF.Position = TimeSpan.Zero;
                    MediaElementF.Play();
                });
            };
            SenderF.SendingStarted += (o, args) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (!checkBox.IsChecked.HasValue || !checkBox.IsChecked.Value)
                        return;
                    if (string.IsNullOrEmpty(DataF.MediaFilePath))
                        return;
                    if (!File.Exists(DataF.MediaFilePath))
                    {
                        MessageBox.Show("Файла по такому пути не существует.", "Error", MessageBoxButton.OK);
                        return;
                    }

                    MediaElementF.Source = new Uri(DataF.MediaFilePath);
                    MediaElementF.Play();
                });
            };
            SenderF.SendingStoped += (o, args) =>
            {
                Dispatcher.Invoke(() =>
                {
                    MediaElementF.Stop();
                });
            };
        }


        public KeysSendingData Data => DataF;


        private void SetKeysControlOnMainWnd(SendingProfile profile = null)
        {
            var keysControl = profile != null ? new KeysControl1(profile) : new KeysControl1();
            var parentWind = Window.GetWindow(this);
            var oldH = parentWind.ActualHeight;
            var oldW = parentWind.ActualWidth;

            parentWind.Height = 314;
            parentWind.Width = 419;
            MainGridF.Children.Remove(this);
            MainGridF.Children.Add(keysControl);
            var thisss = this;
            keysControl.ClosingEvent += (o, tuple) =>
            {
                if (tuple.Item1 != null)
                {
                    KeysF = tuple.Item1;
                    SenderF.StartOnce = tuple.Item2;
                }

                MainGridF.Children.Remove(keysControl);
                parentWind.Height = oldH;
                parentWind.Width = oldW;
                MainGridF.Children.Add(thisss);
                if (profile == null && tuple.Item1 != null)
                    DataF.Profiles.Add(new SendingProfile() {Keys = KeysF, Name = tuple.Item3, Repeat = tuple.Item2});
                else
                {
                    if (tuple.Item1 != null)
                    {
                        profile.Keys = KeysF;
                        profile.Repeat = tuple.Item2;
                    }
                }
            };
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            SetKeysControlOnMainWnd();
        }

        private void ListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBoxx = e.Source as ListBox;
            if (listBoxx == null)
                return;

            var items = listBoxx.SelectedItems.Cast<SendingProfile>().ToArray();
            if (items.Length == 0)
                return;

            if (items.Length > 1)
            {
                MessageBox.Show("Должен быть выделен лишь один профиль.", "Warning", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var itemFirst = items[0];
            SenderF.KeysToSendEnumeration = itemFirst.Keys;
            SenderF.StartOnce = itemFirst.Repeat;
        }

        private void Button1_OnClick(object sender, RoutedEventArgs e)
        {
            var items = listBox.SelectedItems.Cast<SendingProfile>().ToArray();
            if (items.Length == 0)
                return;

            var profiles = ((KeysSendingData)DataContextAsDicF["Options"]).Profiles;
            foreach (SendingProfile item in items)
                profiles.Remove(item);
        }

        private void Button2_OnClick(object sender, RoutedEventArgs e)
        {
            var items = listBox.SelectedItems;
            if (items.Count != 1)
            {
                MessageBox.Show("Должен быть выделен один профиль.", "Warning", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var profile = items[0] as SendingProfile;
            if (profile == null)
                return;

            SetKeysControlOnMainWnd(profile);
        }

        private void Label_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var mainWnd = DataContextAsDicF["MainWindow"] as MainWindow;

            mainWnd?.SetRectangle();
        }
        private void CheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            var checkBox = e.Source as CheckBox;
            if (checkBox == null)
                return;

            button3.IsEnabled = true;
            textBox.IsEnabled = true;
        }
        private void Button3_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Media Files|*.mp3;*.wav",
                Multiselect = false
            };
            if (openFileDialog.ShowDialog() == true)
                DataF.MediaFilePath = openFileDialog.FileName;
        }

        private void CheckBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = e.Source as CheckBox;
            if (checkBox == null)
                return;

            button3.IsEnabled = false;
            textBox.IsEnabled = false;
        }
    }
}
