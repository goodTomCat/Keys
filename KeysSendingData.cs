using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using JabyLib.Other;
using KeySenderLib.KeySenderAdvanced;

namespace KeysSendingApplication2
{
    public class KeysSendingData : INotifyPropertyChanged
    {
        private byte _turnKey;
        public event PropertyChangedEventHandler PropertyChanged;
        private string _mediaFilePathF;


        public KeysSendingData()
        { }
        /// <exception cref="ArgumentNullException">options == null.</exception>
        public KeysSendingData(KeysSendingData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data)) {Source = GetType().AssemblyQualifiedName};

            Clone(data);
        }


        public byte TurnKey
        {
            get { return _turnKey; }
            set
            {
                _turnKey = value;
                OnPropertyChanged("TurnKey");
            }
        }
        public ObservableCollection<SendingProfile> Profiles { get; set; } = new ObservableCollection<SendingProfile>();
        public string MediaFilePath
        {
            get { return _mediaFilePathF; }
            set
            {
                _mediaFilePathF = value;
                OnPropertyChanged("MediaFilePath");
            }
        }


        /// <exception cref="ArgumentNullException">path == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Файла не существует, либо его длина равна нулю.</exception>
        /// <exception cref="IOException">При чтении настроек из файла возникла ошибка ввода вывода.</exception>
        /// <exception cref="SerializationException">При чтении настроек из файла возникла ошибка десеарелизации.</exception>
        /// <exception cref="Exception">При чтении настроек из файла возникла непредвиденная ошибка.</exception>
        public virtual async Task ReadFromFile(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path)) { Source = GetType().AssemblyQualifiedName };
            var infoOfFile = new FileInfo(path);
            if (!infoOfFile.Exists || infoOfFile.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(path), "Файла не существует, либо его длина равна нулю.")
                { Source = GetType().AssemblyQualifiedName };

            int length = -1;
            int numbOfReadedBytes = -1;
            try
            {
                using (var stream = infoOfFile.OpenRead())
                {
                    var bytesOfLength = new byte[100];
                    numbOfReadedBytes =
                        await stream.ReadAsync(bytesOfLength, 0, bytesOfLength.Length).ConfigureAwait(false);
                    length = BitConverter.ToInt32(bytesOfLength, 0);
                    if (length < 0 || length > 20000)
                        throw new InvalidOperationException("Не тот файл.");

                    MemoryStream streamMem;
                    if (length > numbOfReadedBytes)
                    {
                        streamMem = new MemoryStream(bytesOfLength, 4, numbOfReadedBytes - 4, true);
                        bytesOfLength = new byte[length - numbOfReadedBytes];
                        await stream.ReadAsync(bytesOfLength, 0, bytesOfLength.Length).ConfigureAwait(false);
                        streamMem.Seek(streamMem.Length, SeekOrigin.Begin);
                        streamMem.Write(bytesOfLength, 0, bytesOfLength.Length);
                    }
                    else
                        streamMem = new MemoryStream(bytesOfLength, 4, length);


                    var ser = new ProtoBufSerializer();
                    var data = ser.Deserialize<KeysSendingData>(streamMem, false);
                    Clone(data, false);
                }
            }
            catch (IOException ex)
            {
                throw CreateException(0, 0, ex, path, length);
            }
            catch (SerializationException ex)
            {
                throw CreateException(0, 1, ex, path);
            }
            catch (Exception ex)
            {
                throw CreateException(0, 2, ex, path, length, numbOfReadedBytes);
            }


        }
        /// <exception cref="ArgumentNullException">directory == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Указанной директории не существует.</exception>
        public async Task<string> WriteToFile(DirectoryInfo directory)
        {
            if (directory == null)
                throw new ArgumentNullException(nameof(directory)) { Source = GetType().AssemblyQualifiedName };
            if (!directory.Exists)
                throw new ArgumentOutOfRangeException(nameof(directory.Exists))
                { Source = GetType().AssemblyQualifiedName };

            var path = directory + "//" + "Key Sending Options.opt";
            await WriteToFile(path).ConfigureAwait(false);
            return path;
        }
        /// <exception cref="ArgumentNullException">path == null.</exception>
        /// <exception cref="SerializationException">При записи настроек в файл возникла ошибка сериализации.</exception>
        /// <exception cref="ArgumentException"><see cref="File.Create(string)"/>.</exception>
        /// <exception cref="IOException">При записи настроек в файл возникла ошибка ввода/вывода.</exception>
        /// <exception cref="NotSupportedException"><see cref="File.Create(string)"/>.</exception>
        /// <exception cref="Exception">При записи настроек в файл возникла непредвиденная ошибка.</exception>
        public virtual async Task WriteToFile(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path)) { Source = GetType().AssemblyQualifiedName };

            try
            {
                var ser = new ProtoBufSerializer();
                var bytesToWrite = ser.Serialize(this, false);
                using (var stream = File.Create(path))
                {
                    var lengthAsBytes = BitConverter.GetBytes(bytesToWrite.Length);
                    stream.Write(lengthAsBytes, 0, lengthAsBytes.Length);
                    await stream.WriteAsync(bytesToWrite, 0, bytesToWrite.Length).ConfigureAwait(false);
                }
            }
            catch (SerializationException ex)
            {
                throw CreateException(1, 0, ex);
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException(ex.Message, nameof(path), ex) { Source = GetType().AssemblyQualifiedName };
            }
            catch (IOException ex)
            {
                throw CreateException(1, 1, ex, path);
            }
            catch (NotSupportedException ex)
            {
                throw new ArgumentOutOfRangeException(nameof(path), ex.Message)
                { Source = GetType().AssemblyQualifiedName };
            }
            catch (Exception ex)
            {
                throw CreateException(1, 2, ex, path);
            }

        }
        public void Clone(KeysSendingData data)
        {
            Clone(data, true);
        }
        public void Clone(KeysSendingData data, bool deepClone)
        {
            if (deepClone)
            {
                TurnKey = data.TurnKey;
                Profiles = new ObservableCollection<SendingProfile>(data.Profiles.Select(profile =>
                {
                    return new SendingProfile
                    {
                        Repeat = profile.Repeat,
                        Name = (string) profile.Name.Clone(),
                        Keys = profile.Keys.Select(send =>
                        {
                            var keySur = (KeyToSendSur) send;
                            return (KeyToSend) keySur;
                        })
                    };
                }).ToArray());
                MediaFilePath = (string) data.MediaFilePath.Clone();
            }
            else
            {
                TurnKey = data.TurnKey;
                Profiles = data.Profiles;
                MediaFilePath = data.MediaFilePath;
            }
        }


        private Exception CreateException(int numb, int innerNumb, params object[] objs)
        {
            Exception result = null;
            StringBuilder str = new StringBuilder();
            switch (numb)
            {
                case 0:
                    #region ReadFromFile(string path)
                    switch (innerNumb)
                    {
                        case 0:
                            //CreateException(0, 0, 0ex, 1path, 2length)
                            str.AppendLine("При чтении настроек из файла возникла ошибка ввода вывода.");
                            str.AppendLine($"Путь: {objs[1]}.");
                            str.Append($"length: {objs[2]}.");
                            result = new IOException(str.ToString(), (IOException)objs[0]);
                            break;
                        case 1:
                            //CreateException(0, 1, 0ex, 1path)
                            str.AppendLine("При чтении настроек из файла возникла ошибка десеарелизации.");
                            str.Append($"Путь: {objs[1]}.");
                            result = new SerializationException(str.ToString(), (SerializationException)objs[0]);
                            break;
                        case 2:
                            //CreateException(0, 2, 0ex, 1path, 2length, 3numbOfReadedBytes)
                            str.AppendLine("При чтении настроек из файла возникла непредвиденная ошибка.");
                            str.AppendLine($"Путь: {objs[1]}.");
                            str.AppendLine($"length: {objs[2]}.");
                            str.Append($"numbOfReadedBytes: {objs[3]}.");
                            result = new Exception(str.ToString(), (Exception)objs[0]);
                            break;
                    }
                    #endregion
                    break;
                case 1:
                    #region WriteToFile(string path)
                    switch (innerNumb)
                    {
                        case 0:
                            str.Append("При записи настроек в файл возникла ошибка сериализации.");
                            result = new SerializationException(str.ToString(), (SerializationException)objs[0]);
                            break;
                        case 1:
                            //CreateException(1, 1, 0ex, 1path)
                            str.AppendLine("При записи настроек в файл возникла ошибка ввода/вывода.");
                            str.Append($"path: {objs[1]}.");
                            result = new IOException(str.ToString(), (IOException)objs[0]);
                            break;
                        case 2:
                            //CreateException(1, 2, ex, path)
                            str.AppendLine("При записи настроек в файл возникла непредвиденная ошибка.");
                            str.Append($"path: {objs[1]}.");
                            result = new Exception(str.ToString(), (Exception)objs[0]);
                            break;
                    }
                    #endregion
                    break;
            }
            if (result == null)
            {
                str.AppendLine("Не удалось найти подходящего описания ошибки.");
                str.AppendLine($"numb: {numb}.");
                str.Append($"innerNumb: {innerNumb}.");
                result = new Exception(str.ToString());
            }
            result.Source = GetType().AssemblyQualifiedName;
            return result;
        }
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
