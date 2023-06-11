package com.example.btcontroller

data class SongMeta(val artist:String = "", val name:String = "", val album:String = ""){
	override fun toString(): String {
		return "artist=$artist;name=$name;album=$album"
	}
}
