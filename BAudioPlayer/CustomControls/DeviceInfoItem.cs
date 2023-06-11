using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using Windows.Devices.Enumeration;

namespace BAudioPlayer.CustomControls
{
	/// <summary>
	/// Класс предмета выпадающего списка
	/// </summary>
	internal class DeviceInfoItem : ComboBoxItem
	{
		/// <summary>
		/// Конструктор класса
		/// </summary>
		public DeviceInfoItem() : base() { }
		// Устройтсво, хранимое в предмете
		private DeviceInformation _device;
		public DeviceInformation Device {
			get {return _device;}
			set {
				_device = value;
				Content = value?.Name??"";
			}
		}
	}
}
