using System.Drawing;
using System.Windows.Forms;

namespace Zutatensuppe.DiabloInterface.Plugin.PipeServer
{
    class ConfigEditRenderer : IPluginConfigEditRenderer
    {
        private Plugin plugin;
        private FlowLayoutPanel control;
        private TextBox textBoxPipeName;
        private CheckBox chkPipeServerEnabled;
        private RichTextBox txtPipeServer;

        public ConfigEditRenderer(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public Control CreateControl()
        {
            var labelPipeName = new Label();
            labelPipeName.AutoSize = true;
            labelPipeName.Size = new Size(288, 20);
            labelPipeName.Text = "Pipe Name:";

            textBoxPipeName = new TextBox();
            textBoxPipeName.Size = new Size(288, 20);

            chkPipeServerEnabled = new CheckBox();
            chkPipeServerEnabled.AutoSize = true;
            chkPipeServerEnabled.Size = new Size(288, 20);
            chkPipeServerEnabled.Text = "Enable";

            var lblPipeServerStatus = new Label();
            lblPipeServerStatus.AutoSize = true;
            lblPipeServerStatus.Size = new Size(288, 20);
            lblPipeServerStatus.Text = "Status:";

            txtPipeServer = new RichTextBox();
            txtPipeServer.ReadOnly = true;
            txtPipeServer.Size = new Size(288, 34);
            txtPipeServer.Text = "";

            control = new FlowLayoutPanel();
            control.FlowDirection = FlowDirection.TopDown;
            control.Controls.Add(labelPipeName);
            control.Controls.Add(textBoxPipeName);
            control.Controls.Add(chkPipeServerEnabled);
            control.Controls.Add(lblPipeServerStatus);
            control.Controls.Add(txtPipeServer);
            control.Dock = DockStyle.Fill;
            return control;
        }

        public bool IsDirty()
        {
            return plugin.Config.PipeName != textBoxPipeName.Text
                || plugin.Config.Enabled != chkPipeServerEnabled.Checked;
        }

        public IPluginConfig GetEditedConfig()
        {
            var conf = new Config();
            conf.PipeName = textBoxPipeName.Text;
            conf.Enabled = chkPipeServerEnabled.Checked;
            return conf;
        }

        public void ApplyConfig()
        {
            textBoxPipeName.Text = plugin.Config.PipeName;
            chkPipeServerEnabled.Checked = plugin.Config.Enabled;
        }

        public void ApplyChanges()
        {
            txtPipeServer.Text = plugin.StatusTextMsg();
        }
    }
}
