import * as vscode from 'vscode';
import * as Net from 'net';
import * as fs from 'fs';
import * as path from 'path';
import * as uuid from 'uuid/v1';
import * as dotnetLaunchDebug from './clrLaunchDebug';
import { Logger } from './logger';
import { ISolidityMeadowDebugConfig, DEBUG_SESSION_ID, SOLIDITY_MEADOW_TYPE, ISolidityMeadowDebugConfig2 } from './constants';
import * as common from './common';

export class MeadowTestsDebugAdapterDescriptorFactory implements vscode.DebugAdapterDescriptorFactory {
	private _context: vscode.ExtensionContext;

	constructor(context: vscode.ExtensionContext) {
		this._context = context;
	}

	createDebugAdapterDescriptor(session: vscode.DebugSession, executable: vscode.DebugAdapterExecutable | undefined): vscode.ProviderResult<vscode.DebugAdapterDescriptor> {
		let debugSessionID: string;
		let config : ISolidityMeadowDebugConfig;
		if (executable) {
			config = executable;
		} else {
			let executableOptions : vscode.DebugAdapterExecutableOptions = {env: {}, cwd: undefined};
			config = new vscode.DebugAdapterExecutable("dotnet", [], executableOptions);
		}

		// Copy configuration variables over.
		if (session.configuration) {
			Object.assign(config, session.configuration);
		}

		if(!config.options || !config.options.env){
			// TODO: Throw error
			return;
		}

		if (config[DEBUG_SESSION_ID]) {
			debugSessionID = config[DEBUG_SESSION_ID];
		}
		else {
			// If the debug session ID is not already set then the CLR debugger has not been launched
			// so we will launch it.
			debugSessionID = uuid();
			dotnetLaunchDebug.launch(debugSessionID, config).catch(err => Logger.log("Error launching dotnet test", err));
		}

		Logger.log("Resolving debug adapter execution info.");
	
		let debugServerFilePath: string;
	
		if (config && config.debugAdapterFile) {
			debugServerFilePath = config.debugAdapterFile;
			if (!fs.existsSync(debugServerFilePath)) {
				throw new Error(`The "debugAdapterFile" in launch.json does not exist at "${debugServerFilePath}`);
			}
		}
		else {
			debugServerFilePath = path.resolve(path.join(this._context.extensionPath, "out", "debug_adapter", "Meadow.DebugAdapterProxy.dll"));
			if (!fs.existsSync(debugServerFilePath)) {
				throw new Error(`The debug adapter proxy executable not found at "${debugServerFilePath}`);
			}
		}
		
		config.args.push(debugServerFilePath);
		config.args.push("--vscode_debug");
	
		if (config && config.logFile) {
			config.args.push("--log_file", config.logFile);
		}
	
		if (config && config.trace) {
			config.args.push("--trace");
		}
	
		if (config && config.breakDebugAdapter) {
			config.args.push("--attach_debugger");
		}
	
		config.args.push("--session", debugSessionID);
		config.options.env = { [DEBUG_SESSION_ID]: debugSessionID }
	
		Logger.log(`Launching debug adapter with: ${JSON.stringify(config)}`);
	
		return config;
	}
	dispose() {

	}
}

export class MeadowTestsDebugConfigProvider implements vscode.DebugConfigurationProvider {

	private _context: vscode.ExtensionContext;

	constructor(context: vscode.ExtensionContext) {
		this._context = context;
	}

	provideDebugConfigurations(folder: vscode.WorkspaceFolder | undefined, token?: vscode.CancellationToken): vscode.ProviderResult<vscode.DebugConfiguration[]> {

		let configs: vscode.DebugConfiguration[] = [
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

		Logger.log(`Resolving debug configuration: ${JSON.stringify(config)}`);

		let debugConfig = <ISolidityMeadowDebugConfig2>config;
		
		if (debugConfig.withoutSolidityDebugging) {
			// TODO: use "dotnet test" to find and built assembly,
			// then return coreclr launch config for program
			throw new Error("TODO..");
		}

		let workspaceRoot = common.getWorkspaceFolder().uri.fsPath;

		common.validateDotnetVersion();

		let checksReady = false;
		if (checksReady) {

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
		
		config.workspaceDirectory = workspaceRoot;

		let pathKeys = ['debugAdapterFile', 'testAssembly', 'logFile'];
		common.expandConfigPath(workspaceRoot, debugConfig, pathKeys);

		Logger.log(`Using debug configuration: ${JSON.stringify(debugConfig)}`);

		return debugConfig;
	}

	dispose() {

	}
}