﻿using System;
using System.Windows.Forms;

using NDde;
using NDde.Client;

using Sq1.Core;
using Sq1.Core.Backtesting;
using Sq1.Core.DataTypes;
using Sq1.Core.Streaming;
using Sq1.Core.Support;

using Sq1.Adapters.Quik;
using Sq1.Adapters.Quik.Streaming.Livesim.Dde;
//using System.Threading;

namespace Sq1.Adapters.Quik.Streaming.Livesim {
	public class QuikLivesimBatchPublisher {
				QuikStreamingLivesim			quikStreamingLivesim;

				DdeTableGeneratorQuotes			ddeTableGeneratorQuotes;
				DdeTableGeneratorDepth			ddeTableGeneratorDepth;
				string							symbolSingleImLivesimmingDepth;
		
				string							ddeService		{ get { return this.quikStreamingLivesim.QuikStreamingOriginal.DdeServiceName; } }
				string							ddeTopicQuotes	{ get { return this.quikStreamingLivesim.QuikStreamingOriginal.DdeBatchSubscriber.TableQuotes.Topic; } }
				string							ddeTopicDepth	{ get { return this.quikStreamingLivesim.QuikStreamingOriginal.DdeBatchSubscriber.GetDomTopicForSymbol(this.symbolSingleImLivesimmingDepth); } }

		public	string							TopicsAsString { get {
			string ret = "";
			ret +=		this.ddeTableGeneratorQuotes.ToString();
			ret += ","+ this.ddeTableGeneratorDepth .ToString();
			return ret;
		} }

		public QuikLivesimBatchPublisher(QuikStreamingLivesim quikLivesimStreaming) {
			this.quikStreamingLivesim		= quikLivesimStreaming;
			this.ddeTableGeneratorQuotes	= new DdeTableGeneratorQuotes(this.ddeService,	this.ddeTopicQuotes	, this.quikStreamingLivesim);
			if (this.quikStreamingLivesim.DataSource.Symbols.Count != 1) {
				string msg = "LIVESIM_DATASOURCE_MUST_CONTAIN_ONE_SYMBOL_YOU_ARE_BACKTESTING";	// and in the future many symbols, for multi-symbol-within-same-datasource strategies
				Assembler.PopupException(msg);
			} else {
				this.symbolSingleImLivesimmingDepth	= this.quikStreamingLivesim.DataSource.Symbols[0];
				this.ddeTableGeneratorDepth			= new DdeTableGeneratorDepth(this.ddeService,	this.ddeTopicDepth	, this.quikStreamingLivesim);
			}
		}

		internal void SendLevelTwo_DdeClientPokesDdeServer_waitServerProcessed(LevelTwoHalf levelTwoAsks, LevelTwoHalf levelTwoBids) {
			if (this.ddeTableGeneratorDepth == null) {
				string msg = "I_REFUSE_TO_SendLevelTwo()__DATASOURCE_DIDNT_CONTAIN_ANY_SYMBOLS_TO_LAUNCH_DdeClient_FOR";
				Assembler.PopupException(msg);
				return;
			}
			this.ddeTableGeneratorDepth.Send_DdeClientPokesDdeServer_waitServerProcessed(levelTwoAsks, levelTwoBids);
		}

		internal void SendQuote_DdeClientPokesDdeServer_waitServerProcessed(QuoteGenerated quote) {
			this.ddeTableGeneratorQuotes.Send_DdeClientPokesDdeServer_waitServerProcessed(quote);
		}

		public override string ToString() {
			return "QuikLivesimDdeClient[" + this.ddeService + "] TOPICS[" + TopicsAsString + "]";
		}

		internal void ConnectAll() {
			this.ddeTableGeneratorQuotes	.Connect();
			this.ddeTableGeneratorDepth		.Connect();
			//this.ddeClientTrades	.Connect();
		}

		internal void DisconnectAll() {
			this.ddeTableGeneratorQuotes	.Disconnect();
			this.ddeTableGeneratorDepth		.Disconnect();
			//this.ddeClientTrades	.Disconnect();
		}

		internal void DisposeAll() {
			this.ddeTableGeneratorQuotes	.Dispose();
			this.ddeTableGeneratorDepth		.Dispose();
			//this.ddeClientTrades	.Dispose();
		}
	}
}
