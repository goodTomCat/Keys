using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using KeySenderLib;
using KeySenderLib.KeySenderAdvanced;
using ComboBox = System.Windows.Controls.ComboBox;
using DataFormats = System.Windows.DataFormats;
using DataObject = System.Windows.DataObject;
using ProtoBuf;
using ProtoBuf.Meta;
using JabyLib.Other;


namespace KeysSendingApplication2
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        protected KeysSendingData OptionsF;
        protected KeySenderAdvanced SenderF;
        protected bool StartedF;
        public event PropertyChangedEventHandler PropertyChanged;
        protected Dictionary<string, UIElement> ChildrenToRemoveF = new Dictionary<string, UIElement>();
        protected UserControl MainWindowControlF;
        protected Dictionary<string, object> DicAsDataContextF = new Dictionary<string, object>();
        protected ObservableCollection<SendingProfile> ProfilesF;
        protected Task ListenTaskF;


        /// <exception cref="ArgumentOutOfRangeException">Source: <see cref="SendingOptions.ReadFromFile(string)"/></exception>
        /// <exception cref="IOException">Source: <see cref="SendingOptions.ReadFromFile(string)"/></exception>
        /// <exception cref="SerializationException">Source: <see cref="SendingOptions.ReadFromFile(string)"/></exception>
        /// <exception cref="KeyNotFoundException">Не удалось сопоставить название клавиши и её код.</exception>
        /// <exception cref="Exception">Source: <see cref="SendingOptions.ReadFromFile(string)"/> -or- 
        /// При использовании конструктора по умолчанию объекта <see cref="MainWindow"/> возникла непредвиденная ошибка.</exception>
        public MainWindow()
        {
            try
            {
                MainWindowImpl();
            }
            catch (ArgumentOutOfRangeException)
            {
                throw;
            }
            catch (IOException)
            {
                throw;
            }
            catch (SerializationException ex)
            {
                throw;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var type = Type.GetType(ex.Source, false);
                if (type == null)
                    throw new Exception(
                        $"При использовании конструктора по умолчанию объекта {nameof(MainWindow)} возникла непредвиденная ошибка.",
                        ex) {Source = GetType().AssemblyQualifiedName};

                if (typeof(SendingOptions).IsAssignableFrom(type) || GetType().IsAssignableFrom(type))
                    throw;
                else
                    throw new Exception(
                            $"При использовании конструктора по умолчанию объекта {nameof(MainWindow)} возникла непредвиденная ошибка.",
                            ex)
                        {Source = GetType().AssemblyQualifiedName};
            }

        }


        public bool Started
        {
            get { return StartedF; }
            set
            {
                StartedF = value;
                OnPropertyChanged("Started");
            }
        }


        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ProtoBufInitializer()
        {
            var model = RuntimeTypeModel.Default;
            var keyToSendSurMeta = model.Add(typeof(KeyToSendSur), true);
            keyToSendSurMeta.Add("DelayAfterAsMSeconds", "DelayBeforeAsMSeconds", "IsKeyUp", "IsVirtualKeyCode",
                "KeyCode");

            var sendingProfMeta = model.Add(typeof(SendingProfile), true);
            sendingProfMeta.Add("Keys", "Repeat", "Name");

            var keysSendingDataMeta = model.Add(typeof(KeysSendingData), true);
            keysSendingDataMeta.Add("TurnKey", "Profiles", "MediaFilePath");

            var keyTosendMeta = model.Add(typeof(KeyToSend), false);
            keyTosendMeta.SetSurrogate(typeof(KeyToSendSur));
        }

        /// <exception cref="KeyNotFoundException">Не удалось сопоставить название клавиши и её код.</exception>
        private void MainWindowImpl()
        {
            InitializeComponent();
            ProtoBufInitializer();
            DataContext = DicAsDataContextF;
            OptionsF = new KeysSendingData();
            if (File.Exists(Directory.GetCurrentDirectory() + "\\" + "SendKeyToProccesOptions.sel"))
                OptionsF.ReadFromFile(Directory.GetCurrentDirectory() + "\\" + "SendKeyToProccesOptions.sel").Wait();
            DicAsDataContextF.Add("Options", OptionsF);
            DicAsDataContextF.Add("MainWindow", this);
            SenderF = new KeySenderAdvanced();
            SenderF.SendingStarted += (sender, args) => Started = true;
            SenderF.SendingStoped += (sender, args) => Started = false;
            if (OptionsF.TurnKey != 0)
            {
                SenderF.TurnKey = OptionsF.TurnKey;
                SenderF.Unhook();
                SenderF.SetHook();
                SenderF.StartListen();
            }

            MainWindowControlF = new MainWindowControl(SenderF, OptionsF, MainGrid, DicAsDataContextF);
            MainGrid.Children.Add(MainWindowControlF);
            if (OptionsF.TurnKey == 0)
                SetRectangle();
        }

        public void SetRectangle()
        {
            var rectangleAsObject = Application.Current.Resources["Rectangle"];
            if (rectangleAsObject == null)
                return;
            var rectangle = rectangleAsObject as Rectangle;
            if (rectangle == null)
                return;

            rectangle.KeyUp += RectangleKeyUp_Event;

            var lableAsObject = Application.Current.Resources["PressKeyLable"];
            if (lableAsObject == null)
                return;
            var label = lableAsObject as Label;
            if (label == null)
                return;

            MainGrid.Children.Add(rectangle);
            MainGrid.Children.Add(label);
            ChildrenToRemoveF.Add("Rectangle", rectangle);
            ChildrenToRemoveF.Add("label", label);
            var isFocus = label.Focus();
            Keyboard.Focus(rectangle);
            FocusManager.SetFocusedElement(MainGrid, rectangle);
        }

        #region events

        private void RectangleKeyUp_Event(object sender, KeyEventArgs e)
        {
            var keyToTurn = KeyInterop.VirtualKeyFromKey(e.Key);
            OptionsF.TurnKey = (byte) keyToTurn;
            SenderF.TurnKey = keyToTurn;
            while (ChildrenToRemoveF.Count != 0)
            {
                var keyValuePair = ChildrenToRemoveF.First();
                MainGrid.Children.Remove(keyValuePair.Value);
                ChildrenToRemoveF.Remove(keyValuePair.Key);
            }
            if (Started) return;

            SenderF.Unhook();
            SenderF.SetHook();
            SenderF.StartListen();
        }
        private async void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            try
            {
                await OptionsF.WriteToFile(Directory.GetCurrentDirectory() + "\\" + "SendKeyToProccesOptions.sel");
            }
            catch (SerializationException ex)
            {
                MessageBox.Show("При записи настроек в файл возникла ошибка сериализации.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (IOException ex)
            {
                MessageBox.Show("При записи настроек в файл возникла ошибка ввода/вывода.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("При записи настроек в файл возникла непредвиденная ошибка.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            
        }

        #endregion


    }

    public class ListBoxItemData
    {
        public string Name { get; set; }
        public string Country { get; set; }
        public int Numb { get; set; }
    }

}
