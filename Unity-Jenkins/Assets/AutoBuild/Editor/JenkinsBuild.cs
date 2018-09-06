using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public class JenkinsBuild
{
    public static void Build()
    {
#if UNITY_2017
        AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
#endif
        int version = 0;
        var args = Environment.GetCommandLineArgs();
        foreach(var arg in args){
            Debug.Log(arg);
        }
        int.TryParse(GetParam(args, "version=", "0"), out version);
    }

    public static string GetParam(string[] args, string param, string defaultValue)
    {
        string tmpValue = defaultValue;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].IndexOf(param) != -1)
            {
                tmpValue = args[i].Split('=')[1].Trim();
            }
        }

        return String.IsNullOrEmpty(tmpValue) ? defaultValue : tmpValue;
    }

    public void OnBeforeAssemblyReload()
    {
        Debug.Log("Before Assembly Reload.");
    }

    public void OnAfterAssemblyReload()
    {
        Debug.Log("After Assembly Reload.");
    }

#if UNITY_5
    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        Debug.Log("OnScriptsReloaded===>");
    }
#endif
}
