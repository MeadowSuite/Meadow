import * as vscode from 'vscode';
import * as fs from 'fs';
import * as path from 'path';
import { IDebugAdapterExecutable, ISolidityMeadowDebugConfig } from './constants';
import { Logger } from './logger';
import { DEBUG_SESSION_ID, SOLIDITY_MEADOW_TYPE } from './constants';

export async function resolveMeadowDebugAdapter(context: vscode.ExtensionContext, debugSessionID: string, debugConfig?: ISolidityMeadowDebugConfig): Promise<IDebugAdapterExecutable> {

	Logger.log("Resolving debug adapter execution info.");
	
	let debugServerFilePath: string;

	if (debugConfig && debugConfig.debugAdapterFile) {
		debugServerFilePath = debugConfig.debugAdapterFile;
		if (!fs.existsSync(debugServerFilePath)) {
			throw new Error(`The "debugAdapterFile" in launch.json does not exist at "${debugServerFilePath}`);
		}
	}
	else {
        debugServerFilePath = path.resolve(path.join(context.extensionPath, "out", "debug_adapter", "Meadow.DebugAdapterProxy.dll"));
        if (!fs.existsSync(debugServerFilePath)) {
			throw new Error(`The debug adapter proxy executable not found at "${debugServerFilePath}`);
		}
	}
    
    let args: string[] = [debugServerFilePath, "--vscode_debug"];

	if (debugConfig && debugConfig.logFile) {
		args.push("--log_file", debugConfig.logFile);
	}

	if (debugConfig && debugConfig.trace) {
		args.push("--trace");
	}

	if (debugConfig && debugConfig.breakDebugAdapter) {
		args.push("--attach_debugger");
	}

	args.push("--session", debugSessionID);

	let launchInfo : IDebugAdapterExecutable = {
		type: "executable",
		command: "dotnet",
		args: args,
		env: { [DEBUG_SESSION_ID]: debugSessionID }
	};

	Logger.log(`Launching debug adapter with: ${JSON.stringify(launchInfo)}`);

	return launchInfo;
}