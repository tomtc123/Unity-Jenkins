# Unity-Jenkins
Unity3d with Jenkins

## iOS导出设置
**使用方法：**
点击 `iOS/BuildProject` 菜单导出XCode工程。

////
iOSBuildOptions.json注释说明:
```json
{
    "DisplayName":"测试名称",//游戏名称
    "BundleIdentifier":"com.game.test",//包名
    "Version":"0.0.1",//版本
    "AppIconSetPath":"SDK/AppIcon.appiconset",//游戏图标集路径，用于替换游戏图标,需要替换的图标名字保持一致
    "Capability"://内购，推送，GameCenter设置
    [
        "InAppPurchase",
        "PushNotifications"
    ],
    "SystemFiles"://系统的framework和library
    [
        "WebKit.framework",
        "GameKit.framework",
        "StoreKit.framework",
        "libc++.tbd"
    ],
    "SDKFiles"://第三方SDK文件，可以是文件名或目录名
    [
        "SDK/BuglySDK/iOS",
        "SDK/BuglyBridge/iOS"
    ]
}
```
### 1. 基本设置(General)
设置游戏名称，包名，版本等；添加系统framework和library(`urs/lib/[libraryname]`)到 `[XCodeRootPath]/Frameworks/`目录下

**注意:** 第三方SDK文件(*.h,*.a,*.framework,*.mm)比较多，简单处理过程是：执行BuildPipeline.BuildPlayer前把第三方SDK文件拷贝到`Assets/Plugins/iOS/`目录下让Unity自动把sdk文件添加到xcode工程，导出xcode工程后删除该路径还原工程。

### 2. Capabilities
```c#
        public void AddAccessWiFiInformation();
        public void AddAppGroups(string[] groups);
        public void AddApplePay(string[] merchants);
        public void AddAssociatedDomains(string[] domains);
        public void AddBackgroundModes(BackgroundModesOptions options);
        public void AddDataProtection();
        public void AddGameCenter();
        public void AddHealthKit();
        public void AddHomeKit();
        public void AddiCloud(bool enableKeyValueStorage, bool enableiCloudDocument, string[] customContainers);
        public void AddiCloud(bool enableKeyValueStorage, bool enableiCloudDocument, bool enablecloudKit, bool addDefaultContainers, string[] customContainers);
        public void AddInAppPurchase();
        public void AddInterAppAudio();
        public void AddKeychainSharing(string[] accessGroups);
        public void AddMaps(MapsOptions options);
        public void AddPersonalVPN();
        public void AddPushNotifications(bool development);
        public void AddSiri();
        public void AddWallet(string[] passSubset);
        public void AddWirelessAccessoryConfiguration();
```

### 3. 修改Info.plist
修改权限说明:

`NSContactsUsageDescription` -> 通讯录

`NSMicrophoneUsageDescription` -> 麦克风

`NSPhotoLibraryUsageDescription` -> 相册

`NSCameraUsageDescription` -> 相机

`NSLocationAlwaysUsageDescription` -> 地理位置

`NSLocationWhenInUseUsageDescription` -> 地理位置

### 4. Build Settings
```c#
PBXProject.SetBuildProperty(target, "ENABLE_BITCODE", "NO");
```
