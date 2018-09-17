using Meadow.Core.EthTypes;
using Meadow.JsonRpc.Types;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Meadow.Contract
{
    public static class EventLogUtil
    {

        /// <summary>
        /// Finds matching event object types for the event signatures in a given transaction receipt.
        /// </summary>
        /// <param name="receipt"></param>
        /// <returns></returns>
        static Type[] GetEventTypes(TransactionReceipt receipt)
        {
            var eventTypes = new List<Type>();
            foreach (var log in receipt.Logs)
            {
                var type = FindEventType(log.Topics[0].GetHexString(hexPrefix: false), log);
                if (type != null)
                {
                    eventTypes.Add(type);
                }
            }

            return eventTypes.ToArray();
        }

        /// <summary>
        /// Returns all event logs that can be matched to an event type. Unmatched logs are left out of the result.
        /// </summary>
        public static EventLog[] EventLogs(this TransactionReceipt receipt)
        {
            var eventLogs = new List<EventLog>();
            foreach (var log in receipt.Logs)
            {
                var parsed = Parse(log.Topics[0].GetHexString(hexPrefix: false), log);
                if (parsed != null)
                {
                    eventLogs.Add(parsed);
                }
            }

            return eventLogs.ToArray();
        }

        static Exception CreateBadEventTypeException(TransactionReceipt receipt, Type attemptedEventType)
        {
            var availableEvents = string.Join(", ", GetEventTypes(receipt).Select(e => e.Name));
            throw new Exception($"Could not find the {attemptedEventType.Name} event in transaction receipt logs. Available event type(s): {availableEvents}");
        }

        /// <summary>
        /// Returns the first (oldest) event log of the given type.
        /// </summary>
        /// <returns>Throws exception if not found.</returns>
        public static TEventLog FirstEventLog<TEventLog>(this TransactionReceipt receipt) where TEventLog : EventLog
        {
            var eventLog = FirstOrDefaultEventLog<TEventLog>(receipt);
            if (eventLog != null)
            {
                return eventLog;
            }

            throw CreateBadEventTypeException(receipt, typeof(TEventLog));
        }

        /// <summary>
        /// Returns the first (oldest) event log of the given type.
        /// </summary>
        /// <returns>Return null if no matching event is found.</returns>
        public static TEventLog FirstOrDefaultEventLog<TEventLog>(this TransactionReceipt receipt) where TEventLog : EventLog
        {
            foreach (var log in receipt.Logs)
            {
                if (log.TryParse<TEventLog>(out var eventLog))
                {
                    return eventLog;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the last (newest) event log of the given type.
        /// </summary>
        /// <returns>Throws exception if not found.</returns>
        public static TEventLog LastEventLog<TEventLog>(this TransactionReceipt receipt) where TEventLog : EventLog
        {
            var eventLog = LastOrDefaultEventLog<TEventLog>(receipt);
            if (eventLog != null)
            {
                return eventLog;
            }

            throw CreateBadEventTypeException(receipt, typeof(TEventLog));
        }

        /// <summary>
        /// Returns the last (newest) event log of the given type.
        /// </summary>
        /// <returns>Returns null if not matching event is found.</returns>
        public static TEventLog LastOrDefaultEventLog<TEventLog>(this TransactionReceipt receipt) where TEventLog : EventLog
        {
            foreach (var log in receipt.Logs.Reverse())
            {
                if (log.TryParse<TEventLog>(out var eventLog))
                {
                    return eventLog;
                }
            }

            return null;
        }

        /// <summary>
        /// Searches the event logs for any that match the given event type.
        /// </summary>
        /// <returns>Returns an empty array if no matching events are found.</returns>
        public static TEventLog[] EventLogs<TEventLog>(this TransactionReceipt receipt) where TEventLog : EventLog
        {
            var eventLogs = new List<TEventLog>();
            foreach (var log in receipt.Logs)
            {
                if (log.TryParse<TEventLog>(out var eventLog))
                {
                    eventLogs.Add(eventLog);
                }
            }

            return eventLogs.ToArray();
        }

        public static bool TryParse<TEventLog>(this FilterLogObject log, out TEventLog eventLog)
            where TEventLog : EventLog
        {
            var sig = GetEventLogTypeSignature<TEventLog>();
            if (sig == null || sig != log.Topics[0].GetHexString(hexPrefix: false))
            {
                eventLog = null;
                return false;
            }

            eventLog = (TEventLog)Activator.CreateInstance(typeof(TEventLog), log);
            return true;
        }

        /// <param name="eventType">Must be a <see cref="EventLog"/> type with a matching event signature attribute.</param>
        public static bool TryParse(this FilterLogObject log, Type eventType, out EventLog eventLog)
        {
            var sig = GetEventLogTypeSignature(eventType);
            if (sig == null || sig != log.Topics[0].GetHexString(hexPrefix: false))
            {
                eventLog = null;
                return false;
            }

            eventLog = (EventLog)Activator.CreateInstance(eventType, log);
            return true;
        }

        static ConcurrentDictionary<Type, string> _eventTypeSignatureCache = new ConcurrentDictionary<Type, string>();

        /// <summary>
        /// Gets the <see cref="EventSignatureAttribute"/> signature on a <see cref="EventLog"/> type.
        /// </summary>
        public static string GetEventLogTypeSignature(Type eventType)
        {
            return _eventTypeSignatureCache.GetOrAdd(eventType, _ => eventType.GetCustomAttribute<EventSignatureAttribute>()?.Signature);
        }

        public static string GetEventLogTypeSignature<TEventLog>() where TEventLog : EventLog
        {
            return GetEventLogTypeSignature(typeof(TEventLog));
        }

        static readonly ConcurrentDictionary<(string ContractAddress, string EventSignatureHash), Type> _deployedContractEventTypes
            = new ConcurrentDictionary<(string ContractAddress, string EventSignatureHash), Type>();



        /// <summary>
        /// Registers a contract's deployed address and its containing EventLog class types 
        /// so that event log data can be looked up and parsed.
        /// </summary>
        /// <param name="deployedContractAddress">The address of the deployed contract.</param>
        /// <param name="eventTypes">The event class types deriving from <see cref="EventLog"/></param>
        public static void RegisterDeployedContractEventTypes(Address deployedContractAddress, params Type[] eventTypes)
        {
            string addr = deployedContractAddress.ToString(hexPrefix: false);
            foreach (var eventType in eventTypes)
            {
                var entryKey = (addr, GetEventLogTypeSignature(eventType));
                _deployedContractEventTypes.AddOrUpdate(entryKey, eventType, (_, x_) => eventType);
            }
        }

        /// <summary>
        /// Key is event signature hash, value is event class type
        /// </summary>
        static readonly ConcurrentDictionary<string, Type> _reflectedEventTypes = new ConcurrentDictionary<string, Type>();

        static bool _attemptedEventTypeReflection = false;

        static Type FindEventType(string eventSignatureHash, FilterLogObject log)
        {
            // Check if the event signature matches an event defined in the contract that emitted it.
            var logContractAddr = log.Address.ToString(hexPrefix: true);
            var entryKey = (logContractAddr, eventSignatureHash);
            if (_deployedContractEventTypes.TryGetValue(entryKey, out var type))
            {
                return type;
            }

            // Check other contracts for an event that matches the event signature.
            if (!_attemptedEventTypeReflection)
            {
                // Get the type of a generated contract type so we can search its assembly for other contract event types
                var existingContractEventType = _deployedContractEventTypes.FirstOrDefault().Value;
                if (existingContractEventType != null)
                {
                    var attrs = existingContractEventType.Assembly
                        .GetTypes()
                        .Select(t => new { Type = t, Attr = t.GetCustomAttribute<EventSignatureAttribute>() });

                    foreach (var attr in attrs)
                    {
                        if (attr.Attr != null)
                        {
                            _reflectedEventTypes.TryAdd(attr.Attr.Signature, attr.Type);
                        }
                    }
                }

                _attemptedEventTypeReflection = true;
            }

            if (_reflectedEventTypes.TryGetValue(eventSignatureHash, out var eventType))
            {
                return eventType;
            }

            // No matching event type found.
            return null;
        }

        /// <summary>
        /// First checks if the log's address corresponds to a deployed contract address that is
        /// registered using <see cref="RegisterDeployedContractEventTypes"/>.
        /// The constructor in generated contracts automatically register their deployed address
        /// and event class types. 
        /// Otherwise uses reflection to look through other contract event types for a matching
        /// signature.
        /// </summary>
        /// <returns>
        /// Returns null if the signature could not be matched to a known event type.
        /// </returns>
        public static EventLog Parse(string eventSignatureHash, FilterLogObject log)
        {
            var eventType = FindEventType(eventSignatureHash, log);
            if (eventType == null)
            {
                return null;
            }

            return (EventLog)Activator.CreateInstance(eventType, log);
        }

    }
}
