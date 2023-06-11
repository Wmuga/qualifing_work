using System;
using System.Collections.Generic;
using System.Windows;
using Windows.Devices.Enumeration;
using Hardcodet.Wpf.TaskbarNotification;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using BAudioPlayer.CustomControls;
using BluetoothPlaybackControl;
using BluetoothAudio;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.IO;

using ComboBoxItem = System.Windows.Controls.ComboBoxItem;
using Resource = BAudioPlayer.Resources.Resources;
using Pipe;

namespace BAudioPlayer
{
    /// <summary>
    /// Логика для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
		// Показано ли дополнительное окно 
		private bool _owShown = false;
		// Нажата ли ЛКМ на ползунке громкости
		private bool _vbMouseDown = false;
		// BPC
		private readonly IBluetoothPlaybackControl _bpc;
		// A2DP
		private readonly IBluetoothAudio _ba;
		// Иконка в трее
		private TaskbarIcon _tbi;
		// Текущие метаданные музыки
		private SongMeta _meta = new SongMeta();
		// Элементы списка выбора устройства
		private readonly List<ComboBoxItem> _items = new List<ComboBoxItem>() { new ComboBoxItem() { Content = Resource.STR_SELECT_DEVICE, IsEnabled = false} };
		// Дополнительное окно
		private OverlayWindow _ow;
		// IPC
		private PipeServer _pipeServer;
		// Подключен ли IPC
		private bool _ipcConnected = false;

		/// <summary>
		/// Конструктор класса
		/// </summary>
		/// <param name="ba">Класс для подключения по A2DP</param>
		/// <param name="bpc">Класс для подключения по BPC</param>
		public MainWindow(IBluetoothAudio ba, IBluetoothPlaybackControl bpc)
		{
			// Стандартная функция инициализации компонентов
			InitializeComponent();

			try
			{
				_ba = ba;
				_bpc = bpc;
				// Собтвенные функции инициализации компонентов окна
				SetupConnectionBox();
				SetupOverlayWindow();
				SetupText();
				SetupButtonsBackground();
				SetupButtonsClicks();
				SetupIcon();
				SetupEvents();
				SetupPipe();
				// Запуск поиска доступных устройств
				_ba.Start();
			}
			// При непридвиденных обстоятельствах отображения сообщения
			catch(Exception e)
			{
				TrackInfoBlock.Text = e.Message;
			}
		}
		/// <summary>
		/// Инициализация окна выбора подключений
		/// </summary>
		private void SetupConnectionBox()
		{
			ConnectionComboBox.ItemsSource = _items;
			ConnectionComboBox.SelectedIndex = 0;
		}
		/// <summary>
		/// Инициализация фонов для кнопок
		/// </summary>
		private void SetupButtonsBackground()
		{
			// TODO: Change to SVG
			PrevButton.Background = new ImageBrush
			{
				ImageSource = ConvertToBitmapSource(Resource.Prev_DARK)
			};
			NextButton.Background = new ImageBrush
			{
				ImageSource = ConvertToBitmapSource(Resource.Next_DARK)
			};
			PlayPauseButton.Background = new ImageBrush
			{
				ImageSource = ConvertToBitmapSource(Resource.PlayPause_DARK)
			};
			StopButton.Background = new ImageBrush
			{
				ImageSource = ConvertToBitmapSource(Resource.Stop_DARK)
			};

			_ow.PrevButton.Background = new ImageBrush
			{
				ImageSource = ConvertToBitmapSource(Resource.Prev_DARK)
			};
			_ow.NextButton.Background = new ImageBrush
			{
				ImageSource = ConvertToBitmapSource(Resource.Next_DARK)
			};
			_ow.PlayPauseButton.Background = new ImageBrush
			{
				ImageSource = ConvertToBitmapSource(Resource.PlayPause_DARK)
			};
			_ow.StopButton.Background = new ImageBrush
			{
				ImageSource = ConvertToBitmapSource(Resource.Stop_DARK)
			};
		}
		/// <summary>
		/// Привязка текста из ресурсов к элементам
		/// </summary>
		private void SetupText()
		{
			ConnectButton.Content = Resource.STR_CONNECT_BTN;
			ConnectionsLabel.Content = Resource.STR_CONNECTIONS;
			VolumeLabel.Content = Resource.STR_VOLUME;
			TrackInfoBlock.Text = string.Empty;

			LightThemeRadio.Content = Resource.STR_LIGHT_THEME;
			DarkThemeRadio.Content  =  Resource.STR_DARK_THEME;
			AutoconnectCheckBox.Content = Resource.STR_TRY_RECONNECT;
		}
		/// <summary>
		/// Создает дополнительное окно
		/// </summary>
		private void SetupOverlayWindow()
		{
			_ow = new OverlayWindow();
			// При прятании окна обновляем переменную
			_ow.OnWindowHidden += (_, e) => _owShown = false;
			_ow.Hide();
			// Перетаскиваение окна в правый нижний угол
			double screen_width = SystemParameters.PrimaryScreenWidth;
			double screen_heigth = SystemParameters.PrimaryScreenHeight;

			double window_width = _ow.Width;
			double window_height = _ow.Height;

			_ow.Left = screen_width-window_width-5;
			_ow.Top = screen_heigth-window_height-40;

		}

