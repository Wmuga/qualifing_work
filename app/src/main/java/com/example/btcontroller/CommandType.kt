package com.example.btcontroller

enum class CommandType(val byte:Byte) {
	PING(0),
	DispatchButton(1),
	SetVolume(2),
	VolumeLevel(3),
	SongMeta(4),
	SongState(5);

	companion object{
		@JvmStatic
		fun fromByte(byte: Byte) = values().firstOrNull{it.byte == byte}
	}
}