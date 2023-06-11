using BluetoothAudio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Media.Audio;

namespace BAudioPlayer.Testing
{
	/// <summary>
	/// Mock бибилиотека BA для тестирования приложения
	/// </summary>
	public class BATesting : IBluetoothAudio, IAnnouncer 
	{
		public event TypedEventHandler<DeviceInformation, object> Added;
		public event TypedEventHandler<DeviceInformation, object> Removed;
		public event TypedEventHandler<DeviceWatcher, object> EnumerationCompleted;
		public event AudioConnectionEventHandler AudioConnectionStateChanged;
		public event AnnounceEventHandler Announce;


		public static readonly string lib = "BA";

		DeviceWatcher _dw;

		public BATesting() {
			_dw = DeviceInformation.CreateWatcher(AudioPlaybackConnection.GetDeviceSelector());

			_dw.Added += (sender, e) =>
			{
				this.Added?.Invoke(e, this);
			};
		}

		public void Connect(DeviceInformation device)
		{
			Announce?.Invoke(lib, $"{device.Name} Connect");
			AudioConnectionStateChanged?.Invoke(this, new AudioConnectionEventArgs(true, device, "Connected"));
		}

		public void Disconnect()
		{
			Announce?.Invoke(lib, "Disconnect");
		}

		public IEnumerable<DeviceInformation> GetDevices()
		{
			Announce?.Invoke(lib, "GetDevices");
			return Array.Empty<DeviceInformation>();
		}

		public void Start()
		{
			Announce?.Invoke(lib, "Start");
			_dw.Start();
		}
	}
}
