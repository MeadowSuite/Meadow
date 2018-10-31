import * as vscode from 'vscode';
import * as Net from 'net';
import * as fs from 'fs';
import * as path from 'path';
import * as uuid from 'uuid/v1';
import * as child_process from "child_process";
import { Logger } from './logger';
import { ISolidityMeadowDebugConfig, IDebugAdapterExecutable, DEBUG_SESSION_ID } from './constants';
import * as debugSolSourcesTool from './debugSolSourcesTool';
import * as common from './common';
import { debug } from 'util';


export class SolidityDebugConfigProvider implements vscode.DebugConfigurationProvider {

	private _context: vscode.ExtensionContext;

	constructor(context: vscode.ExtensionContext) {
		this._context = context;

		context.subscriptions.push(vscode.debug.onDidReceiveDebugSessionCustomEvent(e => {
			if (e.session.type === 'solidity' && e.event === 'problemEvent' && e.body) {
				let msg : string = e.body.message;
				let err : string = e.body.exception;
				Logger.log(err);
				Logger.show();
				vscode.window.showErrorMessage(msg);
			}
		}));
	
	}

	// Notice: this is working in latest stable vscode but is preview.
	provideDebugAdapter?(session: vscode.DebugSession, folder: vscode.WorkspaceFolder | undefined, executable: IDebugAdapterExecutable | undefined, config: vscode.DebugConfiguration, token?: vscode.CancellationToken): vscode.ProviderResult<IDebugAdapterExecutable> {
	
		return (async () => {
			
			let debugConfig = <ISolidityMeadowDebugConfig>config;

			let args : string[] = [ ];

			if (debugConfig.workspaceDirectory) {
				args.push("--directory", debugConfig.workspaceDirectory);
			}

			if (debugConfig.entryPoint) {
				args.push("--entry", debugConfig.entryPoint);
			}

			if (debugConfig.singleFile) {
				args.push("--singleFile", debugConfig.singleFile)
			}

			let env : { [key: string]: string } = { };

			if (debugConfig.breakDebugAdapter) {
				env["DEBUG_STOP_ON_ENTRY"] = "true";
			}

			let launchInfo : IDebugAdapterExecutable;

			if (debugConfig.debugAdapterFile) {
				args.unshift(debugConfig.debugAdapterFile);
				launchInfo = {
					type: "executable",
					command: "dotnet",
					args: args,
					env: env
				};
			}
			else {
				let debugAdapterFile = await debugSolSourcesTool.getDebugToolPath();
				launchInfo = {
					type: "executable",
					command: debugAdapterFile,
					args: args,
					env: env
				};
			}
		
			Logger.log(`Launching debug adapter with: ${JSON.stringify(launchInfo)}`);
		
			return launchInfo;
		})();
	}

	provideDebugConfigurations?(folder: vscode.WorkspaceFolder | undefined, token?: vscode.CancellationToken): vscode.ProviderResult<vscode.DebugConfiguration[]> {

		let configs: ISolidityMeadowDebugConfig[] = [
			{
				type: "solidity",
				request: "launch",
				name: "Debug Solidity"
			}];

		return configs;
	}

	/**
	 * Massage a debug configuration just before a debug session is being launched,
	 * e.g. add all missing attributes to the debug configuration.
	 */
	resolveDebugConfiguration?(folder: vscode.WorkspaceFolder | undefined, config: vscode.DebugConfiguration, token?: vscode.CancellationToken): vscode.ProviderResult<vscode.DebugConfiguration> {
		
		return (async () => {

			Logger.log(`Resolving debug configuration: ${JSON.stringify(config)}`);

			common.validateDotnetVersion();

			let debugConfig = <ISolidityMeadowDebugConfig>config;
			
			if (vscode.workspace.workspaceFolders && vscode.workspace.workspaceFolders.length > 0) {
				debugConfig.workspaceDirectory = vscode.workspace.workspaceFolders[0].uri.fsPath;
			}

			// if launch.json is missing or empty
			// setup for single file debugging
			if (!debugConfig.type && !debugConfig.request && !debugConfig.name) {
				const editor = vscode.window.activeTextEditor;
				if (editor && editor.document.languageId === 'solidity') {

					if (editor.document.isUntitled) {
						Logger.log("File is unsaved.");
						throw new Error("Cannot debug unsaved Solidity file.");
					}

					if (editor.document.uri.scheme !== 'file') {
						Logger.log(`Unsupported file path: ${editor.document.uri}`);
						throw new Error(`Cannot debug Solidity file with unsupported path scheme: ${editor.document.uri}`);
					}

					if (editor.document.isDirty) {
						Logger.log(`Saving file ${editor.document.uri.fsPath}`)
						await editor.document.save();
					}

					debugConfig.type = 'solidity';
					debugConfig.name = 'Launch';
					debugConfig.request = 'launch';
					debugConfig.singleFile = editor.document.uri.fsPath;
				}
			}

			if (debugConfig.workspaceDirectory) {
				let pathKeys =  ['debugAdapterFile', 'testAssembly', 'logFile'];
				common.expandConfigPath(debugConfig.workspaceDirectory, debugConfig, pathKeys);
			}
		
			Logger.log(`Using debug configuration: ${JSON.stringify(debugConfig)}`);

			return debugConfig;
		})();
	}

	dispose() {

	}
}