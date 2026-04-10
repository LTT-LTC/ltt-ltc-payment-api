using System;
using System.Collections.Generic;
using System.Text;

namespace LTC.Shared.CrossCuttingConcerns.ExtensionMethods
{
    public static class CommonExtensions
    {
        public static string GetValidateMessage(string messageKey, params object[] messageParams)
        {
            var validateMessage = string.Format(messageKey, messageParams);
            return validateMessage;
        }
    }
}
