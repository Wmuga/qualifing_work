using System;
using System.Collections.Generic;
using System.Text;
using Windows.Devices.Enumeration;
using Windows.Foundation;

namespace BluetoothPlaybackControl
{
	/// <summary>
	/// Ссылка на функцию обработчика события добавления устройства
	/// </summary>
	/// <param name="di">Добавленное устройство</param>
	/// <param name="sender">Отправитель</param>
    public delegate void DeviceAddedEventHandler(DeviceInformation di,object sender);
	/// <summary>
	/// Ссылка на функцию обработчика события потери устройства
	/// </summary>
	/// <param name="id">id устройства</param>
	/// <param name="sender">Отправвитель</param>
	public delegate void DeviceRemovedEventHandler(string id,object sender);
	/// <summary>
	/// Ссылка на функцию обработчика события изменения состояния подключения утсройства
	/// </summary>
	/// <param name="e">Данные события</param>
	/// <param name="sender">Отправвитель</param>
	public delegate void DeviceConnectionChangedEventHandler(DeviceConnectionEventArgs e,object sender);
	/// <summary>
	/// Ссылка на функцию обработчика события текущего уровня громкости
	/// </summary>
	/// <param name="volumeLevel">Уровень громкости в %</param>
	/// <param name="sender">Отправвитель</param>
	public delegate void VolumeInformationEventHandler(int volumeLevel, object sender);
	/// <summary>
	/// Ссылка на функцию обработчика события добавления устройства
	/// </summary>
	/// <param name="metadata"></param>
	/// <param name="sender">Отправвитель</param>
	public delegate void PlaybackInformationEventHandler(SongMeta metadata, object sender);
	/// <summary>
	/// Ссылка на функцию обработчика состояния проигрывания
	/// </summary>
	/// <param name="isPlaying">Проигрывается ли</param>
	/// <param name="sender">Отправвитель</param>
	public delegate void PlaybackStateChangedEventHandler(bool isPlaying, object sender);
	/// <summary>
	/// Данные события состояния подключения по BPC
	/// </summary>
	public class DeviceConnectionEventArgs : EventArgs
	{
		public DeviceConnectionEventArgs(bool connected, DeviceInformation device, string statusmsg)
		{
			Connected = connected;
			StatusMsg = statusmsg;
			Device = device;
		}
		// Подключен ли
		public bool Connected { get; set; }
		// Устройство
		public DeviceInformation Device { get; set; }
		// Сообщение, привязанное к статусу
		public string StatusMsg { get; set; }
	}
}
