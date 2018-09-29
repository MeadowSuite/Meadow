import * as vscode from 'vscode';

export function getWorkspaceFolder() : vscode.WorkspaceFolder {
    let workspaceFolder: vscode.WorkspaceFolder;
    if (vscode.workspace.workspaceFolders && vscode.workspace.workspaceFolders.length > 0) {
        workspaceFolder = vscode.workspace.workspaceFolders[0];
    }
    else {
        throw new Error("bad workspace");
    }
    return workspaceFolder;
}
