#if MOONPULL_IAP
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Purchasing.Security;

namespace MoonPull.IAP
{
    /// <summary>
    /// Local receipt validation with Unity IAP's CrossPlatformValidator. The obfuscated Tangle classes are generated
    /// into Assembly-CSharp, so they are found by reflection (and preserved via link.xml). Add server-side validation
    /// before scaling spend on UA; local checks stop casual fraud only.
    /// </summary>
    public sealed class ReceiptValidator
    {
        private const string GoogleTangle = "UnityEngine.Purchasing.Security.GooglePlayTangle";
        private const string AppleTangle = "UnityEngine.Purchasing.Security.AppleTangle";

        private readonly CrossPlatformValidator validator;

        public ReceiptValidator()
        {
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            byte[] google = TangleData(GoogleTangle);
            byte[] apple = TangleData(AppleTangle);
            if (google == null && apple == null)
            {
                Debug.LogError("[IAP] Tangle data missing. Run Services > In-App Purchasing > Receipt Validation Obfuscator.");
                return;
            }

            try
            {
                validator = new CrossPlatformValidator(google, apple, Application.identifier);
            }
            catch (NotImplementedException e)
            {
                Debug.LogWarning($"[IAP] Validator unsupported on this store: {e.Message}");
            }
#endif
        }

        /// <summary>True when validation is actually performed (device builds with tangles).</summary>
        public bool IsActive => validator != null;

        public bool IsValid(string receipt, out string error)
        {
            error = string.Empty;
            if (validator == null)
            {
                return true;
            }

            try
            {
                validator.Validate(receipt);
                return true;
            }
            catch (IAPSecurityException e)
            {
                error = e.Message;
                return false;
            }
        }

        private static byte[] TangleData(string typeName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type = assemblies[i].GetType(typeName, false);
                MethodInfo data = type?.GetMethod("Data", BindingFlags.Public | BindingFlags.Static);
                if (data != null)
                {
                    return data.Invoke(null, null) as byte[];
                }
            }

            return null;
        }
    }
}
#endif
