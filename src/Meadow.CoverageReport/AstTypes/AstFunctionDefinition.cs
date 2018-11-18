using Meadow.CoverageReport.AstTypes.Enums;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Meadow.CoverageReport.AstTypes
{
    public class AstFunctionDefinition : AstNode
    {
        #region Properties
        /// <summary>
        /// The name of the function definition. Can be a blank string if this refers to a constructor.
        /// </summary>
        public string Name { get;  }
        /// <summary>
        /// The visibility of this function.
        /// </summary>
        public AstDeclarationVisibility Visibility { get; }
        /// <summary>
        /// Indicates whether the function is a constructor for its parent <see cref="AstContractDefinition"/> or not.
        /// </summary>
        public bool IsConstructor { get; }
        /// <summary>
        /// The variable declarations which represent the function input parameters.
        /// </summary>
        public AstVariableDeclaration[] Parameters { get; }
        /// <summary>
        /// The variable declarations which represent the function return parameters.
        /// </summary>
        public AstVariableDeclaration[] ReturnParameters { get; }
        #endregion

        #region Constructor
        public AstFunctionDefinition(JObject node) : base(node)
        {
            // Set our properties
            Name = node.SelectToken("name")?.Value<string>();
            Visibility = GetVisibilityFromString(node.SelectToken("visibility")?.Value<string>());
            IsConstructor = node.SelectToken("isConstructor")?.Value<bool?>() == true; // Solidity <= 0.4.x
            IsConstructor |= node.SelectToken("kind")?.Value<string>() == "constructor"; // Solidity 0.5.x
            Parameters = node.SelectTokens("parameters.parameters[*]").Select(x => Create<AstVariableDeclaration>((JObject)x)).ToArray();
            ReturnParameters = node.SelectTokens("returnParameters.parameters[*]").Select(x => Create<AstVariableDeclaration>((JObject)x)).ToArray();
        }

        public AstFunctionDefinition(AstNode node) : this(node.Node) { }
        #endregion
    }
}
