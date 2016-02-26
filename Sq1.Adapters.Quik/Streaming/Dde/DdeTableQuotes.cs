﻿using System;
using System.Globalization;
using System.Collections.Generic;

using Sq1.Core;
using Sq1.Core.DataTypes;

using Sq1.Adapters.Quik.Streaming.Dde.XlDde;

namespace Sq1.Adapters.Quik.Streaming.Dde {
	//public class DdeTableQuotes : XlDdeTable {
	public class DdeTableQuotes : XlDdeTableMonitoreable<QuoteQuik> {
		protected override string DdeConsumerClassName { get { return "DdeTableQuotes"; } }

		protected DateTime		lastQuoteDateTimeForVolume = DateTime.MinValue;
		protected double		lastQuoteSizeForVolume = 0;

		public DdeTableQuotes(string topic, QuikStreaming quikStreaming, List<XlColumn> columns) : base(topic, quikStreaming, columns, true) {}

		//protected override void IncomingTableRow_convertToDataStructure(XlRowParsed row) {
		protected override QuoteQuik IncomingTableRow_convertToDataStructure_monitoreable(XlRowParsed row) {
			//if (rowParsed["SHORTNAME"] == "LKOH") {
			//	int a = 1;
			//}

			// UPSTACK ALREADY_DOES_IT
			//string msig = " //this[" + this + "].IncomingTableRow_convertToDataStructure_monitoreable(" + row + ")";
			//foreach (string msg in row.ErrorMessages) {
			//	Assembler.PopupException(msg + msig, null, false);
			//}

			QuoteQuik quikQuote = new QuoteQuik(DateTime.Now);
			quikQuote.Source			= this.DdeConsumerClassName + " Topic[" + base.Topic + "]";
			quikQuote.Symbol			= row.Get<string>("CODE");
			//quikQuote.Symbol			= row.Get<string>("CODE");
			quikQuote.SymbolClass		= row.Get<string>("CLASS_CODE");
			quikQuote.Bid				= row.Get<double>("bid");
			quikQuote.Ask				= row.Get<double>("offer");

			double	last				= row.Get<double>("last");
			if (last == quikQuote.Bid) quikQuote.TradedAt = BidOrAsk.Bid;
			if (last == quikQuote.Ask) quikQuote.TradedAt = BidOrAsk.Ask;
			if (quikQuote.TradedAt == BidOrAsk.UNKNOWN) {
				string msg = "QUOTE_WASNT_TRADED last must NOT be bid or ask //ROUNDING_ERROR?...";
				Assembler.PopupException(msg, null, false);
			}

			quikQuote.FortsDepositBuy	= row.Get<double>("buydepo");
			quikQuote.FortsDepositSell	= row.Get<double>("selldepo");
			quikQuote.FortsPriceMax		= row.Get<double>("high");
			quikQuote.FortsPriceMin		= row.Get<double>("low");

			this.reconstructServerTime_useNowAndTimezoneFromMarketInfo_ifNotFoundInRow(row);	// upstack@base check rowParsed.ErrorMessages 
			quikQuote.ServerTime		= row.GetDateTime("_ServerTime"	, DateTime.Now);
			//DateTime qChangeTime = DateTime.MinValue;
			//if (quote.ServerTime == DateTime.MinValue && qChangeTime != DateTime.MinValue) {
			//	quote.ServerTime = qChangeTime;
			//}

			double sizeParsed			= row.Get<double>("qty");
			//if (lastQuoteDateTimeForVolume != quikQuote.ServerTime) {
			//	lastQuoteDateTimeForVolume  = quikQuote.ServerTime;
				quikQuote.Size = sizeParsed;
			//} else {
			//	string msg = "SHOULD_I_DELIVER_THE_DUPLIATE_QUOTE?";
			//	Assembler.PopupException(msg, null, false);
			//	return quikQuote;
			//}
			//if (lastQuoteSizeForVolume != sizeParsed) {
			//	lastQuoteSizeForVolume = sizeParsed;
			//	quote.Size = sizeParsed;
			//}

			quikQuote.PriceStepFromDde	= row.Get<double>("SEC_PRICE_STEP");
			this.syncPriceStep_toSymbolInfo(quikQuote);

			base.QuikStreaming.PushQuoteReceived(quikQuote);	//goes to another thread via PUMP and invokes strategies letting me go
			return quikQuote;									//one more delay is to raise and event which will go to GUI thread as well QuikStreamingMonitorForm.tableQuotes_DataStructureParsed_One()
		}

