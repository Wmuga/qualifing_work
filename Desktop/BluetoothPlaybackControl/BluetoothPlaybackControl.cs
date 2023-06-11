using System;
using System.Collections.Generic;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
using Windows.Devices.Bluetooth;
using Windows.Storage.Streams;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

namespace BluetoothPlaybackControl
{
    /// <summary>
    /// Реализация библиотеки подключения по BPC
    /// </summary>
    public class BluetoothPlaybackControl : IDisposable, IBluetoothPlaybackControl
    {
        /// <summary>
        /// Конструктор класса
        /// </summary>
        public BluetoothPlaybackControl() {
            // Поисковик устройств
            _dw = DeviceInformation.CreateWatcher(
                RfcommDeviceService.GetDeviceSelector(_serviceId));
            // Таймер для отправки PING
			_keepAliveTimer = new Timer
			{
				Interval = 30000,
                AutoReset = true
			};
            // Таймер ожидания PONG
            _awaitPongTimer = new Timer
            {
                Interval = 1000,
                AutoReset = false
            };
            // Привязка события истечения таймера
            _keepAliveTimer.Elapsed += (_, e) => SendPing();
            _awaitPongTimer.Elapsed += (_, e) => Disconnect();

            // События поисковика устройств
            _dw.Added += async (_, e) =>
            {
				var bd = await BluetoothDevice.FromIdAsync(e.Id);
				Added?.Invoke(bd.DeviceInformation, this);
            };

			_dw.Removed += (_, e) =>
			{
				Removed?.Invoke(e.Id, this);
			};
		}
		/// <summary>
		/// Начало поиска устройств
		/// </summary>
		public void Start()
        {
            _dw.Start();
        }
		/// <summary>
		/// Подключение к устройству по BPC
		/// </summary>
		/// <param name="device">Устройство для подключения</param>
		/// <returns>Объект ассинхронного события</returns>
		public async Task Connect(DeviceInformation device)
        {
            // Отключиться от текущих соединений
            Disconnect();
            // Проверка на наличии сервиса BPC в устройстве
            var bd = await BluetoothDevice.FromIdAsync(device.Id);
            var services = await bd.GetRfcommServicesForIdAsync(_serviceId, BluetoothCacheMode.Uncached);
            if (services.Services.Count == 0) 
            {
                ConnectionChanged?.Invoke(new DeviceConnectionEventArgs(false,device,Resources.STR_NO_SERVICE),this);
                return;
            }
            var chatService = services.Services[0];

            // Сокет подклюения
            StreamSocket socket;

            lock (this)
            {
                socket = new StreamSocket();
            }
            try
            {
                // Поптыка установить подключение
                await socket.ConnectAsync(
                    chatService.ConnectionHostName,
                    chatService.ConnectionServiceName
                    );
                _connection = new Pair<StreamSocket, DataWriter> {
                    First = socket,
                    Second = new DataWriter(socket.OutputStream)
                };
                _connectedDevice = device;
                // Успешное подключение
                ConnectionChanged?.Invoke( new DeviceConnectionEventArgs(true, device, Resources.STR_CONNECTED), this);
                _keepAliveTimer.Start();
                // Запуск чтения входного потока
                ReadLoop(new DataReader(socket.InputStream));
            }
            // Не найден элемент
            catch (Exception ex) when ((uint)ex.HResult == 0x80070490) // ERROR_ELEMENT_NOT_FOUND
            {
				ConnectionChanged?.Invoke(new DeviceConnectionEventArgs(false, device, Resources.STR_NOT_FOUND), this);
            }
            // Утсройство уже используется
            catch (Exception ex) when ((uint)ex.HResult == 0x80072740) // WSAEADDRINUSE
            { 
			    ConnectionChanged?.Invoke(new DeviceConnectionEventArgs(false, device, Resources.STR_IN_USE), this);
            }
        }
        /// <summary>
        /// Цикл чтения входного потока и обработки сообщений
        /// </summary>
        /// <param name="dataReader">Входной поток</param>
        private async void ReadLoop(DataReader dataReader)
        {
            try
            {
                while (true)
                {
                    // Загрузка размера пакета
                    var loaded = await dataReader.LoadAsync(4);
                    var size = dataReader.ReadInt32();
                    var actual = await dataReader.LoadAsync((uint)size);
                    if (size != actual)
                        throw new Exception(Resources.STR_SMTH_WRONG);
                    // Загрузка самого пакета
                    var buffer = new byte[size];
                    dataReader.ReadBytes(buffer);
                    var type = (CommandType)buffer[0];
                    // Определение типа сообщения
                    switch (type)
                    {
                        // PONG
                        case CommandType.Pong:
                            _awaitPongTimer.Stop();
                            break;
                        // Уровень громкости
                        case CommandType.VolumeLevel:
                            {
								byte volume = buffer[1];
                                VolumeInformation?.Invoke(volume, this);
							}
                            break;
                        // Метаданные медиа
                        case CommandType.SongMeta:
                            {
                                string res = System.Text.Encoding.UTF8.GetString(buffer[1..]);
                                ParseSongMeta(res);
                            }
                            break;
                        // Состояние проигрывания
                        case CommandType.SongState:
                            PlaybackStateChanged?.Invoke(buffer[1] == 1, this);
                            break;
                        default:
                            break;

                    }
                }
            }
            // Утсройство отключилось
            catch(Exception _)
            {
                Disconnect();
			}
        }
        /// <summary>
        /// Преобразует строку в класс метаданных
        /// </summary>
        /// <param name="res">Строка с метаданными</param>
		private void ParseSongMeta(string res)
		{
            // Проверка регулярным выражением
            var regRes = _metaRegex.Match(res);
            if (!regRes.Success) 
                return;
            // Сообщаем о метаданных
			PlaybackInformationRecieved?.Invoke(new SongMeta()
			{
                Artist = regRes.Groups[1].Value,
				Name = regRes.Groups[2].Value,
                Album = regRes.Groups[3].Value,
			}, this);
		}
		/// <summary>
		/// Отправить кнопку воспроизвести приостановить
		/// </summary>
		public void SendPlayPause() => SendBtn(85);
		/// <summary>
		/// Отправить кнопку остановить
		/// </summary>
		public void SendStop() => SendBtn(86);
		/// <summary>
		/// Отправить кнопку предыдущий
		/// </summary>
		public void SendPrev() => SendBtn(88);
		/// <summary>
		/// Отправить кнопку следующий
		/// </summary>
		public void SendNext() => SendBtn(87);
		/// <summary>
		/// Отправить кнопку громкость вверх
		/// </summary>
		public void SendVolumeUp() => SendBtn(24);
		/// <summary>
		/// Отправить кнопку громкость вниз
		/// </summary>
		public void SendVolumeDown() => SendBtn(25);
		/// <summary>
		/// Отправить уровень громкости
		/// </summary>
		/// <param name="volume">Уровень громкости в %</param>
		public void SendVolume(byte volume)
        {
			if (_connection is null || _sending) return;
			_sendVolumeTemplate[1] = volume;
            SendToServer(_sendVolumeTemplate);
		}
        /// <summary>
        /// Отправка команды Ping
        /// </summary>
        private void SendPing()
        {
            if (_connection is null || _sending) return;
            _awaitPongTimer.Start();
            SendToServer(_sendPingTemplate);
        }
        /// <summary>
        /// Отравка команды нажатия на кнопку
        /// </summary>
        /// <param name="btn">Номер кнопки</param>
		private void SendBtn(byte btn)
        {
            if (_connection is null || _sending) return;
            _sendBtnTemplate[1] = btn;
            SendToServer(_sendBtnTemplate);
        }
        /// <summary>
        /// Отправка сообщения на сервер
        /// </summary>
        /// <param name="bytes">Содержание сообщения</param>
        private async void SendToServer(byte[] bytes)
        {
            if (_connection is null || _sending) return;
            _sending = true;
            _connection.Second.WriteUInt32((uint)bytes.Length);
            _connection.Second.WriteBytes(bytes);
            await _connection.Second.StoreAsync();
            _sending = false;
        }
		/// <summary>
		/// Отключиться от устройства
		/// </summary>
		public void Disconnect()
        {
            if (_connection is null) return;
            _keepAliveTimer.Stop();
            ConnectionChanged?.Invoke(new DeviceConnectionEventArgs(false, _connectedDevice, ""), this);
            _connectedDevice = null;
            _connection?.First?.Dispose();
            _connection = null;
        }
        /// <summary>
        /// Освобождение ресурсов
        /// </summary>
        public void Dispose()
        {
            _connection?.First?.Dispose();
        }
		// Добавлено устройство
		public event DeviceAddedEventHandler Added;
		// Утсройство утеряно
		public event DeviceRemovedEventHandler Removed;
		// Изменено состояние подключения
		public event DeviceConnectionChangedEventHandler ConnectionChanged;
		// Получена информация о метаданных
		public event PlaybackInformationEventHandler PlaybackInformationRecieved;
		// Получена инофрмация о состоянии проигрывании
		public event PlaybackStateChangedEventHandler PlaybackStateChanged;
		// Получена информация о текущем уровне громкости
		public event VolumeInformationEventHandler VolumeInformation;
        // Поисковик устройств
        private readonly DeviceWatcher _dw;
        // Подключение
        private Pair<StreamSocket, DataWriter> _connection;
        // UUID сервиса BPC в различных представлениях
        private static readonly string UUID_STR = "099C3D8F-58CE-4746-82C3-A975006885CB";
        private static readonly Guid APP_UUID = Guid.Parse(UUID_STR);
        private static readonly RfcommServiceId _serviceId = RfcommServiceId.FromUuid(APP_UUID);
        // Подключенное устройство
        private DeviceInformation _connectedDevice = null;
        // Отправляется ли какое-либо сообщение
        private bool _sending = false;
        // Шаблоны сообщений о кнопке, уровне громкости и Ping соответственно
		private static readonly byte[] _sendBtnTemplate = { 1, 0 };
		private static readonly byte[] _sendVolumeTemplate = { 2, 0 };
		private static readonly byte[] _sendPingTemplate = { 0, 0 };
        // Регулярное выражение для выделения метаданных
		private readonly Regex _metaRegex = new Regex(@"artist=(.*);name=(.*);album=(.*)", RegexOptions.Compiled);
        // Таймер отправки Ping
        private Timer _keepAliveTimer;
        // Таймер ожидания Pong
        private Timer _awaitPongTimer;
	}

	class Pair<T, T1>
    {
        public T First { get; set; }
        public T1 Second { get; set; }
    }
    // Перечисления для присваивание названий номерам команд
    enum CommandType
    {
        Pong = 0,
		DispatchButton,
		SetVolume,
		VolumeLevel,
		SongMeta,
		SongState,
	}
}
