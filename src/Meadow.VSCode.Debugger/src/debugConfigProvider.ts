import * as vscode from 'vscode';
import * as Net from 'net';
import * as fs from 'fs';
import * as path from 'path';
import * as uuid from 'uuid/v1';
import * as dotnetLaunchDebug from './dotnetLaunchDebug';
import { Logger } from './logger';
import { ISolidityMeadowDebugConfig, IDebugAdapterExecutable, SOLIDITY_MEADOW_TYPE } from './constants';
import { resolveMeadowDebugAdapter } from './debugAdapterExecutable';
import * as common from './common';

export class SolidityMeadowConfigurationProvider implements vscode.DebugConfigurationProvider {

	private _server?: Net.Server;

	private _context: vscode.ExtensionContext;

	private _debugConfig: ISolidityMeadowDebugConfig;

	constructor(context: vscode.ExtensionContext) {
		this._context = context;
	}

	// Notice: this is working in latest stable vscode but is preview.
	// Keep the 'getDebuggerPath' command method intact in case its ever removed or broken for this use case.
	debugAdapterExecutable(folder: vscode.WorkspaceFolder | undefined, token?: vscode.CancellationToken): vscode.ProviderResult<IDebugAdapterExecutable> {

		/*
		let session = vscode.debug.activeDebugSession;
		let workspaceConfig = vscode.workspace.getConfiguration();
		let launchConfig = vscode.workspace.getConfiguration('launch');
		let launchCompoundConfig = vscode.workspace.getConfiguration('launch.compounds');
		let debugConfig = this._debugConfig;
		let ctx = this._context;
        */

		let debugSessionID: string = uuid();

		dotnetLaunchDebug.launch(debugSessionID, this._debugConfig).catch(err => Logger.log("Error launching dotnet test", err));

		return resolveMeadowDebugAdapter(this._context, debugSessionID, this._debugConfig);
	}

	provideDebugConfigurations(folder: vscode.WorkspaceFolder | undefined, token?: vscode.CancellationToken): vscode.ProviderResult<vscode.DebugConfiguration[]> {

		let configs: ISolidityMeadowDebugConfig[] = [
			{
				type: SOLIDITY_MEADOW_TYPE,
				request: "launch",
				name: "Debug Solidity (via unit test run)"
			}, {
				type: SOLIDITY_MEADOW_TYPE,
				request: "launch",
				withoutSolidityDebugging: true,
				name: "Debug Unit Tests (without Solidity debugging)"
			}];

		return configs;
	}

	/**
	 * Massage a debug configuration just before a debug session is being launched,
	 * e.g. add all missing attributes to the debug configuration.
	 */
	resolveDebugConfiguration(folder: vscode.WorkspaceFolder | undefined, config: vscode.DebugConfiguration, token?: vscode.CancellationToken): vscode.ProviderResult<vscode.DebugConfiguration> {

		// if launch.json is missing or empty
		if (!config.type && !config.request && !config.name) {
			const editor = vscode.window.activeTextEditor;
			if (editor && editor.document.languageId === 'solidity') {
				config.type = SOLIDITY_MEADOW_TYPE;
				config.name = 'Launch';
				config.request = 'launch';
				config.program = '${file}';
				config.stopOnEntry = true;
			}
		}

		let debugConfig = <ISolidityMeadowDebugConfig>config;
		
		if (debugConfig.withoutSolidityDebugging) {
			// TODO: use "dotnet test" to find and built assembly,
			// then return coreclr launch config for program
			throw new Error("TODO..");
		}

		let checksReady = false;

		if (checksReady) {

			// TODO: ensure omnisharp extension is install, if not prompt to install link
			let omnisharpInstalled = false;
			if (!omnisharpInstalled) {
				throw new Error("TODO..");
			}

			// TODO: ensure "dotnet" sdk of min version is installed, if not prompt to download link
			let dotnetSdkOkay = false;
			if (!dotnetSdkOkay) {
				throw new Error("TODO..");
			}

			// TODO: ensure a main .csproj file exists, if not prompt to setup
			let workspaceHasCsproj = false;
			if (!workspaceHasCsproj) {
				throw new Error("TODO..");
			}

			// TODO: if .csproj file exists, check that it references Meadow.UnitTestTemplate package (need to have this reference solcodegen).
			//		 if it doesn't, prompt to install nuget package
			let projMeadowPackagesOkay = false;
			if (!projMeadowPackagesOkay) {
				throw new Error("TODO..");
			}

		}


		/*
		if (!config.program) {
			return vscode.window.showInformationMessage("Cannot find a program to debug").then(_ => {
				return undefined;	// abort launch
			});
		}
		*/

		let workspaceRoot = common.getWorkspaceFolder().uri.fsPath;
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
	

		this._debugConfig = debugConfig
		return debugConfig;
	}

	dispose() {
		if (this._server) {
			this._server.close();
		}
	}
}