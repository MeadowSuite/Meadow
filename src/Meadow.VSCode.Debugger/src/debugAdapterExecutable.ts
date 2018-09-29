import * as vscode from 'vscode';
import * as fs from 'fs';
import * as path from 'path';
import { IDebugAdapterExecutable, ISolidityMeadowDebugConfig } from './constants';
import { Logger } from './logger';

export async function resolveMeadowDebugAdapter(context: vscode.ExtensionContext, debugSessionID: string, debugConfig?: ISolidityMeadowDebugConfig): Promise<IDebugAdapterExecutable> {

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
	else if (context["logPath"]) {
		args.push("--log_file", context["logPath"]);
	}

	if (debugConfig && debugConfig.trace) {
		args.push("--trace");
	}

	if (debugConfig && debugConfig.breakDebugAdapter) {
		args.push("--attach_debugger");
	}

	args.push("--session", debugSessionID);

	return {
		command: "dotnet",
		args: args,
		env: { "DEBUG_SESSION_ID": debugSessionID }
	}
}