using Meadow.CoverageReport.AstTypes;
using Meadow.CoverageReport.AstTypes.Enums;
using Meadow.CoverageReport.Debugging.Variables.Enums;
using Meadow.CoverageReport.Debugging.Variables.Storage;
using Meadow.CoverageReport.Debugging.Variables.UnderlyingTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.CoverageReport.Debugging.Variables
{
    /// <summary>
    /// Represents a state variable derived from certain execution state components from execution traces/runtime.
    /// A state variable can be understood as a variable which is operated on in persistent storage,
    /// altering the state of storage, and the overall Ethereum state.
    /// </summary>
    public class StateVariable : BaseVariable
    {
        #region Properties
        /// <summary>
        /// Represents a location/pointer to access data in storage where this variable can be resolved at.
        /// </summary>
        public StorageLocation StorageLocation { get; set; }
        /// <summary>
        /// Represents the variable's underlying data location in less trivial cases.
        /// In the case of a state variable, the default value refers to storage.
        /// </summary>
        public override VarLocation VariableLocation
        {
            get
            {
                // Obtain our base value
                AstVariableStorageLocation storageLoc = Declaration?.StorageLocation ?? AstVariableStorageLocation.Default;

                // If our location is stated as the default location, we return our specific values.
                switch (storageLoc)
                {
                    case AstVariableStorageLocation.Memory:
                        return VarLocation.Memory;
                    case AstVariableStorageLocation.Storage:
                        return VarLocation.Storage;
                    case AstVariableStorageLocation.Default:
                    default:
                        return VarLocation.Storage;
                }
            }
        }
        #endregion

        #region Constructor
        public StateVariable(AstVariableDeclaration declaration)
        {
            // Initialize by declaration
            Initialize(declaration);
        }

        public StateVariable(string name, AstElementaryTypeName astTypeName)
        {
            // Initialize by name and type
            Initialize(name, astTypeName);
        }

        public StateVariable(string name, string typeString)
        {
            // Initialize by name and type
            Initialize(name, typeString);
        }
        #endregion
    }
}
