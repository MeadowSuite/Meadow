import * as vscode from 'vscode';
import * as semver from 'semver';
import * as child_process from "child_process";

export function getWorkspaceFolder(): vscode.WorkspaceFolder {
    let workspaceFolder: vscode.WorkspaceFolder;
    if (vscode.workspace.workspaceFolders && vscode.workspace.workspaceFolders.length > 0) {
        workspaceFolder = vscode.workspace.workspaceFolders[0];
    }
    else {
        throw new Error("bad workspace");
    }
    return workspaceFolder;
}

export function validateDotnetVersion() {

    // ensure "dotnet" sdk of min version is installed, if not prompt to download link
    let minVersion = "2.1.0";

    let dotnetVersionResult = child_process.spawnSync("dotnet", ["--version"]);
    if (dotnetVersionResult.error) {
        dotnetVersionResult.error.message += ". Error identifying dotnet SDK version. Is it installed?"
        throw dotnetVersionResult.error;
    }

    if (dotnetVersionResult.status !== 0) {
        let dotnetError = dotnetVersionResult.stdout.toString() + dotnetVersionResult.stderr.toString();
        throw new Error(`dotnet returned exit code ${dotnetVersionResult.status} - ` + dotnetError);
    }

    let dotnetVersionString = dotnetVersionResult.stdout.toString().trim();
    let dotnetVersionSatisfied = semver.gte(dotnetVersionString, minVersion);
    if (!dotnetVersionSatisfied) {
        throw new Error(`Invalid dotnet version "${dotnetVersionString}" - must be ${minVersion} or greater. Download SDK from https://www.microsoft.com/net/download`);
    }

}