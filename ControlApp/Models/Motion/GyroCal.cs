namespace Nefarius.DsHidMini.ControlApp.Models.Motion;

/// <summary>
///     Gyro auto-zero tracker ported from the DsHidMini research probe.
///     Maintains a running zero reference and cal byte so the gyro output
///     stays centered as the controller warms up.
/// </summary>
internal sealed class GyroCal
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

    private int _calByte;
    private int _zeroRef;
    private int _lastRaw;
    private int _output;
    private int _settleLeft;
    private bool _moving;

    private int _blockCount, _blockSum, _blockMin = int.MaxValue, _blockMax = int.MinValue;
    private int _longCount, _longSum;

    private readonly int[] _ring = new int[RingN];
    private int _ringFilled, _ringIdx, _ringSum, _ringRange;

    private bool _pending;
    private int _pendingCal, _pendingZero, _pendingTol;

    public int CalByte => Math.Clamp(_calByte, 0, 255);

    private static int Q10(int x) => (x + ((x >> 31) & 0x3FF)) >> 10;

    private bool Retarget(int restAvg, out int zeroRef, out int calByte)
    {
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

    /// <summary>Initializes from EEPROM values and returns the first cal byte to send.</summary>
    internal byte Initial(ushort eepromCal, ushort eepromZero)
    {
        _calByte = eepromCal;
        _lastRaw = eepromZero;
        _settleLeft = SettleInitial;
        Retarget(eepromZero, out _zeroRef, out _calByte);
        _output = Math.Clamp(_zeroRef - eepromZero + Target, 0, 1023);
        return unchecked((byte)CalByte);
    }

    /// <summary>Processes a raw gyro sample and returns the calibrated output.</summary>
    internal int Runtime(int raw, out byte calByte, out bool calChanged)
    {
        calChanged = false;

        if (_settleLeft > 0)
        {
            _settleLeft--;
            _lastRaw = raw;
            calByte = unchecked((byte)CalByte);
            return _output;
        }

        _output = Math.Clamp(Target - raw + _zeroRef, 0, 1023);
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
        calByte = unchecked((byte)CalByte);
        return _output;
    }

    private bool Track(int raw)
    {
        if (raw > RawMax || raw < RawMin)
        {
            _moving = true;
        }

        _blockSum += raw;
        if (raw > _blockMax) _blockMax = raw;
        if (raw < _blockMin) _blockMin = raw;
        if (++_blockCount != BlockN)
        {
            return false;
        }

        _blockCount = 0;
        int blockAvg = (_blockSum + BlockN / 2) / BlockN;
        _blockSum = 0;
        int lastMin = _blockMin, lastMax = _blockMax;
        _blockMin = int.MaxValue;
        _blockMax = int.MinValue;
        int var = (lastMax - blockAvg) * (lastMax - blockAvg) + (blockAvg - lastMin) * (blockAvg - lastMin);

        _longSum += blockAvg;
        if (++_longCount == LongN)
        {
            _longCount = 0;
            int longAvg = (_longSum + LongN / 2) / LongN;
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

        if (var < BlockVarMax)
        {
            if (RingPush(blockAvg))
            {
                _ringRange = RingRangeOf();
                if (_ringRange < RingRangeMax)
                {
                    int restAvg = (_ringSum + RingN / 2) / RingN;
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
                if (_ringIdx == RingN) _ringIdx = 0;
                return false;
            }
        }
        else
        {
            _ringSum -= old;
        }

        if (_ringIdx == RingN) _ringIdx = 0;
        return true;
    }

    private int RingRangeOf()
    {
        int min = int.MaxValue, max = int.MinValue;
        foreach (int v in _ring)
        {
            if (v > max) max = v;
            if (v < min) min = v;
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
