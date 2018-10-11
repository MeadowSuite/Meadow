import * as vscode from 'vscode';
import { DEBUG_SESSION_ID, SOLIDITY_MEADOW_TYPE } from './constants';
import * as uuid from 'uuid/v1';
import { Logger } from './logger';
import * as common from './common';

export class ClrDebugConfigProvider implements vscode.DebugConfigurationProvider {

	private _context: vscode.ExtensionContext;

	constructor(context: vscode.ExtensionContext) {
		this._context = context;
	}

	/**
	 * Provides initial [debug configuration](#DebugConfiguration). If more than one debug configuration provider is
	 * registered for the same type, debug configurations are concatenated in arbitrary order.
	 *
	 * @param folder The workspace folder for which the configurations are used or undefined for a folderless setup.
	 * @param token A cancellation token.
	 * @return An array of [debug configurations](#DebugConfiguration).
	 */
	provideDebugConfigurations?(folder: vscode.WorkspaceFolder | undefined, token?: vscode.CancellationToken): vscode.ProviderResult<vscode.DebugConfiguration[]> {
        // We have nothing to add here. Implemented to just fulfill the interface.
        return [];
	}

	/**
	 * Resolves a [debug configuration](#DebugConfiguration) by filling in missing values or by adding/changing/removing attributes.
	 * If more than one debug configuration provider is registered for the same type, the resolveDebugConfiguration calls are chained
	 * in arbitrary order and the initial debug configuration is piped through the chain.
	 * Returning the value 'undefined' prevents the debug session from starting.
	 *
	 * @param folder The workspace folder from which the configuration originates from or undefined for a folderless setup.
	 * @param debugConfiguration The [debug configuration](#DebugConfiguration) to resolve.
	 * @param token A cancellation token.
	 * @return The resolved debug configuration or undefined.
	 */
	resolveDebugConfiguration?(folder: vscode.WorkspaceFolder | undefined, debugConfiguration: vscode.DebugConfiguration, token?: vscode.CancellationToken): vscode.ProviderResult<vscode.DebugConfiguration> {
        
        // Check if the solidity debugger has already been launched and set the session ID
        if (debugConfiguration.env && debugConfiguration.env[DEBUG_SESSION_ID]) {
			Logger.log(`Clr debugger already setup with solidity debug session ID.`);
            return debugConfiguration;
        }
        
        let config : any = debugConfiguration;
		if (!config.env) {
			config.env = {};
		}

		let debugSessionID = uuid();
		config.env[DEBUG_SESSION_ID] = debugSessionID;

        let workspaceFolder = common.getWorkspaceFolder();

        let solDebugConfig: vscode.DebugConfiguration = {
            name: "Solidity Debugger",
            type: SOLIDITY_MEADOW_TYPE,
            request: "launch",
            cwd: workspaceFolder.uri.fsPath,
            [DEBUG_SESSION_ID]: debugSessionID
        };

		setTimeout(() => {
			(async () => {
				Logger.log(`Launching solidity debugger for: ${JSON.stringify(solDebugConfig)}`);
				let startSolDebugResult = await vscode.debug.startDebugging(workspaceFolder, solDebugConfig);
				console.log("Sol debugger result: " + startSolDebugResult);
			})();
		}, 1);

		Logger.log(`Using clr debug config with sol debug session ID ${JSON.stringify(config)}`);
		return config;
	}

}