		void reconstructServerTime_useNowAndTimezoneFromMarketInfo_ifNotFoundInRow(XlRowParsed rowParsed) {
			string msig = " //this[" + this + "].reconstructServerTime_useNowAndTimezoneFromMarketInfo_ifNotFoundInRow(" + rowParsed + ")";

			DateTime ret = DateTime.MinValue;
			string errmsg = "DATE_NOT_FOUND_IN_rowParsed__RETURNING_DateTime.MinValue";

			string dateReceived = rowParsed.GetString("TRADE_DATE_CODE",	"QUOTE_DATE_NOT_DELIVERED_DDE");
			string timeReceived = rowParsed.GetString("time",				"QUOTE_TIME_NOT_DELIVERED_DDE");
			
			if (dateReceived == "QUOTE_DATE_NOT_DELIVERED_DDE" || timeReceived == "QUOTE_TIME_NOT_DELIVERED_DDE") {
				MarketInfo marketInfo = this.QuikStreaming.DataSource.MarketInfo;
				if (marketInfo != null) {
					ret = TimeZoneInfo.ConvertTime(DateTime.Now, marketInfo.TimeZoneInfo);
					errmsg = "DATE_NOT_FOUND_IN_rowParsed__RETURNING_DateTime.Now=>marketInfo[" + this.QuikStreaming.DataSource.MarketName + "]"
						+ ".TimeZoneInfo.BaseUtcOffset[" + marketInfo.TimeZoneInfo.BaseUtcOffset + "]";
				}
				rowParsed.AddOrReplace("_ServerTime", ret);
				rowParsed.ErrorMessages.Add(errmsg + msig);
				return;
			}

			string dateTimeReceived = dateReceived + " " + timeReceived;

			try {
				ret = DateTime.Parse(dateTimeReceived);
				rowParsed.AddOrReplace("_ServerTime", ret);
				return;		// if not Parse()d fromAnyFormat then it'll throw and I'll continue with ParseExact()
			} catch (Exception ex) {
				errmsg = "TROWN_DateTime.Parse(" + dateTimeReceived + "): " + ex.Message;
				rowParsed.ErrorMessages.Add(errmsg + msig);
			}

			string dateFormat = base.ColumnDefinitionFor("TRADE_DATE_CODE")	.ToDateParseFormat;
			string timeFormat = base.ColumnDefinitionFor("time")			.ToTimeParseFormat;
			string dateTimeFormat = dateFormat + " " + timeFormat;
			try {
				ret = DateTime.ParseExact(dateTimeReceived, dateTimeFormat, CultureInfo.InvariantCulture);
				rowParsed.AddOrReplace("_ServerTime", ret);
			} catch (Exception ex) {
				errmsg = "TROWN_DateTime.ParseExact(" + dateTimeReceived + ", " + dateTimeFormat + "): " + ex.Message;
				rowParsed.ErrorMessages.Add(errmsg + msig);
			}
		}

		void syncPriceStep_toSymbolInfo(QuoteQuik quikQuote) {
			if (double.IsNaN(quikQuote.PriceStepFromDde)) return;
			if (Assembler.InstanceInitialized.RepositorySymbolInfos == null) return;

			SymbolInfo symbolInfo = Assembler.InstanceInitialized.RepositorySymbolInfos.FindSymbolInfo_nullUnsafe(quikQuote.Symbol);
			if (symbolInfo == null) return;

			//int priceStep_fromDde_asInt = Convert.ToInt32(Math.Round(priceStep_fromDde));
			if (symbolInfo.PriceStepFromDde == quikQuote.PriceStepFromDde) return;

			symbolInfo.PriceStepFromDde = quikQuote.PriceStepFromDde;
			Assembler.InstanceInitialized.RepositorySymbolInfos.Serialize();	// YEAH (double)0 != (double)0... serializing as many times as many quotes we received first; but only once/symbol/session koz Infos are cached
		}
	}
}