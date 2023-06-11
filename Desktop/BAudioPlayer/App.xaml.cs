using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LightInject;
using BluetoothAudio;
using BluetoothPlaybackControl;
using BAudioPlayer.Testing;

namespace BAudioPlayer
{
    /// <summary>
    /// Логика для приложения
    /// </summary>
    public partial class App : Application
    {
        // Контейнер сервисов
        private static ServiceContainer serviceContainer = new ServiceContainer();
        // Тестируется ли приложение
        private static readonly bool isTesting = false;
		/// <summary>
		/// Точка входа в приложение
		/// </summary>
		/// <param name="sender">Объект отправитель</param>
		/// <param name="e">Данные события</param>
		void AppStart(object sender, EventArgs e)
        {
            // Регистрация реализаций интрефейсов
            if (!isTesting)
            {
                RegisterServices();
            }
            else
            {
                RegisterTestingServices();
                var tw = serviceContainer.GetInstance<TestingWindow>();
                tw.Show();
            }
            // Запуск главного окна
            var mw = serviceContainer.GetInstance<MainWindow>();
            mw.Show();
        }
        /// <summary>
        /// Регистрация сервисов
        /// </summary>
		private static void RegisterServices()
        {
            serviceContainer.Register<IBluetoothAudio, BluetoothAudio.BluetoothAudio>(new PerContainerLifetime());
            serviceContainer.Register<IBluetoothPlaybackControl, BluetoothPlaybackControl.BluetoothPlaybackControl>(new PerContainerLifetime());
            serviceContainer.Register<MainWindow>(new PerContainerLifetime());
		}
        /// <summary>
        /// Регистрация сервисов для тестирования графического интерфейса
        /// </summary>
        private static void RegisterTestingServices()
        {
			serviceContainer.Register<IBluetoothAudio, BATesting>(new PerContainerLifetime());
			serviceContainer.Register<IBluetoothPlaybackControl, BPCTesting>(new PerContainerLifetime());
			serviceContainer.Register<MainWindow>(new PerContainerLifetime());
			serviceContainer.Register<TestingWindow>(new PerContainerLifetime());
		}
        /// <summary>
        /// Исполнение кода в основном потоке приложения
        /// </summary>
        /// <param name="a">Код для исполнения</param>
		public static void RunAsDispatcher(Action a)
        {
            Application.Current.Dispatcher.BeginInvoke(a);
        }

	}

    
}
