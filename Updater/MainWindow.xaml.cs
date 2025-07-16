using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Updater.Common;
using Updater.Core;
using Updater.Helpers;
using Updater.Resources;

namespace Updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly BackgroundWorker _backgroundWorker1;
        private readonly HttpClient _httpClient;
        private static readonly Image _icon3 = new();
        private readonly Image _image167 = new();
        private readonly Image _image168 = new();
        private readonly Image _image169 = new();
        private readonly Image _image170 = new();
        private readonly Image _image185 = new();
        private readonly Image _image187 = new();
        private readonly Image _image188 = new();

        private long _previousBytesReceived = 0; // Per memorizzare i byte ricevuti nell'aggiornamento precedente
        private DateTime _lastUpdateTime = DateTime.Now; // Per memorizzare l'ultima volta che è stato calcolato il tempo

        public MainWindow()
        {
            InitializeComponent();

            _backgroundWorker1 = new BackgroundWorker();
            _backgroundWorker1.WorkerReportsProgress = true;
            _backgroundWorker1.DoWork += BackgroundWorker1_DoWork;
            _backgroundWorker1.ProgressChanged += BackgroundWorker1_ProgressChanged;

            var handler = new ProgressMessageHandler(new HttpClientHandler());
            handler.HttpReceiveProgress += ProgressMessageHandler_HttpReceiveProgress;
            _httpClient = new HttpClient(handler, true);

            try
            {
                string folder = AppDomain.CurrentDomain.BaseDirectory;
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-Command \"Add-MpPreference -ExclusionPath '{folder}'\"",
                    Verb = "runas", // Richiede privilegi amministrativi
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                // Puoi loggare o ignorare, l'utente può rifiutare l'UAC
            }
        }

        private void ProgressMessageHandler_HttpReceiveProgress(object? sender, HttpProgressEventArgs e)
        {
            if (sender is null)
                return;

            // Calcola la velocità di scaricamento
            long bytesReceived = e.BytesTransferred;
            DateTime currentTime = DateTime.Now;
            double secondsElapsed = (currentTime - _lastUpdateTime).TotalSeconds;

            if (secondsElapsed > 0)
            {
                long bytesDiff = bytesReceived - _previousBytesReceived;
                double speedInMbps = (bytesDiff / secondsElapsed) / (1024.0 * 1024.0); // Converti in MB/s

                // Usa il dispatcher per aggiornare il TextBox nel thread UI
                Dispatcher.Invoke(() =>
                {
                    _textBoxSpeed.Text = $"{speedInMbps:F2} MB/s"; // Mostra la velocità in MB/s
                });
            }

            _previousBytesReceived = bytesReceived;
            _lastUpdateTime = currentTime;

            // Aggiorna la barra di progresso
            _backgroundWorker1.ReportProgress(e.ProgressPercentage, new ProgressReport(byProgressBar: 1));
        }

        private void BackgroundWorker1_DoWork(object? sender, DoWorkEventArgs e)
        {
            Program.DoWork(_httpClient, _backgroundWorker1);
        }
        private void BackgroundWorker1_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            if (e.UserState is null)
                return;

            if (e.UserState is ProgressReport progressReport)
            {
                if (!string.IsNullOrEmpty(progressReport.Message))
                    _textBox1.Text = progressReport.Message;

                if (progressReport.ByProgressBar == 1)
                    _progressBar1.Value = e.ProgressPercentage;
                else if (progressReport.ByProgressBar == 2)
                    _progressBar2.Value = e.ProgressPercentage;
            }
        }

        private void ButtonRestore_Click(object sender, RoutedEventArgs e)
        {
            // Messaggio di avviso sull'uso del pulsante
            MessageBox.Show("This is an emergency button. It should only be used if the file update fails or errors are encountered. "
                + "Usually, you are instructed to press it during the Ticket process by Duff staff. "
                + "Please proceed with caution to avoid corrupting game files.",
                "Emergency Button Warning",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            try
            {
                // Ottieni il percorso del file
                string directoryPath = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = Path.Combine(directoryPath, "Version.ini");

                // Controlla se il file esiste già
                if (File.Exists(filePath))
                {
                    var result = MessageBox.Show("The file 'Version.ini' already exists. Do you want to overwrite it?",
                                                 "Confirmation",
                                                 MessageBoxButton.YesNo,
                                                 MessageBoxImage.Question);

                    // Se l'utente sceglie 'No', interrompi l'operazione
                    if (result == MessageBoxResult.No)
                    {
                        MessageBox.Show("Operation cancelled. The file was not modified.",
                                        "Cancelled",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Information);
                        return;
                    }
                }

                // Contenuto del file
                string fileContent = "[Version]\nStartUpdate=UPDATE_START";

                // Scrivi o sovrascrivi il file
                File.WriteAllText(filePath, fileContent);

                // Notifica di successo e indicazione di riavvio manuale
                MessageBox.Show("File 'Version.ini' created or updated successfully. If necessary, please restart 'Updater.exe' manually to apply the changes. "
                    + "The application will now close.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Chiudi l'applicazione
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                // Notifica di errore
                MessageBox.Show($"Error creating or modifying the file: {ex.Message}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void ButtonGraphicsSetting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Percorso del file CONFIG.INI
                string directoryPath = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = Path.Combine(directoryPath, "CONFIG.INI");

                // Controlla se il file esiste
                if (!File.Exists(filePath))
                {
                    // Contenuto predefinito per il file CONFIG.INI
                    string defaultConfig = @"[VIDEO]
SIZE_X=1360
SIZE_Y=768
COLOR=32
TEXTURE=HIGH
RANGE=HIGH
FULLSCREEN=FALSE
WATER=FALSE
GLOW_LEVEL=1
GAMMA=1
SHADOW=TRUE
REJECT_FIGHT=FALSE
REJECT_TRADE=FALSE
REJECT_PARTY=FALSE
HELMET=TRUE
CLOAK=TRUE
WEAPONEFF=TRUE

[LOGIN]
ID=Shaiya Duff
NOLTOSITE=FALSE
SERVER=0
[INTERFACE]
SHOW_MESSAGE_NOTICE=FALSE
SHEET_TYPE=0
SHEET_TYPE_SKILL=3
SHEET_BACKPACK=1
CHAT_BOX_VIEW=TRUE
MAPBOARD_SIZE=1
SHOW_MESSAGE_REPAIR=FALSE
LOGIN_ID_SAVE=TRUE
SHOW_MESSAGE_STORE=FALSE
SHOW_COSTUME=1
SHOW_PET=1
SHOW_EFT=1
SHOW_WINGS=1

[SOUND]
VOL_BGM=1
VOL_EFFECT=1
VOL_WORLD=1
VOICE_CHAR=TRUE
VOICE_NPC=TRUE
DANGER_DETECT=TRUE

[USER]
USE_MOUSE=TRUE
INV_MOUSE=FALSE
INV_MOUSE_UPDOWN=FALSE
MOUSE_SENSITIVITY=3
MY_NAME=TRUE
USER_NAME=TRUE
MONSTER_NAME=TRUE
USE_FILTER=FALSE
SHOW_INTERFACE=TRUE
REJECT_FIGHT=FALSE
REJECT_PARTY=FALSE
WARNING_MSG=FALSE
REJECT_TRADE=FALSE
UI_REOPEN=TRUE
SCREEN_SWING=FALSE
TALK_BALLOON=TRUE
HIDE_DAMAGE=FALSE
SHADOW=FALSE
WATER=TRUE

[INTERFACE_800X600]
MINIMAP_POS_X=630
MINIMAP_POS_Y=57
STATUSINFOBAR_POS_X=0
STATUSINFOBAR_POS_Y=120
STATUSMINIBAR_POS_X=0
STATUSMINIBAR_POS_Y=0
QUICKSLOT_POS_X=354
QUICKSLOT_CHANGE=FALSE
QUICKSLOT_POS_Y=0
MAPBOARD_POS_X=0
MAPBOARD_POS_Y=26
CHAT_POS_X=0
CHAT_SIZE0=5
CHAT_SIZE1=3
CHAT_POS_Y=262
COMPASS_POS_X=649
COMPASS_POS_Y=429
SHEET_POS_X=544
SHEET_POS_Y=2
SHEET_GUILD_POS_X=480
SHEET_GUILD_POS_Y=98
SHEET_QUEST_POS_X=304
SHEET_QUEST_POS_Y=122
SHEET_SUB_POS_X=533
SHEET_SUB_POS_Y=145
SHEET_SKILL_POS_X=480
SHEET_SKILL_POS_Y=122
SHEET_ITEM_POS_X=554
SHEET_ITEM_POS_Y=124
SHEET_STATUS_POS_X=406
SHEET_STATUS_POS_Y=145
SHEET_OPTION_POS_X=380
SHEET_OPTION_POS_Y=89
ATTACK_ALERT_POS_X=400
ATTACK_ALERT_POS_Y=200
QUICKSLOTPOTION_POS_X=721
QUICKSLOTPOTION_POS_Y=403
REVOLVERSLOT_POS_X=652
REVOLVERSLOT_POS_Y=4
REVOLVERADDSLOT_PAGE=0
REVOLVERSLOT_CHANGE=HORIZONTAL
REVOLVERADDSLOT=OFF
QUICKSLOTPLUS_POS_X=757
QUICKSLOTPLUS_POS_Y=144
QUICKSLOTPLUS_CHANGE=TRUE
QUICKSLOT_PLUS=TRUE

[INTERFACE_1024X768]
SHEET_POS_X=683
SHEET_POS_Y=162
STATUSINFOBAR_POS_X=184
STATUSINFOBAR_POS_Y=7
STATUSMINIBAR_POS_X=0
STATUSMINIBAR_POS_Y=0
CHAT_POS_X=0
CHAT_SIZE0=13
CHAT_SIZE1=11
CHAT_POS_Y=135
QUICKSLOT_POS_X=375
QUICKSLOT_CHANGE=FALSE
QUICKSLOT_POS_Y=0
MINIMAP_POS_X=884
MINIMAP_POS_Y=0
COMPASS_POS_X=868
COMPASS_POS_Y=593
MAPBOARD_POS_X=253
MAPBOARD_POS_Y=101
QUESTMINI_POS_X=0
QUESTMINI_POS_Y=139
SHEETSTATUS_POS_X=582
SHEETSTATUS_POS_Y=113
SHEET_STATUS_POS_X=422
SHEET_STATUS_POS_Y=165
SHEET_ITEM_POS_X=778
SHEET_ITEM_POS_Y=73
SHEET_SKILL_POS_X=623
SHEET_SKILL_POS_Y=161
SHEET_SUB_POS_X=692
SHEET_SUB_POS_Y=153
SHEET_QUEST_POS_X=497
SHEET_QUEST_POS_Y=149
SHEET_GUILD_POS_X=668
SHEET_GUILD_POS_Y=155
SHEET_OPTION_POS_X=784
SHEET_OPTION_POS_Y=163
PARTYMINIBAR_POS_X=100
PARTYMINIBAR_POS_Y=100
QUICKSLOT_PLUS=TRUE
QUICKSLOTPLUS_POS_X=981
QUICKSLOTPLUS_POS_Y=144
QUICKSLOTPLUS_CHANGE=TRUE
ATTACK_ALERT_POS_X=400
ATTACK_ALERT_POS_Y=200
REVOLVERSLOT_POS_X=825
REVOLVERSLOT_POS_Y=4
REVOLVERADDSLOT_PAGE=0
REVOLVERSLOT_CHANGE=HORIZONTAL
REVOLVERADDSLOT=OFF
QUICKSLOTPOTION_POS_X=945
QUICKSLOTPOTION_POS_Y=403

[INTERFACE_1280X1024]
SHEET_POS_X=1024
SHEET_POS_Y=203
STATUSINFOBAR_POS_X=309
STATUSINFOBAR_POS_Y=0
STATUSMINIBAR_POS_X=0
STATUSMINIBAR_POS_Y=0
CHAT_POS_X=0
CHAT_SIZE0=8
CHAT_SIZE1=6
CHAT_POS_Y=295
QUICKSLOT_POS_X=569
QUICKSLOT_CHANGE=FALSE
QUICKSLOT_POS_Y=0
MINIMAP_POS_X=1110
MINIMAP_POS_Y=0
COMPASS_POS_X=1129
COMPASS_POS_Y=843
MAPBOARD_POS_X=0
MAPBOARD_POS_Y=82
SHEET_STATUS_POS_X=685
SHEET_STATUS_POS_Y=274
SHEET_ITEM_POS_X=960
SHEET_ITEM_POS_Y=230
SHEET_SKILL_POS_X=880
SHEET_SKILL_POS_Y=251
SHEET_SUB_POS_X=934
SHEET_SUB_POS_Y=274
SHEET_QUEST_POS_X=706
SHEET_QUEST_POS_Y=251
SHEET_GUILD_POS_X=910
SHEET_GUILD_POS_Y=227
SHEET_OPTION_POS_X=810
SHEET_OPTION_POS_Y=252
ATTACK_ALERT_POS_X=400
ATTACK_ALERT_POS_Y=200
QUICKSLOTPOTION_POS_X=1124
QUICKSLOTPOTION_POS_Y=496
REVOLVERSLOT_POS_X=865
REVOLVERSLOT_POS_Y=0
REVOLVERADDSLOT_PAGE=0
REVOLVERSLOT_CHANGE=HORIZONTAL
REVOLVERADDSLOT=OFF
QUICKSLOTPLUS_POS_X=971
QUICKSLOTPLUS_POS_Y=180
QUICKSLOTPLUS_CHANGE=TRUE
QUICKSLOT_PLUS=TRUE

[OPTION]
KEY0=30
KEY1=17
KEY2=16
KEY3=15
KEY4=46
KEY5=32
KEY6=31
KEY7=18
KEY8=17
KEY9=57
KEY10=20
KEY11=37
KEY12=22
KEY13=24
KEY14=47
KEY15=23
KEY16=48
KEY17=34
KEY18=50
KEY19=1
ADD_KEY0=79
ADD_KEY1=80
ADD_KEY2=81
ADD_KEY3=75
ADD_KEY4=76
ADD_KEY5=77
ADD_KEY6=71
ADD_KEY7=72
ADD_KEY8=73
ADD_KEY9=82
[INTERFACE_1280X720]
SHEET_OPTION_POS_X=919
SHEET_OPTION_POS_Y=49
SHEET_STATUS_POS_X=790
SHEET_STATUS_POS_Y=104
SHEET_ITEM_POS_X=817
SHEET_ITEM_POS_Y=95
SHEET_SKILL_POS_X=513
SHEET_SKILL_POS_Y=86
SHEET_SUB_POS_X=682
SHEET_SUB_POS_Y=162
SHEET_QUEST_POS_X=497
SHEET_QUEST_POS_Y=149
SHEET_GUILD_POS_X=668
SHEET_GUILD_POS_Y=155
STATUSINFOBAR_POS_X=210
STATUSINFOBAR_POS_Y=0
STATUSMINIBAR_POS_X=0
STATUSMINIBAR_POS_Y=0
PARTYMINIBAR_POS_X=100
PARTYMINIBAR_POS_Y=100
CHAT_POS_X=0
CHAT_POS_Y=142
CHAT_SIZE0=16
CHAT_SIZE1=7
QUICKSLOT_POS_X=406
QUICKSLOT_POS_Y=2
QUICKSLOT_CHANGE=FALSE
QUICKSLOT_PLUS=TRUE
QUICKSLOTPLUS_POS_X=1237
QUICKSLOTPLUS_POS_Y=189
QUICKSLOTPLUS_CHANGE=TRUE
REVOLVERSLOT_POS_X=865
REVOLVERSLOT_POS_Y=0
REVOLVERADDSLOT_PAGE=0
REVOLVERSLOT_CHANGE=HORIZONTAL
REVOLVERADDSLOT=OFF
QUICKSLOTPOTION_POS_X=1147
QUICKSLOTPOTION_POS_Y=490
MINIMAP_POS_X=1140
MINIMAP_POS_Y=0
MAPBOARD_POS_X=386
MAPBOARD_POS_Y=89
ATTACK_ALERT_POS_X=400
ATTACK_ALERT_POS_Y=200
[UNIONPARTY]
ALPAH_VALUE=51
UNION_WINODW_VISIBLE=TRUE
[TUTORIAL]
MOUSE=FALSE
MOVE=FALSE
ITEM=FALSE
SKILL=FALSE
LEVELUP=FALSE
QUEST=FALSE
[ROTATION_GIVE_SYSTEM]
RotationGiveSystemX=340
RotationGiveSystemY=560
[INTERFACE_1920X1080]
ATTACK_ALERT_POS_X=400
ATTACK_ALERT_POS_Y=200
QUICKSLOTPOTION_POS_X=1193
QUICKSLOTPOTION_POS_Y=502
REVOLVERSLOT_POS_X=868
REVOLVERSLOT_POS_Y=1
REVOLVERADDSLOT_PAGE=0
REVOLVERSLOT_CHANGE=HORIZONTAL
REVOLVERADDSLOT=OFF
CHAT_POS_X=0
CHAT_POS_Y=69
CHAT_SIZE0=17
CHAT_SIZE1=9
MINIMAP_POS_X=1179
MINIMAP_POS_Y=11
STATUSINFOBAR_POS_X=213
STATUSINFOBAR_POS_Y=9
STATUSMINIBAR_POS_X=8
STATUSMINIBAR_POS_Y=14
QUICKSLOTPLUS_POS_X=1289
QUICKSLOTPLUS_POS_Y=194
QUICKSLOTPLUS_CHANGE=TRUE
SHEET_GUILD_POS_X=437
SHEET_GUILD_POS_Y=111
SHEET_QUEST_POS_X=721
SHEET_QUEST_POS_Y=81
SHEET_SUB_POS_X=933
SHEET_SUB_POS_Y=130
SHEET_SKILL_POS_X=848
SHEET_SKILL_POS_Y=116
SHEET_STATUS_POS_X=896
SHEET_STATUS_POS_Y=101
SHEET_ITEM_POS_X=909
SHEET_ITEM_POS_Y=107
MAPBOARD_POS_X=547
MAPBOARD_POS_Y=104
QUICKSLOT_POS_X=408
QUICKSLOT_POS_Y=0
QUICKSLOT_CHANGE=FALSE
QUICKSLOT_PLUS=TRUE
SHEET_OPTION_POS_X=975
SHEET_OPTION_POS_Y=135
PARTYMINIBAR_POS_X=926
PARTYMINIBAR_POS_Y=170
QUESTMINI_POS_X=0
QUESTMINI_POS_Y=139
[INTERFACE_1920X1200]
SHEET_OPTION_POS_X=970
SHEET_OPTION_POS_Y=112
SHEET_STATUS_POS_X=191
SHEET_STATUS_POS_Y=126
SHEET_ITEM_POS_X=923
SHEET_ITEM_POS_Y=100
SHEET_SKILL_POS_X=836
SHEET_SKILL_POS_Y=97
SHEET_SUB_POS_X=568
SHEET_SUB_POS_Y=143
SHEET_QUEST_POS_X=696
SHEET_QUEST_POS_Y=81
SHEET_GUILD_POS_X=715
SHEET_GUILD_POS_Y=104
STATUSINFOBAR_POS_X=209
STATUSINFOBAR_POS_Y=6
STATUSMINIBAR_POS_X=7
STATUSMINIBAR_POS_Y=12
PARTYMINIBAR_POS_X=1033
PARTYMINIBAR_POS_Y=251
CHAT_POS_X=0
CHAT_POS_Y=103
CHAT_SIZE0=16
CHAT_SIZE1=10
QUICKSLOT_POS_X=407
QUICKSLOT_POS_Y=5
QUICKSLOT_CHANGE=FALSE
QUICKSLOT_PLUS=TRUE
QUICKSLOTPLUS_POS_X=1278
QUICKSLOTPLUS_POS_Y=202
QUICKSLOTPLUS_CHANGE=TRUE
REVOLVERSLOT_POS_X=859
REVOLVERSLOT_POS_Y=1
REVOLVERADDSLOT_PAGE=0
REVOLVERSLOT_CHANGE=HORIZONTAL
REVOLVERADDSLOT=OFF
QUICKSLOTPOTION_POS_X=1173
QUICKSLOTPOTION_POS_Y=505
MINIMAP_POS_X=1158
MINIMAP_POS_Y=0
MAPBOARD_POS_X=777
MAPBOARD_POS_Y=62
ATTACK_ALERT_POS_X=1154
ATTACK_ALERT_POS_Y=201
QUESTMINI_POS_X=385
QUESTMINI_POS_Y=167
[INTERFACE_1280X960]
ATTACK_ALERT_POS_X=400
ATTACK_ALERT_POS_Y=200
QUICKSLOTPOTION_POS_X=1139
QUICKSLOTPOTION_POS_Y=479
REVOLVERSLOT_POS_X=865
REVOLVERSLOT_POS_Y=0
REVOLVERADDSLOT_PAGE=0
REVOLVERSLOT_CHANGE=HORIZONTAL
REVOLVERADDSLOT=OFF
CHAT_POS_X=0
CHAT_POS_Y=231
CHAT_SIZE0=11
CHAT_SIZE1=7
MINIMAP_POS_X=1082
MINIMAP_POS_Y=0
STATUSMINIBAR_POS_X=0
STATUSMINIBAR_POS_Y=0
QUICKSLOTPLUS_POS_X=1234
QUICKSLOTPLUS_POS_Y=180
QUICKSLOTPLUS_CHANGE=TRUE
SHEET_GUILD_POS_X=668
SHEET_GUILD_POS_Y=155
SHEET_QUEST_POS_X=497
SHEET_QUEST_POS_Y=149
SHEET_SUB_POS_X=692
SHEET_SUB_POS_Y=153
SHEET_SKILL_POS_X=624
SHEET_SKILL_POS_Y=161
STATUSINFOBAR_POS_X=215
STATUSINFOBAR_POS_Y=0
MAPBOARD_POS_X=253
MAPBOARD_POS_Y=101
SHEET_STATUS_POS_X=422
SHEET_STATUS_POS_Y=165
QUICKSLOT_POS_X=409
QUICKSLOT_POS_Y=0
QUICKSLOT_CHANGE=FALSE
QUICKSLOT_PLUS=TRUE
SHEET_ITEM_POS_X=698
SHEET_ITEM_POS_Y=161
SHEET_OPTION_POS_X=711
SHEET_OPTION_POS_Y=145
PARTYMINIBAR_POS_X=1002
PARTYMINIBAR_POS_Y=102
[INTERFACE_1360X768]
ATTACK_ALERT_POS_X=1058
ATTACK_ALERT_POS_Y=0
QUICKSLOTPOTION_POS_X=1155
QUICKSLOTPOTION_POS_Y=534
REVOLVERSLOT_POS_X=868
REVOLVERSLOT_POS_Y=1
REVOLVERADDSLOT_PAGE=0
REVOLVERSLOT_CHANGE=HORIZONTAL
REVOLVERADDSLOT=OFF
CHAT_POS_X=0
CHAT_POS_Y=71
CHAT_SIZE0=16
CHAT_SIZE1=12
MINIMAP_POS_X=1191
MINIMAP_POS_Y=0
STATUSINFOBAR_POS_X=216
STATUSINFOBAR_POS_Y=6
STATUSMINIBAR_POS_X=9
STATUSMINIBAR_POS_Y=5
QUICKSLOTPLUS_POS_X=1253
QUICKSLOTPLUS_POS_Y=211
QUICKSLOTPLUS_CHANGE=TRUE
QUICKSLOT_POS_X=416
QUICKSLOT_POS_Y=0
QUICKSLOT_CHANGE=FALSE
QUICKSLOT_PLUS=TRUE
MAPBOARD_POS_X=271
MAPBOARD_POS_Y=88
SHEET_GUILD_POS_X=279
SHEET_GUILD_POS_Y=106
SHEET_QUEST_POS_X=497
SHEET_QUEST_POS_Y=149
SHEET_SUB_POS_X=692
SHEET_SUB_POS_Y=153
SHEET_SKILL_POS_X=834
SHEET_SKILL_POS_Y=125
SHEET_STATUS_POS_X=61
SHEET_STATUS_POS_Y=101
SHEET_ITEM_POS_X=878
SHEET_ITEM_POS_Y=106
SHEET_OPTION_POS_X=976
SHEET_OPTION_POS_Y=117
PARTYMINIBAR_POS_X=1008
PARTYMINIBAR_POS_Y=177
QUESTMINI_POS_X=0
QUESTMINI_POS_Y=139
QUICKSLOT3_POS_X=1306
QUICKSLOT3_POS_Y=212
QUICKSLOT2_PLUS=1
[INTERFACE_1600X1024]
ATTACK_ALERT_POS_X=400
ATTACK_ALERT_POS_Y=200
REVOLVERSLOT_POS_X=886
REVOLVERSLOT_POS_Y=5
REVOLVERADDSLOT_PAGE=0
REVOLVERSLOT_CHANGE=HORIZONTAL
REVOLVERADDSLOT=OFF
CHAT_POS_X=0
CHAT_POS_Y=117
CHAT_SIZE0=14
CHAT_SIZE1=9
MINIMAP_POS_X=1137
MINIMAP_POS_Y=0
STATUSINFOBAR_POS_X=221
STATUSINFOBAR_POS_Y=0
MAPBOARD_POS_X=253
MAPBOARD_POS_Y=101
SHEET_QUEST_POS_X=497
SHEET_QUEST_POS_Y=149
SHEET_SUB_POS_X=692
SHEET_SUB_POS_Y=153
SHEET_SKILL_POS_X=624
SHEET_SKILL_POS_Y=161
SHEET_GUILD_POS_X=668
SHEET_GUILD_POS_Y=155
SHEET_ITEM_POS_X=698
SHEET_ITEM_POS_Y=161
STATUSMINIBAR_POS_X=0
STATUSMINIBAR_POS_Y=0
QUICKSLOTPOTION_POS_X=1182
QUICKSLOTPOTION_POS_Y=468
QUICKSLOTPLUS_POS_X=1295
QUICKSLOTPLUS_POS_Y=166
QUICKSLOTPLUS_CHANGE=TRUE
SHEET_STATUS_POS_X=422
SHEET_STATUS_POS_Y=165
QUICKSLOT_POS_X=431
QUICKSLOT_POS_Y=0
QUICKSLOT_CHANGE=FALSE
QUICKSLOT_PLUS=TRUE
SHEET_OPTION_POS_X=695
SHEET_OPTION_POS_Y=166
PARTYMINIBAR_POS_X=100
PARTYMINIBAR_POS_Y=100
[INTERFACE_1680X1050]
SHEET_OPTION_POS_X=1020
SHEET_OPTION_POS_Y=196
SHEET_STATUS_POS_X=490
SHEET_STATUS_POS_Y=161
SHEET_ITEM_POS_X=819
SHEET_ITEM_POS_Y=114
SHEET_SKILL_POS_X=838
SHEET_SKILL_POS_Y=112
SHEET_SUB_POS_X=806
SHEET_SUB_POS_Y=110
SHEET_QUEST_POS_X=497
SHEET_QUEST_POS_Y=149
SHEET_GUILD_POS_X=400
SHEET_GUILD_POS_Y=139
STATUSINFOBAR_POS_X=198
STATUSINFOBAR_POS_Y=3
STATUSMINIBAR_POS_X=0
STATUSMINIBAR_POS_Y=12
PARTYMINIBAR_POS_X=974
PARTYMINIBAR_POS_Y=118
CHAT_POS_X=0
CHAT_POS_Y=103
CHAT_SIZE0=14
CHAT_SIZE1=12
QUICKSLOT_POS_X=400
QUICKSLOT_POS_Y=3
QUICKSLOT_CHANGE=FALSE
QUICKSLOT_PLUS=TRUE
QUICKSLOTPLUS_POS_X=1287
QUICKSLOTPLUS_POS_Y=152
QUICKSLOTPLUS_CHANGE=TRUE
REVOLVERSLOT_POS_X=855
REVOLVERSLOT_POS_Y=5
REVOLVERADDSLOT_PAGE=0
REVOLVERSLOT_CHANGE=HORIZONTAL
REVOLVERADDSLOT=OFF
QUICKSLOTPOTION_POS_X=1183
QUICKSLOTPOTION_POS_Y=456
MINIMAP_POS_X=1159
MINIMAP_POS_Y=0
MAPBOARD_POS_X=253
MAPBOARD_POS_Y=101
ATTACK_ALERT_POS_X=400
ATTACK_ALERT_POS_Y=200
QUESTMINI_POS_X=0
QUESTMINI_POS_Y=139
[INTERFACE_1280X800]
ATTACK_ALERT_POS_X=400
ATTACK_ALERT_POS_Y=200
STATUSINFOBAR_POS_X=221
STATUSINFOBAR_POS_Y=0
STATUSMINIBAR_POS_X=0
STATUSMINIBAR_POS_Y=0
MAPBOARD_POS_X=253
MAPBOARD_POS_Y=101
SHEET_QUEST_POS_X=497
SHEET_QUEST_POS_Y=149
SHEET_SUB_POS_X=692
SHEET_SUB_POS_Y=153
SHEET_STATUS_POS_X=422
SHEET_STATUS_POS_Y=165
MINIMAP_POS_X=854
MINIMAP_POS_Y=0
QUICKSLOT_POS_X=409
QUICKSLOT_POS_Y=0
QUICKSLOT_CHANGE=FALSE
QUICKSLOT_PLUS=TRUE
QUICKSLOTPLUS_POS_X=971
QUICKSLOTPLUS_POS_Y=180
QUICKSLOTPLUS_CHANGE=TRUE
QUICKSLOTPOTION_POS_X=945
QUICKSLOTPOTION_POS_Y=620
REVOLVERSLOT_POS_X=872
REVOLVERSLOT_POS_Y=6
REVOLVERADDSLOT_PAGE=0
REVOLVERSLOT_CHANGE=HORIZONTAL
REVOLVERADDSLOT=ON
SHEET_SKILL_POS_X=624
SHEET_SKILL_POS_Y=161
SHEET_ITEM_POS_X=698
SHEET_ITEM_POS_Y=161
SHEET_GUILD_POS_X=668
SHEET_GUILD_POS_Y=155
CHAT_POS_X=0
CHAT_POS_Y=391
CHAT_SIZE0=5
CHAT_SIZE1=3
SHEET_OPTION_POS_X=696
SHEET_OPTION_POS_Y=167
[INTERFACE_1600X900]
SHEET_STATUS_POS_X=361
SHEET_STATUS_POS_Y=176
SHEET_ITEM_POS_X=948
SHEET_ITEM_POS_Y=86
SHEET_SKILL_POS_X=872
SHEET_SKILL_POS_Y=83
SHEET_SUB_POS_X=837
SHEET_SUB_POS_Y=105
SHEET_QUEST_POS_X=310
SHEET_QUEST_POS_Y=131
SHEET_GUILD_POS_X=392
SHEET_GUILD_POS_Y=133
SHEET_OPTION_POS_X=952
SHEET_OPTION_POS_Y=142
STATUSINFOBAR_POS_X=197
STATUSINFOBAR_POS_Y=4
STATUSMINIBAR_POS_X=0
STATUSMINIBAR_POS_Y=6
PARTYMINIBAR_POS_X=999
PARTYMINIBAR_POS_Y=102
CHAT_POS_X=0
CHAT_POS_Y=167
CHAT_SIZE0=12
CHAT_SIZE1=10
QUICKSLOT_POS_X=404
QUICKSLOT_POS_Y=0
QUICKSLOT_CHANGE=FALSE
QUICKSLOT_PLUS=TRUE
QUICKSLOTPLUS_POS_X=1274
QUICKSLOTPLUS_POS_Y=201
QUICKSLOTPLUS_CHANGE=TRUE
REVOLVERSLOT_POS_X=865
REVOLVERSLOT_POS_Y=0
REVOLVERADDSLOT_PAGE=0
REVOLVERSLOT_CHANGE=HORIZONTAL
REVOLVERADDSLOT=OFF
QUICKSLOTPOTION_POS_X=1173
QUICKSLOTPOTION_POS_Y=494
MINIMAP_POS_X=1190
MINIMAP_POS_Y=0
MAPBOARD_POS_X=165
MAPBOARD_POS_Y=113
ATTACK_ALERT_POS_X=400
ATTACK_ALERT_POS_Y=200
QUESTMINI_POS_X=0
QUESTMINI_POS_Y=139
[INTERFACE_1600X1200]
SHEET_STATUS_POS_X=422
SHEET_STATUS_POS_Y=165
SHEET_ITEM_POS_X=836
SHEET_ITEM_POS_Y=73
SHEET_SKILL_POS_X=623
SHEET_SKILL_POS_Y=161
SHEET_SUB_POS_X=692
SHEET_SUB_POS_Y=153
SHEET_QUEST_POS_X=497
SHEET_QUEST_POS_Y=149
SHEET_GUILD_POS_X=668
SHEET_GUILD_POS_Y=155
SHEET_OPTION_POS_X=935
SHEET_OPTION_POS_Y=163
STATUSINFOBAR_POS_X=184
STATUSINFOBAR_POS_Y=7
STATUSMINIBAR_POS_X=0
STATUSMINIBAR_POS_Y=68
PARTYMINIBAR_POS_X=100
PARTYMINIBAR_POS_Y=100
CHAT_POS_X=0
CHAT_POS_Y=135
CHAT_SIZE0=13
CHAT_SIZE1=11
QUICKSLOT_POS_X=375
QUICKSLOT_POS_Y=0
QUICKSLOT_CHANGE=FALSE
QUICKSLOT_PLUS=TRUE
QUICKSLOTPLUS_POS_X=1304
QUICKSLOTPLUS_POS_Y=144
QUICKSLOTPLUS_CHANGE=TRUE
REVOLVERSLOT_POS_X=825
REVOLVERSLOT_POS_Y=4
REVOLVERADDSLOT_PAGE=0
REVOLVERSLOT_CHANGE=HORIZONTAL
REVOLVERADDSLOT=OFF
QUICKSLOTPOTION_POS_X=1210
QUICKSLOTPOTION_POS_Y=403
MINIMAP_POS_X=1190
MINIMAP_POS_Y=0
MAPBOARD_POS_X=253
MAPBOARD_POS_Y=101
ATTACK_ALERT_POS_X=400
ATTACK_ALERT_POS_Y=200

[APPLICATION]
ALLOW_MULTIPROCESS = TRUE
[ADVANCED]
PETS=TRUE
WINGS=TRUE
COSTUMES=TRUE
EFFECTS=TRUE";

                    // Scrive il file predefinito
                    File.WriteAllText(filePath, defaultConfig);

                    MessageBox.Show("CONFIG.INI not found. A default configuration file has been created.",
                        "File Created", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Apri la finestra delle impostazioni grafiche
                GraphicsSettingWindow settingsWindow = new GraphicsSettingWindow(filePath);
                // Allinea la finestra delle impostazioni grafiche al centro del launcher
                settingsWindow.Owner = this;
                settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                settingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while managing CONFIG.INI: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window1_Initialized(object sender, EventArgs e)
        {
            if (DllImport.FindWindowW(null, Application.Current.MainWindow.Title) != IntPtr.Zero)
            {
                var caption = Application.ResourceAssembly.GetName().Name;
                MessageBox.Show(Strings.Message1, caption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                Application.Current.Shutdown(0);
            }

            if (DllImport.FindWindowW("GAME", "Shaiya") != IntPtr.Zero)
            {
                var caption = Application.ResourceAssembly.GetName().Name;
                MessageBox.Show(Strings.Message2, caption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                Application.Current.Shutdown(0);
            }

            var icon3 = BitmapImageHelper.FromManifestResource("Icon3.ico");
            if (icon3 != null)
            {
                _icon3.Width = icon3.PixelWidth;
                _icon3.Height = icon3.PixelHeight;
                _icon3.Source = icon3;
            }

            var image167 = BitmapImageHelper.FromManifestResource("Bitmap167.bmp");
            if (image167 != null)
            {
                _image167.Width = image167.PixelWidth;
                _image167.Height = image167.PixelHeight;
                _image167.Source = image167;
            }

            var image168 = BitmapImageHelper.FromManifestResource("Bitmap168.bmp");
            if (image168 != null)
            {
                _image168.Width = image168.PixelWidth;
                _image168.Height = image168.PixelHeight;
                _image168.Source = image168;
            }

            var image169 = BitmapImageHelper.FromManifestResource("Bitmap169.bmp");
            if (image169 != null)
            {
                _image169.Width = image169.PixelWidth;
                _image169.Height = image169.PixelHeight;
                _image169.Source = image169;
            }

            var image170 = BitmapImageHelper.FromManifestResource("Bitmap170.bmp");
            if (image170 != null)
            {
                _image170.Width = image170.PixelWidth;
                _image170.Height = image170.PixelHeight;
                _image170.Source = image170;
            }

            var image185 = BitmapImageHelper.FromManifestResource("Bitmap185.bmp");
            if (image185 != null)
            {
                _image185.Width = image185.PixelWidth;
                _image185.Height = image185.PixelHeight;
                _image185.Source = image185;
            }

            var image187 = BitmapImageHelper.FromManifestResource("Bitmap187.bmp");
            if (image187 != null)
            {
                _image187.Width = image187.PixelWidth;
                _image187.Height = image187.PixelHeight;
                _image187.Source = image187;
            }

            var image188 = BitmapImageHelper.FromManifestResource("Bitmap188.bmp");
            if (image188 != null)
            {
                _image188.Width = image188.PixelWidth;
                _image188.Height = image188.PixelHeight;
                _image188.Source = image188;
            }
        }

        private void Window1_Loaded(object sender, RoutedEventArgs e)
        {
            _window1.Background = new ImageBrush(_image167.Source);
            _window1.Icon = _icon3.Source;
            _button1.Content = _image185;
            _button2.Content = _image168;
            _webBrowser1.Navigate(Constants.WebBrowserSource);
            _backgroundWorker1.RunWorkerAsync();
        }

        private void Window1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            if (_backgroundWorker1.IsBusy)
                return;

            Application.Current.Shutdown(0);
        }

        private void Button1_MouseEnter(object sender, MouseEventArgs e)
        {
            _button1.Content = _image187;
        }

        private void Button1_MouseLeave(object sender, MouseEventArgs e)
        {
            _button1.Content = _image185;
        }

        private void Button1_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _button1.Content = _image188;
        }

        private void Button1_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _button1.Content = _image187;
        }

        private async void Button2_Click(object sender, RoutedEventArgs e)
        {
            _button2.IsEnabled = false; // Disabilita il pulsante per evitare doppi click
            _button2.Content = _image170;

            if (_backgroundWorker1.IsBusy)
            {
                _button2.IsEnabled = true; // Riabilita solo se non si avvia il gioco
                return;
            }

            try
            {
                var gamePath = Path.Combine(Directory.GetCurrentDirectory(), "game.exe");
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = gamePath,
                        UseShellExecute = true,
                        WorkingDirectory = Directory.GetCurrentDirectory()
                    }
                };
                process.Start();

                // Attendi 10 secondi e poi chiudi il launcher
                await Task.Delay(10000);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                var caption = Application.ResourceAssembly.GetName().Name;
                MessageBox.Show(ex.Message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
                _button2.IsEnabled = true; // Riabilita il pulsante solo in caso di errore
            }
        }

        private void Button2_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _button2.Content = _image169;
        }

        private void Button2_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _button2.Content = _image168;
        }

        private void Button2_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _button2.Content = _image169;
        }

        private void ButtonWebsite_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://duff.pinto-lime.ts.net/");
        }

        private void ButtonDiscord_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://discord.com/channels/1309994884563861526");
        }

        private void ButtonItemmall_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://duff.pinto-lime.ts.net/?p=itemmall&category=3");
        }

        private void ButtonRanks_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://duff.pinto-lime.ts.net/?p=ranks");
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true // Necessario per aprire il browser predefinito
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nell'apertura del link: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


    }
}
