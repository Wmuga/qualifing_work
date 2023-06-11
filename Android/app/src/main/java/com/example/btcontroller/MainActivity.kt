package com.example.btcontroller

import android.Manifest
import android.app.Notification
import android.app.NotificationManager
import android.content.*
import android.content.pm.PackageManager
import android.media.session.MediaSessionManager
import android.net.Uri
import android.os.Build
import android.os.Build.VERSION_CODES
import android.os.Bundle
import android.os.PowerManager
import android.provider.Settings
import android.widget.Button
import android.widget.TextView
import android.widget.Toast
import androidx.annotation.RequiresApi
import androidx.appcompat.app.AppCompatActivity
import androidx.core.app.ActivityCompat
import androidx.core.content.ContextCompat
import androidx.core.content.getSystemService


class MainActivity : AppCompatActivity() {
	// id для запроса разрешения Bluetooth
	private val _rqBTCON = 2
	// ВКлючены ли сервисы
	private var _mediaEnabled = false
	private var _btEnabled = false
	// Выводит информацию на экран
	private fun Log(log:String){
		runOnUiThread(Runnable {
			val logV = findViewById<TextView>(R.id.logView)
			logV.text = log
		})
	}
	// действия при создании
	override fun onCreate(savedInstanceState: Bundle?) {
		super.onCreate(savedInstanceState)
		setContentView(R.layout.activity_main)
		title="BTController"

		Shared.Log = ::Log
		// Проверка на наличие Bluetooth
		if (!packageManager.hasSystemFeature(PackageManager.FEATURE_BLUETOOTH)){
			Toast.makeText(this@MainActivity, "Can't use app on device without bluetooth", Toast.LENGTH_LONG).show()
			return
		}

		setupButtons()
	}

	// Запуск сервисов
	private fun startApp(){
		if (!checkBatteryOptimisation()) return
		if (!checkNotificationPermission()) return

		if (!_mediaEnabled){
			startService(Intent(this, MediaInfo::class.java))
			_mediaEnabled = true
		}
		if (!_btEnabled) {
			startServer()
		}
	}

	// Привязка к кнопкамдействий при нажатии
	private fun setupButtons(){
		val btnCodes = arrayOf<Byte>(MediaButtons.PlayPause.byte,MediaButtons.Stop.byte,MediaButtons.VolumeUp.byte
			,MediaButtons.VolumeDown.byte,MediaButtons.Previous.byte,MediaButtons.Next.byte)
		val ids = arrayOf(R.id.PlayPauseBtn,R.id.StopBtn,R.id.VupBtn,R.id.VdownBtn,R.id.PrevBtn,R.id.NextBtn)
		// Действия к медиа кнопкам
		for(i in ids.indices){
			findViewById<Button>(ids[i]).setOnClickListener{
				MediaController.sendBtn(this, btnCodes[i])
			}
		}
		// Кнопка включения сервисов
		findViewById<Button>(R.id.EnableBtn).setOnClickListener{
			startApp()
		}
	}

	// Запуск BT сервиса
	private fun startServer(){
		try {
			// Проверка на наличие разрешений
			val BTPerm =
				if (Build.VERSION.SDK_INT >= VERSION_CODES.S)
					Manifest.permission.BLUETOOTH_CONNECT
				else
					Manifest.permission.BLUETOOTH
			if (ContextCompat.checkSelfPermission(this@MainActivity, BTPerm) != PackageManager.PERMISSION_GRANTED) {
				ActivityCompat.requestPermissions(this@MainActivity, arrayOf(BTPerm), _rqBTCON)
			}
			else{
				startForegroundService(Intent(this, BluetoothControlServer::class.java))
				_btEnabled = true
			}
		}
		catch (e:java.lang.Exception) {
			Toast.makeText(this, e.message, Toast.LENGTH_SHORT).show()
		}
	}
	// Прием результатов запроса разрешения
	override fun onRequestPermissionsResult(
		requestCode: Int,
		permissions: Array<out String>,
		grantResults: IntArray
	)
	{
		super.onRequestPermissionsResult(requestCode, permissions, grantResults)
		when(requestCode){
			_rqBTCON -> {
				if (grantResults.isNotEmpty() && grantResults[0] == PackageManager.PERMISSION_GRANTED){
					startForegroundService(Intent(this, BluetoothControlServer::class.java))
					_btEnabled = true
				} else{
					Toast.makeText(this, "Can't create BT server without permissions",Toast.LENGTH_SHORT).show()
				}
			}
			else -> {
				// Nothing
			}
		}
	}

	override fun onDestroy() {
		stopService(Intent(this, MediaInfo::class.java ))
		stopService(Intent(this, BluetoothControlServer::class.java ))
		super.onDestroy()
	}


	private fun checkBatteryOptimisation():Boolean{
		val intent = Intent()
		val pm : PowerManager = getSystemService(Context.POWER_SERVICE) as PowerManager
		if (!pm.isIgnoringBatteryOptimizations(packageName)) {
			Toast.makeText(this,"Disable battery optimization for working in background",Toast.LENGTH_SHORT).show()
			intent.action = Settings.ACTION_IGNORE_BATTERY_OPTIMIZATION_SETTINGS
			startActivity(intent)
			return false
		}
		return true
	}

	private fun checkNotificationPermission():Boolean{
		val intent = Intent()
		val m: NotificationManager = getSystemService<NotificationManager>()!!
		val component = ComponentName(this, NotiService::class.java)
		if (!m.isNotificationListenerAccessGranted(component)) {
			Toast.makeText(this,"Enable notification listener to grant access to media metadata",Toast.LENGTH_SHORT).show()
			intent.action = Settings.ACTION_NOTIFICATION_LISTENER_SETTINGS
			startActivity(intent)
			return false
		}
		return true
	}

}