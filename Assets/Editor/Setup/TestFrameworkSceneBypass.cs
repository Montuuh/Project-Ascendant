using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Patches Unity Test Framework scene tasks so SaveCurrentModifiedScenesIfUserWantsTo
/// never blocks Coplay MCP (clears dirtiness, proceeds without dialog).
/// </summary>
public static class TestFrameworkSceneBypass
{
    const string TestJobRunnerTypeName = "UnityEditor.TestTools.TestRunner.TestRun.TestJobRunner";
    const string TestJobDataTypeName = "UnityEditor.TestTools.TestRunner.TestRun.TestJobData";

    static Delegate _originalGetTasks;

    /// <summary>Replace TestRunnerApi.ScheduleJob on this instance with a patched runner.</summary>
    public static void InstallOn(TestRunnerApi api)
    {
        var field = typeof(TestRunnerApi).GetField("ScheduleJob", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            Debug.LogWarning("[TestRunner] Could not patch ScheduleJob — scene save dialog may appear.");
            return;
        }

        field.SetValue(api, (Func<ExecutionSettings, string>)RunWithSceneBypass);
        Debug.Log("[TestRunner] Scene bypass installed on TestRunnerApi.");
    }

    static string RunWithSceneBypass(ExecutionSettings settings)
    {
        var testRunnerAssembly = typeof(TestRunnerApi).Assembly;
        var runnerType = testRunnerAssembly.GetType(TestJobRunnerTypeName, throwOnError: true);
        var jobDataType = testRunnerAssembly.GetType(TestJobDataTypeName, throwOnError: true);

        var runner = Activator.CreateInstance(runnerType);
        PatchRunnerGetTasks(runner);

        var jobData = Activator.CreateInstance(jobDataType, settings);
        var runJob = runnerType.GetMethod("RunJob", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return (string)runJob.Invoke(runner, new[] { jobData });
    }

    static void PatchRunnerGetTasks(object runner)
    {
        var field = runner.GetType().GetField("GetTasks", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (field == null)
            return;

        var original = (Delegate)field.GetValue(runner);
        _originalGetTasks = original;

        var originalType = original.GetType();
        var taskBaseType = originalType.GenericTypeArguments[1].GetGenericArguments()[0];
        var wrapperMethod = typeof(TestFrameworkSceneBypass)
            .GetMethod(nameof(WrapGetTaskListGeneric), BindingFlags.Static | BindingFlags.NonPublic)
            .MakeGenericMethod(taskBaseType);

        field.SetValue(runner, Delegate.CreateDelegate(originalType, wrapperMethod));
    }

    static IEnumerable<T> WrapGetTaskListGeneric<T>(ExecutionSettings settings)
    {
        var original = (Func<ExecutionSettings, IEnumerable<T>>)_originalGetTasks;
        foreach (var task in original(settings))
        {
            PatchSceneTask(task);
            yield return task;
        }
    }

    static void PatchSceneTask(object task)
    {
        switch (task.GetType().Name)
        {
            case "SaveModifiedSceneTask":
                SetDelegateField(task, "SaveCurrentModifiedScenesIfUserWantsTo", (Func<bool>)BypassSaveDialog);
                break;
            case "RestoreSceneSetupTask":
                SetDelegateField(task, "RestoreSceneManagerSetup", (Action<SceneSetup[]>)BypassRestoreSceneSetup);
                break;
            case "RemoveAdditionalUntitledSceneTask":
                PatchRemoveUntitledCloseScene(task);
                break;
        }
    }

    static bool BypassSaveDialog()
    {
        EditorSceneDiscardUtility.ClearDirtinessOnAllLoadedScenes();
        return true;
    }

    static void BypassRestoreSceneSetup(SceneSetup[] setup)
    {
        EditorSceneDiscardUtility.ClearDirtinessOnAllLoadedScenes();

        if (setup != null && setup.Length > 0)
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        else
            EditorSceneDiscardUtility.OpenSceneDiscardingUntitled(
                TestRunSceneGuard.ReadRestorePrimaryPath(),
                "Assets/Scenes/Boot.unity");
    }

    static void PatchRemoveUntitledCloseScene(object task)
    {
        var field = task.GetType().GetField("CloseScene", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (field?.GetValue(task) is not Delegate original)
            return;

        field.SetValue(task, new Func<object, bool, bool>((sceneWrapper, remove) =>
        {
            if (TryGetWrappedScene(sceneWrapper, out var scene))
                EditorSceneDiscardUtility.ClearDirtiness(scene);
            return (bool)original.DynamicInvoke(sceneWrapper, remove);
        }));
    }

    static bool TryGetWrappedScene(object sceneWrapper, out Scene scene)
    {
        scene = default;
        if (sceneWrapper == null)
            return false;

        var prop = sceneWrapper.GetType().GetProperty("WrappedScene", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop == null)
            return false;

        scene = (Scene)prop.GetValue(sceneWrapper);
        return scene.IsValid();
    }

    static void SetDelegateField<T>(object task, string fieldName, T handler) where T : Delegate
    {
        var field = task.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        field?.SetValue(task, handler);
    }
}
