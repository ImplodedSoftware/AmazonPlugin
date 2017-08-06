using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ImPluginEngine.Helpers;
using Nager.AmazonProductAdvertising.Model;
using Newtonsoft.Json;

namespace AmazonPlugin
{
    public partial class AmazonConfigForm : Form
    {
        public AmazonConfigForm()
        {
            InitializeComponent();
            var settingsFile = Path.Combine(PluginConstants.SettingsPath, "amazon.json");
            if (File.Exists(settingsFile))
            {
                var json = File.ReadAllText(settingsFile);
                var sf = JsonConvert.DeserializeObject<AmazonSettings>(json);
                comboBoxCatalog.SelectedIndex = (int) sf.Endpoint;
                if (sf.SearchType == AmazonSearchIndex.Music)
                    comboBoxSearchType.SelectedIndex = 0;
                else
                {
                    comboBoxSearchType.SelectedIndex = 1;
                }
            }
            else
            {
                comboBoxCatalog.SelectedIndex = 11;
                comboBoxSearchType.SelectedIndex = 0;
            }
        }

        public void SaveSettings()
        {
            var sf = new AmazonSettings();
            sf.Endpoint = (AmazonEndpoint) comboBoxCatalog.SelectedIndex;
            if (comboBoxSearchType.SelectedIndex == 0)
                sf.SearchType = AmazonSearchIndex.Music;
            else
            {
                sf.SearchType = AmazonSearchIndex.Classical;
            }
            var settingsFile = Path.Combine(PluginConstants.SettingsPath, "amazon.json");
            var json = JsonConvert.SerializeObject(sf);
            File.WriteAllText(settingsFile, json);
        }
    }
}
