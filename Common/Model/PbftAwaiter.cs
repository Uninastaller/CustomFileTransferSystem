using Common.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace Common.Model
{
    public class PbftAwaiter : IDisposable
    {
        private readonly int _timerSeconds = 20;

        public static bool BlockNewRequests { get; private set; } = false;

        private Timer? _processTimer;
        private List<string>? _awaitingPrepareRepliesFrom;
        private List<string>? _awaitingCommitRepliesFrom;
        public string HashOfRequest { get; private set; } = string.Empty;
        public int MaxFaultyReplicas { get; private set; }
        private State _myState = State.NONE;

        public bool IsDisposed { get; private set; } = false;

        public PbftAwaiter(IEnumerable<string> awaitingRepliesFrom, string hashOfRequest)
        {
            Logger.Log.WriteLog(Logger.LogLevel.INFO, "Starting new pbft awaiter");

            if (BlockNewRequests)
            {
                Logger.Log.WriteLog(Logger.LogLevel.WARNING, "Can not start new awaiter bcs its blocked by old one");
                return;
            }

            BlockNewRequests = true;

            _processTimer = new Timer(_timerSeconds * 1000);
            _processTimer.Elapsed += Timer_elapsed;

            _awaitingPrepareRepliesFrom = awaitingRepliesFrom.ToList();
            _awaitingCommitRepliesFrom = awaitingRepliesFrom.ToList();
            MaxFaultyReplicas = (awaitingRepliesFrom.Count() - 1) / 3; // f = (N-1)/3 // Calculation of supermajority
            HashOfRequest = hashOfRequest;

            _processTimer.Start();
            _myState = State.AWAITING_PREPARE;
        }

        public void ReceivedMessage(string fromReplica, SocketMessageFlag messageType)
        {

            if (IsDisposed || _awaitingCommitRepliesFrom == null || _awaitingPrepareRepliesFrom == null)
            {
                return;
            }

            switch (messageType)
            {
                case SocketMessageFlag.PBFT_PREPARE:
                    if (_awaitingPrepareRepliesFrom.Contains(fromReplica))
                    {
                        Logger.Log.WriteLog(Logger.LogLevel.INFO, $"Removing {fromReplica} from awaiting list for message prepare");
                        _awaitingPrepareRepliesFrom.Remove(fromReplica);
                    }
                    break;
                case SocketMessageFlag.PBFT_COMMIT:
                    if (_awaitingCommitRepliesFrom.Contains(fromReplica))
                    {
                        Logger.Log.WriteLog(Logger.LogLevel.INFO, $"Removing {fromReplica} from awaiting list for message commit");
                        _awaitingCommitRepliesFrom.Remove(fromReplica);
                    }
                    break;
                default:
                    break;
            }
        }

        private void Timer_elapsed(object? sender, ElapsedEventArgs e)
        {
            _processTimer?.Stop();

            // Check if we awaited prepare
            if (_myState == State.AWAITING_PREPARE)
            {
                // set state to none for now
                _myState = State.NONE;

                // check for list
                if (_awaitingPrepareRepliesFrom == null)
                {
                    Logger.Log.WriteLog(Logger.LogLevel.ERROR, "Invalid state of program, disposing awaiter!");
                    Dispose();
                    return;
                }
                else
                {
                    // Check if enough prepare messages were received
                    if (MaxFaultyReplicas < _awaitingPrepareRepliesFrom.Count)
                    {
                        Logger.Log.WriteLog(Logger.LogLevel.WARNING, $"PBFT consensus ERROR on awaiting prepare, too many faultyReplicas: {_awaitingPrepareRepliesFrom.Count}!");
                        Dispose();
                        return;
                    }
                    else
                    {
                        // If yes, double timer and change state to awaiting commit
                        if (_processTimer != null)
                        {
                            _processTimer.Interval = _processTimer.Interval * 2;
                            _myState = State.AWAITING_COMMIT;
                            _processTimer.Start();
                            return;
                        }
                        else
                        {
                            Logger.Log.WriteLog(Logger.LogLevel.ERROR, "Invalid state of program, disposing awaiter!");
                            Dispose();
                            return;
                        }
                    }
                }
            }
            else if (_myState == State.AWAITING_COMMIT && _awaitingCommitRepliesFrom != null)
            {
                if (MaxFaultyReplicas < _awaitingCommitRepliesFrom.Count)
                {
                    Logger.Log.WriteLog(Logger.LogLevel.WARNING, $"PBFT consensus ERROR on awaiting commit, too many faultyReplicas: {_awaitingCommitRepliesFrom.Count}!");
                }
            }
            Dispose();
        }

        public ActionRequired CheckActionRequired()
        {
            if (IsDisposed)
            {
                return ActionRequired.NONE;
            }

            if (_myState == State.AWAITING_PREPARE && _awaitingPrepareRepliesFrom != null)
            {
                if (MaxFaultyReplicas >= _awaitingPrepareRepliesFrom.Count)
                {
                    return ActionRequired.SEND_COMMIT;
                }
            }
            else if (_myState == State.AWAITING_COMMIT && _awaitingCommitRepliesFrom != null)
            {
                if (MaxFaultyReplicas >= _awaitingCommitRepliesFrom.Count)
                {
                    return ActionRequired.ADD_BLOCK_TO_BLOCKCHAIN;
                }
            }

            return ActionRequired.NONE;
        }

        public void CommitSent()
        {
            Logger.Log.WriteLog(Logger.LogLevel.INFO, "Gui signalizing that: CommitSent");
            _processTimer?.Stop();

            if (_processTimer != null)
            {
                _processTimer.Interval = _processTimer.Interval * 2;
                _myState = State.AWAITING_COMMIT;
                _processTimer.Start();
            }
        }

        public void BlockAdded()
        {
            Logger.Log.WriteLog(Logger.LogLevel.INFO, "Gui signalizing that: BlockAdded");
            Dispose();
        }

        public void Dispose()
        {
            _myState = State.NONE;

            _awaitingCommitRepliesFrom = null;
            _awaitingPrepareRepliesFrom = null;

            // Unsubscribe from the Elapsed event
            if (_processTimer != null)
            {
                _processTimer.Elapsed -= Timer_elapsed;
                // Stop and dispose of the timer
                _processTimer?.Stop();
                _processTimer?.Dispose();
                _processTimer = null;
            }

            IsDisposed = true;

            // unblock new requests
            BlockNewRequests = false;
        }
    }

    enum State
    {
        NONE,
        AWAITING_PREPARE,
        AWAITING_COMMIT
    }

    public enum ActionRequired
    {
        NONE,
        SEND_COMMIT,
        ADD_BLOCK_TO_BLOCKCHAIN
    }
}
