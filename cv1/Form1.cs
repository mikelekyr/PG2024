using cv1.Enums;
using cv1.Network;
using cv1.Tools;
using Microsoft.VisualBasic.Devices;
using System.Runtime.Serialization;
using System.Windows.Forms;
using System.Xml;

namespace cv1
{
    public partial class Form1 : Form
    {
        private EnumEditorState state = EnumEditorState.None;
        private EnumEditorMode mode = EnumEditorMode.Edit;

        private Point startMousePos;
        private Point currentMousePos;
        private NetworkData network;
        private bool framedSelectionBox = true;
        private NetworkNode? edgeNodeStart = null;
        private string projectPath; 

        public Form1()
        {
            InitializeComponent();
        }

        private void doubleBufferPanelDrawing_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            if (network != null)
            {
                network.Draw(g);
            }

            if (mode == EnumEditorMode.Edit)
            {
                if (state == EnumEditorState.Selecting)
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;

                    if (framedSelectionBox)
                    {
                        // selection box without frame
                        SelectionBoxFramed.Draw(g);
                    }
                    else
                    {
                        // selection box with frame
                        SelectionBox.Draw(g);
                    }
                }
            }
            if (mode == EnumEditorMode.InsertEdge)
            {
                if (edgeNodeStart != null && currentMousePos.X != 0 && currentMousePos.Y != 0)
                {
                    using Pen p = new(Color.DarkGray, 2.0f);
                    g.DrawLine(p, edgeNodeStart.Position, currentMousePos);
                }
            }
        }

        private void doubleBufferPanelDrawing_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (mode == EnumEditorMode.Edit)
                {
                    if (state == EnumEditorState.PossibleDrag)
                    {
                        state = EnumEditorState.NodeDragging;
                        startMousePos = e.Location;
                    }
                    else
                    {
                        bool ctrlPressed = (network.Key == Keys.ControlKey);

                        if (!network.SelectNode(e.Location, ctrlPressed))
                        {
                            network.SelectNode(new Rectangle(), ctrlPressed);
                            state = EnumEditorState.SelectBegin;

                            if (framedSelectionBox)
                                SelectionBoxFramed.InitSelectionBox(e.Location);
                            else
                                SelectionBox.InitSelectionBox(e.Location);
                        }
                    }
                }
                else if (mode == EnumEditorMode.InsertNode)
                {
                    network.InsertNode(e.Location);
                }
                else if (mode == EnumEditorMode.InsertEdge)
                {
                    edgeNodeStart = network.IsNodeHitByMouse(e.Location);

                    if (edgeNodeStart != null)
                        state = EnumEditorState.InsertEdge;
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
            }

            doubleBufferPanelDrawing.Invalidate();
        }

        private void doubleBufferPanelDrawing_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (mode == EnumEditorMode.Edit)
                {
                    if (state == EnumEditorState.NodeDragging)
                    {
                        network.NodeDrawingOffset = new(e.Location.X - startMousePos.X, e.Location.Y - startMousePos.Y);
                    }
                    else if (state == EnumEditorState.Selecting || state == EnumEditorState.SelectBegin)
                    {
                        state = EnumEditorState.Selecting;

                        if (framedSelectionBox)
                        {
                            using Region r = SelectionBoxFramed.Track(e.Location);
                            doubleBufferPanelDrawing.Invalidate(r);
                        }
                        else
                        {
                            using Region r = SelectionBox.Track(e.Location);
                            doubleBufferPanelDrawing.Invalidate(r);
                        }

                        return;
                    }
                }
                if (mode == EnumEditorMode.InsertEdge)
                {
                    currentMousePos = e.Location;
                }
            }
            else if (e.Button == MouseButtons.Right)
            {

            }
            else if (e.Button == MouseButtons.None)
            {
                if (network.HoverOverSelectedNode(e.Location))
                {
                    state = EnumEditorState.PossibleDrag;
                    Cursor = Cursors.Hand;
                }
                else
                {
                    state = EnumEditorState.None;
                    Cursor = Cursors.Default;
                }
            }

            doubleBufferPanelDrawing.Invalidate();
        }

        private void doubleBufferPanelDrawing_MouseUp(object sender, MouseEventArgs e)
        {
            if (mode == EnumEditorMode.Edit)
            {
                if (state == EnumEditorState.NodeDragging)
                {
                    network.UpdateNodesPositionsAfterDrag();
                }
                else if (state == EnumEditorState.Selecting)
                {
                    bool ctrlPressed = (network.Key == Keys.ControlKey);

                    if (framedSelectionBox)
                        network.SelectNode(SelectionBoxFramed.TrackedRectangle, ctrlPressed);
                    else
                        network.SelectNode(SelectionBox.TrackedRectangle, ctrlPressed);
                }
            }
            if (mode == EnumEditorMode.InsertEdge)
            {
                if (edgeNodeStart != null)
                {
                    NetworkNode? edgeNodeEnd = network.IsNodeHitByMouse(e.Location);

                    if (edgeNodeEnd != null)
                    {
                        if (!network.InsertEdge(edgeNodeStart, edgeNodeEnd))
                            MessageBox.Show("Can not insert. Edge exists.", "Network editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            state = EnumEditorState.None;
            SelectionBoxFramed.IsActive = false;
            SelectionBox.IsActive = false;
            edgeNodeStart = null;
            currentMousePos = new(0, 0);
            startMousePos = new(0, 0);
            network.NodeDrawingOffset = new(0, 0);

            doubleBufferPanelDrawing.Invalidate();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string path = @"C:\\Users\\lekyr1\\Desktop\\data\\MapaPodklad.png";

            network = new(path)
            {
                BackgroundAlpha = trackBarBackgroundTransparency.Value
            };

            doubleBufferPanelDrawing.Invalidate();
        }

        private void checkBoxBackgroundVisible_CheckedChanged(object sender, EventArgs e)
        {
            if (network != null)
                network.BackgroundVisible = checkBoxBackgroundVisible.Checked;

            trackBarBackgroundTransparency.Enabled = checkBoxBackgroundVisible.Checked;

            doubleBufferPanelDrawing.Invalidate();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            network.Key = e.KeyCode;

            if (e.KeyCode == Keys.Delete && network.SeletedAny)
            {
                if (MessageBox.Show("Delete selected elements?", "Network editor", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    network.RemoveSelected();
                    doubleBufferPanelDrawing.Invalidate();
                }
            }
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            network.Key = Keys.None;
        }

        private void radioButtonEdit_CheckedChanged(object sender, EventArgs e)
        {
            // deselect all nodes
            network.SelectNode(new Rectangle());
            doubleBufferPanelDrawing.Invalidate();

            if (radioButtonEdit.Checked)
                mode = EnumEditorMode.Edit;
        }

        private void radioButtonInsertNode_CheckedChanged(object sender, EventArgs e)
        {
            // deselect all nodes
            network.SelectNode(new Rectangle());
            doubleBufferPanelDrawing.Invalidate();

            if (radioButtonInsertNode.Checked)
                mode = EnumEditorMode.InsertNode;
        }

        private void radioButtonInsertEdge_CheckedChanged(object sender, EventArgs e)
        {
            // deselect all nodes
            network.SelectNode(new Rectangle());
            doubleBufferPanelDrawing.Invalidate();

            if (radioButtonInsertEdge.Checked)
                mode = EnumEditorMode.InsertEdge;

        }

        private void trackBarBackgroundTransparency_Scroll(object sender, EventArgs e)
        {
            network.BackgroundAlpha = trackBarBackgroundTransparency.Value;
            doubleBufferPanelDrawing.Invalidate();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveNetworkData();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new();
            saveFileDialog.InitialDirectory = @"Documents";
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.Title = "Browse XML Files";
            saveFileDialog.DefaultExt = "xml";
            saveFileDialog.Filter = "xml files (*.xml)|*.xml|All files (*.*)|*.*";
            saveFileDialog.FilterIndex = 2;
            saveFileDialog.ShowDialog();

            projectPath = saveFileDialog.FileName;
            SaveNetworkData();
        }

        private void SaveNetworkData()
        {
            if (string.IsNullOrWhiteSpace(projectPath))
                return;

            // Creates an instance of the XmlSerializer class;
            // specifies the type of object to serialize.
            var serializer = new DataContractSerializer(network.GetType());
            FileStream writer = new(projectPath, FileMode.Create);

            serializer.WriteObject(writer, network);
            writer.Close();
        }

        private void LoadNetworkData()
        {
            if (string.IsNullOrWhiteSpace(projectPath))
                return;

            FileStream fs = new(projectPath, FileMode.Open);
            XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas());
            DataContractSerializer? serializer = new(typeof(NetworkData));

            if (serializer == null)
                return;

            if (serializer.ReadObject(reader, true) is not NetworkData data)
                return;

            string path = @"C:\\Users\\lekyr1\\Desktop\\data\\MapaPodklad.png";

            network = new(path)
            {
                BackgroundAlpha = trackBarBackgroundTransparency.Value
            };

            foreach (var node in data.Nodes)
            {
                network.Nodes.Add(node);
            }

            foreach (var edge in data.Edges)
            {
                NetworkNode? startNode = network.Nodes.Find(n => n.ID == edge.StartNodeID);
                NetworkNode? endNode = network.Nodes.Find(n => n.ID == edge.EndNodeID);

                if (startNode == null || endNode == null)
                    continue;

                network.Edges.Add(new NetworkEdge(startNode, endNode, edge.ID));
            }

            network.BackgroundVisible = data.BackgroundVisible;

            reader.Close();
            fs.Close();

            doubleBufferPanelDrawing.Invalidate();
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                InitialDirectory = @"Documents",
                RestoreDirectory = true,
                Title = "Browse XML Files",
                DefaultExt = "xml",
                Filter = "xml files (*.xml)|*.xml|All files (*.*)|*.*",
                FilterIndex = 2,
                CheckFileExists = true,
                CheckPathExists = true
            };

            openFileDialog.ShowDialog();

            projectPath = openFileDialog.FileName;

            LoadNetworkData(); 
        }

        private void buttonShortestPath_Click(object sender, EventArgs e)
        {

        }
    }
}
