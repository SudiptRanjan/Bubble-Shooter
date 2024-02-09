using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
//using UnityEditor.iOS.Xcode;
using System.IO;
//using UnityEditor.iOS;

#if (UNITY_IOS || UNITY_TVOS)
using XcodeUnityCapability = UnityEditor.iOS.Xcode.ProjectCapabilityManager;
#endif

public class PlistManager 
{
#if (UNITY_IOS || UNITY_TVOS)
    [PostProcessBuild(999)]
    static void OnPostprocessBuild(BuildTarget buildTarget, string path)
    {
        // Read plist
        if (buildTarget != BuildTarget.iOS) return;
        // Set Project Capabilities
        var pbxPath = PBXProject.GetPBXProjectPath(path);
        var capManager = new ProjectCapabilityManager(pbxPath, "ios.entitlements", "Unity-iPhone");
        capManager.AddInAppPurchase();
        capManager.AddPushNotifications(true);
        capManager.AddBackgroundModes(BackgroundModesOptions.RemoteNotifications);
        capManager.AddAssociatedDomains(new[]{"applinks:kos-retar.onelink.me"});
        capManager.AddSignInWithApple();
        capManager.WriteToFile();

        // Set Bitcode Disable in Build settings
        var projectPath = path + "/Unity-iPhone.xcodeproj/project.pbxproj";
        var pbxProject = new PBXProject(); 
        pbxProject.ReadFromFile(projectPath);
        //string target = pbxProject.TargetGuidByName("Unity-iPhone");
        var guid = pbxProject.GetUnityMainTargetGuid();
        pbxProject.SetBuildProperty(guid, "ENABLE_BITCODE", "NO");

        // Add Frameworks
        //string targetName = pbxProject.getunity;
        var unityTargetGuid = pbxProject.GetUnityFrameworkTargetGuid();
        pbxProject.AddFrameworkToProject(unityTargetGuid, "UserNotifications.framework", true);
        pbxProject.AddFrameworkToProject(unityTargetGuid, "NotificationCenter.framework", true);
        pbxProject.AddFrameworkToProject(unityTargetGuid, "libsqlite3.tbd", false);
        pbxProject.AddFrameworkToProject(unityTargetGuid, "AppTrackingTransparency.framework", true);
        pbxProject.WriteToFile(projectPath);
            
        // Add New Item in Info.plist
        var plistPath = Path.Combine(path, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        // Update value
        var rootDict = plist.root;
        rootDict.SetString("NSPhotoLibraryUsageDescription", "KOS Requires access to the Photos Library for update profile");
        rootDict.SetString("NSLocationAlwaysUsageDescription", "Your location is required for Connect in region for multiplayer");
        rootDict.SetString("NSLocationWhenInUseUsageDescription", "Your location is required for Connect in region for multiplayer");
        rootDict.SetString("NSUserTrackingUsageDescription", "Tracking will be used to deliver personalized ads to you");
        var exitsOnSuspendKey = "UIApplicationExitsOnSuspend";
        if(rootDict.values.ContainsKey(exitsOnSuspendKey))
        {
            rootDict.values.Remove(exitsOnSuspendKey);
        }

        // Write plist
        File.WriteAllText(plistPath, plist.WriteToString());
    }
#endif
}
