using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Meadow.SolCodeGen
{
    static class LegacySolcNet
    {
        public static string ResolveNativeLibPath(string solcNetLegacyLibFile, Version solcVersion)
        {
            Assembly solcNetLegacyAssembly;
            try
            {
                solcNetLegacyAssembly = Assembly.LoadFile(solcNetLegacyLibFile);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to load SolcNet.Legacy assembly from file: " + solcNetLegacyLibFile, ex);
            }

            Type resolverClassType;
            const string SOLCNET_LEGACY_LIBPATH = "SolcNet.Legacy.LibPath";
            try
            {
                resolverClassType = solcNetLegacyAssembly.GetType(SOLCNET_LEGACY_LIBPATH, throwOnError: true, ignoreCase: false);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load class {SOLCNET_LEGACY_LIBPATH} from SolcNet.Legacy", ex);
            }

            const string GET_LIB_PATH = "GetLibPath";
            MethodInfo getLibPathMethod;

            try
            {
                getLibPathMethod = resolverClassType.GetMethod(GET_LIB_PATH, new[] { typeof(Version) });
                if (getLibPathMethod == null)
                {
                    throw new Exception("Null GetMethod result");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to find method {GET_LIB_PATH} in SolcNet.Legacy", ex);
            }

            string nativeLib;

            try
            {
                nativeLib = (string)getLibPathMethod.Invoke(null, new object[] { solcVersion });
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception when invoking {GET_LIB_PATH}", ex);
            }

            return nativeLib;
        }
    }
}
