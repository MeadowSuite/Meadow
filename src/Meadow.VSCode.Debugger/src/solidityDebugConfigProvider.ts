import * as vscode from 'vscode';
import * as Net from 'net';
import * as fs from 'fs';
import * as path from 'path';
import * as uuid from 'uuid/v1';
import * as child_process from "child_process";
import { Logger } from './logger';
import { ISolidityMeadowDebugConfig, IDebugAdapterExecutable, DEBUG_SESSION_ID, SOLIDITY_MEADOW_TYPE } from './constants';
import { resolveMeadowDebugAdapter } from './meadowDebugAdapter';
import * as common from './common';
import { debug } from 'util';


export class SolidityDebugConfigProvider implements vscode.DebugConfigurationProvider {

	private _server?: Net.Server;

	private _context: vscode.ExtensionContext;

	constructor(context: vscode.ExtensionContext) {
		this._context = context;
	}

	// Notice: this is working in latest stable vscode but is preview.
	provideDebugAdapter?(session: vscode.DebugSession, folder: vscode.WorkspaceFolder | undefined, executable: IDebugAdapterExecutable | undefined, config: vscode.DebugConfiguration, token?: vscode.CancellationToken): vscode.ProviderResult<IDebugAdapterExecutable> {
		
		let debugConfig = <ISolidityMeadowDebugConfig>config;

		let debugSessionID: string;

		if (debugConfig.debugSessionID) {
			debugSessionID = debugConfig.debugSessionID;
		}
		else {
			debugSessionID = debugConfig.debugSessionID = uuid();
		}

		// TODO: get this from debugSolSourcesTool..
		let testAssembly = "C:/Users/matt/Projects/Meadow/src/Meadow.DebugSolSources/bin/Debug/netcoreapp2.1/Meadow.DebugSolSources.dll";
		let args = [ testAssembly ];

		if (debugConfig.workspaceDirectory) {
			args.push("--directory", debugConfig.workspaceDirectory);
		}

		if (debugConfig.entryPoint) {
			args.push("--entry", debugConfig.entryPoint);
		}

		if (debugConfig.singleFile) {
			args.push("--singleFile", debugConfig.singleFile)
		}

		let opts : child_process.ExecFileOptions = { 
			env: {
				[DEBUG_SESSION_ID]: debugSessionID
			}
		};

		if (debugConfig.breakDebugServer) {
			opts.env["DEBUG_STOP_ON_ENTRY"] = "true";
		}

		child_process.execFile("dotnet", args, opts, (err, stdout, stderr) => {
			Logger.log("Meadow.DebugSolSources execution finished.");
			if (stderr) {
				Logger.log(stderr);
			}
        });

		return resolveMeadowDebugAdapter(this._context, debugSessionID, config);
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
				for (let pathProp of ['debugAdapterFile', 'testAssembly', 'logFile']) {
					let pathItem = debugConfig[pathProp];
					if (pathItem) {
						pathItem = pathItem.replace('${workspaceFolder}', debugConfig.workspaceDirectory);
						if (!path.isAbsolute(pathItem)) {
							pathItem = path.resolve(debugConfig.workspaceDirectory, pathItem);
						}
						else {
							pathItem = path.resolve(pathItem);
						}
						debugConfig[pathProp] = pathItem;
					}
				}
			}
		
			Logger.log(`Using debug configuration: ${JSON.stringify(debugConfig)}`);

			return debugConfig;
		})();
	}

	dispose() {
		if (this._server) {
			this._server.close();
		}
	}
}