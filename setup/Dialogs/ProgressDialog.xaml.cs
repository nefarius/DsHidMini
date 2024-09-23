using System.Security.Principal;
using System.Windows.Media.Imaging;

using WixSharp;
using WixSharp.CommonTasks;
using WixSharp.UI.Forms;
using WixSharp.UI.WPF;

using WixToolset.Dtf.WindowsInstaller;

namespace Nefarius.DsHidMini.Setup.Dialogs
{
    /// <summary>
    /// The standard ProgressDialog.
    /// </summary>
    /// <seealso cref="WixSharp.UI.WPF.WpfDialog" />
    /// <seealso cref="WixSharp.IWpfDialog" />
    /// <seealso cref="System.Windows.Markup.IComponentConnector" />
    public partial class ProgressDialog : WpfDialog, IWpfDialog, IProgressDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressDialog" /> class.
        /// </summary>
        public ProgressDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// This method is invoked by WixSharp runtime when the custom dialog content is internally fully initialized.
        /// This is a convenient place to do further initialization activities (e.g. localization).
        /// </summary>
        public void Init()
        {
            UpdateTitles(ManagedFormHost.Runtime.Session);

            DataContext = model = new ProgressDialogModel { Host = ManagedFormHost };
            model.StartExecute();
        }

        /// <summary>
        /// Updates the titles of the dialog depending on what type of installation action MSI is performing.
        /// </summary>
        /// <param name="session">The session.</param>
        public void UpdateTitles(ISession session)
        {
            if (session.IsUninstalling())
            {
                DialogTitleLabel.Text = "[ProgressDlgTitleRemoving]";
                DialogDescription.Text = "[ProgressDlgTextRemoving]";
            }
            else if (session.IsRepairing())
            {
                DialogTitleLabel.Text = "[ProgressDlgTextRepairing]";
                DialogDescription.Text = "[ProgressDlgTitleRepairing]";
            }
            else if (session.IsInstalling())
            {
                DialogTitleLabel.Text = "[ProgressDlgTitleInstalling]";
                DialogDescription.Text = "[ProgressDlgTextInstalling]";
            }

            // `Localize` resolves [...] titles and descriptions into the localized strings stored in MSI resources tables
            this.Localize();
        }

        ProgressDialogModel model;

        /// <summary>
        /// Processes information and progress messages sent to the user interface.
        /// <para> This method directly mapped to the
        /// <see cref="T:Microsoft.Deployment.WindowsInstaller.IEmbeddedUI.ProcessMessage" />.</para>
        /// </summary>
        /// <param name="messageType">Type of the message.</param>
        /// <param name="messageRecord">The message record.</param>
        /// <param name="buttons">The buttons.</param>
        /// <param name="icon">The icon.</param>
        /// <param name="defaultButton">The default button.</param>
        /// <returns></returns>
        public override MessageResult ProcessMessage(InstallMessage messageType, Record messageRecord, MessageButtons buttons, MessageIcon icon, MessageDefaultButton defaultButton)
            => model?.ProcessMessage(messageType, messageRecord, CurrentStatus.Text) ?? MessageResult.None;

        /// <summary>
        /// Called when MSI execution is complete.
        /// </summary>
        public override void OnExecuteComplete()
            => model?.OnExecuteComplete();

        /// <summary>
        /// Called when MSI execution progress is changed.
        /// </summary>
        /// <param name="progressPercentage">The progress percentage.</param>
        public override void OnProgress(int progressPercentage)
        {
            if (model != null)
                model.ProgressValue = progressPercentage;
        }

        void Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
            => model.Cancel();
    }

    /// <summary>
    /// ViewModel for standard ProgressDialog.
    /// </summary>
    internal class ProgressDialogModel : NotifyPropertyChangedBase
    {
        public ManagedForm Host;

        ISession session => Host?.Runtime.Session;
        IManagedUIShell shell => Host?.Shell;

        public BitmapImage Banner => session?.GetResourceBitmap("WixSharpUI_Bmp_Banner").ToImageSource() ??
                                     session?.GetResourceBitmap("WixUI_Bmp_Banner").ToImageSource();

        public bool UacPromptIsVisible => (!WindowsIdentity.GetCurrent().IsAdmin() && Uac.IsEnabled() && !uacPromptActioned);

        public string CurrentAction { get => currentAction; set { currentAction = value; base.NotifyOfPropertyChange(nameof(CurrentAction)); } }
        public int ProgressValue { get => progressValue; set { progressValue = value; base.NotifyOfPropertyChange(nameof(ProgressValue)); } }

        bool uacPromptActioned = false;
        string currentAction;
        int progressValue;

        public string UacPrompt
        {
            get
            {
                if (Uac.IsEnabled())
                {
                    var prompt = session?.Property("UAC_WARNING");
                    if (prompt.IsNotEmpty())
                        return prompt;
                    else
                        return
                            "Please wait for UAC prompt to appear. " +
                            "If it appears minimized then activate it from the taskbar.";
                }
                else
                    return null;
            }
        }

        public void StartExecute()
            => shell?.StartExecute();

        public void Cancel()
        {
            if (shell.IsDemoMode)
                shell.GoNext();
            else
                shell.Cancel();
        }

        public MessageResult ProcessMessage(InstallMessage messageType, Record messageRecord, string currentStatus)
        {
            switch (messageType)
            {
                case InstallMessage.InstallStart:
                case InstallMessage.InstallEnd:
                    {
                        uacPromptActioned = true;
                        base.NotifyOfPropertyChange(nameof(UacPromptIsVisible));
                    }
                    break;

                case InstallMessage.ActionStart:
                    {
                        try
                        {
                            //messageRecord[0] - is reserved for FormatString value

                            /*
                            messageRecord[2] unconditionally contains the string to display

                            Examples:

                               messageRecord[0]    "Action 23:14:50: [1]. [2]"
                               messageRecord[1]    "InstallFiles"
                               messageRecord[2]    "Copying new files"
                               messageRecord[3]    "File: [1],  Directory: [9],  Size: [6]"

                               messageRecord[0]    "Action 23:15:21: [1]. [2]"
                               messageRecord[1]    "RegisterUser"
                               messageRecord[2]    "Registering user"
                               messageRecord[3]    "[1]"

                            */

                            if (messageRecord.FieldCount >= 3)
                                CurrentAction = messageRecord[2].ToString();
                            else
                                CurrentAction = null;
                        }
                        catch
                        {
                            //Catch all, we don't want the installer to crash in an attempt to process message.
                        }
                    }
                    break;
            }
            return MessageResult.OK;
        }

        public void OnExecuteComplete()
        {
            CurrentAction = null;
            shell?.GoNext();
        }
    }
}