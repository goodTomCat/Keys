using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
using KeySenderLib.KeySenderAdvanced;

namespace KeysSendingApplication2
{
    /// <summary>
    /// Логика взаимодействия для KeysControl1.xaml
    /// </summary>
    public partial class KeysControl1 : UserControl
    {
        public event EventHandler<Tuple<IList<KeyToSend>, bool, string>> ClosingEvent;


        public KeysControl1()
        {
            InitializeComponent();
        }
        /// <exception cref="ArgumentException">Профайл не должен равняться null или быть пустым.</exception>
        public KeysControl1(SendingProfile profile) : this()
        {
            if (profile == null || !profile.Keys.Any())
                throw new ArgumentException("Профайл не должен равняться null или быть пустым.", nameof(profile))
                {Source = GetType().AssemblyQualifiedName};

            var str = new StringBuilder();
            foreach (KeyToSend key in profile.Keys)
            {
                var keyy = key.IsVirtualKeyCode
                    ? KeyInterop.KeyFromVirtualKey(key.KeyCode)
                    : KeyInterop.KeyFromVirtualKey((byte) KeySenderAdvanced.ScanCodeToVirtual(key.KeyCode));
                var strToAdd = $"[{Enum.GetName(typeof(Key), keyy)}{(key.IsKeyUp ? "_Up" : "_Down")}]";
                if (key.DelayBeforeAsMSeconds != 0)
                {
                    if (str.Length > 0)
                        str.Append($"+[{key.DelayBeforeAsMSeconds}ms]");
                    else 
                        str.Append($"[{key.DelayBeforeAsMSeconds}ms]");
                }

                if (str.Length > 0)
                    str.Append($"+{strToAdd}");
                else
                    str.Append(strToAdd);

                if (key.DelayAfterAsMSeconds != 0)
                    str.Append($"+[{key.DelayAfterAsMSeconds}ms]");
            }
            textBox.Text = str.ToString();
            checkBox1.IsChecked = profile.Repeat;
            textBox1.Text = profile.Name;
            textBox1.IsReadOnly = true;
            radioButton.IsChecked = profile.Keys.First().IsVirtualKeyCode;
        }


        /// <exception cref="InvalidOperationException">Не верный формат подстроки. Оцутствует символ "[". -or- 
        /// Не верный формат строки. Оцутствует символ ]. -or- Не верный формат строки. Попытка присвоить задержку второй раз. -or- 
        /// Не верный формат строки. Попытка присвоить задержку второй раз. -or- 
        /// Не верный формат подстроки. Отсутствует символ "_". -or- Не удалось определить клавишу. -or- 
        /// Не удалось преобразовать предположительную задержку в число.</exception>
        protected IList<KeyToSend> ReadKeys(string str)
        {
            if (string.IsNullOrEmpty(str))
                return new KeyToSend[0];

            var strsAdd = str.Split('+');
            var delay = -1;
            var list = new List<KeyToSend>(strsAdd.Length/2);
            var converter = new KeyConverter();
            KeyToSend lastElement = null;
            foreach (string s in strsAdd)
            {
                try
                {
                    if (s[0] != '[')
                        throw new InvalidOperationException("Не верный формат подстроки. Оцутствует символ \"[\".")
                            {Source = GetType().AssemblyQualifiedName};
                    if (s[s.Length - 1] != ']')
                        throw new InvalidOperationException("Не верный формат строки. Оцутствует символ \"]\".")
                            {Source = GetType().AssemblyQualifiedName};

                    var str2 = s.Remove(0, 1);
                    str2 = str2.Remove(str2.Length - 1, 1);
                    if (str2.Contains("ms"))
                    {
                        var ind = str2.IndexOf("ms");
                        str2 = str2.Remove(ind);
                        var dela = int.Parse(str2);
                        if (list.Count > 0)
                        {
                            if (lastElement.DelayAfterAsMSeconds != 0)
                            {
                                var strBuild = new StringBuilder();
                                strBuild.AppendLine("Не верный формат строки. Попытка присвоить задержку второй раз.");
                                strBuild.Append($"Задержка: {dela}.");
                                throw new InvalidOperationException(strBuild.ToString())
                                    {Source = GetType().AssemblyQualifiedName};
                            }

                            lastElement.DelayAfterAsMSeconds = dela;
                            if (delay != -1)
                            {
                                lastElement.DelayBeforeAsMSeconds = delay;
                                delay = -1;
                            }
                        }
                        else
                        {
                            if (delay != -1)
                            {
                                var strBuild = new StringBuilder();
                                strBuild.AppendLine("Не верный формат строки. Попытка присвоить задержку второй раз.");
                                strBuild.Append($"Задержка: {dela}.");
                                throw new InvalidOperationException(strBuild.ToString())
                                    {Source = GetType().AssemblyQualifiedName};
                            }
                            delay = dela;
                        }
                    }
                    else
                    {
                        var ind = str2.LastIndexOf("_");
                        if (ind == -1)
                        {
                            var strBuild = new StringBuilder();
                            strBuild.AppendLine("Не верный формат подстроки. Отсутствует символ \"_\".");
                            strBuild.Append($"Подстрока: {s}.");
                            throw new InvalidOperationException(strBuild.ToString())
                                {Source = GetType().AssemblyQualifiedName};
                        }

                        var isKeyUp = str2.Contains("Up");
                        str2 = str2.Remove(ind);
                        var key = (Key) converter.ConvertFromString(str2);
                        var keyCode = radioButton.IsChecked.HasValue && radioButton.IsChecked.Value
                            ? KeyInterop.VirtualKeyFromKey(key)
                            : (int) KeySenderAdvanced.VirtualCodeToScan((uint) KeyInterop.VirtualKeyFromKey(key));
                        lastElement = new KeyToSend(keyCode,
                            radioButton.IsChecked.HasValue && radioButton.IsChecked.Value, isKeyUp);
                        list.Add(lastElement);
                    }
                }
                catch (NotSupportedException ex)
                {
                    var strBuild = new StringBuilder();
                    strBuild.AppendLine("Не удалось определить клавишу.");
                    strBuild.Append($"Подстрока с ошибкой: {s}.");
                    throw new InvalidOperationException(strBuild.ToString(), ex)
                        {Source = GetType().AssemblyQualifiedName};
                }
                catch (FormatException ex)
                {
                    var strBuild = new StringBuilder();
                    strBuild.AppendLine("Не удалось преобразовать предположительную задержку в число.");
                    strBuild.Append($"Подстрока с ошибкой: {s}.");
                    throw new InvalidOperationException(strBuild.ToString(), ex)
                        {Source = GetType().AssemblyQualifiedName};
                }
                catch (OverflowException ex)
                {
                    var strBuild = new StringBuilder();
                    strBuild.AppendLine("Не удалось преобразовать предположительную задержку в число.");
                    strBuild.Append($"Подстрока с ошибкой: {s}.");
                    throw new InvalidOperationException(strBuild.ToString(), ex)
                        {Source = GetType().AssemblyQualifiedName};
                }

            }
            return list;
        }

