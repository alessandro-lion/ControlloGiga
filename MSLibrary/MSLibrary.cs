using System;
using System.Threading.Tasks;
using Xamarin.Essentials;

//Android App Manage Settings Library
//2020 Created By Alex "Pi" Lion, for more info ask at https://www.linkedin.com/in/alessandrolion
namespace MSLibrary
{
    public static class ManageSettingsLibrary
    {
        public static async Task<String> GetStoredParmValueAsync(String strParmName)
        {
            try
            {
                String oauthToken = await SecureStorage.GetAsync(strParmName);
                return oauthToken;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("FAILED GET " + strParmName);
                System.Diagnostics.Debug.WriteLine(ex.Message);
                // Possible that device doesn't support secure storage on device.
                return "";
            }

        }
        public static async Task<bool> SetStoredParmValueAsync(String strParmName, String strParVal)
        {
            try
            {
                await SecureStorage.SetAsync(strParmName, strParVal);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("FAILED SET " + strParmName);
                System.Diagnostics.Debug.WriteLine(ex.Message);
                // Possible that device doesn't support secure storage on device.
                return false;
            }
        }

    }
}