package com.example.btcontroller

import android.Manifest
import android.app.*
import android.bluetooth.BluetoothManager
import android.bluetooth.BluetoothServerSocket
import android.bluetooth.BluetoothSocket
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Binder
import android.os.Build
import android.os.IBinder
import androidx.core.app.ActivityCompat
import java.io.DataInputStream
import java.util.*

// Севрис, обеспечивающий связь по BT
class BluetoothControlServer : Service(){
	private lateinit var _server: BluetoothServerSocket
	private lateinit var _connection: BluetoothSocket
	private var _connected = false
	private val _serverUUID:UUID = UUID.fromString(Shared.APP_UUID)
	private var _serverRunning = false

	inner class  BCSBinder : Binder() {
		fun getService() = this@BluetoothControlServer
	}

	// Запуск функций сервиса
	private fun setupServer(context: Context):Number{
		val manager = context.getSystemService(Context.BLUETOOTH_SERVICE) as BluetoothManager
		// Выбор необходимого разрешения для проверки
		val BTPerm =
			if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S)
				Manifest.permission.BLUETOOTH_CONNECT
			else
				Manifest.permission.BLUETOOTH
		// Проверка наличия разрешения на подключение
		if (ActivityCompat.checkSelfPermission(
				context,
				BTPerm
			) != PackageManager.PERMISSION_GRANTED
		) return 1
		// Проверка на работу BT менеджера
		if (manager.adapter == null) return 2
		if (!manager.adapter.isEnabled) return 3
		// Запуск прослушки на RFCOMM
		_server = manager.adapter.listenUsingRfcommWithServiceRecord("BTController", _serverUUID)
		_serverRunning = true

		return 0
	}
	// Считывание пришедшей информации.
	private fun readLoop(stream:DataInputStream){
		val buffer = ByteArray(2048)
		try{
			while(true) {
        // считывание размера
				val size = stream.readInt()
				stream.read(buffer, 0, size);
				// size + type + content
				val type = CommandType.fromByte(buffer[0])
				val info = buffer[1]
				Shared.Log("Data: Type $type Add.Info $info")
				// Определение типа пакета
				when(type){
					CommandType.PING -> {
						sendPong()
					}
					CommandType.DispatchButton -> {
						if (MediaButtons.fromByte(buffer[1]) != null)
							MediaController.sendBtn(this,info)
					}
					CommandType.SetVolume -> {
						MediaController.setVolume(this,info.toInt())
					}
					else -> {}
				}
			}
		} catch (e:Exception){
			// Потеря соединения
			Shared.Log("Disconnected")
			_connected = false
		}
	}

	private fun awaitConnection(){
		// Цикл приема подключений
		// Если сервер запущен и никто не подключен
		while (_serverRunning) {
			if (!_connected) {
				try {
					// Ожидание подключения
					_connection = _server.accept()
					// Выбор разрешения на проверку в зависимости от версии
					val BTPerm =
						if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S)
							Manifest.permission.BLUETOOTH_CONNECT
						else
							Manifest.permission.BLUETOOTH
					// Проверка на наличие разрешения на подключение
					if (ActivityCompat.checkSelfPermission(this,BTPerm) == PackageManager.PERMISSION_GRANTED)
					{
						Shared.Log("Connection found")
						if (!_connection.isConnected) _connection.connect()
						_connected = true
						// Подключились. Отправляем уровень звука. Важно для настройки ползунка громкости
						writeToConnection(
							arrayOf<Byte>(CommandType.VolumeLevel.byte, MediaController.getVolume(this).toByte()))
						readLoop(DataInputStream(_connection.inputStream))
					}
				}
				// На случай неожиданных исключений
				catch (e: java.lang.Exception) {
					Shared.Log(e.message.toString())
				}
			}
		}
	}
	// Отправка метаданных музыки
	fun sendSongInfo(info:SongMeta){
		if (_connection.isConnected){
			Shared.Log("Meta: ${info.artist} - ${info.name}")
			val infoStr = info.toString()
			writeToConnection(arrayOf<Byte>(CommandType.SongMeta.byte,*infoStr.toByteArray().toTypedArray()))
		}
	}

	// Отправка состояние музыки (играет/ не играет)
	fun sendSongState(isPlaying:Boolean){
		if (_connection.isConnected){
			Shared.Log(if (isPlaying) "Playing" else "Stop")
			writeToConnection(arrayOf(CommandType.SongState.byte,(if (isPlaying) 1 else 0).toByte()))
		}
	}

	// Отправка PONG
	private fun sendPong(){
		if (_connection.isConnected){
			Shared.Log("Pong")
			writeToConnection(arrayOf(CommandType.PING.byte,1.toByte()))
		}
	}

	// Запись входных байт в поток
	private fun writeToConnection(bytes:Array<Byte>){
		// При наличии подключения записываем байты в выходной поток
		if(_connection.isConnected){
			var size = bytes.size;
			for(i in 3 downTo 0){
				_connection.outputStream.write((size shl i*8) % 256);
			}
			_connection.outputStream.write(bytes.toByteArray());
			_connection.outputStream.flush()
		}
	}

	// Создание канала уведомлений для foreground сервиса
	private fun setupNotifications(){
		val androidChannel = NotificationChannel(
			Shared.ANDROID_CHANNEL_ID,
			Shared.ANDROID_CHANNEL_NAME, NotificationManager.IMPORTANCE_DEFAULT)
		// Настройка уведомления, чтобы быть максимально ненавязчивым
		androidChannel.enableLights(false);
		androidChannel.enableVibration(false);
		androidChannel.lockscreenVisibility = Notification.VISIBILITY_PRIVATE;
		// регистрация в системе
		val manager = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
		manager.createNotificationChannel(androidChannel);
	}

	override fun onBind(p0: Intent?): IBinder =  BCSBinder()

	// функция запуска сервиса
	override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
		// Создание канала уведомлений
		setupNotifications()

		// создание Intent для уведомления
		val notificationIntent = Intent(this, MainActivity::class.java)
		notificationIntent.action = Shared.MAIN_ACTION
		notificationIntent.flags = (Intent.FLAG_ACTIVITY_NEW_TASK
				or Intent.FLAG_ACTIVITY_CLEAR_TASK)
		val pendingIntent = PendingIntent.getActivity(
			this, 0,
			notificationIntent, 0
		)
		// Сброка уведомления
		val notification: Notification = Notification.Builder(this, Shared.ANDROID_CHANNEL_ID)
			.setContentTitle("Bluetooth Controller")
			.setContentText("Bluetooth Controller is running")
			.setSmallIcon(android.R.drawable.stat_notify_more)
			.setContentIntent(pendingIntent)
			.setOngoing(true)
			.build()
		// Запуск сервиса переднего плана
		startForeground(Shared.ONGOING_NOTIFICATION_ID, notification)
		// Запуск сервера при прохождении проверки
		// Код между startForeground и return выполняется в контексте сервиса
		when(setupServer(this)){
			1 -> Shared.Log("No permissions for BT server")
			2 -> Shared.Log("Can't grab adapter")
			3 -> Shared.Log("Bluetooth is off")
			0 -> {
				Shared.Log("Server running")
				Thread(Runnable { awaitConnection() }).start()
			}
		}
		// Сервис должен быть пересоздан при освобождении памяти
		return START_STICKY
	}
}