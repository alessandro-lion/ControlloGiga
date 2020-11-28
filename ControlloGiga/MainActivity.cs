using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using MSLibrary;
using PilionUtilities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ControlloGiga
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]

    public class MainActivity : AppCompatActivity, BottomNavigationView.IOnNavigationItemSelectedListener
    {
        internal String strpar1 = "";
        internal String strpar2 = "";
        private bool bAtOpening = false;
        private TextView textMessage, textUsage, textCharge, textDate, textStatus;
        private EditText EditEntry, EditEntryPass;
        private Android.Widget.ProgressBar prgBar1;
        private Android.Widget.Button okButton;
        private Android.Widget.ImageButton imgButton;
        private DateTime LastDataFetch = DateTime.Now.AddSeconds(-33);
        private BottomNavigationView navigation;
        //TODO: Prevedere gestione di un nickname per le credenziali e la possibilità di usare più di un set di credenziali per controllare i dati di più schede
        //TO DO: Vedere se vi è modo di recuperare i dati di utilizzo anche di altri operatori di telefonia, tre, tim, vodafone
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            textMessage = FindViewById<TextView>(Resource.Id.message);
            textUsage = FindViewById<TextView>(Resource.Id.textViewUsage);
            textCharge = FindViewById<TextView>(Resource.Id.textViewCharge);
            textDate = FindViewById<TextView>(Resource.Id.textViewDate);
            textStatus = FindViewById<TextView>(Resource.Id.textViewStatus);

            EditEntry = FindViewById<EditText>(Resource.Id.entry);
            EditEntryPass = FindViewById<EditText>(Resource.Id.entrypass);

            prgBar1 = FindViewById<Android.Widget.ProgressBar>(Resource.Id.progressBar1);

            navigation = FindViewById<BottomNavigationView>(Resource.Id.navigation);
            navigation.SetOnNavigationItemSelectedListener(this);

            var confLayout = FindViewById<Android.Widget.RelativeLayout>(Resource.Id.conf_layout);
            confLayout.Visibility = ViewStates.Invisible;

            okButton = FindViewById<Android.Widget.Button>(Resource.Id.buttonok);
            okButton.Click += (sender, e) =>
            {
                OnClickOK();
            };
            imgButton = FindViewById<Android.Widget.ImageButton>(Resource.Id.imageButtonRefresh);
            imgButton.Click += (sender, e) =>
            {
                OnImageButtonClicked();
            };
            //Leggere i valori per le credenziali e se mancanti predisporre l'app su configurazione.
            bool gotcred = false;
            String strval = Resources.GetString(Resource.String.account_store);
            var fret = ManageSettingsLibrary.GetStoredParmValueAsync(strval);
            String sval = fret.Result;
            if (sval != null && sval != "")
            {
                EditEntry.Text = sval;
                System.Diagnostics.Debug.WriteLine("OnCreate EDIT ENTRY SET FROM STORED DATA");
                strval = Resources.GetString(Resource.String.pass_store);
                fret = ManageSettingsLibrary.GetStoredParmValueAsync(strval);
                sval = fret.Result;
                if (sval != null && sval != "")
                {
                    EditEntryPass.Text = sval;
                    gotcred = true;
                }
            }
            if (!gotcred)
            {
                System.Diagnostics.Debug.WriteLine("OnCreate INVALID CREDENTIALS");
                navigation.SelectedItemId = Resource.Id.navigation_dashboard;
            }
            else
            {
                //Se ci son le credenziali usarle
                strpar1 = EditEntry.Text;
                strpar2 = EditEntryPass.Text;
                //FIX USO thread solo su versioni >=8 (se iniziano con 5, 6 e 7 chiamo direttamente il metodo)
                String myversionstring= Build.VERSION.Release;
                switch (myversionstring.Substring(0, 1))
                {
                    case "5":
                    case "6":
                    case "7":
                        //No multithreading
                        bool bretc = RefreshData(strpar1, strpar2);
                        break;
                    default:
                        //Async Refresh Data con thread separato
                        RefreshData();
                        break;
                }
            }
        }
        private void OnImageButtonClicked()
        {
            RefreshData(EditEntry.Text, EditEntryPass.Text);
        }
        protected override void OnResume()
        {
            System.Diagnostics.Debug.WriteLine("OnResume");
            base.OnResume();
        }
        private void OnClickOK()
        {
            textStatus.SetText(Resource.String.title_saving);
            //DONE: Salvare i valori degli editbox
            String strval = Resources.GetString(Resource.String.account_store);
            var myret = ManageSettingsLibrary.SetStoredParmValueAsync(strval, EditEntry.Text);
            bool bret = myret.Result;
            if (bret)
            {
                strval = Resources.GetString(Resource.String.pass_store);
                myret = ManageSettingsLibrary.SetStoredParmValueAsync(strval, EditEntryPass.Text);
                bret = myret.Result;
            }

            if (bret)
            {
                //DONE: Invocare funzione per lettura 
                if (RefreshData(EditEntry.Text, EditEntryPass.Text) == true)
                {
                    //DONE: predisporre l'app su Home qualora la connessione sia avvenuta con successo.
                    navigation.SelectedItemId = Resource.Id.navigation_home;
                } 
            }
        }
        async void RefreshData()
        {
            /* V. 15 */ 
            navigation.SelectedItemId = Resource.Id.navigation_home;
            textStatus.Text = Resources.GetString(Resource.String.title_refreshing);
            await Task.Run(() => {
            bool btret = false;
            btret = RefreshData(strpar1, strpar2);
        });

            /* V. 14 
             * navigation.SelectedItemId = Resource.Id.navigation_home;
             * textStatus.Text = Resources.GetString(Resource.String.title_refreshing);
             * bool btret = false;
             * var tasko = Task.Run(() =>btret=RefreshData(strpar1,strpar2));
             * await tasko;
             * */
        }
        private bool RefreshData(String StrUser, String StrPass)
        {
            System.TimeSpan diffResult = DateTime.Now.Subtract(LastDataFetch);

            if (diffResult.TotalSeconds < 30)
            {
                textStatus.SetText(Resource.String.title_refreshskip);
                return false;
                //EXIT
            }

            try
            {
                //prgBar1.SetProgress(0, false);  FIX ISSUE WITH Android 6.0 Marshmallow
                prgBar1.Progress = 0;
                if ((StrUser != null) && (StrUser != ""))
                {
                    textStatus.SetText(Resource.String.title_refreshing);
                }
                else
                {
                    textStatus.SetText(Resource.String.title_refreshfailed);
                    return false;
                    //EXIT
                }

                String strval = Resources.GetString(Resource.String.url_iliad);
                decimal result = UsedDataCheckLibrary.IliadUsedDataCheck(strval, StrUser, StrPass, out string strunitm, out decimal quotamax, out string strcurr, out decimal deccred, out DateTime dtrenew, out string strnumber);

                if (result > 0)
                {
                    LastDataFetch = DateTime.Now;
                    //Update Show Results View Objects on Layout home
                    if (quotamax > 0)
                    {
                        //prgBar1.IncrementProgressBy(Convert.ToInt16(Math.Round(result / quotamax * 100))); FIX ISSUE WITH Android 6.0 Marshmallow
                        int valprog = Convert.ToInt16(Math.Round((result / quotamax * 100), 0, MidpointRounding.AwayFromZero));
                        if ( result > 0 )
                        {
                            valprog = Math.Max(1, valprog);
                        }
                        prgBar1.Progress = valprog;
                    }

                    textUsage.Text = Resources.GetString(Resource.String.label_utilizzati) + " " + result.ToString() + " / " + quotamax.ToString() + " " + strunitm;
                    textCharge.Text = Resources.GetString(Resource.String.label_charge) + " " + deccred.ToString() + " " + strcurr;
                    textDate.Text = Resources.GetString(Resource.String.label_datascad) + " " + dtrenew.ToString("dd/MM/yyyy");
                    textStatus.Text = Resources.GetString(Resource.String.title_refreshdone) + " " + strnumber;
                    if (bAtOpening)
                    {
                        //Se le credenziali danno accesso ai dati mostro informazioni recuperate
                        //0 or 1 ??
                        navigation.SelectedItemId = Resource.Id.navigation_home;
                        bAtOpening = false;
                    }
                    return true;
                }
                else
                {
                    textStatus.SetText(Resource.String.title_refreshfailednet);
                    if (bAtOpening)
                    {
                        //Se le credenziali non danno accesso mostro la configurazione
                        navigation.SelectedItemId = Resource.Id.navigation_dashboard;
                        bAtOpening = false;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                textStatus.Text = "!ERROR: " + ex.Message;
                return false;
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void SetHomeOnFocus()
        //TODO:: Riscrivere le SetFocus in maniera da avera una terza funzione con un parametro true false chiamata da entrambe
        {
            textMessage.SetText(Resource.String.title_home);
            var confLayout = FindViewById<Android.Widget.RelativeLayout>(Resource.Id.conf_layout);
            confLayout.Visibility = ViewStates.Invisible;
            var HLayout = FindViewById<Android.Widget.RelativeLayout>(Resource.Id.home_layout);
            HLayout.Visibility = ViewStates.Visible;
        }
        private void SetDashBoardOnFocus()
        {
            textMessage.SetText(Resource.String.title_dashboard);
            var confLayout = FindViewById<Android.Widget.RelativeLayout>(Resource.Id.conf_layout);
            confLayout.Visibility = ViewStates.Visible;
            var HLayout = FindViewById<Android.Widget.RelativeLayout>(Resource.Id.home_layout);
            HLayout.Visibility = ViewStates.Invisible;
        }

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.navigation_home:
                    SetHomeOnFocus();
                    return true;
                case Resource.Id.navigation_dashboard:
                    SetDashBoardOnFocus();
                    return true;
            }
            return false;
        }
    }

 
}