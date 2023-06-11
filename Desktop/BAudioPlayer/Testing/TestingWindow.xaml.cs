using BluetoothAudio;
using BluetoothPlaybackControl;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Windows.Devices.Enumeration;

namespace BAudioPlayer.Testing
{
	/// <summary>
	/// Логика взаимодействия для TestingWindow.xaml
	/// </summary>
	public partial class TestingWindow : Window
	{
		private BATesting _ba;
		private BPCTesting _bpc;
		public TestingWindow(IBluetoothAudio ba, IBluetoothPlaybackControl bpc)
		{
			if (!(ba is BATesting && bpc is BPCTesting))
			{
				throw new ArgumentException();
			}

			InitializeComponent();

			_ba = ba as BATesting;
			_bpc = bpc as BPCTesting;

			_ba.Announce += AddMethodText;
			_bpc.Announce += AddMethodText;
		}

		private void AddMethodText(string lib, string method)
		{
			Dispatcher.Invoke(() =>
			{
				DebugTextBlock.Text += string.Format("{0}: {1}()\n", lib, method);
			});
		}
	}

	public static class Ext
	{
	}
}
