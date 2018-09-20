'use strict';

import * as vscode from 'vscode';
import * as child_process from "child_process";
import * as util from 'util';
import * as debugConfigProvider from './debugConfigProvider';
import { Logger } from './logger';


export function activate(context: vscode.ExtensionContext) {

	// TODO: is it reasonable to support debugging a single .sol file - how would it work with deployments and interaction?

	context.subscriptions.push(vscode.commands.registerCommand('extension.meadow.vscode.debugger.getDebuggerPath', async (config) => {
		let debugServerFilePath = await debugConfigProvider.getMeadowDebugAdapter(context, vscode.env.sessionId);
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
	*/

	vscode.debug.onDidStartDebugSession(async (e) => {
		Logger.log("onDidStartDebugSession", e);
		//if (e.type === debugConfigProvider.SOLIDITY_MEADOW_TYPE) {
		//	let debugSessionID = e.id!;
		//}
	});

	// register a configuration provider for the debug type
	const provider = new debugConfigProvider.SolidityMeadowConfigurationProvider(context);

	context.subscriptions.push(vscode.debug.registerDebugConfigurationProvider(debugConfigProvider.SOLIDITY_MEADOW_TYPE, provider));
	context.subscriptions.push(provider);

}

export function deactivate() {
	// nothing to do
}
