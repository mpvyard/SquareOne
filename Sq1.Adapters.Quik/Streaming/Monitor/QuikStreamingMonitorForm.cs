﻿using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Forms;

using Sq1.Widgets;
using Sq1.Adapters.Quik.Streaming.Dde.XlDde;
using Sq1.Adapters.Quik.Streaming.Dde;

namespace Sq1.Adapters.Quik.Streaming.Monitor {
	public partial class QuikStreamingMonitorForm : DockContentImproved {
		QuikStreaming quikStreaming;

		// I_DONT_WANT_MONITOR_TO_STAY_AFTER_APPRESTART__HOPING_NEVER_INVOKED_BY_DESERIALIZER
		public QuikStreamingMonitorForm() {
			InitializeComponent();
		}

		// HUMAN_INVOKED_CONSTRUCTOR
		public QuikStreamingMonitorForm(QuikStreaming quikStreamingInstantiatedForDataSource) : this() {
			this.quikStreaming = quikStreamingInstantiatedForDataSource;
			this.QuikStreamingMonitorControl.Initialize(this.quikStreaming);
		}

		void quikStreaming_OnConnectionStateChanged(object sender, EventArgs e) {
			this.populateWindowTitle_grpStatuses();
		}

		void populateWindowTitle_grpStatuses() {
			if (this.InvokeRequired) {
				base.BeginInvoke((MethodInvoker)delegate { this.populateWindowTitle_grpStatuses(); });
				return;
			}
			base.Text = this.quikStreaming.DdeBatchSubscriber.WindowTitle;
			this.QuikStreamingMonitorControl.Populate_grpStatuses();
		}

		void tableQuotes_DataStructuresParsed_Table(object sender, XlDdeTableMonitoringEventArg<List<QuoteQuik>> e) {
			if (base.IsDisposed) return;
			if (this.InvokeRequired) {
				base.BeginInvoke((MethodInvoker)delegate { this.tableQuotes_DataStructuresParsed_Table(sender, e); });
				return;
			}
			this.QuikStreamingMonitorControl.OlvQuotes.SetObjects(e.DataStructureParsed);
			XlDdeTableMonitoreable<QuoteQuik> xlDdeTable = sender as XlDdeTableMonitoreable<QuoteQuik>;
			if (xlDdeTable == null) return;
			this.QuikStreamingMonitorControl.grpQuotes.Text = xlDdeTable.ToString();
		}
		void tableQuotes_DataStructureParsed_One(object sender, XlDdeTableMonitoringEventArg<QuoteQuik> e) {
		}

		internal void PopulateWindowTitle_dataSourceName_market_quotesTopic() {
			base.Text = this.ToString();
		}
		public override string ToString() {
			return this.quikStreaming.IdentForMonitorWindowTitle;
		}
	}
}
