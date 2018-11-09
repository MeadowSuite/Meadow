import * as vscode from 'vscode';
import { MeadowTestsDebugConfigProvider } from './meadowTestsDebugConfigProvider';
import { Logger } from './logger';
import { resolveMeadowDebugAdapter } from './meadowDebugAdapter';
import { SOLIDITY_MEADOW_TYPE } from './constants';
import { ClrDebugConfigProvider } from './clrDebugConfigProvider';
import * as common from './common';
import * as debugSolSourcesTool from './debugSolSourcesTool';
import { SolidityDebugConfigProvider } from './solidityDebugConfigProvider';


export function activate(context: vscode.ExtensionContext) {

	Logger.log("Extension activation.");
	common.setExtensionPath(context.extensionPath);

	/*
	vscode.debug.onDidChangeActiveDebugSession(e => {
		Logger.log("onDidChangeActiveDebugSession", e);
	});

	vscode.debug.onDidTerminateDebugSession(e => {
		Logger.log("onDidTerminateDebugSession", e);
	});

	vscode.debug.onDidReceiveDebugSessionCustomEvent(e => {
		Logger.log("custom event", e);
	});

	context.subscriptions.push(vscode.debug.onDidStartDebugSession(async (e) => {
		Logger.log("onDidStartDebugSession", e);
	}));
	*/


	const solidityDebugProvider = new SolidityDebugConfigProvider(context);
	context.subscriptions.push(vscode.debug.registerDebugConfigurationProvider("solidity", solidityDebugProvider));
	context.subscriptions.push(solidityDebugProvider);

	// register a configuration provider for the debug type
	const meadowTestDebugProvider = new MeadowTestsDebugConfigProvider(context);
	context.subscriptions.push(vscode.debug.registerDebugConfigurationProvider(SOLIDITY_MEADOW_TYPE, meadowTestDebugProvider));
	context.subscriptions.push(meadowTestDebugProvider);

	// Register config provider for coreclr / omnisharp to hook in our solidity debugger.
	const coreClrProvider = new ClrDebugConfigProvider(context);
	context.subscriptions.push(vscode.debug.registerDebugConfigurationProvider("coreclr", coreClrProvider));
}



export function deactivate() {
	// nothing to do
}
