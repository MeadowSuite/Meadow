import * as vscode from 'vscode';
import * as child_process from "child_process";
import * as util from 'util';
import * as debugConfigProvider from './debugConfigProvider';
import { Logger } from './logger';

export async function launch(debugSessionID: string, debugConfig: debugConfigProvider.ISolidityMeadowDebugConfig) {

    // let activeDebugSession = vscode.debug.activeDebugSession;
    // let responseTest = await e.customRequest("customRequestExample", { sessionID: debugSessionID });

    const EXTERNAL_UNIT_TEST_DEBUG = true;

    let envOpts = { 
        "DEBUG_SESSION_ID": debugSessionID,
    };

    if (debugConfig.breakDebugServer) {
        envOpts["DEBUG_STOP_ON_ENTRY"] = "true";
    }

    const meadowDotnetDebugName = "Meadow: MSTest Runner";
    let workspaceFolder = getWorkspaceFolder();

    let testAssembly : string;
    if (debugConfig.testAssembly) {
        testAssembly = debugConfig.testAssembly;
    }
    else {
        let testAssemblies = await getDotnetTestAssemblies();
        if (testAssemblies.length > 1) {
            throw new Error("Multiple assemblies for testing found: " + testAssemblies.join("; "));
        }
        else if (testAssemblies.length === 0) {
            throw new Error("No assemblies found for testing");
        }
        testAssembly = testAssemblies[0];
    }

    // TODO: run dotnet build before this

    if (debugConfig.disableUnitTestDebugging) {
        let externalOpts = {
            env: envOpts
        };
        child_process.execFile("dotnet", [testAssembly], externalOpts, (err, stdout, stderr) => {
            Logger.log("run finished");
        });
    }
    else {

        let unitTestRunnerDebugConfig: vscode.DebugConfiguration = {
            name: meadowDotnetDebugName,
            type: "coreclr",
            request: "launch",
            program: testAssembly,
            cwd: workspaceFolder,
            env: envOpts,
            console: "internalConsole",
            internalConsoleOptions: "openOnSessionStart"
        };

        vscode.debug.startDebugging(workspaceFolder, unitTestRunnerDebugConfig);
    }
}



function getWorkspaceFolder() : vscode.WorkspaceFolder {
    let workspaceFolder: vscode.WorkspaceFolder;
    if (vscode.workspace.workspaceFolders && vscode.workspace.workspaceFolders.length > 0) {
        workspaceFolder = vscode.workspace.workspaceFolders[0];
    }
    else {
        throw new Error("bad workspace");
    }
    return workspaceFolder;
}

async function getDotnetTestAssemblies() : Promise<string[]> {
    let workspaceFolder = getWorkspaceFolder();
    let dotnetTestResult: { stderr: string, stdout: string };
    try {
        let testRunArgs = ["test", "-t", "-v=q"];
        let testRunOpts = { cwd: workspaceFolder.uri.fsPath };
        dotnetTestResult = await util.promisify(child_process.execFile)("dotnet", testRunArgs, testRunOpts);
        if (dotnetTestResult.stderr) {
            throw new Error(`Error when running dotnet test discovery: ${dotnetTestResult.stderr}`);
        }
    }
    catch (err) {
        throw err;
    }

    let testAssemblyRegex = /^Test run for (.+\.dll)\(.+\)/gm;
    let testAssemblies: string[] = [];
    let testAssemblyRegexMatch: RegExpExecArray | null = null;
    do {
        testAssemblyRegexMatch = testAssemblyRegex.exec(dotnetTestResult.stdout);
        if (testAssemblyRegexMatch) {
            testAssemblies.push(testAssemblyRegexMatch[1]);
        }
    }
    while (testAssemblyRegexMatch);
    return testAssemblies;
}
