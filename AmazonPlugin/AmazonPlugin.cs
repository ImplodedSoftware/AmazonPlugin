using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using ImPluginEngine.Abstractions.Interfaces;
using ImPluginEngine.Helpers;
using ImPluginEngine.Types;
using Nager.AmazonProductAdvertising;
using Nager.AmazonProductAdvertising.Model;
using Newtonsoft.Json;

namespace AmazonPlugin
{
    public class AmazonPlugin : IPlugin, IAlbumPicture, IPluginConfig
    {
        public string Name => "Amazon plug-in";
        public string Version => "1.0";

        private string downloadImage(string imageUrl)
        {
            var uid = Guid.NewGuid().ToString();
            var ext = imageUrl.Substring(imageUrl.LastIndexOf(".", StringComparison.CurrentCultureIgnoreCase));
            uid = uid + ext;
            var pFile = Path.Combine(PluginConstants.TempPath, uid);
            File.WriteAllBytes(pFile, new WebClient().DownloadData(imageUrl));
            return pFile;
        }
        public async Task GetAlbumPicture(PluginAlbum album, CancellationToken ct, Action<PluginImage> updateAction)
        {
            var expr = album.AlbumArtist + " - " + album.AlbumName;
            var authentication = new AmazonAuthentication
            {
                AccessKey = AmazonConstants.AWS_ACCESS_KEY_ID,
                SecretKey = AmazonConstants.AWS_SECRET_KEY
            };

            var endPoint = AmazonEndpoint.US;
            var searchType = AmazonSearchIndex.Music;
            var settingsFile = Path.Combine(PluginConstants.SettingsPath, "amazon.json");
            if (File.Exists(settingsFile))
            {
                var json = File.ReadAllText(settingsFile);
                var sf = JsonConvert.DeserializeObject<AmazonSettings>(json);
                endPoint = sf.Endpoint;
                searchType = sf.SearchType;
            }
            AmazonItemResponse result = null;
            await Task.Run(() =>
            {
                try
                {
                    var wrapper = new AmazonWrapper(authentication, endPoint, "neon");
                    result = wrapper.Search(expr, searchType, AmazonResponseGroup.Large);
                }
                catch
                {
                    result = null;
                }
            }, ct);

            if (result?.Items?.Item == null)
                return;
            foreach (var item in result.Items.Item)
            {
                try
                {
                    var url = string.Empty;
                    if (item.LargeImage != null && !string.IsNullOrEmpty(item.LargeImage.URL))
                    {
                        url = item.LargeImage.URL;
                    }
                    if (string.IsNullOrEmpty(url) && item.MediumImage != null &&
                        !string.IsNullOrEmpty(item.MediumImage.URL))
                    {
                        url = item.MediumImage.URL;
                    }
                    if (string.IsNullOrEmpty(url) && item.SmallImage != null &&
                        !string.IsNullOrEmpty(item.SmallImage.URL))
                    {
                        url = item.SmallImage.URL;
                    }
                    if (!string.IsNullOrEmpty(url))
                    {
                        var res = new PluginImage {Filename = downloadImage(url)};
                        using (var stream = new FileStream(res.Filename, FileMode.Open, FileAccess.Read, FileShare.Read)
                            )
                        {
                            var bitmapFrame = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation,
                                BitmapCacheOption.None);
                            res.Width = bitmapFrame.PixelWidth;
                            res.Height = bitmapFrame.PixelHeight;
                        }
                        res.Id = album.Id;
                        res.FoundByPlugin = Name;
                        updateAction(res);
                    }
                }
                catch
                {
                    // tolerate exceptions
                }
            }
        }

        public void ConfigurePlugin()
        {
            var dlg = new AmazonConfigForm();
            var res = dlg.ShowDialog();
            if (res == DialogResult.OK)
                dlg.SaveSettings();
        }
    }
}
