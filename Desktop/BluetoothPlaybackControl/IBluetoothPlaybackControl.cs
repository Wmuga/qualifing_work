using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace BluetoothPlaybackControl
{
	/// <summary>
	/// Интерфейс для библиотеки подключения по BPC
	/// </summary>
	public interface IBluetoothPlaybackControl
	{
		/// <summary>
		/// Начало поиска устройств
		/// </summary>
		public void Start();
		/// <summary>
		/// Подключение к устройству по BPC
		/// </summary>
		/// <param name="device">Устройство для подключения</param>
		/// <returns>Объект ассинхронного события</returns>
		public Task Connect(DeviceInformation device);
		/// <summary>
		/// Отправить кнопку воспроизвести приостановить
		/// </summary>
		public void SendPlayPause();
		/// <summary>
		/// Отправить кнопку остановить
		/// </summary>
		public void SendStop();
		/// <summary>
		/// Отправить кнопку предыдущий
		/// </summary>
		public void SendPrev();
		/// <summary>
		/// Отправить кнопку следующий
		/// </summary>
		public void SendNext();
		/// <summary>
		/// Отправить кнопку громкость вверх
		/// </summary>
		public void SendVolumeUp();
		/// <summary>
		/// Отправить кнопку громкость вниз
		/// </summary>
		public void SendVolumeDown();
		/// <summary>
		/// Отправить уровень громкости
		/// </summary>
		/// <param name="volume">Уровень громкости в %</param>
		public void SendVolume(byte volume);
		/// <summary>
		/// Отключиться от устройства
		/// </summary>
		public void Disconnect();
		/// <summary>
		/// Добавлено устройство
		/// </summary>
		public event DeviceAddedEventHandler Added;
		/// <summary>
		/// Утсройство утеряно
		/// </summary>
		public event DeviceRemovedEventHandler Removed;
		/// <summary>
		/// Изменено состояние подключения
		/// </summary>
		public event DeviceConnectionChangedEventHandler ConnectionChanged;
		/// <summary>
		/// Получена информация о метаданных
		/// </summary>
		public event PlaybackInformationEventHandler PlaybackInformationRecieved;
		/// <summary>
		/// Получена инофрмация о состоянии проигрывании
		/// </summary>
		public event PlaybackStateChangedEventHandler PlaybackStateChanged;
		/// <summary>
		/// Получена информация о текущем уровне громкости
		/// </summary>
		public event VolumeInformationEventHandler VolumeInformation;
	}
}
