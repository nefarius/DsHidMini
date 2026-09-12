namespace Nefarius.DsHidMini.ControlApp.Tests;

/// <summary>
///     Driver-faithful port of <c>research/ds3-motion/probe/GyroCal.cs</c>.
///     <see cref="CalByte" /> is the truncated unclamped value Sony writes.
/// </summary>
internal sealed class SonyGyroTracker
{
    private const int Target = 512;
    private const int StepQ10 = 0x6999;
    private const int SettleInitial = 32;
    private const int SettleAfterCal = 2;
    private const int RawMax = 0x333;
    private const int RawMin = 0xCC;
    private const int BlockN = 16;
    private const int BlockVarMax = 10;
    private const int RingN = 4;
    private const int RingRangeMax = 4;
    private const int LongN = 0xEC;
    private const int PendingTol0 = 100;
    private const int PendingDecay = 1;

    private readonly int[] _ring = new int[RingN];
    private int _blockCount;
    private int _blockMax = int.MinValue;
    private int _blockMin = int.MaxValue;
    private int _blockSum;
    private int _calByte;
    private int _lastRaw;
    private int _longCount;
    private int _longSum;
    private bool _moving;
    private int _output;
    private bool _pending;
    private int _pendingCal;
    private int _pendingTol;
    private int _pendingZero;
    private int _ringFilled;
    private int _ringIdx;
    private int _ringSum;
    private int _settleLeft;
    private bool _softwareOnly;
    private int _zeroRef;

    public byte CalByte => unchecked((byte)_calByte);

    public int CalByteRaw => _calByte;

    public int ZeroRef => _zeroRef;

    public int Output => _output;

    private static int Q10(int x) => (x + ((x >> 31) & 0x3FF)) >> 10;

    private static int Clamp10(int value) => value < 0 ? 0 : value > 1023 ? 1023 : value;

    public byte Initial(ushort eepromCal, ushort eepromZero, bool softwareOnly = false)
    {
        _softwareOnly = softwareOnly;
        _calByte = eepromCal;
        _lastRaw = eepromZero;
        _settleLeft = SettleInitial;
        Retarget(eepromZero, out _zeroRef, out _calByte);
        _output = Clamp10(_zeroRef - eepromZero + Target);
        return CalByte;
    }

    public int Runtime(int raw, out bool calChanged)
    {
        calChanged = false;

        if (_settleLeft > 0)
        {
            _settleLeft--;
            _lastRaw = raw;
            return _output;
        }

        _output = Clamp10(Target - raw + _zeroRef);
        calChanged = Track(raw);

        if (_pending)
        {
            int jump = raw - _lastRaw;
            if (-_pendingTol <= jump && jump <= _pendingTol)
            {
                _pendingTol -= PendingDecay;
            }
            else
            {
                ApplyPending();
                ResetBlock();
                calChanged = true;
            }
        }

        _lastRaw = raw;
        return _output;
    }

    private bool Retarget(int restAvg, out int zeroRef, out int calByte)
    {
        if (_softwareOnly)
        {
            zeroRef = restAvg;
            calByte = _calByte;
            return false;
        }

        int delta = (Target - restAvg) * 1024;
        if (-StepQ10 <= delta && delta <= StepQ10)
        {
            zeroRef = restAvg;
            calByte = _calByte;
            return false;
        }

        int steps = delta / StepQ10;
        zeroRef = restAvg + Q10(steps * StepQ10);
        calByte = _calByte + steps;
        return true;
    }

    private bool Track(int raw)
    {
        if (raw > RawMax || raw < RawMin)
        {
            _moving = true;
        }

        _blockSum += raw;
        if (raw > _blockMax)
        {
            _blockMax = raw;
        }

        if (raw < _blockMin)
        {
            _blockMin = raw;
        }

        if (++_blockCount != BlockN)
        {
            return false;
        }

        _blockCount = 0;
        int blockAvg = (_blockSum + (BlockN / 2)) / BlockN;
        _blockSum = 0;
        int lastMin = _blockMin;
        int lastMax = _blockMax;
        _blockMin = int.MaxValue;
        _blockMax = int.MinValue;
        int variance = ((lastMax - blockAvg) * (lastMax - blockAvg)) + ((blockAvg - lastMin) * (blockAvg - lastMin));

        _longSum += blockAvg;
        if (++_longCount == LongN)
        {
            _longCount = 0;
            int longAvg = (_longSum + (LongN / 2)) / LongN;
            _longSum = 0;
            if (Retarget(longAvg, out int z, out int c))
            {
                _pendingZero = z;
                _pendingCal = c;
                _pending = true;
                _pendingTol = PendingTol0;
            }
            else
            {
                _pendingZero = _zeroRef = longAvg;
            }
        }

        if (_moving)
        {
            _moving = false;
            return false;
        }

        if (variance < BlockVarMax)
        {
            if (RingPush(blockAvg))
            {
                if (RingRange() < RingRangeMax)
                {
                    int restAvg = (_ringSum + (RingN / 2)) / RingN;
                    _longCount = 0;
                    _longSum = 0;
                    bool changed = Retarget(restAvg, out _zeroRef, out int newCal);
                    if (changed)
                    {
                        _calByte = newCal;
                        RingReset();
                        _settleLeft = SettleAfterCal;
                    }

                    _pending = false;
                    return changed;
                }
            }

            if (_pending)
            {
                ApplyPending();
                RingReset();
                _settleLeft = SettleAfterCal;
                return true;
            }
        }

        return false;
    }

    private void ApplyPending()
    {
        _zeroRef = _pendingZero;
        _calByte = _pendingCal;
        _pending = false;
        RingReset();
        _longCount = 0;
        _longSum = 0;
    }

    private void ResetBlock()
    {
        _blockCount = 0;
        _blockSum = 0;
        _blockMin = int.MaxValue;
        _blockMax = int.MinValue;
    }

    private bool RingPush(int value)
    {
        int old = _ring[_ringIdx];
        _ring[_ringIdx] = value;
        _ringSum += value;
        _ringIdx++;
        if (_ringFilled < RingN)
        {
            _ringFilled++;
            if (_ringFilled < RingN)
            {
                if (_ringIdx == RingN)
                {
                    _ringIdx = 0;
                }

                return false;
            }
        }
        else
        {
            _ringSum -= old;
        }

        if (_ringIdx == RingN)
        {
            _ringIdx = 0;
        }

        return true;
    }

    private int RingRange()
    {
        int min = int.MaxValue;
        int max = int.MinValue;
        foreach (int v in _ring)
        {
            if (v > max)
            {
                max = v;
            }

            if (v < min)
            {
                min = v;
            }
        }

        return max - min;
    }

    private void RingReset()
    {
        Array.Clear(_ring);
        _ringFilled = 0;
        _ringIdx = 0;
        _ringSum = 0;
    }
}
