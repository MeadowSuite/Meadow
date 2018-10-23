using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Meadow.Contract
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class GeneratedSolcDataAttribute : Attribute
    {
        public readonly string SolCodeBaseHash;

        public GeneratedSolcDataAttribute(string solCodeBaseHash)
        {
            SolCodeBaseHash = solCodeBaseHash;
        }


    }

}
