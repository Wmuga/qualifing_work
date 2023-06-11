package com.example.btcontroller

import android.app.Service
import android.content.*
import android.media.AudioManager
import android.os.IBinder
import java.util.*


class MediaInfo : Service() {
    private var _btControl: BluetoothControlServer? = null
    private var _isPlaying = false
    private lateinit var _timer: Timer

    inner class SerCon : ServiceConnection {
        override fun onServiceConnected(classname: ComponentName?, binder: IBinder?) {
            _btControl = (binder as BluetoothControlServer.BCSBinder).getService()
        }

        override fun onServiceDisconnected(p0: ComponentName?) {
            _btControl = null
        }
    }

    private val _servcon: SerCon = SerCon()

    private val mReceiver: BroadcastReceiver = object : BroadcastReceiver() {
        override fun onReceive(context: Context, intent: Intent) {
            val action = intent.action

            //val cmd = intent.getStringExtra("command")

            val artist = intent.getStringExtra("artist").toString()
            val album = intent.getStringExtra("album").toString()
            val track = intent.getStringExtra("track").toString()

            _btControl?.sendSongInfo(SongMeta(artist, track, album))
            _isPlaying = true
            Shared.Log("$track $action")
        }
    }

    override fun onCreate() {
        bindService(Intent(this, BluetoothControlServer::class.java), _servcon, BIND_AUTO_CREATE)
        val iF = IntentFilter()

        iF.addAction("com.android.music.metachanged");
        iF.addAction("com.htc.music.metachanged");
        iF.addAction("fm.last.android.metachanged");
        iF.addAction("com.sec.android.app.music.metachanged");
        iF.addAction("com.nullsoft.winamp.metachanged");
        iF.addAction("com.amazon.mp3.metachanged");
        iF.addAction("com.miui.player.metachanged");
        iF.addAction("com.real.IMP.metachanged");
        iF.addAction("com.sonyericsson.music.metachanged");
        iF.addAction("com.rdio.android.metachanged");
        iF.addAction("com.samsung.sec.android.MusicPlayer.metachanged");
        iF.addAction("com.andrew.apollo.metachanged");

        // set timer to tell if song ended
        _timer = Timer()

        val checkIsPlaying: TimerTask = object : TimerTask() {
            override fun run() {
                checkMusicIsPlaying()
            }
        }
        _timer.schedule(checkIsPlaying, 0, 1000)

        registerReceiver(mReceiver, iF)
    }

    private fun checkMusicIsPlaying() {
        val am = getSystemService(Context.AUDIO_SERVICE) as AudioManager
        if (!am.isMusicActive && _isPlaying) {
            _btControl?.sendSongState(false)
            _isPlaying = false
            Shared.Log("Track stopped")
            return
        }

        if (am.isMusicActive && !_isPlaying) {
            _btControl?.sendSongState(true)
            _isPlaying = true
            Shared.Log("Track is playing")
        }
    }

    override fun onBind(p0: Intent?): IBinder? {
        return null
    }
}
