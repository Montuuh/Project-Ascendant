using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public class RunEditModeTests : ICallbacks
{
    private static RunEditModeTests _instance;

    public static void Execute()
    {
        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        _instance = new RunEditModeTests();
        api.RegisterCallbacks(_instance);

        var filter = new Filter
        {
            testMode = TestMode.EditMode,
            assemblyNames = new[] { "ProjectAscendant.Tests.EditMode" }
        };

        api.Execute(new ExecutionSettings(filter));
    }

    public void RunStarted(ITestAdaptor testsToRun) =>
        Debug.Log($"[TestRunner] Started: {testsToRun.Name}");

    public void RunFinished(ITestResultAdaptor result) =>
        Debug.Log($"[TestRunner] Finished — Pass: {result.PassCount}, Fail: {result.FailCount}, Skip: {result.SkipCount}");

    public void TestStarted(ITestAdaptor test) { }

    public void TestFinished(ITestResultAdaptor result)
    {
        if (!result.HasChildren && result.FailCount > 0)
            Debug.LogError($"[TestRunner] FAIL: {result.Test.FullName}\n{result.Message}");
    }
}
