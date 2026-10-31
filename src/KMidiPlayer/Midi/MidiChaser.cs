using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace KMidiPlayer.Midi
{
    /// <summary>
    /// 曲の途中から再生するときに、その位置までのチャンネルの状態（音色、コントロールチェンジ、
    /// ピッチベンドなど）を再現して音源に送る。
    /// </summary>
    internal static class MidiChaser
    {
        /// <param name="output">送信先。</param>
        /// <param name="events">曲のイベント一覧。</param>
        /// <param name="endIndex">このインデックスより前のイベントの状態を再現する。</param>
        /// <param name="sysExSentCount">先頭からこの数までの SysEx は送信済みなので送らない。</param>
        /// <param name="outputChannels">曲データのチャンネルごとの、送信先のチャンネル。</param>
        /// <param name="blockedControllers">送らない CC（添字が CC 番号）。</param>
        public static void Chase(MidiOutDevice output, IReadOnlyList<MidiEvent> events, int endIndex, int sysExSentCount, IReadOnlyList<int> outputChannels, bool[] blockedControllers)
        {
            ChannelState[] states = CreateStates();
            for (int i = 0; i < endIndex; i++)
            {
                MidiEvent e = events[i];
                if (e.Kind == MidiEventKind.Channel)
                {
                    states[e.Channel].Apply(e);
                }
                else if (e.Kind == MidiEventKind.SysEx)
                {
                    bool isReset = MidiReset.IsResetMessage(e.Data);
                    if (isReset)
                    {
                        // リセットより前のチャンネルの状態は音源側で消えるので、記録し直す
                        states = CreateStates();
                    }
                    if (i >= sysExSentCount)
                    {
                        output.SendLongMessage(e.Data);
                        if (isReset)
                        {
                            Thread.Sleep(MidiReset.WaitMilliseconds);
                        }
                    }
                }
            }

            for (int channel = 0; channel < states.Length; channel++)
            {
                states[channel].Send(output, outputChannels[channel], blockedControllers);
            }
        }

        /// <summary>
        /// 1 つのチャンネルの状態だけを再現して、指定したチャンネルに送る（送信先のチャンネルを変えたとき用）。
        /// SysEx は送らない。
        /// </summary>
        public static void ChaseChannel(MidiOutDevice output, IReadOnlyList<MidiEvent> events, int endIndex, int channel, int outputChannel, bool[] blockedControllers)
        {
            var state = new ChannelState();
            for (int i = 0; i < endIndex; i++)
            {
                MidiEvent e = events[i];
                if (e.Kind == MidiEventKind.Channel && e.Channel == channel)
                {
                    state.Apply(e);
                }
                else if (e.Kind == MidiEventKind.SysEx && MidiReset.IsResetMessage(e.Data))
                {
                    state = new ChannelState();
                }
            }
            state.Send(output, outputChannel, blockedControllers);
        }

        /// <summary>
        /// 音源が曲のイベントを <paramref name="sentCount"/> の手前まで受け取った状態から、
        /// <paramref name="index"/> の位置の状態に、<see cref="Chase"/>（記録した状態の送り直し）だけでは戻せないかどうか。
        /// 戻せなければ、リセットしてから曲の SysEx を初めから送り直す必要がある。
        /// </summary>
        /// <remarks>
        /// <paramref name="index"/> から <paramref name="sentCount"/> の手前までに、次のものがあると戻せない。
        /// <list type="bullet">
        /// <item>SysEx（音源の設定を変えるが、変える前の値が分からない）</item>
        /// <item>
        /// <paramref name="index"/> までに一度も設定していない項目（CC・プログラム・RPN・NRPN）の設定。
        /// 送り直すのは記録した項目だけなので、音源側に後の値が残る（例: 終盤のフェードアウトの CC7 で、無音のまま戻る）。
        /// Reset All Controllers（CC121）で初期値に戻る項目（モジュレーション・エクスプレッション・ペダル・ピッチベンドなど）は、
        /// 送り直しの初めに CC121 を送るので除く。
        /// </item>
        /// </list>
        /// </remarks>
        public static bool NeedsReset(IReadOnlyList<MidiEvent> events, int index, int sentCount)
        {
            sentCount = Math.Min(sentCount, events.Count);
            for (int i = index; i < sentCount; i++)
            {
                if (events[i].Kind == MidiEventKind.SysEx)
                {
                    return true;
                }
            }

            ChannelState[] before = CollectStates(events, index);
            ChannelState[] after = CollectStates(events, sentCount);
            for (int channel = 0; channel < before.Length; channel++)
            {
                if (after[channel].HasItemsNotSetIn(before[channel]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>曲の先頭から <paramref name="endIndex"/> の手前までの、チャンネルごとの状態を記録する（送らない）。</summary>
        private static ChannelState[] CollectStates(IReadOnlyList<MidiEvent> events, int endIndex)
        {
            ChannelState[] states = CreateStates();
            for (int i = 0; i < endIndex; i++)
            {
                MidiEvent e = events[i];
                if (e.Kind == MidiEventKind.Channel)
                {
                    states[e.Channel].Apply(e);
                }
                else if (e.Kind == MidiEventKind.SysEx && MidiReset.IsResetMessage(e.Data))
                {
                    states = CreateStates();
                }
            }
            return states;
        }

        private static ChannelState[] CreateStates()
        {
            return Enumerable.Range(0, 16).Select(_ => new ChannelState()).ToArray();
        }

        /// <summary>
        /// 1 チャンネル分の状態。値が -1 の項目は「曲中で一度も設定されていない」ことを表す。
        /// </summary>
        private sealed class ChannelState
        {
            private const int BankSelectMsb = 0;
            private const int BankSelectLsb = 32;
            private const int DataEntryMsb = 6;
            private const int DataEntryLsb = 38;
            private const int NrpnLsb = 98;
            private const int NrpnMsb = 99;
            private const int RpnLsb = 100;
            private const int RpnMsb = 101;
            private const int ResetAllControllers = 121;

            /// <summary>Reset All Controllers（CC121）で、音源側で初期値に戻る CC。</summary>
            private static readonly int[] ControllersResetByCc121 = { 1, 11, 64, 65, 66, 67 };

            private readonly int[] _controllers = Enumerable.Repeat(-1, 128).ToArray();

            // RPN / NRPN のパラメーター番号ごとの値（[0] = データエントリー MSB、[1] = LSB）
            private readonly Dictionary<int, int[]> _rpnValues = new Dictionary<int, int[]>();
            private readonly Dictionary<int, int[]> _nrpnValues = new Dictionary<int, int[]>();

            private int _program = -1;
            private int _programBankMsb = -1;
            private int _programBankLsb = -1;
            private int _rpnMsb = -1;
            private int _rpnLsb = -1;
            private int _nrpnMsb = -1;
            private int _nrpnLsb = -1;
            private bool _nrpnSelected;
            private int _pitchBend = -1;
            private int _channelPressure = -1;

            public void Apply(MidiEvent e)
            {
                switch (e.Command)
                {
                    case 0xB0:
                        ApplyControlChange(e.Data1, e.Data2);
                        break;
                    case 0xC0:
                        // バンクセレクトはプログラムチェンジの時点の値が音色を決める
                        _program = e.Data1;
                        _programBankMsb = _controllers[BankSelectMsb];
                        _programBankLsb = _controllers[BankSelectLsb];
                        break;
                    case 0xD0:
                        _channelPressure = e.Data1;
                        break;
                    case 0xE0:
                        _pitchBend = e.Data1 | (e.Data2 << 7);
                        break;
                }
            }

            /// <param name="blockedControllers">送らない CC（添字が CC 番号）。</param>
            public void Send(MidiOutDevice output, int channel, bool[] blockedControllers)
            {
                var control = new ControlSender(output, channel, blockedControllers);

                // いったんコントローラーを初期状態に戻してから、記録した値を送る
                control.Send(ResetAllControllers, 0);

                if (_program >= 0)
                {
                    control.SendIfSet(BankSelectMsb, _programBankMsb);
                    control.SendIfSet(BankSelectLsb, _programBankLsb);
                    output.SendShortMessage(0xC0 | channel, _program, 0);
                }
                // プログラムチェンジの後でバンクセレクトだけ変わっていた場合
                if (_controllers[BankSelectMsb] != _programBankMsb)
                {
                    control.SendIfSet(BankSelectMsb, _controllers[BankSelectMsb]);
                }
                if (_controllers[BankSelectLsb] != _programBankLsb)
                {
                    control.SendIfSet(BankSelectLsb, _controllers[BankSelectLsb]);
                }

                for (int controller = 1; controller < 120; controller++)
                {
                    if (controller != BankSelectLsb)
                    {
                        control.SendIfSet(controller, _controllers[controller]);
                    }
                }

                bool parameterSent = SendParameters(control, _rpnValues, RpnMsb, RpnLsb);
                parameterSent |= SendParameters(control, _nrpnValues, NrpnMsb, NrpnLsb);

                // 最後に選ばれていたパラメーターを選び直す
                if (_nrpnSelected && _nrpnMsb >= 0 && _nrpnLsb >= 0)
                {
                    control.Send(NrpnMsb, _nrpnMsb);
                    control.Send(NrpnLsb, _nrpnLsb);
                }
                else if (!_nrpnSelected && _rpnMsb >= 0 && _rpnLsb >= 0)
                {
                    control.Send(RpnMsb, _rpnMsb);
                    control.Send(RpnLsb, _rpnLsb);
                }
                else if (parameterSent)
                {
                    // RPN Null（どのパラメーターも選ばれていない状態）にする
                    control.Send(RpnMsb, 127);
                    control.Send(RpnLsb, 127);
                }

                if (_pitchBend >= 0)
                {
                    output.SendShortMessage(0xE0 | channel, _pitchBend & 0x7F, _pitchBend >> 7);
                }
                if (_channelPressure >= 0)
                {
                    output.SendShortMessage(0xD0 | channel, _channelPressure, 0);
                }
            }

            /// <summary>
            /// <paramref name="earlier"/> では設定していない項目（CC・プログラム・RPN・NRPN）を、この状態では設定しているかどうか。
            /// CC121 で初期値に戻る項目は数えない。
            /// </summary>
            public bool HasItemsNotSetIn(ChannelState earlier)
            {
                for (int controller = 0; controller < 120; controller++)
                {
                    if (_controllers[controller] >= 0 && earlier._controllers[controller] < 0
                        && Array.IndexOf(ControllersResetByCc121, controller) < 0)
                    {
                        return true;
                    }
                }
                return (_program >= 0 && earlier._program < 0)
                    || HasParametersNotSetIn(_rpnValues, earlier._rpnValues)
                    || HasParametersNotSetIn(_nrpnValues, earlier._nrpnValues);
            }

            private static bool HasParametersNotSetIn(Dictionary<int, int[]> table, Dictionary<int, int[]> earlierTable)
            {
                foreach (KeyValuePair<int, int[]> parameter in table)
                {
                    earlierTable.TryGetValue(parameter.Key, out int[] earlierValue);
                    for (int part = 0; part < parameter.Value.Length; part++)
                    {
                        if (parameter.Value[part] >= 0 && (earlierValue == null || earlierValue[part] < 0))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            private void ApplyControlChange(int controller, int value)
            {
                switch (controller)
                {
                    case RpnMsb:
                        _rpnMsb = value;
                        _nrpnSelected = false;
                        break;
                    case RpnLsb:
                        _rpnLsb = value;
                        _nrpnSelected = false;
                        break;
                    case NrpnMsb:
                        _nrpnMsb = value;
                        _nrpnSelected = true;
                        break;
                    case NrpnLsb:
                        _nrpnLsb = value;
                        _nrpnSelected = true;
                        break;
                    case DataEntryMsb:
                        StoreDataEntry(0, value);
                        break;
                    case DataEntryLsb:
                        StoreDataEntry(1, value);
                        break;
                    case 96:
                    case 97:
                        // データインクリメント / デクリメントは再現しない
                        break;
                    case ResetAllControllers:
                        // 音源側で初期値に戻る項目は、記録を消して送らないようにする
                        foreach (int resetController in ControllersResetByCc121)
                        {
                            _controllers[resetController] = -1;
                        }
                        _pitchBend = -1;
                        _channelPressure = -1;
                        _rpnMsb = _rpnLsb = _nrpnMsb = _nrpnLsb = -1;
                        break;
                    default:
                        if (controller < 120)
                        {
                            _controllers[controller] = value;
                        }
                        break;
                }
            }

            private void StoreDataEntry(int part, int value)
            {
                Dictionary<int, int[]> table;
                int key;
                if (_nrpnSelected)
                {
                    if (_nrpnMsb < 0 || _nrpnLsb < 0)
                    {
                        return;
                    }
                    table = _nrpnValues;
                    key = (_nrpnMsb << 7) | _nrpnLsb;
                }
                else
                {
                    if (_rpnMsb < 0 || _rpnLsb < 0 || (_rpnMsb == 127 && _rpnLsb == 127))
                    {
                        return;
                    }
                    table = _rpnValues;
                    key = (_rpnMsb << 7) | _rpnLsb;
                }

                if (!table.TryGetValue(key, out int[] entry))
                {
                    entry = new[] { -1, -1 };
                    table[key] = entry;
                }
                entry[part] = value;
            }

            private static bool SendParameters(ControlSender control, Dictionary<int, int[]> table, int msbController, int lsbController)
            {
                foreach (KeyValuePair<int, int[]> parameter in table)
                {
                    control.Send(msbController, parameter.Key >> 7);
                    control.Send(lsbController, parameter.Key & 0x7F);
                    control.SendIfSet(DataEntryMsb, parameter.Value[0]);
                    control.SendIfSet(DataEntryLsb, parameter.Value[1]);
                }
                return table.Count > 0;
            }
        }

        /// <summary>1 つのチャンネルにコントロールチェンジを送る。送らない CC は送らない。</summary>
        private struct ControlSender
        {
            private readonly MidiOutDevice _output;
            private readonly int _status;
            private readonly bool[] _blocked;

            public ControlSender(MidiOutDevice output, int channel, bool[] blockedControllers)
            {
                _output = output;
                _status = 0xB0 | channel;
                _blocked = blockedControllers;
            }

            public void Send(int controller, int value)
            {
                if (_blocked == null || !_blocked[controller])
                {
                    _output.SendShortMessage(_status, controller, value);
                }
            }

            /// <summary>値が記録されていれば（0 以上なら）送る。</summary>
            public void SendIfSet(int controller, int value)
            {
                if (value >= 0)
                {
                    Send(controller, value);
                }
            }
        }
    }
}
