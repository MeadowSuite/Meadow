import * as vscode from 'vscode';
import * as child_process from "child_process";
import * as util from 'util';
import * as debugConfigProvider from './debugConfigProvider';
import { Logger } from './logger';
import { resolveMeadowDebugAdapter } from './debugAdapterExecutable';
import { SOLIDITY_MEADOW_TYPE } from './constants';


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

}

export function deactivate() {
	// nothing to do
}
