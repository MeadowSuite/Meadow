using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Meadow.EVM.Data_Types.State
{
    public class StateSnapshot
    {
        #region Properties
        public Dictionary<PropertyInfo, object> Properties { get; private set; }
        public byte[] StateRootHash { get; private set; }
        #endregion

        #region Constructor
        public StateSnapshot(State state)
        {
            // Obtain every property of the state class.
            PropertyInfo[] propertyInfos = state.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Create a dictionary for our snapshot.
            Properties = new Dictionary<PropertyInfo, object>();

            // For every property we have, we store it in our state values
            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                // Set our property value.
                Properties[propertyInfo] = CloneObject(propertyInfo.GetValue(state));
            }

            // We'll also back up our state root
            StateRootHash = state.Trie.GetRootNodeHash();
        }
        #endregion

        #region Functions
        public void Apply(State state)
        {
            // We loop through all of our properties and set it on this state.
            foreach (PropertyInfo propertyInfo in Properties.Keys)
            {
                propertyInfo.SetValue(state, CloneObject(Properties[propertyInfo]));
            }

            // We'll create a new trie and load our root node hash
            state.Trie = new Trees.Trie(state.Trie.Database, StateRootHash);
        }

        private object CloneObject(object obj)
        {
            // If it's null, return null
            if (obj == null)
            {
                return null;
            }

            // If it's a list, we copy it.
            if (obj is IList && obj.GetType().IsGenericType && obj.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>)))
            {
                // Create a new list of the same type.
                IList originalList = ((IList)obj);
                IList copyList = (IList)Activator.CreateInstance(originalList.GetType());

                // Copy all the items from one list to the other.
                for (int i = 0; i < originalList.Count; i++)
                {
                    copyList.Add(CloneObject(originalList[i]));
                }

                // Return our copied list
                return copyList;
            }

            // If it's a dictionary, we copy it.
            if (obj is IDictionary && obj.GetType().IsGenericType && obj.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>)))
            {
                // Create a new dictionary of the same type.
                IDictionary originalDictionary = ((IDictionary)obj);
                IDictionary copyDictionary = ((IDictionary)Activator.CreateInstance(originalDictionary.GetType()));

                // Copy all of the items from one dictionary to the other.
                foreach (object key in originalDictionary.Keys)
                {
                    copyDictionary[key] = CloneObject(originalDictionary[key]);
                }

                // Return our copied
                return copyDictionary;
            }

            // If this is a cloneable object, clone it.
            if (obj is ICloneable)
            {
                return ((ICloneable)obj).Clone();
            }


            // Return the object.
            return obj;
        }

        public State ToState()
        {
            // Create a new state
            State state = new State();

            // Revert using this snapshot
            state.Revert(this);

            // Return the state
            return state;
        }
        #endregion
    }
}
