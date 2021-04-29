﻿
using AdasVetelClient.layout;
using AdasVetelServer.model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SimpleTcp;


namespace AdasVetelClient
{
    
    public partial class SzemelyekDisplay : DisplayForm
    {
       
        public SzemelyekDisplay(DataGridViewRow clickedRow): base(clickedRow)
        {
            Szemely sz = (Szemely)clickedRow.Tag;
            InitializeComponent();
            Text = $"{sz.Az} - {sz.CsaladiNev} {sz.UtoNev}";
            Route = $"{Route}/szemelyek/{sz.Az}";
        }     
        protected override void createGridViews()
        {           

            listView1.Items.AddRange(new ListViewItem[] {
            listViewItem1,
            listViewItem2,      
            listViewItem10});
            dataGrids.Add(new DbHolderGridView<Cim>((string)listViewItem1.Tag));
            dataGrids.Add(new DbHolderGridView<Szerzodes>((string)listViewItem2.Tag));            
            dataGrids.Add(new DbHolderGridView<Reszvetel>((string)listViewItem10.Tag));
        }
    }
}
