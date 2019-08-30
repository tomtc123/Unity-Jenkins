using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;
using UnityEditor.Build.Reporting;

public class BuildIOS
{
    [PostProcessBuildAttribute(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject) {
        Debug.LogFormat("[BuildIOS].OnPostprocessBuild() target={0}, pathToBuiltProject={1}", target, pathToBuiltProject);
        if (target == BuildTarget.iOS)
        {
            ProcessPBXProject(pathToBuiltProject);
        }
    }

    static void ProcessPBXProject(string pathToBuiltProject)
    {
        var unityProjPath = Application.dataPath.Replace("Assets", "");

        string projPath = pathToBuiltProject + "/Unity-iPhone.xcodeproj/project.pbxproj";
        PBXProject proj = new PBXProject();

        proj.ReadFromString(File.ReadAllText(projPath));
        string target = proj.TargetGuidByName("Unity-iPhone");

        var settings = GetiOSBuildOptions();

        //1 General
        PlayerSettings.productName = settings.DisplayName;
        PlayerSettings.applicationIdentifier = settings.BundleIdentifier;
        PlayerSettings.bundleVersion = settings.Version;

        //1.1 framework && lib
        if (settings.SystemFiles != null)
        {
            foreach(var fileName in settings.SystemFiles)
            {
                if (fileName.EndsWith(".framework"))
                {
                    proj.AddFrameworkToProject(target, fileName, false);
                }
                else
                {
                    var fileGuid = proj.AddFile("usr/lib/" + fileName, "Frameworks/" + fileName, PBXSourceTree.Sdk);
                }
            }
        }

        //2 Capabilities
        if (settings.Capability != null)
        {
            var bundleIdLastIndex = settings.BundleIdentifier.LastIndexOf('.') + 1;
            var entitlementName = string.Format("{0}.entitlements", settings.BundleIdentifier.Substring(bundleIdLastIndex));
            var tempPath = Path.GetTempPath();
            var capManager = new ProjectCapabilityManager(projPath, entitlementName, PBXProject.GetUnityTargetName());
            foreach (var name in settings.Capability)
            {
                switch (name)
                {
                    case "PushNotifications":
                        capManager.AddPushNotifications(true);
                        break;
                    case "InAppPurchase":
                        capManager.AddInAppPurchase();
                        break;
                    default: break;
                }
            }
            capManager.WriteToFile();
            File.Copy(Path.Combine(pathToBuiltProject, entitlementName), Path.Combine(pathToBuiltProject, "Unity-iPhone", entitlementName));
        }

        //3 Info(Info.plist)
        string plistPath = pathToBuiltProject + "/Info.plist";
        var plist = new PlistDocument();
        plist.ReadFromString(File.ReadAllText(plistPath));
        var plistRoot = plist.root;
        //麦克风权限
        plistRoot.SetString("NSMicrophoneUsageDescription", "用于游戏内语音功能时访问麦克风收录语音");
        plist.WriteToFile(plistPath);

        //4 Build Settings
        // 关闭bitcode（tolua不支持）
        proj.SetBuildProperty(target, "ENABLE_BITCODE", "NO");

        //5 class
        string unityAppControllerPath = pathToBuiltProject + "/Classes/UnityAppController.mm";

        // 6 appiconset
        if (!string.IsNullOrEmpty(settings.AppIconSetPath))
        {
            string destPath = pathToBuiltProject + "/Unity-iPhone/Images.xcassets/AppIcon.appiconset/";
            string srcPath = Path.Combine(unityProjPath, settings.AppIconSetPath);
            CopyAndReplaceDirectory(srcPath, destPath);
        }

        // Save PBXProject
        proj.WriteToFile(projPath);
    }

    static void AddDirectoryToBuild(PBXProject proj, string target, string pathToBuiltProject, string path)
    {
        foreach (string filePath in Directory.GetFiles(path))
        {
            if (!filePath.EndsWith(".DS_Store"))
            {
                var tmpPath = filePath.Replace(pathToBuiltProject, "");
                string fileGuid = proj.AddFile(tmpPath, tmpPath, PBXSourceTree.Source);
                proj.AddFileToBuild(target, fileGuid);
                Debug.LogFormat("[BuildIOS] AddDirectoryToBuild(): {0}", tmpPath);
            }
        }
    }

    static void CopyAndReplaceDirectory(string srcPath, string dstPath)
    {
        foreach (var file in Directory.GetFiles(srcPath))
        {
            var to = Path.Combine(dstPath, Path.GetFileName(file));
            Debug.LogFormat("[BuildIOS] Copy File From:{0}, To:{1}", file, to);
            if (!Directory.Exists(dstPath))
            {
                Directory.CreateDirectory(dstPath);
            }
            File.Copy(file, to, true);
        }

        foreach (var dir in Directory.GetDirectories(srcPath))
        {
            CopyAndReplaceDirectory(dir, Path.Combine(dstPath, Path.GetFileName(dir)));
        }
    }

    static void ProcessSDKFile(bool clean)
    {
        var unityProjPath = Application.dataPath.Replace("Assets", "");
        var settings = GetiOSBuildOptions();
        if (settings.SDKFiles != null)
        {
            foreach (var fileName in settings.SDKFiles)
            {
                var dstPath = Path.Combine(Application.dataPath, "Plugins/iOS", fileName);
                if (clean)
                {
                    FileUtil.DeleteFileOrDirectory(dstPath);
                    Debug.LogFormat("[BuildIOS].ProcessSDKFile() clean:{0}", dstPath);
                }
                else
                {
                    var srcPath = Path.Combine(unityProjPath, fileName);
                    var dirName = Path.GetDirectoryName(dstPath);
                    if(!Directory.Exists(dirName))
                    {
                        Directory.CreateDirectory(dirName);
                    }
                    FileUtil.CopyFileOrDirectory(srcPath, dstPath);
                    Debug.LogFormat("[BuildIOS].ProcessSDKFile() Copy:{0}, To:{1}", srcPath, dstPath);
                }
            }
        }
        AssetDatabase.Refresh();
    }

    [MenuItem("iOS/BuildProject")]
    public static void BuildIOSProject()
    {
        ProcessSDKFile(false);
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        List<string> scenes = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                scenes.Add(scene.path);
            }
        }
        buildPlayerOptions.scenes = scenes.ToArray();
        buildPlayerOptions.locationPathName = "iOSBuild";
        buildPlayerOptions.target = BuildTarget.iOS;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        ProcessSDKFile(true);
    }

    [MenuItem("iOS/TestProcessPBXProject")]
    public static void TestProcessPBXProject()
    {
        var path = Path.Combine(Application.dataPath.Replace("Assets", ""), "iOSBuild");
        if (Directory.Exists(path))
            ProcessPBXProject(path);
    }

    [System.Serializable]
    public class iOSBuildOptions {
        public string DisplayName;
        public string BundleIdentifier;
        public string Version;
        public string AppIconSetPath;
        public string[] Capability;
        public string[] SystemFiles;
        public string[] SDKFiles;
    }

    public static iOSBuildOptions GetiOSBuildOptions()
    {
        var filePath = Application.dataPath + "/iOSBuildOptions.json";
        var text = File.ReadAllText(filePath);
        var options = JsonUtility.FromJson<iOSBuildOptions>(text);
        foreach(var item in options.SystemFiles) {
            Debug.LogFormat("[BuildIOS] SystemFiles:{0}", item);
        }
        foreach (var item in options.SDKFiles)
        {
            Debug.LogFormat("[BuildIOS] SDKFiles:{0}", item);
        }
        return options;
    }

}