using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace TribalWarsTribe
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Tribe tribe = new Tribe();

        string last_data_used = "";

        public MainWindow()
        {
            InitializeComponent();

        }

        private void Reload(string data = "", bool reloadOnlineData = false)
        {
            if (data == "")
                data = last_data_used;

            tribe = new Tribe();
            tribe.LoadData(data, reloadOnlineData);

            last_data_used = data;

            tbServer.Text = tribe.world_link;
            tbPacketSize.Text = tribe.PacketSize;

            grdPackets.ItemsSource = tribe.GetCalculatedVillageSupportList();
        }

        private void grdPackets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VillageSupport s = (VillageSupport)grdPackets.SelectedItem;

            if (s == null)
                return;

            Village v = tribe.bweb.GetVillageByCoord(s.Village);
            Player p = tribe.bweb.GetPlayerByID(v.Owner);

            tbVillageName.Text = BrowseWeb.NormalizeString(v.Name);
            tbVillageOwner.Text = p.Name.Replace('+', ' ') + $" ({p.Points}p)";
            tbVillageCoords.Text = s.Village;

            tbSupportBy.Text = tribe.GetSupportsByCoords(v.Pos);
            if (tbSupportBy.Text == string.Empty)
                tbSupportBy.Text = "This village has no help :(";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            VillageSupport s = (VillageSupport)grdPackets.SelectedItem;

            Village v = tribe.bweb.GetVillageByCoord(s.Village);

            System.Diagnostics.Process.Start($"https://{tribe.world_link}.tribalwars.net/game.php?screen=info_village&id={v.ID}");
        }

        private void btnDataImport_Click(object sender, RoutedEventArgs e)
        {
            var bytes = Convert.FromBase64String(tbData.Text);

            Reload(Encoding.UTF8.GetString(bytes), true);

            tbData.Text = "";
        }

        private void btnDataExport_Click(object sender, RoutedEventArgs e)
        {

            tbData.Text = Convert.ToBase64String(Encoding.ASCII.GetBytes(tribe.GetFile()));
        }

        private void btnUpdatePackets_Click(object sender, RoutedEventArgs e)
        {
            Player player = tribe.bweb.GetPlayerByName(tbSupporter.Text);
            if (player == null)
            {
                MessageBox.Show("User not found with this username", "User not found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            tribe.AddPlayerSupports(player, tribe.bweb.GetVillageByCoord(tbVillageCoords.Text), int.Parse(tbPacketsChanges.Text));

            tbData.Text = Convert.ToBase64String(Encoding.ASCII.GetBytes(tribe.GetFile()));
            btnDataImport_Click(null, null);
        }

        private void btnUpdateVillageToSupport_Click(object sender, RoutedEventArgs e)
        {
            Village v = tribe.bweb.GetVillageByCoord(tbVillageToSupport.Text);
            if (v == null)
            {
                MessageBox.Show("Village not found in this position", "Village not found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            tribe.AddRequiredSupports(v, int.Parse(tbPacketVillageToSupport.Text));

            tbData.Text = Convert.ToBase64String(Encoding.ASCII.GetBytes(tribe.GetFile()));
            btnDataImport_Click(null, null);
        }

        private void btnUpdateHeaderInfo_Click(object sender, RoutedEventArgs e)
        {
            tribe.PacketSize = tbPacketSize.Text;
            tribe.world_link = tbServer.Text;

            tribe.ReloadBweb();
        }
    }
}
