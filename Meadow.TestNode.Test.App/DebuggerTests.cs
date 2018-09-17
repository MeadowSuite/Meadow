using Meadow.CoverageReport.Debugging;
using Meadow.JsonRpc.Client;
using Meadow.JsonRpc.Types;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Meadow.TestNode.Test.App
{
    public abstract class DebuggerTests
    {
        /*
        public static async Task Run(IJsonRpcClient client)
        {
            // Deploy our variable analysis test contract.
            var varContract = await VarAnalysisContract.New(client, new TransactionParams { Gas = 4712388 });

            // Update our values 7 times.
            for (int i = 0; i < 7; i++)
            {
                await varContract.updateStateValues();
            }

            // Throw an exception in a function call (so we can simply check locals on latest execution point, since we have no front end/UI to choose an execution point at the time of writing this).
            await varContract.throwWithLocals().ExpectRevertTransaction();

            // Obtain an execution trace and parse locals from it.
            var executionTrace = await client.GetExecutionTrace();
            ExecutionTraceAnalysis traceAnalysis = new ExecutionTraceAnalysis(executionTrace);

            // Obtain our locals.
            var locals = traceAnalysis.GetLocalVariables();

            // TODO: Obtain state variables, our global uint should have a value of 7.

            //string x = "test";
            //x = "setABreakpointAroundTheseUselessLinesTest";
        }
        */
    }
}
