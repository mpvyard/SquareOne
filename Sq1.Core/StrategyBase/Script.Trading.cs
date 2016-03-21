using System;
using System.Collections.Generic;
using System.Diagnostics;

using Sq1.Core.DataTypes;
using Sq1.Core.Execution;

namespace Sq1.Core.StrategyBase {
	public abstract partial class Script {
		public Position BuyAtLimit(Bar bar, double limitPrice, string signalName = "BOUGHT_AT_LIMIT") {
			return this.Executor.BuyOrShort_alertCreateRegister(bar, limitPrice, signalName, Direction.Buy, MarketLimitStop.Limit);
		}
		public Position BuyAtMarket(Bar bar, string signalName = "BOUGHT_AT_MARKET") {
			return this.Executor.BuyOrShort_alertCreateRegister(bar, 0, signalName, Direction.Buy, MarketLimitStop.Market);
		}
		public Position BuyAtStop(Bar bar, double stopPrice, string signalName = "BOUGHT_AT_STOP") {
			return this.Executor.BuyOrShort_alertCreateRegister(bar, stopPrice, signalName, Direction.Buy, MarketLimitStop.Stop);
		}
		
		public Alert CoverAtLimit(Bar bar, Position position, double limitPrice, string signalName = "COVERED_AT_LIMIT") {
			return this.Executor.SellOrCover_alertCreateRegister(bar, position, limitPrice, signalName, Direction.Cover, MarketLimitStop.Limit);
		}
		public Alert CoverAtMarket(Bar bar, Position position, string signalName = "COVERED_AT_MARKET") {
			return this.Executor.SellOrCover_alertCreateRegister(bar, position, 0, signalName, Direction.Cover, MarketLimitStop.Market);
		}
		public Alert CoverAtStop(Bar bar, Position position, double stopPrice, string signalName = "COVERED_AT_STOP") {
			return this.Executor.SellOrCover_alertCreateRegister(bar, position, stopPrice, signalName, Direction.Cover, MarketLimitStop.Stop);
		}

		public Alert SellAtLimit(Bar bar, Position position, double limitPrice, string signalName = "SOLD_AT_LIMIT") {
			return this.Executor.SellOrCover_alertCreateRegister(bar, position, limitPrice, signalName, Direction.Sell, MarketLimitStop.Limit);
		}
		public Alert SellAtMarket(Bar bar, Position position, string signalName = "SOLD_AT_MARKET") {
			return this.Executor.SellOrCover_alertCreateRegister(bar, position, 0, signalName, Direction.Sell, MarketLimitStop.Market);
		}
		public Alert SellAtStop(Bar bar, Position position, double stopPrice, string signalName = "SOLD_AT_STOP") {
			return this.Executor.SellOrCover_alertCreateRegister(bar, position, stopPrice, signalName, Direction.Sell, MarketLimitStop.Stop);
		}
		
		public Position ShortAtLimit(Bar bar, double limitPrice, string signalName = "SHORTED_AT_LIMIT") {
			return this.Executor.BuyOrShort_alertCreateRegister(bar, limitPrice, signalName, Direction.Short, MarketLimitStop.Limit);
		}
		public Position ShortAtMarket(Bar bar, string signalName = "SHORTED_AT_MARKET") {
			return this.Executor.BuyOrShort_alertCreateRegister(bar, 0, signalName, Direction.Short, MarketLimitStop.Market);
		}
		public Position ShortAtStop(Bar bar, double stopPrice, string signalName = "SHORTED_AT_STOP") {
			return this.Executor.BuyOrShort_alertCreateRegister(bar, stopPrice, signalName, Direction.Short, MarketLimitStop.Stop);
		}
		public Alert ExitAtMarket(Bar bar, Position position, string signalName = "EXITED_AT_MARKET") {
			if (position.PositionLongShort == PositionLongShort.Long) {
				return this.SellAtMarket(bar, position, signalName);
			} else {
				return this.CoverAtMarket(bar, position, signalName);
			}
		}
		public Alert ExitAtLimit(Bar bar, Position position, double price, string signalName = "EXITED_AT_LIMIT") {
			if (position.PositionLongShort == PositionLongShort.Long) {
				return this.SellAtLimit(bar, position, price, signalName);
			} else {
				return this.CoverAtLimit(bar, position, price, signalName);
			}
		}
		public Alert ExitAtStop(Bar bar, Position position, double price, string signalName = "EXITED_AT_STOP") {
			if (position.PositionLongShort == PositionLongShort.Long) {
				return this.SellAtStop(bar, position, price, signalName);
			} else {
				return this.CoverAtStop(bar, position, price, signalName);
			}
		}

		#region Kill pending alert
		public void AlertPending_kill(Alert alert) {
			this.Executor.AlertPending_kill(alert);
		}

		[Obsolete("looks unreliable until refactored; must kill previous alertExit AFTER killing market completes => userland callback or more intelligent management in CORE level")]
		public List<Alert> PositionClose_immediately(Position position, string signalName) {
			List<Alert> killedOnce = this.Position_exitAlert_kill(position, signalName);
			this.ExitAtMarket(this.Bars.BarStreaming_nullUnsafe, position, signalName);
			// BETTER WOULD BE KILL PREVIOUS PENDING ALERT FROM A CALBACK AFTER MARKET EXIT ORDER GETS FILLED, IT'S UNRELIABLE EXIT IF WE KILL IT HERE
			// LOOK AT EMERGENCY CLASSES, SOLUTION MIGHT BE THERE ALREADY
			return killedOnce;
		}
		public List<Alert> Position_exitAlert_kill(Position position, string signalName) {
			List<Alert> alertsSubmittedToKill = new List<Alert>();
			if (position.IsEntryFilled == false) {
				string msg = "I_REFUSE_TO_KILL_UNFILLED_ENTRY_ALERT position[" + position + "]";
				Assembler.PopupException(msg);
				return alertsSubmittedToKill;
			}
			if (null == position.ExitAlert) {
				string msg = "FIXME I_REFUSE_TO_KILL_UNFILLED_EXIT_ALERT {for prototyped position, position.ExitAlert contains TakeProfit} position[" + position + "]";
				Debugger.Break();
				return alertsSubmittedToKill;
			}
			if (string.IsNullOrEmpty(signalName)) signalName = "PositionCloseImmediately()";
			if (position.Prototype != null) {
				alertsSubmittedToKill = this.PositionPrototype_killWhateverIsPending(position.Prototype, signalName);
				return alertsSubmittedToKill;
			}
			this.AlertPending_kill(position.ExitAlert);
			alertsSubmittedToKill.Add(position.ExitAlert);
			return alertsSubmittedToKill;
		}
		
		public List<Alert> PositionPrototype_killWhateverIsPending(PositionPrototype proto, string signalName) {
			List<Alert> alertsSubmittedToKill = new List<Alert>();
			if (proto.StopLossAlert_forMoveAndAnnihilation != null) {
				this.AlertPending_kill(proto.StopLossAlert_forMoveAndAnnihilation);
				alertsSubmittedToKill.Add(proto.StopLossAlert_forMoveAndAnnihilation);
			}
			if (proto.TakeProfitAlert_forMoveAndAnnihilation != null) {
				this.AlertPending_kill(proto.TakeProfitAlert_forMoveAndAnnihilation);
				alertsSubmittedToKill.Add(proto.TakeProfitAlert_forMoveAndAnnihilation);
			}
			return alertsSubmittedToKill;
		}
		#endregion
	}
}