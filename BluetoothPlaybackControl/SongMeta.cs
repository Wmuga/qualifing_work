using System;
using System.Collections.Generic;
using System.Text;

namespace BluetoothPlaybackControl
{
	/// <summary>
	/// Метаднные медиа
	/// </summary>
	public class SongMeta
	{
		// Исполнитель
		public string Artist { get; set; }
		// Название
		public string Name { get; set; }
		// Альбом
		public string Album { get; set; }
		/// <summary>
		/// Преобразование в строку
		/// </summary>
		/// <returns>Строка метаданных</returns>
		public override string ToString()
		{
			return $"{Artist}: {Name}.";
		}
	}
}
