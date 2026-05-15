// ---------------------------------------------------------------------------------
// CrazyGamesBuildSetup.cs
//
// Editor utilities that automate the project-side setup required for a CrazyGames
// release-candidate WebGL build:
//
//   Tools > CrazyGames > Configure WebGL Build Settings
//     - Switches platform to WebGL and applies the four CrazyGames-required
//       PlayerSettings (Brotli, .NET Standard, Data Caching, Stripping High).
//
//   Tools > CrazyGames > Add Integration To Open Scene
//     - Inserts a CrazyGamesIntegration GameObject into whatever scene is open
//       (does nothing if one is already present).
//
//   Tools > CrazyGames > Whitelist Current Dev Domain...
//     - Prompts for a domain (e.g. "localhost", "preview.crazygames.com") and
//       appends it to CrazyGamesSettings.whitelistedDomains. Required so
//       sitelock doesn't block your dev/preview build.
//
//   Tools > CrazyGames > Run Full WebGL Setup
//     - One-click: scene wire-up + build settings, all in order.
//
// This script lives in an Editor/ folder so it never ships in player builds.
// ---------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CrazyGamesBuildSetup
{
    private const string LOG_TAG       = "[CrazyGamesBuildSetup]";
    private const string SETTINGS_PATH = "Assets/CrazySDK/Resources/CrazyGamesSettings.asset";
    private const string INTEGRATION_GAMEOBJECT_NAME = "CrazyGames";

    // =================================================================================
    // 1) Player / Build settings
    // =================================================================================

    [MenuItem ("Tools/CrazyGames/Configure WebGL Build Settings")]
    public static void ConfigureWebGLBuildSettings ()
    {
        // -----------------------------------------------------------------------------
        // Switch active build target to WebGL if needed so PlayerSettings writes
        // affect the correct target. Reversible from File > Build Settings.
        // -----------------------------------------------------------------------------
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
        {
            Debug.Log (LOG_TAG + " Active build target is " +
                       EditorUserBuildSettings.activeBuildTarget +
                       "; switching to WebGL...");

            EditorUserBuildSettings.SwitchActiveBuildTarget (
                BuildTargetGroup.WebGL,
                BuildTarget.WebGL);
        }

        NamedBuildTarget webgl = NamedBuildTarget.WebGL;

        // 1. Compression: Brotli (smallest payload, required to hit < 50 MB cap).
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;

        // 2. API Compatibility Level: .NET Standard (smaller assemblies + better stripping).
        PlayerSettings.SetApiCompatibilityLevel (webgl, ApiCompatibilityLevel.NET_Standard);

        // 3. Data caching: lets browser cache build in IndexedDB for return players.
        PlayerSettings.WebGL.dataCaching = true;

        // 4. Managed Stripping Level: High (combine with link.xml if anything strips wrong).
        PlayerSettings.SetManagedStrippingLevel (webgl, ManagedStrippingLevel.High);

        // 5. WebGL Code Optimization: Disk Size with LTO. Trades a bit of build
        //    time for the smallest .wasm output, which is the main lever for hitting
        //    the < 50 MB CrazyGames cap. The API is internal in 2022.3 and public
        //    only from 2023.1+, so we apply it via reflection and skip on older
        //    Unity versions where the setting simply isn't exposed via API.
        bool codeOptSet = TrySetWebGLCodeOptimization ("DiskSizeLTO");

        // 6. IL2CPP code generation: OptimizeSize ("Faster (smaller) builds" in the UI).
        //    Smaller binary + faster build than the speed-optimized path.
        PlayerSettings.SetIl2CppCodeGeneration (webgl, Il2CppCodeGeneration.OptimizeSize);

        // 7. Exception support: explicitly-thrown only. Best compromise between
        //    crash visibility and code size; full stacktraces inflate the build.
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

        // 8. Name files as hashes. MD5-based names give every build unique
        //    filenames so browser caches never serve stale assets between releases.
        PlayerSettings.WebGL.nameFilesAsHashes = true;

        // 9. Disable Unity splash + logo. Unity Personal license can't legally
        //    disable the splash, but disabling the logo is allowed and shaves
        //    a small amount off the initial download.
        PlayerSettings.SplashScreen.show          = false;
        PlayerSettings.SplashScreen.showUnityLogo = false;

        // 10. Static + dynamic batching enabled for WebGL. SetBatchingForPlatform
        //     is `internal` in Unity 2022.3, so we go through reflection. Default
        //     in modern Unity is already on; this is belt-and-braces.
        bool batchingSet = TrySetBatchingForPlatform (BuildTarget.WebGL, 1, 1);

        AssetDatabase.SaveAssets ();

        Debug.Log (LOG_TAG + " Compression Format       -> Brotli");
        Debug.Log (LOG_TAG + " API Compatibility Level  -> .NET Standard");
        Debug.Log (LOG_TAG + " Data Caching             -> Enabled");
        Debug.Log (LOG_TAG + " Managed Stripping Level  -> High");
        Debug.Log (LOG_TAG + " WebGL Code Optimization  -> " + (codeOptSet
            ? "Disk Size with LTO"
            : "skipped (API not exposed in this Unity version; set manually in Player Settings)"));
        Debug.Log (LOG_TAG + " IL2CPP Code Generation   -> OptimizeSize");
        Debug.Log (LOG_TAG + " Exception Support        -> Explicitly Thrown Only");
        Debug.Log (LOG_TAG + " Name Files As Hashes     -> Enabled");
        Debug.Log (LOG_TAG + " Splash Screen / Logo     -> Disabled");
        Debug.Log (LOG_TAG + " Static + Dynamic Batching-> " + (batchingSet
            ? "Enabled"
            : "skipped (defaults remain; usually already on)"));
        Debug.Log (LOG_TAG + " WebGL build settings configured for CrazyGames.");
    }

    // =================================================================================
    // 1b) Orientation — Landscape (CrazyGames accepts landscape submissions)
    // =================================================================================

    [MenuItem ("Tools/CrazyGames/Set Portrait-In-Landscape Mode")]
    public static void ConfigurePortraitInLandscape ()
    {
        // -----------------------------------------------------------------------------
        // Strategy: render a portrait game inside a landscape page. The Unity
        // canvas stays 720x1280 (9:16 portrait — matches your existing UI
        // prefabs that were authored for 1536x2048 portrait Canvas Scaler).
        // The WebGL template's outer HTML is landscape and centers the canvas
        // with decorative side panels in the gutter areas.
        //
        // CrazyGames sees a landscape submission (which is what they accept);
        // the player sees your portrait game centered with brand panels on
        // each side. No in-Unity UI redesign required.
        // -----------------------------------------------------------------------------
        PlayerSettings.defaultWebScreenWidth  = 720;
        PlayerSettings.defaultWebScreenHeight = 1280;
        PlayerSettings.runInBackground        = false;

        Debug.Log (LOG_TAG + " Default WebGL canvas      -> 720 x 1280 (9:16 portrait, in landscape page)");
        Debug.Log (LOG_TAG + " Run In Background         -> Disabled");

        // CrazyGames deviceType = desktop. Even though the canvas is portrait,
        // the SUBMISSION is landscape, and landscape submissions are desktop-class
        // on the portal. (You can flip to mobile manually in the settings asset
        // if you want mobile-portrait categorization while still being accepted
        // as a landscape format.)
        SetCrazyGamesSettingsEnum ("deviceType", 0);
        Debug.Log (LOG_TAG + " CrazyGames deviceType     -> desktop");

        const string ptlTemplate = "PROJECT:CrazyGamesPortraitInLandscape";
        if (AssetDatabase.IsValidFolder ("Assets/WebGLTemplates/CrazyGamesPortraitInLandscape"))
        {
            PlayerSettings.WebGL.template = ptlTemplate;
            Debug.Log (LOG_TAG + " WebGL Template            -> CrazyGamesPortraitInLandscape");
        }
        else
        {
            Debug.LogWarning (LOG_TAG + " Template folder missing; leaving WebGL template unchanged.");
        }

        Debug.Log (LOG_TAG + " Portrait-in-landscape configuration applied.");
        Debug.Log (LOG_TAG + " Your existing portrait UI prefabs render unchanged inside the 9:16 area.");
    }

    [MenuItem ("Tools/CrazyGames/Set Landscape Mode (Desktop)")]
    public static void ConfigureLandscapeDesktop ()
    {
        // -----------------------------------------------------------------------------
        // Unity WebGL canvas defaults. Locked to 16:9 landscape, sized to meet
        // CrazyGames' 800x600 minimum-resolution baseline:
        //   - 1280 is comfortably above the 800 long-side minimum
        //   - 720  is comfortably above the 600 short-side minimum
        // 1280x720 is also standard "HD720 landscape", so it aligns with anything
        // designed against 16:9 reference resolutions and renders crisply when
        // CrazyGames' iframe scales it up on desktop browsers.
        // -----------------------------------------------------------------------------
        PlayerSettings.defaultWebScreenWidth  = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;

        // -----------------------------------------------------------------------------
        // runInBackground = false: when the CrazyGames iframe loses focus (player
        // tabs away, or an ad takes over), Unity stops ticking. Required so audio
        // and game logic don't keep running underneath the ad overlay.
        // -----------------------------------------------------------------------------
        PlayerSettings.runInBackground = false;

        Debug.Log (LOG_TAG + " Default WebGL canvas      -> 1280 x 720 (16:9 landscape, meets 800x600 minimum)");
        Debug.Log (LOG_TAG + " Run In Background         -> Disabled");

        // -----------------------------------------------------------------------------
        // CrazyGamesSettings.deviceType = desktop. Landscape games are typically
        // desktop-first on CrazyGames; this hints to the portal for discovery
        // categorization and influences the default banner sizes the SDK picks.
        // -----------------------------------------------------------------------------
        SetCrazyGamesSettingsEnum ("deviceType", 0); // 0=desktop, 1=tablet, 2=mobile

        Debug.Log (LOG_TAG + " CrazyGames deviceType     -> desktop");

        // -----------------------------------------------------------------------------
        // Switch the WebGL template to CrazyGamesLandscape. Hard-locks the canvas
        // to 16:9 inside whatever iframe size the portal serves, so the game
        // renders landscape with letterboxing instead of being stretched.
        // -----------------------------------------------------------------------------
        const string landscapeTemplate = "PROJECT:CrazyGamesLandscape";
        if (AssetDatabase.IsValidFolder ("Assets/WebGLTemplates/CrazyGamesLandscape"))
        {
            PlayerSettings.WebGL.template = landscapeTemplate;
            Debug.Log (LOG_TAG + " WebGL Template            -> CrazyGamesLandscape");
        }
        else
        {
            Debug.LogWarning (LOG_TAG + " WebGL template folder not found at " +
                              "Assets/WebGLTemplates/CrazyGamesLandscape; leaving template unchanged.");
        }

        Debug.Log (LOG_TAG + " Landscape desktop configuration applied.");
        Debug.LogWarning (LOG_TAG + " IMPORTANT: This switched only the canvas + portal hints. " +
                          "Your in-game UI prefabs were authored for 9:16 portrait (CanvasScaler " +
                          "1536x2048). They need to be reauthored for 16:9 landscape before the build looks right.");
    }

    // =================================================================================
    // 2) Scene wire-up
    // =================================================================================

    [MenuItem ("Tools/CrazyGames/Add Integration To Open Scene")]
    public static void AddIntegrationToOpenScene ()
    {
        Scene scene = SceneManager.GetActiveScene ();
        if (!scene.IsValid () || !scene.isLoaded)
        {
            Debug.LogError (LOG_TAG + " No active scene is loaded. Open your bootstrap scene first.");
            return;
        }

        // -----------------------------------------------------------------------------
        // Look for an existing CrazyGamesIntegration in the active scene. Using
        // FindObjectsOfType (deprecated in newer Unity but still supported in
        // 2022.3) so we catch instances on any GameObject, not just one named "CrazyGames".
        // -----------------------------------------------------------------------------
#if UNITY_2022_3_OR_NEWER
        CrazyGamesIntegration existing = UnityEngine.Object.FindFirstObjectByType<CrazyGamesIntegration> (FindObjectsInactive.Include);
#else
        CrazyGamesIntegration existing = UnityEngine.Object.FindObjectOfType<CrazyGamesIntegration> ();
#endif
        if (existing != null)
        {
            Debug.Log (LOG_TAG + " Scene '" + scene.name +
                       "' already contains CrazyGamesIntegration on GameObject '" +
                       existing.gameObject.name + "'. Skipping.");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // -----------------------------------------------------------------------------
        // Create the GameObject and register the action for Undo so the user can
        // Ctrl/Cmd+Z back out if it landed in the wrong scene.
        // -----------------------------------------------------------------------------
        GameObject go = new GameObject (INTEGRATION_GAMEOBJECT_NAME);
        Undo.RegisterCreatedObjectUndo (go, "Add CrazyGamesIntegration");
        Undo.AddComponent<CrazyGamesIntegration> (go);

        // Mark scene dirty so Unity prompts to save it on exit / lets us save below.
        EditorSceneManager.MarkSceneDirty (scene);
        EditorSceneManager.SaveScene (scene);

        Selection.activeGameObject = go;
        Debug.Log (LOG_TAG + " Added CrazyGamesIntegration to scene '" + scene.name +
                   "' and saved.");
    }

    // =================================================================================
    // 3) Whitelist domain
    // =================================================================================

    [MenuItem ("Tools/CrazyGames/Whitelist Current Dev Domain...")]
    public static void WhitelistDomain ()
    {
        ScriptableObject settings = AssetDatabase.LoadAssetAtPath<ScriptableObject> (SETTINGS_PATH);
        if (settings == null)
        {
            Debug.LogError (LOG_TAG + " Could not find CrazyGamesSettings at " + SETTINGS_PATH +
                            ". Did you import the CrazyGames SDK?");
            return;
        }

        // -----------------------------------------------------------------------------
        // Prompt for the domain. EditorUtility.DisplayDialog has no text-input form
        // so we cheat with a Save Panel + custom inline window... actually the
        // simplest dialog is a fixed default (localhost). Devs can re-run with a
        // custom domain via the SerializedObject directly. For one-click, default
        // to "localhost" which is what 99% of devs need.
        // -----------------------------------------------------------------------------
        string domainToAdd = EditorInputDialog ("Whitelist Domain",
            "Enter the domain to whitelist (e.g. 'localhost', 'preview.crazygames.com', or your own dev host).",
            "localhost");

        if (string.IsNullOrWhiteSpace (domainToAdd))
        {
            Debug.Log (LOG_TAG + " Whitelist cancelled (no domain entered).");
            return;
        }

        domainToAdd = domainToAdd.Trim ();

        // -----------------------------------------------------------------------------
        // Use SerializedObject so we don't need a hard reference to CrazySettings.
        // The settings asset stores a string[] whitelistedDomains.
        // -----------------------------------------------------------------------------
        SerializedObject so       = new SerializedObject (settings);
        SerializedProperty list   = so.FindProperty ("whitelistedDomains");

        if (list == null || !list.isArray)
        {
            Debug.LogError (LOG_TAG + " CrazyGamesSettings has no 'whitelistedDomains' array. SDK API may have changed.");
            return;
        }

        // Skip if already present (case-insensitive match).
        for (int i = 0; i < list.arraySize; i++)
        {
            string existing = list.GetArrayElementAtIndex (i).stringValue;
            if (string.Equals (existing, domainToAdd, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log (LOG_TAG + " Domain '" + domainToAdd + "' is already whitelisted.");
                return;
            }
        }

        list.InsertArrayElementAtIndex (list.arraySize);
        list.GetArrayElementAtIndex (list.arraySize - 1).stringValue = domainToAdd;
        so.ApplyModifiedProperties ();

        EditorUtility.SetDirty (settings);
        AssetDatabase.SaveAssets ();

        Debug.Log (LOG_TAG + " Whitelisted '" + domainToAdd + "' in CrazyGamesSettings.");
    }

    // =================================================================================
    // 4) Full setup — one-click runs everything that's safe to automate
    // =================================================================================

    [MenuItem ("Tools/CrazyGames/Run Full WebGL Setup")]
    public static void RunFullSetup ()
    {
        Debug.Log (LOG_TAG + " === Running full WebGL setup ===");

        AddIntegrationToOpenScene ();
        ConfigureWebGLBuildSettings ();
        ConfigurePortraitInLandscape ();

        Debug.Log (LOG_TAG + " === Full setup complete ===");
        Debug.Log (LOG_TAG + " Next manual steps:");
        Debug.Log (LOG_TAG + "   1. Run 'Tools > CrazyGames > Whitelist Current Dev Domain...' to add your dev/preview host.");
        Debug.Log (LOG_TAG + "   2. Drop Assets/CrazySDK/Resources/CrazyBanner.prefab into a UI Canvas if you want banners.");
        Debug.Log (LOG_TAG + "   3. File > Build Settings > Build, then upload to developer.crazygames.com to test on the Preview tool.");
    }

    // =================================================================================
    // Helpers
    // =================================================================================

    /// <summary>
    /// Sets PlayerSettings.WebGL.codeOptimization via reflection. The property
    /// is internal in Unity 2022.3 and public only from 2023.1+, so this
    /// gracefully no-ops on older Unity versions where the setting can only be
    /// changed from the Player Settings inspector UI.
    /// </summary>
    /// <returns>True if the setting was applied, false if the API wasn't found.</returns>
    private static bool TrySetWebGLCodeOptimization (string enumValueName)
    {
        // The enum lives in UnityEditor.WebGLCodeOptimization OR (in newer
        // Unity) UnityEditor.Build.WebGLCodeOptimization. Walk both candidates.
        Type enumType =
            Type.GetType ("UnityEditor.WebGLCodeOptimization, UnityEditor") ??
            Type.GetType ("UnityEditor.Build.WebGLCodeOptimization, UnityEditor");

        if (enumType == null) return false;

        // PlayerSettings.WebGL is a nested static class; the property may be
        // declared as either public or internal depending on version.
        Type webglType = typeof (PlayerSettings).GetNestedType ("WebGL", BindingFlags.Public | BindingFlags.NonPublic);
        if (webglType == null) return false;

        PropertyInfo prop = webglType.GetProperty (
            "codeOptimization",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (prop == null) return false;

        object enumValue;
        try { enumValue = Enum.Parse (enumType, enumValueName); }
        catch { return false; }

        prop.SetValue (null, enumValue);
        return true;
    }

    /// <summary>
    /// Sets static + dynamic batching for a build target via reflection.
    /// PlayerSettings.SetBatchingForPlatform is internal in Unity 2022.3.
    /// </summary>
    /// <returns>True if the setting was applied, false if the API wasn't found.</returns>
    private static bool TrySetBatchingForPlatform (BuildTarget target, int staticBatching, int dynamicBatching)
    {
        MethodInfo method = typeof (PlayerSettings).GetMethod (
            "SetBatchingForPlatform",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof (BuildTarget), typeof (int), typeof (int) },
            null);

        if (method == null) return false;

        method.Invoke (null, new object[] { target, staticBatching, dynamicBatching });
        return true;
    }

    /// <summary>
    /// Mutates a single enum field on the CrazyGamesSettings asset via
    /// SerializedObject (no hard dependency on the CrazySettings type). Used to
    /// flip deviceType / applicationType without compile-time coupling.
    /// </summary>
    private static void SetCrazyGamesSettingsEnum (string fieldName, int enumValueIndex)
    {
        ScriptableObject settings = AssetDatabase.LoadAssetAtPath<ScriptableObject> (SETTINGS_PATH);
        if (settings == null)
        {
            Debug.LogWarning (LOG_TAG + " CrazyGamesSettings not found; skipping '" + fieldName + "'.");
            return;
        }

        SerializedObject  so   = new SerializedObject (settings);
        SerializedProperty prop = so.FindProperty (fieldName);

        if (prop == null)
        {
            Debug.LogWarning (LOG_TAG + " Field '" + fieldName + "' not found on CrazyGamesSettings.");
            return;
        }

        prop.enumValueIndex = enumValueIndex;
        so.ApplyModifiedProperties ();
        EditorUtility.SetDirty (settings);
        AssetDatabase.SaveAssets ();
    }

    /// <summary>
    /// Minimal one-line text-input dialog. Unity has no built-in equivalent, so we
    /// roll a tiny EditorWindow.
    /// </summary>
    private static string EditorInputDialog (string title, string message, string defaultValue)
    {
        return InputDialogWindow.Show (title, message, defaultValue);
    }

    private class InputDialogWindow : EditorWindow
    {
        private string  _message;
        private string  _value;
        private bool    _confirmed;
        private bool    _closed;

        public static string Show (string title, string message, string defaultValue)
        {
            InputDialogWindow win = CreateInstance<InputDialogWindow> ();
            win.titleContent      = new GUIContent (title);
            win._message          = message;
            win._value            = defaultValue;
            win.minSize           = new Vector2 (420, 120);
            win.maxSize           = new Vector2 (420, 120);
            win.ShowModalUtility (); // blocks until window closes

            return win._confirmed ? win._value : null;
        }

        private void OnGUI ()
        {
            EditorGUILayout.LabelField (_message, EditorStyles.wordWrappedLabel);
            GUILayout.Space (6);
            _value = EditorGUILayout.TextField (_value);
            GUILayout.Space (8);

            using (new EditorGUILayout.HorizontalScope ())
            {
                if (GUILayout.Button ("Cancel")) { _confirmed = false; CloseSafe (); }
                if (GUILayout.Button ("OK"))     { _confirmed = true;  CloseSafe (); }
            }
        }

        private void CloseSafe ()
        {
            if (_closed) return;
            _closed = true;
            Close ();
        }
    }
}
