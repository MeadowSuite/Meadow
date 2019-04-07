using System;
using System.Runtime.InteropServices;

namespace SolcNet.NativeLib
{
    public class Utf8StringMarshaler : ICustomMarshaler
    {
        static readonly Utf8StringMarshaler _instance = new Utf8StringMarshaler();

        public static ICustomMarshaler GetInstance(string cookie) => _instance;

        public void CleanUpManagedData(object ManagedObj) {
        }

        public void CleanUpNativeData(IntPtr pNativeData)
        {
            if (pNativeData != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(pNativeData);
            }
        }

        public int GetNativeDataSize() => -1;

        public IntPtr MarshalManagedToNative(object obj) => EncodingUtils.StringToUtf8((string)obj);

        public object MarshalNativeToManaged(IntPtr ptr) => EncodingUtils.Utf8ToString(ptr);
    }

    public class Utf8StringMarshalerNoCleanup : ICustomMarshaler
    {
        static readonly Utf8StringMarshalerNoCleanup _instance = new Utf8StringMarshalerNoCleanup();

        public static ICustomMarshaler GetInstance(string cookie) => _instance;

        public void CleanUpManagedData(object ManagedObj) { }

        public void CleanUpNativeData(IntPtr pNativeData) { }

        public int GetNativeDataSize() => -1;

        public IntPtr MarshalManagedToNative(object obj) => EncodingUtils.StringToUtf8((string)obj);

        public object MarshalNativeToManaged(IntPtr ptr) => EncodingUtils.Utf8ToString(ptr);
    }

}
