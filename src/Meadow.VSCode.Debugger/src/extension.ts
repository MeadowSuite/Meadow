import * as vscode from 'vscode';
import * as debugConfigProvider from './debugConfigProvider';
import { Logger } from './logger';
import { resolveMeadowDebugAdapter } from './debugAdapterExecutable';
import { SOLIDITY_MEADOW_TYPE } from './constants';
import { ClrDebugConfigProvider } from './clrDebugConfigProvider';


export function activate(context: vscode.ExtensionContext) {

	Logger.log("Extension activation.");

	// TODO: is it reasonable to support debugging a single .sol file - how would it work with deployments and interaction?

	context.subscriptions.push(vscode.commands.registerCommand('extension.meadow.vscode.debugger.getDebuggerPath', async (config) => {
		let debugServerFilePath = await resolveMeadowDebugAdapter(context, vscode.env.sessionId);
		return debugServerFilePath;
	}));

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

	// register a configuration provider for the debug type
	const provider = new debugConfigProvider.SolidityMeadowConfigurationProvider(context);
	context.subscriptions.push(vscode.debug.registerDebugConfigurationProvider(SOLIDITY_MEADOW_TYPE, provider));
	context.subscriptions.push(provider);

	// Register config provider for coreclr / omnisharp to hook in our solidity debugger.
	const coreClrProvider = new ClrDebugConfigProvider(context);
	context.subscriptions.push(vscode.debug.registerDebugConfigurationProvider("coreclr", coreClrProvider));
}



export function deactivate() {
	// nothing to do
}
