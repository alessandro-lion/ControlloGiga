using System;
using Xamarin.Essentials;

namespace ManageSettings
{
	public static class ManageSettingsLibrary
	{
		public static string GetStoredParmValue(this String strParmName)
		{
			try
			{
				String oauthToken = await SecureStorage.GetAsync(strParmName);
			}
			catch (Exception ex)
			{
				// Possible that device doesn't support secure storage on device.
				return "";
			}
			finally
            {
				return oauthToken;
			}
			
		}
		public static bool SetStoredParmValue(this String strParmName, String strParVal)
		{
			try
			{
				await SecureStorage.SetAsync(strParmName, strParVal);
			}
			catch (Exception ex)
			{
				// Possible that device doesn't support secure storage on device.
				return false;
			}
			finally
			{
				return true;
			}

		}
	}
}