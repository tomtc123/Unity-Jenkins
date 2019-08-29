using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;
using UnityEditor.Build.Reporting;

public class BuildIOS
{
    [PostProcessBuildAttribute(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuildProject) {
        Debug.Log( pathToBuildProject );
        if (target == BuildTarget.iOS)
        {
            ProcessPBXProject(pathToBuildProject);
        }
    }

    static void ProcessPBXProject(string pathToBuildProject)
    {
        string projPath = pathToBuildProject + "/Unity-iPhone.xcodeproj/project.pbxproj";
        PBXProject pbxProj = new PBXProject();

        pbxProj.ReadFromString(File.ReadAllText(projPath));
        string target = pbxProj.TargetGuidByName("Unity-iPhone");
        //1.General(framework && lib)

        //2.Capabilities

        // var capManager = new ProjectCapabilityManager(projPath, "XXXXX(自己替换).entitlements", PBXProject.GetUnityTargetName());
        // capManager.AddPushNotifications(true);
        // capManager.AddGameCenter();
        // capManager.AddInAppPurchase();
        // capManager.WriteToFile();

        //3.Info(Info.plist)
    
        //4.Build Settings

        //5.xclass

    }

    [MenuItem("iOS/BuildProject")]
    public static void BuildIOSProject()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scene1.unity", "Assets/Scene2.unity" };
        buildPlayerOptions.locationPathName = "iOSBuild";
        buildPlayerOptions.target = BuildTarget.iOS;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
    }

    [System.Serializable]
    public class BuildOptions {
        public string[] Capability;
        public Dictionary<string, bool> system_framework;
        public Dictionary<string, bool> sdk_framework;

    }

    [MenuItem("iOS/ParseBuildOptions")]
    public static void TestBuildOptions()
    {
        var filePath = Application.dataPath + "/Editor/BuildOptions.json";
        var text = File.ReadAllText(filePath);
        var jsonObj = JsonUtility.FromJson<BuildOptions>(text);
        foreach(var item in jsonObj.system_framework) {
            Debug.Log(item.Key);
        }
    }

}