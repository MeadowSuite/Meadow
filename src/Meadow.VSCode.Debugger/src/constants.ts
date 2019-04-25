import * as vscode from 'vscode';

export const SOLIDITY_MEADOW_TYPE: string = 'solidityMeadow';

export const DEBUG_SESSION_ID: string = 'DEBUG_SESSION_ID';

export interface ISolidityMeadowDebugConfig extends vscode.DebugAdapterExecutable {
	readonly stopOnEntry?: boolean;
	readonly debugAdapterFile?: string;
	readonly testAssembly?: string;
	readonly logFile?: string;
	readonly trace?: boolean;
	readonly breakDebugAdapter?: boolean;
	readonly breakDebugServer?: boolean;
	readonly withoutSolidityDebugging?: boolean;
	readonly entryPoint?: string;
	readonly disableUnitTestDebugging?: boolean;
	singleFile?: string;
	debugSessionID?: string;
}

export interface ISolidityMeadowDebugConfig2 extends vscode.DebugConfiguration {
	readonly stopOnEntry?: boolean;
	readonly debugAdapterFile?: string;
	readonly testAssembly?: string;
	readonly logFile?: string;
	readonly trace?: boolean;
	readonly breakDebugAdapter?: boolean;
	readonly breakDebugServer?: boolean;
	readonly withoutSolidityDebugging?: boolean;
	readonly entryPoint?: string;
	singleFile?: string;
	debugSessionID?: string;
}