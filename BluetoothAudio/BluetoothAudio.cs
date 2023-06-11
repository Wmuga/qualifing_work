using System;
using System.Collections.Generic;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;

namespace BluetoothAudio
{
    /// <summary>
    /// Реализация библиотеки подключения по A2DP
    /// </summary>
    public class BluetoothAudio : IBluetoothAudio, IDisposable
    {
        /// <summary>
        /// Конструктор класса
        /// </summary>
        public BluetoothAudio()
        {
            // Создание поисковика устройств и привязка его события к собственным
            _devices = new Dictionary<string, DeviceInformation>();
            _dw = DeviceInformation.CreateWatcher(AudioPlaybackConnection.GetDeviceSelector());

            _dw.Added += (sender, e) =>
            {
                _devices.Add(e.Id, e);
                this.Added?.Invoke(e, this);
            };

            _dw.EnumerationCompleted += (sender, e) =>
            {
                this.EnumerationCompleted?.Invoke(sender, e);
            };

            _dw.Removed += (s, e) =>
            {
                this.Removed?.Invoke(_devices[e.Id], this);
                _devices.Remove(e.Id);
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
		/// Подключение к устройству
		/// </summary>
		/// <param name="device">Устройство для подключения</param>
		public void Connect(DeviceInformation device)
        {
            // Было ли найдено это устройство библиотекой
            if (!_devices.ContainsKey(device.Id))
            {
                AudioConnectionStateChanged?.Invoke(this, new AudioConnectionEventArgs(false, device, string.Format(Resources.STR_NOT_FOUND, device.Name)));
                return;
            }
            // Если было подключение, отключиться
            _connection?.Second.Dispose();
            // Попытка создания подключения
            var connect = AudioPlaybackConnection.TryCreateFromId(device.Id);
            // Получилось ли
            if (connect == null)
            {
				AudioConnectionStateChanged?.Invoke(this, new AudioConnectionEventArgs(false, device, string.Format(Resources.STR_CANT_CONNECT, device.Name)));
                return;
            }
            // Открытие канала подключения
            connect.Start();
            connect.OpenAsync().Completed += (asyncResults, status) =>
            {
                // Проверка состояния
                switch (asyncResults.GetResults().Status)
                {
                    // Успешное подключение
                    case AudioPlaybackConnectionOpenResultStatus.Success:
                        if (!_devices.ContainsKey(device.Id))
                        {
							AudioConnectionStateChanged?.Invoke(this, new AudioConnectionEventArgs(false, device, string.Format(Resources.STR_LOST_DEVICE, device.Name)));
                            break;
                        }
                        _connection = new Pair<string, AudioPlaybackConnection> { First=device.Id, Second=connect };
                        AudioConnectionStateChanged?.Invoke(this, new AudioConnectionEventArgs(true, device, Resources.STR_CONNECTED));
                        break;
                    // Превышено время ожидания
                    case AudioPlaybackConnectionOpenResultStatus.RequestTimedOut:
                        AudioConnectionStateChanged?.Invoke(this, new AudioConnectionEventArgs(false, device, Resources.STR_TIMEOUT));
                        break;
                    // Отказано системой
                    case AudioPlaybackConnectionOpenResultStatus.DeniedBySystem:
                        AudioConnectionStateChanged?.Invoke(this, new AudioConnectionEventArgs(false, device, Resources.STR_SYS_DENIED));
                        break;
                    // Неизвестная ошибка
                    case AudioPlaybackConnectionOpenResultStatus.UnknownFailure:
                        AudioConnectionStateChanged?.Invoke(this, new AudioConnectionEventArgs(false, device, Resources.STR_UNKNOWN_FAIL));
                        break;
                }
            };
            // Если соединение закрылось - освободить ресурсы, сообщить
            connect.StateChanged += (connection, status) =>
            {
                if (connect.State == AudioPlaybackConnectionState.Closed)
                {
                    connect.Dispose();
                    _connection = null;
                    AudioConnectionStateChanged?.Invoke(this, new AudioConnectionEventArgs(false, device, ""));
                }
            };
        }
		/// <summary>
		/// Отключение от устройства
		/// </summary>
		public void Disconnect()
        {
            if (_connection == null) return;
            AudioConnectionStateChanged?.Invoke(this, new AudioConnectionEventArgs(false, _devices[_connection.First], ""));
            _connection.Second.Dispose();
            _connection = null;
        }
		/// <summary>
		/// Получение всех найденных устройств
		/// </summary>
		/// <returns>Найденные устройства</returns>
		public IEnumerable<DeviceInformation> GetDevices()
        {
            foreach (var info in _devices.Values)
            {
                yield return info;
            }
        }
        /// <summary>
        /// Освобождение ресурсов устройств
        /// </summary>
        public void Dispose()
        {
            _connection?.Second?.Dispose();
            _devices = null;

        }
		// Найдено новое устройство
		public event Windows.Foundation.TypedEventHandler<DeviceInformation, object> Added;
        // Устройство утеряно
        public event Windows.Foundation.TypedEventHandler<DeviceInformation, object> Removed;
        // Закончено перечисление устройств
        public event Windows.Foundation.TypedEventHandler<DeviceWatcher, object> EnumerationCompleted;
        // Изменено состояние подключения
        public event AudioConnectionEventHandler AudioConnectionStateChanged;
        // Текущее подключение
        private Pair<string,AudioPlaybackConnection> _connection;
        // Смотритель за устройствами
        private DeviceWatcher _dw;
        // Найденные устройства
        private Dictionary<string, DeviceInformation> _devices;

    }

    class Pair<T,T1>
    {
        public T First { get; set; }
        public T1 Second { get; set; }
    }
}
