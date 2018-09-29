import * as vscode from 'vscode';

export const SOLIDITY_MEADOW_TYPE: string = 'solidityMeadow';

export interface ISolidityMeadowDebugConfig extends vscode.DebugConfiguration {
	readonly stopOnEntry?: boolean;
	readonly debugAdapterFile?: string;
	readonly externalTestAssembly?: string;
	readonly testAssembly?: string;
	readonly logFile?: string;
	readonly trace?: boolean;
	readonly breakDebugAdapter?: boolean;
	readonly breakDebugServer?: boolean;
	readonly withoutSolidityDebugging?: boolean;
}

export interface IDebugAdapterExecutable {
	readonly type?: 'executable';
	readonly command: string;
	readonly args: string[];
	readonly env?: { [key: string]: string };
	readonly cwd?: string;
}