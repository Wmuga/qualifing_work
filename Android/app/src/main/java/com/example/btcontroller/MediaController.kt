package com.example.btcontroller

import android.content.Context
import android.media.AudioManager
import android.view.KeyEvent

class MediaController {
	companion object{
		@JvmStatic
		fun sendBtn(context:Context, keyCode:Byte){
			val keyCodeInt = keyCode.toInt()
			if (keyCodeInt == 24) return changeVolume(context,true)
			if (keyCodeInt == 25) return changeVolume(context,false)

			val am = context.getSystemService(Context.AUDIO_SERVICE) as AudioManager
			val downEvent = KeyEvent(KeyEvent.ACTION_DOWN, keyCodeInt)
			am.dispatchMediaKeyEvent(downEvent)
			val upEvent = KeyEvent(KeyEvent.ACTION_UP, keyCodeInt)
			am.dispatchMediaKeyEvent(upEvent)
		}
		@JvmStatic
		fun changeVolume(context:Context,up:Boolean){
			val am = context.getSystemService(Context.AUDIO_SERVICE) as AudioManager
			am.adjustVolume(if(up)  AudioManager.ADJUST_RAISE else AudioManager.ADJUST_LOWER,
				AudioManager.FLAG_SHOW_UI)
		}
		@JvmStatic
		fun setVolume(context:Context,volume:Int){
			if (volume > 100 || volume < 0) return
			val am = context.getSystemService(Context.AUDIO_SERVICE) as AudioManager
			val maxVolume = am.getStreamMaxVolume(AudioManager.STREAM_MUSIC)
			val minVolume = am.getStreamMinVolume(AudioManager.STREAM_MUSIC)
			val streamVolume = kotlin.math.min(volume*(maxVolume-minVolume)/100+minVolume,maxVolume)
			am.setStreamVolume(AudioManager.STREAM_MUSIC,streamVolume, AudioManager.FLAG_SHOW_UI)
		}
		@JvmStatic
		fun getVolume(context:Context):Int{
			val am = context.getSystemService(Context.AUDIO_SERVICE) as AudioManager
			val curVolume = am.getStreamVolume(AudioManager.STREAM_MUSIC)
			val maxVolume = am.getStreamMaxVolume(AudioManager.STREAM_MUSIC)
			return curVolume*100/maxVolume
		}
	}
}