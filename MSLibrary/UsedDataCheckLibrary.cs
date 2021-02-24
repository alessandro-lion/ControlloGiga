using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Net;

//Iliad Used Data Check Functions Library
//2020 Created By Alex "Pi" Lion, for more info ask at https://www.linkedin.com/in/alessandrolion
namespace PilionUtilities
{
    public static class StringLibrary
    {
        public static bool StartsWithUpper(this String str)
        {
            if (String.IsNullOrWhiteSpace(str))
                return false;

            Char ch = str[0];
            return Char.IsUpper(ch);
        }
    }

    public static class UsedDataCheckLibrary
    {
        public static decimal IliadUsedDataCheck(this String strURL, String strUser, string strPassword, out string strUnit, out decimal decQuotaMax, out string strCurrency, out decimal decCredito, out DateTime dtRenewal, out string strMobileNo)
        {
            //Based on sample at https://stackoverflow.com/questions/930807/login-to-website-via-c-sharp

            //main output parameter
            decimal decRet;

            //Web accesss
            var StrLoginAddress = "https://www.iliad.it/account/";
            if (strURL.Length != 0)
            {
                StrLoginAddress = strURL;
            }
            var ObjLoginData = new NameValueCollection
            {
                  { "login-ident", strUser },
            { "login-pwd", strPassword }
            };
            var ObjClient = new CookieAwareWebClient();
            ObjClient.Login(StrLoginAddress, ObjLoginData);
            string StrRetVal = ObjClient.DownloadString(strURL);
            try
            {
                decRet = IliadExtractUsedFromSource(StrRetVal, out strUnit);
                decQuotaMax = IliadExtractQuotaMaxFromSource(StrRetVal);
                decCredito = IliadExtractCreditLeftFromSource(StrRetVal, out strCurrency);
                dtRenewal = IliadExtractRenewalDateFromSource(StrRetVal);
                strMobileNo = IliadExtractMobileNumberFromSource(StrRetVal);
                //FIX: Converting to mb the quota value in case used is returned as mb
                if (strUnit == "mb")
                {
                    decQuotaMax *= 1024;
                }

            }
            catch
            {
                //ERR
                decRet = -1;
                strUnit = "??";
                decQuotaMax = -1;
                decCredito = -1;
                strCurrency = "?";
                dtRenewal = DateTime.Now;
                strMobileNo = "??";
            }

            return decRet;
        }

        public static decimal IliadUsedDataCheck(this String strURL, String strUser, string strPassword, out string strUnit, out decimal decQuotaMax, out string strCurrency, out decimal decCredito, out DateTime dtRenewal, out string strMobileNo, out int intElapseddays, out int intLeftDays)
        {
            decimal decNestedRet = IliadUsedDataCheck(strURL, strUser, strPassword, out strUnit, out decQuotaMax, out strCurrency, out decCredito, out dtRenewal, out strMobileNo);
            try
            {
                DateTime dtLastDate=dtRenewal.AddMonths(-1);
                TimeSpan sincelast = DateTime.Today - dtLastDate;
                intElapseddays = Convert.ToInt32(sincelast.TotalDays);
                sincelast = dtRenewal - DateTime.Today;
                intLeftDays=Convert.ToInt32(sincelast.TotalDays);
                //if (intElapseddays.Equals(0))
                //    { intElapseddays = intLeftDays; }

                Console.WriteLine(intLeftDays);
            }
            catch
            {
                {
                    //ERR
                    intElapseddays = 0;
                    intLeftDays = 0;
                }
            }
            return decNestedRet;
        }
            private static DateTime IliadExtractRenewalDateFromSource(this String strSourceHTML)
        {
            DateTime dtRet;

            int PositionRenew = strSourceHTML.IndexOf("iliad si rinnoverà alle") + 34;
            int PositionDiv = strSourceHTML.IndexOf("</div>", PositionRenew);

            //string StrRetToInspect = strSourceHTML.Substring(PositionRenew, PositionDiv - PositionRenew);
            string StrRetToInspect = strSourceHTML[PositionRenew..PositionDiv];
            StrRetToInspect = StrRetToInspect.Substring(0, 10);

            string StrY = StrRetToInspect[^4..];
            string StrD = StrRetToInspect.Substring(0, 2);
            string StrM = StrRetToInspect.Substring(3, 2);

            dtRet = new DateTime(int.Parse(StrY), int.Parse(StrM), int.Parse(StrD), 0, 0, 0);

            return dtRet;
        }

        private static String IliadExtractMobileNumberFromSource(this String strSourceHTML)
        {
            String strRet;

            int PositionRenew = strSourceHTML.IndexOf(">Numero: ") + 9;
            int PositionDiv = strSourceHTML.IndexOf("</div>", PositionRenew);
            string StrRetToInspect = strSourceHTML[PositionRenew..PositionDiv];
            strRet = StrRetToInspect.TrimStart();

            return strRet;
        }

