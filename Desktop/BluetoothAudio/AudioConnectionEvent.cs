using System;
using Windows.Devices.Enumeration;

namespace BluetoothAudio
{
    /// <summary>
    /// Тип функции обработчика события изменения соотояния подключения
    /// </summary>
    /// <param name="sender">Объект отправитель</param>
    /// <param name="e">Данные события</param>
    public delegate void AudioConnectionEventHandler(object sender, AudioConnectionEventArgs e);
    /// <summary>
    /// Событие изменение состояния подключения
    /// </summary>
    public class AudioConnectionEventArgs : EventArgs
    {

        public AudioConnectionEventArgs(bool connected, DeviceInformation device, string statusmsg)
        {
            Connected = connected;
            StatusMsg = statusmsg;
            Device = device;
        }
        // Подключено ли устройстов
        public bool Connected { get; set; }
        // Какое
        public DeviceInformation Device { get; set; }
        // Сообщение, привязанное к статусу подключения
        public string StatusMsg { get; set; }
    }
}
