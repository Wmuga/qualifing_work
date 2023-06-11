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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Devices.Enumeration;

namespace BAudioPlayer
{
    /// <summary>
    /// Логика взаимодействия для DeviceConnectionInfo.xaml
    /// </summary>
    public partial class DeviceConnectionInfo : UserControl
    {
		/// <summary>
		/// Конструктор класса
		/// </summary>
        public DeviceConnectionInfo()
        {
            InitializeComponent();
			DeviceLabel.Content = $"{BAudioPlayer.Resources.Resources.STR_DEVICE}: ";
		}
		// Устройство
		private DeviceInformation _device;
		public DeviceInformation Device
		{
			get { return _device; }
			set
			{
				_device = value;
				DeviceLabel.Content = $"{BAudioPlayer.Resources.Resources.STR_DEVICE}: {value?.Name??""}";
			}
		}
		// Статус подключения по A2DP
		public string A2DPStatus
		{
			set
			{
				App.RunAsDispatcher(() => A2DPLabel.Content = $"A2DP: {value}");
			}
		}
		// Статус подключения по BPC
		public string BPCStatus
		{
			set
			{
				App.RunAsDispatcher(() => BPCLabel.Content = $"BPC: {value}");
			}
		}
	}
}
