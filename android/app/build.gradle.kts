plugins { id("com.android.application"); id("org.jetbrains.kotlin.android") }

android { namespace = "com.mobiflux.agent"; compileSdk = 36
    defaultConfig { applicationId = "com.mobiflux.agent"; minSdk = 26; targetSdk = 36; versionCode = 1; versionName = "0.1.0" }
}
