package com.example.btcontroller

class Shared {
	companion object {
		@JvmStatic
		lateinit var Log:(log:String)->Unit
		@JvmStatic
		val APP_UUID:String ="099C3D8F-58CE-4746-82C3-A975006885CB"
		@JvmStatic
		val ANDROID_CHANNEL_ID = "com.example.btcontorller.ANDROID"
		@JvmStatic
		val ANDROID_CHANNEL_NAME = "ANDROID CHANNEL"
		@JvmStatic
		val MAIN_ACTION = "com.example.BluetoothService.action.main"
		@JvmStatic
		val ONGOING_NOTIFICATION_ID = 100
	}
}