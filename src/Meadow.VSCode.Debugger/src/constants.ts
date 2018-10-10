import * as vscode from 'vscode';

export const SOLIDITY_MEADOW_TYPE: string = 'solidityMeadow';

export const DEBUG_SESSION_ID: string = 'DEBUG_SESSION_ID';

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
	readonly type: 'executable';

	/**
	 * The command path of the debug adapter executable.
	 * A command must be either an absolute path or the name of an executable looked up via the PATH environment variable.
	 * The special value 'node' will be mapped to VS Code's built-in node runtime.
	 */
	readonly command: string;

	/**
	 * Optional arguments passed to the debug adapter executable.
	 */
	readonly args: string[];

	/**
	 * The additional environment of the executed program or shell. If omitted
	 * the parent process' environment is used. If provided it is merged with
	 * the parent process' environment.
	 */
	readonly env?: { [key: string]: string };

	/**
	 * The working directory for the debug adapter.
	 */
	readonly cwd?: string;
}