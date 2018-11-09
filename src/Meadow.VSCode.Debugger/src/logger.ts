import * as vscode from "vscode";
import * as util from 'util';

export class Logger {

    private static outputChannel : vscode.OutputChannel = vscode.window.createOutputChannel("Meadow Solidity Debugger");

    private static _didShow: boolean = false;

    public static log(message: string, ...params: any[]): void {
        if (!this._didShow) {
            this._didShow = true;
            this.show();
        }
        this.outputChannel.appendLine(message);
        params.forEach(p => this.outputChannel.appendLine(util.inspect(p)));

        if (this._isDebugging) {
            console.log(message);
            params.forEach(p => console.log(p));
        }
    }

    public static show(): void {
        this.outputChannel.show();
    }

    private static _isDebugging: boolean;

    static init() {
        try {
            const isDebuggingRegex = /^--inspect(-brk)?=?/;
            const args = process.execArgv;
            this._isDebugging = args ? args.some(arg => isDebuggingRegex.test(arg)) : false;
        } catch { }
    }
}

Logger.init();