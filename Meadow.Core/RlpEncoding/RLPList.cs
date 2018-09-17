using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.Core.RlpEncoding
{
    /// <summary>
    /// Represents a list which can be RLP serialized.
    /// </summary>
    public class RLPList : RLPItem
    {
        #region Properties
        /// <summary>
        /// The embedded item list inside of this RLP item.
        /// </summary>
        public List<RLPItem> Items { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public RLPList()
        {
            Items = new List<RLPItem>();
        }

        /// <summary>
        /// Initializes the RLP list using the provided list as the internal list.
        /// </summary>
        /// <param name="items">Initializes the RLP list with the given internal item list.</param>
        public RLPList(List<RLPItem> items)
        {
            Items = items;
        }

        /// <summary>
        /// Initializes the RLP list using the provided list as the internal list.
        /// </summary>
        /// <param name="items">Initializes the RLP list with the given internal item list.</param>
        public RLPList(params RLPItem[] items)
        {
            Items = new List<RLPItem>(items);
        }
        #endregion
    }
}
