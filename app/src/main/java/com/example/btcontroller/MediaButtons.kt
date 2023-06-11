package com.example.btcontroller

enum class MediaButtons(val byte: Byte) {
	PlayPause(85),
	Stop(86),
	VolumeUp(24),
	VolumeDown(25),
	Previous(88),
	Next(87);

	companion object{
		@JvmStatic
		fun fromByte(byte: Byte) = MediaButtons.values().firstOrNull{it.byte == byte}
	}
}