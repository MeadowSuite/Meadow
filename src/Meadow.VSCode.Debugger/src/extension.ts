import * as vscode from 'vscode';
import { MeadowTestsDebugConfigProvider, MeadowTestsDebugAdapterDescriptorFactory } from './meadowTestsDebugConfigProvider';
import { Logger } from './logger';
import { SOLIDITY_MEADOW_TYPE } from './constants';
import { ClrDebugConfigProvider } from './clrDebugConfigProvider';
import * as common from './common';
import { SolidityDebugConfigProvider, SolidityDebugAdapterDescriptorFactory } from './solidityDebugConfigProvider';


export function activate(context: vscode.ExtensionContext) {

	Logger.log("Extension activation.");
	common.setExtensionPath(context.extensionPath);

	const solidityDebugProvider = new SolidityDebugConfigProvider(context);
	context.subscriptions.push(vscode.debug.registerDebugConfigurationProvider("solidity", solidityDebugProvider));
	context.subscriptions.push(solidityDebugProvider);
	const solidityDebugAdapterDescriptionFactory = new SolidityDebugAdapterDescriptorFactory(context);
	context.subscriptions.push(vscode.debug.registerDebugAdapterDescriptorFactory("solidity", solidityDebugAdapterDescriptionFactory));
	context.subscriptions.push(solidityDebugAdapterDescriptionFactory);

	// register a configuration provider for the debug type
	const meadowTestDebugProvider = new MeadowTestsDebugConfigProvider(context);
	context.subscriptions.push(vscode.debug.registerDebugConfigurationProvider(SOLIDITY_MEADOW_TYPE, meadowTestDebugProvider));
	context.subscriptions.push(meadowTestDebugProvider);
	const meadowTestDebugAdapterDescriptionFactory = new MeadowTestsDebugAdapterDescriptorFactory(context);
	context.subscriptions.push(vscode.debug.registerDebugAdapterDescriptorFactory(SOLIDITY_MEADOW_TYPE, meadowTestDebugAdapterDescriptionFactory));
	context.subscriptions.push(meadowTestDebugAdapterDescriptionFactory);

	// Register config provider for coreclr / omnisharp to hook in our solidity debugger.
	const coreClrProvider = new ClrDebugConfigProvider(context);
	context.subscriptions.push(vscode.debug.registerDebugConfigurationProvider("coreclr", coreClrProvider));
}



export function deactivate() {

}
