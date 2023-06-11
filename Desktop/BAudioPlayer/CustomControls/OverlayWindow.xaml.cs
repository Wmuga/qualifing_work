using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BAudioPlayer.CustomControls
{
	/// <summary>
	/// Логика взаимодействия для OverlayWindow.xaml
	/// </summary>
	public partial class OverlayWindow : Window
	{
		/// <summary>
		/// Конструктор класса
		/// </summary>
		public OverlayWindow()
		{
			InitializeComponent();
		}
		/// <summary>
		/// Изменение состояния окна. Прячет окно
		/// </summary>
		/// <param name="e">Данные собыимя</param>
		protected override void OnStateChanged(EventArgs e)
		{
			if (WindowState == WindowState.Minimized)
			{
				Hide();
				OnWindowHidden?.Invoke(this, null);
			}
        }
		/// <summary>
		/// Закрытие окна
		/// </summary>
		/// <param name="e">Данные собыимя</param>
		protected override void OnClosing(CancelEventArgs e)
		{
			// Если закрывается не основным окном, отменяем событие
			if (ForceClose)
			{
				base.OnClosing(e);
				return;
			}
			e.Cancel = true;
			WindowState = WindowState.Minimized;
		}
		// Закрывается ли основным окном
		public bool ForceClose = false;
		public event EventHandler<object> OnWindowHidden;
    }
}
