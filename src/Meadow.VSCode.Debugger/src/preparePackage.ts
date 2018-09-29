import * as child_process from "child_process";
import * as util from 'util';
import * as path from 'path';
import * as fs from 'fs';
import * as assert from 'assert';
import * as trash from 'trash';

// Builds the Meadow.DebugAdapterProxy project using "dotnet publish".
// Publishes build output into ./out/debug_adapter for inclusion in vscode extension package.
function buildDebugAdapterProject() {

    let workspace = path.resolve(__dirname, '../');

    assert.ok(
        fs.existsSync(path.join(workspace, 'package.json')),
        `Directory "${workspace}" does not appear to be the workspace directory.`
    );

    // Resolve path to the DebugAdapterProxy dotnet project.
    let adapterProjectPath = path.resolve(workspace, '../Meadow.DebugAdapterProxy');

    // Resolve path to publish output directory.
    let outputDir = path.join(workspace, "out", "debug_adapter");

    console.log(`Building debug adapter project: "${adapterProjectPath}"`);
    console.log(`Publishing build output to: "${outputDir}".`);

    // If publish output directory already exists, move it to trash.
    if (fs.existsSync(outputDir)) {
        console.log(`Clearing build output directory: "${outputDir}"`);
        trash(outputDir);
    }

    // "dotnet" command arguments.
    let runArgs = [
        'publish', adapterProjectPath,
        '--configuration', 'Release',
        '--output', outputDir
    ];

    // "dotnet" process options.
    let runOpts = {
        cwd: workspace
    };

    try {
        // Run dotnet publish process, blocks until process completes.
        let { status, error, stderr, stdout } = child_process.spawnSync('dotnet', runArgs, runOpts);

        if (error) {
            throw error;
        }

        if (status !== 0) {
            if (stderr.length > 0) {
                throw new Error(stderr.toString());
            }
            throw new Error(stdout.toString());
        }

        console.log(stdout.toString());
        console.log('Debug adapter build successful.');
    }
    catch (error) {
        console.error(error);
        console.error('Failed to build debug adapter.');
        process.exit(1);
    }
}

buildDebugAdapterProject();