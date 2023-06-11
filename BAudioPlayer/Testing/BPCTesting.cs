using BluetoothAudio;
using BluetoothPlaybackControl;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace BAudioPlayer.Testing
{
	/// <summary>
	/// Mock библиотека для тестирования приложения
	/// </summary>
	public class BPCTesting : IBluetoothPlaybackControl, IAnnouncer
	{
		public event DeviceAddedEventHandler Added;
		public event DeviceRemovedEventHandler Removed;
		public event DeviceConnectionChangedEventHandler ConnectionChanged;
		public event PlaybackInformationEventHandler PlaybackInformationRecieved;
		public event PlaybackStateChangedEventHandler PlaybackStateChanged;
		public event VolumeInformationEventHandler VolumeInformation;
		public event AnnounceEventHandler Announce;
		public static readonly string lib = "BPC";
		public Task Connect(DeviceInformation device)
		{
			Announce?.Invoke(lib, $"{device.Name} Connect");
			ConnectionChanged?.Invoke(new DeviceConnectionEventArgs(true, device, "Connected"), this);
			VolumeInformation?.Invoke(20, this);
			PlaybackInformationRecieved?.Invoke(new SongMeta() { Artist="Test", Name="Testing"}, this);
			return Task.Delay(1);
		}

		public void Disconnect()
		{
			Announce?.Invoke(lib, "Disconnect");
		}

		public void SendNext()
		{
			Announce?.Invoke(lib, "SendNext");
		}

		public void SendPlayPause()
		{
			Announce?.Invoke(lib, "SendPlayPause");
		}

		public void SendPrev()
		{
			Announce?.Invoke(lib, "SendPrev");
		}

		public void SendStop()
		{
			Announce?.Invoke(lib, "SendStop");
		}

		public void SendVolume(byte volume)
		{
			Announce?.Invoke(lib, $"{volume}, SendVolume");
		}

		public void SendVolumeDown()
		{
			Announce?.Invoke(lib, "SendVolumeDown");
		}

		public void SendVolumeUp()
		{
			Announce?.Invoke(lib, "SendVolumeUp");
		}

		public void Start()
		{
			Announce?.Invoke(lib, "Start");
		}
	}
}
