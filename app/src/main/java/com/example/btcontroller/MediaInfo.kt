package com.example.btcontroller

import android.app.Service
import android.content.*
import android.media.MediaMetadata
import android.media.session.MediaSessionManager
import android.media.session.PlaybackState
import android.os.IBinder
import androidx.core.content.getSystemService
import java.util.*

// Сервис получения информации о текущей музыки
class MediaInfo : Service() {
	private var _btControl: BluetoothControlServer? = null
	private lateinit var _timer: Timer
	private var _isPlaying = false
	private var curMeta:SongMeta = SongMeta()

	inner class SerCon : ServiceConnection {
		override fun onServiceConnected(classname: ComponentName?, binder: IBinder?) {
			_btControl = (binder as BluetoothControlServer.BCSBinder).getService()
		}

		override fun onServiceDisconnected(p0: ComponentName?) {
			_btControl = null
		}
	}

	private val _servcon: SerCon = SerCon()

	override fun onCreate() {
		// Привязка BluetoothControlService
		bindService(Intent(this, BluetoothControlServer::class.java), _servcon, BIND_AUTO_CREATE)

		// По таймеру проверка что играет
		_timer = Timer()
		// формирование функции для таймера
		val checkIsPlaying: TimerTask = object : TimerTask() {
			override fun run() {
				checkMusicIsPlaying()
			}
		}
		// запуск таймера
		_timer.schedule(checkIsPlaying, 0, 1000)

	}
	// Функция проверки проигрывается ли музыка. Уведомляет о состоянии проигрывания медиа.
	// Если проигрывается - предоставляет ее метаданные.
	private fun checkMusicIsPlaying() {
		// Получение активных медиа сессий
		val m = getSystemService<MediaSessionManager>()!!
		val component = ComponentName(this, NotiService::class.java)
		val sessions = m.getActiveSessions(component)
		// По активным медиа - сессиям
		sessions.forEach {
			// Если что-то играет, то получаем что
			if (it.playbackState != null && it.playbackState?.state == PlaybackState.STATE_PLAYING) {
				val newMeta = SongMeta(
					it?.metadata?.getString(MediaMetadata.METADATA_KEY_ARTIST)?:"",
					it?.metadata?.getString(MediaMetadata.METADATA_KEY_TITLE)?:"",
					it?.metadata?.getString(MediaMetadata.METADATA_KEY_ALBUM)?:"")
				// Если раньше не играло - говорим, играет
				if (!_isPlaying){
					_btControl?.sendSongState(true)
					_isPlaying = true
				}
				// Если играло то же самое - не отправляем
				if (newMeta == curMeta) return
				curMeta = newMeta

				_btControl?.sendSongInfo(newMeta)
				return
			}
		}
		// Если раньше играло, а сейчас перестало - сообщаем
		if(_isPlaying){
			_btControl?.sendSongState(false)
			_isPlaying = false
			curMeta = SongMeta()
		}
	}

	override fun onBind(p0: Intent?): IBinder? {
		return null
	}
}
