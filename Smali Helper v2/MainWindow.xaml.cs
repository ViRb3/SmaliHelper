using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Xps.Packaging;
using System.Xml;
using Path = System.IO.Path;

namespace Smali_Helper_v2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            if (!Directory.Exists("HelpFiles"))
            {
                MessageBox.Show("Help files not found!", "Error");
                return;
            }
            this.List();
        }

        private void List()
        {
            string[] helpCategories = Directory.GetDirectories(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "HelpFiles"));
            List<TreeViewItem> items = new List<TreeViewItem>();

            foreach (string path in helpCategories)
            {
                TreeViewItem item = new TreeViewItem();
                item.Header = Path.GetFileNameWithoutExtension(path);

                string[] helpFiles = Directory.GetFiles(path);
                List<TreeViewItem> subItems = new List<TreeViewItem>();

                for (int j = 0; j < helpFiles.Length; j++)
                {
                    string text = helpFiles[j];
                    TreeViewItem newItem = new TreeViewItem();
                    newItem.Header = Path.GetFileNameWithoutExtension(text).Replace("#", "/");
                    newItem.Tag = text;

                    subItems.Add(newItem);
                }

                foreach (TreeViewItem currentSubItem in
                    from node in subItems
                    orderby node.Header
                    select node)
                {
                    item.Items.Add(currentSubItem);
                }

                items.Add(item);
                subItems.Clear();
            }

            foreach (TreeViewItem currentItem in from a in items
                orderby a.Header
                select a)
            {
                this.treeView.Items.Add(currentItem);
            }
        }

        private void Read(string file)
        {
            BackgroundWorker backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += new DoWorkEventHandler(this.BackgroundWorker_DoWork);
            backgroundWorker.RunWorkerAsync(file);
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string rawXamlText;

            using (StreamReader streamReader = File.OpenText(e.Argument as string))
            {
                rawXamlText = streamReader.ReadToEnd();
            }

            this.Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    FlowDocument flowDocument = XamlReader.Load(new XmlTextReader(new StringReader(rawXamlText))) as FlowDocument;
                    var paginator = ((IDocumentPaginatorSource)flowDocument).DocumentPaginator;
                    var package = Package.Open(new MemoryStream(), FileMode.Create, FileAccess.ReadWrite);
                    var packUri = new Uri("pack://temp.xps");
                    PackageStore.RemovePackage(packUri);
                    PackageStore.AddPackage(packUri, package);
                    var xps = new XpsDocument(package, CompressionOption.NotCompressed, packUri.ToString());
                    XpsDocument.CreateXpsDocumentWriter(xps).Write(paginator);

                    FixedDocument fixedDocument = xps.GetFixedDocumentSequence().References[0].GetDocument(true);

                    this.documentViewer.Document = fixedDocument;
                    this.documentViewer.Zoom = 125;
                }));
        }

        private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItem item = e.NewValue as TreeViewItem;

            if (item?.Tag == null)
                return;

            Read(item.Tag.ToString());
        }
    }
}