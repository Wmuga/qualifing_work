using System;
using System.Collections.Generic;
using System.Text;
using Windows.Devices.Enumeration;

namespace BluetoothAudio
{
    /// <summary>
    /// Интерфейс для библиотеки подключения по A2DP
    /// </summary>
    public interface IBluetoothAudio
    {
        /// <summary>
        /// Начало поиска устройств
        /// </summary>
        public void Start();
        /// <summary>
        /// Подключение к устройству
        /// </summary>
        /// <param name="device">Устройство для подключения</param>
        public void Connect(DeviceInformation device);
        /// <summary>
        /// Отключение от устройства
        /// </summary>
        public void Disconnect();
        /// <summary>
        /// Получение всех найденных устройств
        /// </summary>
        /// <returns>Найденные устройства</returns>
        public IEnumerable<DeviceInformation> GetDevices();
        /// <summary>
        /// Найдено новое устройство
        /// </summary>
		public event Windows.Foundation.TypedEventHandler<DeviceInformation, object> Added;
        /// <summary>
        /// Устройство утеряно
        /// </summary>
		public event Windows.Foundation.TypedEventHandler<DeviceInformation, object> Removed;
        /// <summary>
        /// Закончено перечисление устройств
        /// </summary>
		public event Windows.Foundation.TypedEventHandler<DeviceWatcher, object> EnumerationCompleted;
        /// <summary>
        /// Изменение состояния подключения
        /// </summary>
		public event AudioConnectionEventHandler AudioConnectionStateChanged;
	}
}
