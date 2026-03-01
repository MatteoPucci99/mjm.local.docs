namespace Mjm.LocalDocs.Core.Models;

/// <summary>
/// Represents the lifecycle status of a trading system.
/// </summary>
public enum TradingSystemStatus
{
    /// <summary>
    /// Initial state - just created, idea stage.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Testing on historical data.
    /// </summary>
    Backtesting = 1,

    /// <summary>
    /// Walk-forward or out-of-sample validation in progress.
    /// </summary>
    Validating = 2,

    /// <summary>
    /// Validated and ready for paper trading.
    /// </summary>
    Validated = 3,

    /// <summary>
    /// Live trading with real capital.
    /// </summary>
    Live = 4,

    /// <summary>
    /// Temporarily paused.
    /// </summary>
    Paused = 5,

    /// <summary>
    /// No longer in use.
    /// </summary>
    Archived = 6
}
