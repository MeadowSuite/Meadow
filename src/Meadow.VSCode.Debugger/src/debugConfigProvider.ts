import * as vscode from 'vscode';
import * as Net from 'net';
import * as fs from 'fs';
import * as path from 'path';
import * as dotnetLaunchDebug from './dotnetLaunchDebug';
import { Logger } from './logger';
import * as uuid from 'uuid/v1';

export const SOLIDITY_MEADOW_TYPE : string = 'solidityMeadow';
export const DEBUG_SERVER_FILE_PATH_KEY = "debugServerFilePath";

export interface ISolidityMeadowDebugConfig extends vscode.DebugConfiguration {
	readonly stopOnEntry?: boolean;
	readonly debugAdapterFile?: string;
	readonly externalTestAssembly?: string;
	readonly testAssembly?: string;
	readonly logFile?: string;
	readonly trace?: boolean;
	readonly breakDebugAdapter?: boolean;
	readonly breakDebugServer?: boolean;
}

export interface DebugAdapterExecutable {
    readonly type?: 'executable';
	readonly command: string;
    readonly args: string[];
    readonly env?: { [key: string]: string };
    readonly cwd?: string;
}


export async function getMeadowDebugAdapter(context: vscode.ExtensionContext, debugSessionID: string, debugConfig?: ISolidityMeadowDebugConfig) : Promise<DebugAdapterExecutable> {

	let debugServerFilePath : string;
	let storedFilePath = context.globalState.get<string>(DEBUG_SERVER_FILE_PATH_KEY);
	if (storedFilePath && !fs.existsSync(storedFilePath)) {
		storedFilePath = undefined;
	}

	if (debugConfig && debugConfig.debugAdapterFile) {
		debugServerFilePath = debugConfig.debugAdapterFile;
	}
	else if (storedFilePath) {
		debugServerFilePath = storedFilePath;
	}
	else {
		let selectFileResult = await vscode.window.showOpenDialog({
			canSelectFiles: true, 
			canSelectFolders: false, 
			canSelectMany: false,
			filters: { "Debugger": [ 'VSCodeDebugServer.dll' ] },
			openLabel: "Select Meadow.VSCodeDebugServer.dll"
		})
		
		if (!selectFileResult || selectFileResult.length < 1) {
			throw new Error("Must select debugger file");
		}

		debugServerFilePath = selectFileResult[0].fsPath;
		context.globalState.update(DEBUG_SERVER_FILE_PATH_KEY, debugServerFilePath);
	}
	
	let args : string[] = [debugServerFilePath, "--vscode_debug"];

	if (debugConfig && debugConfig.logFile){
		args.push("--log_file", debugConfig.logFile);
	}
	else if (context["logPath"]) {
		args.push("--log_file", context["logPath"]);
	}

	if (debugConfig && debugConfig.trace){
		args.push("--trace");
    }
    
    if (debugConfig && debugConfig.breakDebugAdapter) {
        args.push("--attach_debugger");
    }

    args.push("--session", debugSessionID);

	return {
		command: "dotnet",
        args: args,
        env: { "DEBUG_SESSION_ID" : debugSessionID }
	}
}

export class SolidityMeadowConfigurationProvider implements vscode.DebugConfigurationProvider {

	private _server?: Net.Server;

	private _context: vscode.ExtensionContext;

	private _debugConfig : ISolidityMeadowDebugConfig;

	constructor(context: vscode.ExtensionContext) {
		this._context = context;
	}

	// Notice: this is working in latest stable vscode but is preview.
	// Keep the 'getDebuggerPath' command method intact in case its ever removed or broken for this use case.
	debugAdapterExecutable(folder: vscode.WorkspaceFolder | undefined, token?: vscode.CancellationToken): vscode.ProviderResult<DebugAdapterExecutable> {
	
		/*
		let session = vscode.debug.activeDebugSession;
		let workspaceConfig = vscode.workspace.getConfiguration();
		let launchConfig = vscode.workspace.getConfiguration('launch');
		let launchCompoundConfig = vscode.workspace.getConfiguration('launch.compounds');
		let debugConfig = this._debugConfig;
		let ctx = this._context;
        */
		
		let debugSessionID : string = uuid();

		dotnetLaunchDebug.launch(debugSessionID, this._debugConfig).catch(err => Logger.log("Error launching dotnet test", err));
        
		return getMeadowDebugAdapter(this._context, debugSessionID, this._debugConfig);
	}

	provideDebugConfigurations(folder: vscode.WorkspaceFolder | undefined, token?: vscode.CancellationToken): vscode.ProviderResult<vscode.DebugConfiguration[]> {
		return [{
			type: SOLIDITY_MEADOW_TYPE,
			request: "launch",
			name: "Launch Solidity Debug"
		}, {
			type: SOLIDITY_MEADOW_TYPE,
			request: "attach",
			name: "Attach Solidity Debug"
		}, {
			type: "coreclr",
			request: "launch",
			name: "Run Tests",
			program: "dotnet",
			args: [ "test" ],
			cwd: "${workspaceFolder}"
		}];
	  }

	/**
	 * Massage a debug configuration just before a debug session is being launched,
	 * e.g. add all missing attributes to the debug configuration.
	 */
	resolveDebugConfiguration(folder: vscode.WorkspaceFolder | undefined, config: vscode.DebugConfiguration, token?: vscode.CancellationToken): vscode.ProviderResult<vscode.DebugConfiguration> {

		// if launch.json is missing or empty
		if (!config.type && !config.request && !config.name) {
			const editor = vscode.window.activeTextEditor;
			if (editor && editor.document.languageId === 'solidity' ) {
				config.type = SOLIDITY_MEADOW_TYPE;
				config.name = 'Launch';
				config.request = 'launch';
				config.program = '${file}';
				config.stopOnEntry = true;
			}
		}

		/*
		if (!config.program) {
			return vscode.window.showInformationMessage("Cannot find a program to debug").then(_ => {
				return undefined;	// abort launch
			});
		}
		*/

		let debugConfig = <ISolidityMeadowDebugConfig>config;

		if (vscode.workspace.workspaceFolders !== undefined) {
			let workspaceRoot = vscode.workspace.workspaceFolders[0].uri.fsPath;
			debugConfig.workspaceDirectory = workspaceRoot;

			for (let pathProp of ['debugAdapterFile', 'testAssembly', 'logFile']) {
				let pathItem = debugConfig[pathProp];
				if (pathItem) {
					pathItem = pathItem.replace('${workspaceFolder}', workspaceRoot);
					if (!path.isAbsolute(pathItem)) {
						pathItem = path.resolve(workspaceRoot, pathItem);
					}
					else {
						pathItem = path.resolve(pathItem);
					}
					debugConfig[pathProp] = pathItem;
				}
			}
		}

		this._debugConfig = debugConfig
		return debugConfig;
	}

	dispose() {
		if (this._server) {
			this._server.close();
		}
	}
}