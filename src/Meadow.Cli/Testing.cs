using Meadow.Contract;
using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Runtime.Serialization;
using System.Management.Automation.Internal;
using System.Globalization;

namespace Meadow.Cli
{

    public delegate string AddDelegate(int num1, int num2);

    public class AdderClass
    {
        public string Add(int num1, int num2)
        {
            return (num1 + num2).ToString(CultureInfo.InvariantCulture);
        }
    }

    [Cmdlet("Test", "Function")]
    [OutputType(typeof(AdderClass))]
    public class TestFunctionCommand : PSCmdlet
    {
        protected override void BeginProcessing()
        {
            System.Console.WriteLine("test begin process");
            base.BeginProcessing();
        }

        protected override void ProcessRecord()
        {
            System.Console.WriteLine("test process record");
            string AddTest(int p1, int p2)
            {
                return (p1 + p2).ToString(CultureInfo.InvariantCulture);
            }

            AddDelegate myDel = AddTest;
            //base.ProcessRecord();
            //WriteObject(myDel);
            WriteObject(new AdderClass());
        }

        protected override void EndProcessing()
        {
            System.Console.WriteLine("test end process");
        }
    }


}