        #region WindowEvents

        private void TextBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (checkBox.IsChecked != null && checkBox.IsChecked.Value)
                e.Handled = true;
        }
        private void CheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            var isFocused = textBox.Focus();
        }
        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            IList<KeyToSend> keys = null;
            try
            {
                keys = ReadKeys(textBox.Text);
            }
            catch (InvalidOperationException ex)
            {
                var type = Type.GetType(ex.Source, false);
                if (type != null || type.IsAssignableFrom(GetType()))
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show("Во время чтения клавиш возникла непредвиденная ошибка", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Во время чтения клавиш возникла непредвиденная ошибка", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (keys == null || keys.Count == 0)
                return;
            if (string.IsNullOrEmpty(textBox1.Text) || string.IsNullOrWhiteSpace(textBox1.Text))
            {
                MessageBox.Show("Введите имя профиля.", "Warning", MessageBoxButton.OK);
                return;
            }

            var events = ClosingEvent;
            if (events == null)
                return;

            var delegates = events.GetInvocationList().Cast<EventHandler<Tuple<IList<KeyToSend>, bool, string>>>();
            foreach (EventHandler<Tuple<IList<KeyToSend>, bool, string>> handler in delegates)
            {
                handler(this,
                    new Tuple<IList<KeyToSend>, bool, string>(keys, checkBox1.IsChecked.HasValue && checkBox1.IsChecked.Value, textBox1.Text));
            }
        }

        #endregion

        private void Button1_OnClick(object sender, RoutedEventArgs e)
        {
            var events = ClosingEvent;
            if (events == null)
                return;

            var delegates = events.GetInvocationList().Cast<EventHandler<Tuple<IList<KeyToSend>, bool, string>>>();
            foreach (EventHandler<Tuple<IList<KeyToSend>, bool, string>> handler in delegates)
            {
                handler(this,
                    new Tuple<IList<KeyToSend>, bool, string>(null, false, null));
            }
        }

        private void TextBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (checkBox.IsChecked == null || !checkBox.IsChecked.Value)
                return;

            var textToAdd =
                $"[{Enum.GetName(typeof(Key), e.Key)}_Down]+[{KeyToSend.DelayKeyUpDown}ms]+[" +
                $"{Enum.GetName(typeof(Key), e.Key)}_Up]+[{KeyToSend.DelayKeyPress}ms]";
            if (textBox.Text.Length <= 1)
                textBox.Text = textToAdd;
            else
                textBox.Text += "+" + textToAdd;
            e.Handled = true;
        }
        private void TextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            textBox.ScrollToEnd();
        }
    }
}