		/// <summary>
		/// Привязывает события библиотек к их обработчикам
		/// </summary>
		private void SetupEvents()
		{
			// Изменение состояния подключения A2DP
			_ba.AudioConnectionStateChanged += (_, e) =>
			{
				CurrentConnectionController.A2DPStatus = e.StatusMsg;
			};
			// Изменение состояния подключения BPC
			_bpc.ConnectionChanged += (e, _) =>
			{
				CurrentConnectionController.BPCStatus = e.StatusMsg;
			};
			// Информация о текущем уровне громкости
			_bpc.VolumeInformation += (e, _) =>
			{
				VolumeSlider.Value = e;
				_ow.VolumeSlider.Value = VolumeSlider.Value;
			};
			// Состояние проигрывания
			_bpc.PlaybackInformationRecieved += (e, _) =>
			{
				App.RunAsDispatcher(() =>
				{
					_meta = e;
					SendMetaToPipe(e);
					ShowMeta();
				});
			};
			// Изменение состояния проигрывания
			_bpc.PlaybackStateChanged += (e, _) =>
			{
				App.RunAsDispatcher(() =>
				{
					ClearMeta();
					if (e) ShowMeta();
					SendMetaToPipe(e ? _meta : new SongMeta());
				});
			};
			// Найдено устройство
			_ba.Added += (di, sender) =>
			{
				// Добавить к текущему списку
				AddItem(di);
				// Если совпало с ранее подключенным - выбрать сразу его
				if (di.Name == Properties.Settings.Default.LastDevice)
				{
					App.RunAsDispatcher(() =>
					{
						ConnectionComboBox.SelectedIndex = ConnectionComboBox.Items.Count - 1;
						// Если автоподключение - сразу подключиться
						if (Properties.Settings.Default.Autoconnect)
							Connect();
					});
				}
			};
			// Изменен выбранный элемент
			ConnectionComboBox.SelectionChanged += (_, e) =>
			{
				if (!(ConnectionComboBox.SelectedItem is DeviceInfoItem item)) return;
				ConnectButton.Content = item.Device == CurrentConnectionController.Device 
				? Resource.STR_DISCONNECT_BTN 
				: Resource.STR_CONNECT_BTN;
			};
		}
		/// <summary>
		/// Отображение метаданных иузыки
		/// </summary>
		private void ShowMeta()
		{
			TrackInfoBlock.Text = _meta.ToString();
			_ow.TrackInfoBlock.Text = _meta.ToString();
		}
		/// <summary>
		/// Очистить поле с метаданных
		/// </summary>
		private void ClearMeta()
		{
			TrackInfoBlock.Text = string.Empty;
			_ow.TrackInfoBlock.Text = string.Empty;
		}
		/// <summary>
		/// Привязка действий к нажатию на кнопку
		/// </summary>
		private void SetupButtonsClicks()
		{
			PlayPauseButton.Click += (_, e) => _bpc.SendPlayPause();
			_ow.PlayPauseButton.Click += (_, e) => _bpc.SendPlayPause();

			StopButton.Click += (_, e) => _bpc.SendStop();
			_ow.StopButton.Click += (_, e) => _bpc.SendStop();

			PrevButton.Click += (_, e) => _bpc.SendPrev();
			_ow.PrevButton.Click += (_, e) => _bpc.SendPrev();

			NextButton.Click += (_, e) => _bpc.SendNext();
			_ow.NextButton.Click += (_, e) => _bpc.SendNext();


			VolumeSlider.PreviewMouseDown += (_, e) => 
				_vbMouseDown = true;
			_ow.VolumeSlider.PreviewMouseDown += (_, e) => 
				_vbMouseDown = true;

			VolumeSlider.PreviewMouseUp += (_, e) =>
			{
				if (_vbMouseDown)
				{
					_vbMouseDown = false;
					_bpc.SendVolume((byte)VolumeSlider.Value);
					_ow.VolumeSlider.Value = VolumeSlider.Value;
				}
			};

			_ow.VolumeSlider.PreviewMouseUp += (_, e) =>
			{
				if (_vbMouseDown)
				{
					_vbMouseDown = false;
					_bpc.SendVolume((byte)VolumeSlider.Value);
					VolumeSlider.Value = _ow.VolumeSlider.Value;
				}
			};
		
			ConnectButton.Click += (_, e) => Connect();

		}
		/// <summary>
		/// Добавление элемента к выпадающему списку подключений
		/// </summary>
		/// <param name="di">Информация об устройстве</param>
		private void AddItem(DeviceInformation di)
		{
			App.RunAsDispatcher(new Action(() =>
			{
				var con = new DeviceInfoItem() { Device = di };
				_items.Add(con);
			}));
		}
		/// <summary>
		/// Подключение к устройству с заданными параметрами
		/// </summary>
		private void Connect()
		{
			// Ничего не выбрано
			if (!(ConnectionComboBox.SelectedItem is DeviceInfoItem item)) return;
			// Не выбраны параметры
			if (!(ConnectBPCBox.IsChecked ??false) && !(ConnectA2DPBox.IsChecked ?? false)) return;
			// Запись последенего подключенного устройства
			Properties.Settings.Default.LastDevice = item.Device.Name;
			// Отключение от текущего устройства
			if (item.Device == CurrentConnectionController.Device)
			{
				Disconnect();
				return;
			}

			Disconnect();
			//Подключение к ноаому устройству
			ConnectButton.Content = Resource.STR_DISCONNECT_BTN;
			CurrentConnectionController.Device = item.Device;
			if (ConnectBPCBox.IsChecked  ?? false) _bpc.Connect(item.Device);
			if (ConnectA2DPBox.IsChecked ?? false) _ba.Connect(item.Device);
		}
		/// <summary>
		/// Отключение от устройства
		/// </summary>
		private void Disconnect()
		{
			ConnectButton.Content = Resource.STR_CONNECT_BTN;
			CurrentConnectionController.A2DPStatus = "";
			CurrentConnectionController.BPCStatus = "";
			CurrentConnectionController.Device = null;
			_bpc.Disconnect();
			_ba.Disconnect();
		}
		/// <summary>
		/// Инициплизации иконки в системном трее
		/// </summary>
		private void SetupIcon()
		{
			// Иконка
			_tbi = new TaskbarIcon
			{
				Icon = Resource.TaskbarIcon,
				Visibility = Visibility.Collapsed
			};
			// Клики
			_tbi.TrayLeftMouseDown += Tbi_TrayLeftMouseDown;
			_tbi.TrayMouseDoubleClick += Tbi_TrayMouseDoubleClick;
		}
		/// <summary>
		/// Двойной клик на иконку в трее
		/// </summary>
		/// <param name="sender">Объект отправитель</param>
		/// <param name="e">Данные события</param>
		private void Tbi_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
		{
			// Прячет дополнительное окно
			if (_owShown)
			{
				_ow.Hide();
				_owShown = false;
			}
			// Отображает основное окно
			Show();
			WindowState = WindowState.Normal;
			_tbi.Visibility = Visibility.Collapsed;
		}
		/// <summary>
		/// Одинарный клик на иконку в трее
		/// </summary>
		/// <param name="sender">Объект отправитель</param>
		/// <param name="e">Данные события</param>
		private void Tbi_TrayLeftMouseDown(object sender, RoutedEventArgs e)
		{
			// Прячет дополнительное окно, если было отображено
			if (_owShown)
			{
				_ow.WindowState= WindowState.Minimized;
				_ow.Hide();
				_owShown = false;
				return;
			}
			// Иначе выводит его
			_owShown = true;
			_ow.Show();
			_ow.WindowState= WindowState.Normal;
		}
		/// <summary>
		/// Прячет окно вместо сворачивает
		/// </summary>
		/// <param name="e">Данные события</param>
		protected override void OnStateChanged(EventArgs e)
		{
			if (WindowState == WindowState.Minimized)
			{
				this.Hide();
				_tbi.Visibility = Visibility.Visible;
			}
		}
		/// <summary>
		/// Преобрахование ресурса в источник картинки
		/// </summary>
		/// <param name="gdiPlusBitmap">Картинка из ресурсов</param>
		/// <returns>Источник картинки</returns>
		public static BitmapSource ConvertToBitmapSource(Bitmap gdiPlusBitmap)
		{
			IntPtr hBitmap = gdiPlusBitmap.GetHbitmap();
			return Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

		}
		/// <summary>
		/// Событие закрытия окна
		/// </summary>
		/// <param name="sender">Объект отправитель</param>
		/// <param name="e">Данные события</param>
		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			// Закрывает доп окно
			_ow.ForceClose = true;
			_ow.Close();
			Disconnect();
			// Сохраняет настройки
			Properties.Settings.Default.Save();
		}

		/// <summary>
		/// Создает канал для IPC
		/// </summary>
		private void SetupPipe()
		{
			Debug("Pipe: Waiting");
			_pipeServer = new PipeServer("BAP_IPC");
			_pipeServer.Connect += (_, e) => { 
				Debug("Pipe: Connect"); 
				_ipcConnected = true;
			};
			_pipeServer.Disconnect += (_, e) => {
				Debug("Pipe: Disconnected");
				_ipcConnected = false;
			};
			_pipeServer.MessageReceived += (_,msg) => {
				if (msg == null) return;
				Debug("Pipe: Message: {0}", msg.Message);
				ProccessCommand(msg.Message);
			};
			_pipeServer.Open();
		}
		/// <summary>
		/// Обрабатывает приходящую команду
		/// </summary>
		/// <param name="commandStr">команда</param>
		private void ProccessCommand(string commandStr)
		{
			var commandAr = commandStr.Trim().Split(" ");
			if (commandAr.Length == 0) return;
			switch (commandAr[0])
			{
				case "PlayPause":
					_bpc.SendPlayPause();
					break;
				case "Next":
					_bpc.SendNext();
					break;
				case "Volume":
					{
						if (commandAr.Length < 1 || !double.TryParse(commandAr[1], out double number))
						{
							return;
						}
						if (0 < number && number < 1) return;
						_bpc.SendVolume((byte)(number * 100));
						break;
					}
				default:
					break;
			}
		}

		/// <summary>
		/// Отправка метаданных по IPC
		/// </summary>
		/// <param name="meta">Отправляемые метаданные</param>
		private void SendMetaToPipe(SongMeta meta)
		{
			if (_ipcConnected)
			{
				var metaStr = meta.ToPipeData();
				Debug("Pipe out: {0}", metaStr);
				_pipeServer.SendMessage(metaStr);
			}
		}
		/// <summary>
		/// Выводит сообщение в Debug панель
		/// </summary>
		/// <param name="format">Формат сообщения</param>
		/// <param name="args">Аргументы форматирования</param>
		private void Debug(string format,  params object[] args)
		{
			App.RunAsDispatcher(() =>
			{
				DebugBlock.Text += string.Format(format, args) + "\r\n";
			});
		}
	}


	public static class Extensions
	{
		public static string ToPipeData(this SongMeta meta)
		{
			return string.Format("{0};;{1};;{2}",meta.Artist,meta.Name,meta.Album);
		}
	}
}
