using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using TXTextControl.DocumentServer.Fields;

namespace tx_sample_html5
{
    public partial class index : System.Web.UI.Page
    {
        protected override void Render(HtmlTextWriter writer)
        {
            // register the hidden button event
            Page.ClientScript.RegisterForEventValidation(hiddenBtnUpdateNavigationPanel.UniqueID);
            base.Render(writer);
        }

        // read all blocks from the current document and fill the TreeView
        private void updateNavigationPanel()
        {
            List<MergeBlock> blocks;

            try
            {
                using (TXTextControl.ServerTextControl tx = new TXTextControl.ServerTextControl())
                {
                    tx.Create();
                    byte[] data = null;

                    TextControl1.SaveText(out data, TXTextControl.Web.BinaryStreamType.InternalUnicodeFormat);
                    tx.Load(data, TXTextControl.BinaryStreamType.InternalUnicodeFormat);

                    blocks = MergeBlock.GetMergeBlocks(MergeBlock.GetBlockMarkersOrdered(tx), tx);
                }

                TreeView1.Nodes.Clear();
                fillTreeView(blocks);
                TreeView1.ExpandAll();
            }
            catch { }
        }

        // fill the main nodes of the TreeView
        private void fillTreeView(IList<MergeBlock> blocks)
        {
            if (blocks == null) return;

            foreach (MergeBlock block in blocks)
            {
                TreeNode tnMain = new TreeNode(block.Name);
                tnMain.ImageUrl = "images/block.png";
                TreeView1.Nodes.Add(tnMain);

                fillMergeFields(block, tnMain);

                if(block.Children != null)
                    fillChildren(block.Children, tnMain);
            }
        }

        // fill all the children nodes
        private void fillChildren(IList<MergeBlock> blocks, TreeNode node)
        {
            if (blocks == null) return;

            foreach (MergeBlock block in blocks)
            {
                TreeNode tnChildren = new TreeNode(block.Name);
                tnChildren.ImageUrl = "images/block.png";

                node.ChildNodes.Add(tnChildren);

                fillMergeFields(block, tnChildren);

                if (block.Children != null)
                    fillChildren(block.Children, tnChildren);
            }
        }

        // add the merge fields to the nodes
        private void fillMergeFields(MergeBlock block, TreeNode node)
        {
            foreach(MergeField field in block.Fields)
            {
                TreeNode tnField = new TreeNode(field.Name);
                tnField.ImageUrl = "images/field.png";

                node.ChildNodes.Add(tnField);
            }
        }

        protected void hiddenBtnUpdateNavigationPanel_Click(object sender, EventArgs e)
        {
            updateNavigationPanel();
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // load a default sample document
            if(IsPostBack != true)
                TextControl1.LoadTextAsync(Server.MapPath("invoice.docx"), TXTextControl.Web.StreamType.WordprocessingML);
        }

        protected void TreeView1_SelectedNodeChanged(object sender, EventArgs e)
        {
            List<MergeBlock> blocks;

            using (TXTextControl.ServerTextControl tx = new TXTextControl.ServerTextControl())
            {
                tx.Create();
                byte[] data = null;

                TextControl1.SaveText(out data, TXTextControl.Web.BinaryStreamType.InternalUnicodeFormat);
                tx.Load(data, TXTextControl.BinaryStreamType.InternalUnicodeFormat);

                blocks = MergeBlock.GetMergeBlocksFlattened(tx);
            }

            // select the selected block in the TextControl.Web
            foreach (MergeBlock block in blocks)
            {
                if (block.Name == TreeView1.SelectedValue)
                {
                    TextControl1.Selection = new TXTextControl.Web.Selection(block.StartMarker.Start, block.Length);
                    break;
                }
            }
        }
    }
}