        private static decimal IliadExtractCreditLeftFromSource(this String strSourceHTML, out String strCurr)
        {
#pragma warning disable IDE0018 // Inline variable declaration
            decimal decCred = 0;
#pragma warning restore IDE0018 // Inline variable declaration
            int PositionCred = strSourceHTML.IndexOf("Credito : ");
            int PositionBr = strSourceHTML.IndexOf("</b>", PositionCred);
            //string StrRetToInspect = strSourceHTML.Substring(PositionCred, PositionBr - PositionCred);
            string StrRetToInspect = strSourceHTML[PositionCred..PositionBr];
            StrRetToInspect = StrRetToInspect[(StrRetToInspect.LastIndexOf(">") + 1)..];

            //string StrVal = StrRetToInspect.Substring(0, StrRetToInspect.Length - 1);
            string StrVal = StrRetToInspect[0..^1];
            if (GetDecValue(StrVal, out decCred))
            {
                //Done Convertion
                strCurr = StrRetToInspect.Substring(StrRetToInspect.Length - 1, 1);
            }
            else
            {
                //Failed Convertion
                //TO DO: Error raising or management
                strCurr = "?";
            }
            return decCred;
        }
        private static decimal IliadExtractUsedFromSource(this String strSourceHTML, out String strUnitMes)
        {
            //Html inspection (Old School, position based)
            //TODO: Use variables for position markers that can set as property on the class instance 
            decimal decUsed = 0;
            int PositionConsumi = strSourceHTML.IndexOf("Consumi Dati:");
            int PositionDivStart = strSourceHTML.LastIndexOf("<div class", PositionConsumi);
            int PositionDivFinish = strSourceHTML.IndexOf("</div>", PositionDivStart);
            //string StrRetToInspect = strSourceHTML.Substring(PositionDivStart, PositionDivFinish - PositionDivStart);
            string StrRetToInspect = strSourceHTML[PositionDivStart..PositionDivFinish];
            int PositionRed = StrRetToInspect.IndexOf("red\">");
            int PositionSpan = StrRetToInspect.IndexOf("</span>");
            StrRetToInspect = StrRetToInspect.Substring(PositionRed + 5, PositionSpan - 5 - PositionRed);
            string StrValue = StrRetToInspect[0..^2];
            if (GetDecValue(StrValue, out decUsed))
            {
                //Conversion Done
                strUnitMes = StrRetToInspect[^2..];
            }
            else
            {
                //Failed Convertion
                //TO DO: Error raising or management
                strUnitMes = "?";
            }
            return decUsed;
        }
        private static decimal IliadExtractQuotaMaxFromSource(this String strSourceHTML)
        {
            //Html inspection (Old School, position based)
            //TODO: Use variables for position markers that can set as property on the class instance 
#pragma warning disable IDE0018 // Inline variable declaration
            decimal decQuotaMax = 0;
#pragma warning restore IDE0018 // Inline variable declaration
            int PositionConsumi = strSourceHTML.IndexOf("Consumi Dati:");
            int PositionDivStart = strSourceHTML.LastIndexOf("<div class", PositionConsumi);
            int PositionDivFinish = strSourceHTML.IndexOf("</div>", PositionDivStart);
            //string StrRetToInspect = strSourceHTML.Substring(PositionDivStart, PositionDivFinish - PositionDivStart);
            string StrRetToInspect = strSourceHTML[PositionDivStart..PositionDivFinish];
            string StrQuotaPart = "";
            int PositionRed = StrRetToInspect.IndexOf("red\">");
            int PositionSpan = StrRetToInspect.IndexOf("</span>");
            StrQuotaPart = StrRetToInspect.Substring(PositionSpan + 6, 10);
            int PositionSlash = StrQuotaPart.IndexOf("/");
            int PositionBr = StrQuotaPart.IndexOf("<b");
            StrQuotaPart = StrQuotaPart.Substring(PositionSlash + 1, PositionBr - 1 - PositionSlash - 2);
            if (GetDecValue(StrQuotaPart, out decQuotaMax))
            {
                //Conversion Done
            }
            else
            {
                //Failed Conversione
                //TO DO: Error raising or management
            }
            return decQuotaMax;
        }
        private static bool GetDecValue(this String strVal, out decimal decRetVal)
        {
            //String to Decimal conversions
            bool bSuccess = false;
            NumberStyles NSMystyle;
            CultureInfo CIUKCulture;
            NSMystyle = NumberStyles.Number | NumberStyles.AllowCurrencySymbol;
            CIUKCulture = CultureInfo.CreateSpecificCulture("en-GB");

            if (Decimal.TryParse(strVal.Replace(",", "."), NSMystyle, CIUKCulture, out decRetVal))
            { bSuccess = true; }
            else
            { decRetVal = 0; }
            return bSuccess;
        }

    }

}