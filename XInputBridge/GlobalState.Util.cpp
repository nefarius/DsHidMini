#include "GlobalState.h"

SHORT GlobalState::ScaleDsToXi(UCHAR value, BOOLEAN invert)
{
	auto intValue = value - 0x80;
	if (intValue == -128)
		intValue = -127;

	const auto wtfValue = intValue * 258.00787401574803149606299212599f; // what the fuck?

	return static_cast<short>(invert ? -wtfValue : wtfValue);
}

float GlobalState::ClampAxis(float value)
{
	if (value > 1.0f)
		return 1.0f;
	if (value < -1.0f)
		return -1.0f;
	return value;
}

float GlobalState::ToAxis(UCHAR value)
{
	return ClampAxis((((value & 0xFF) - 0x7F) * 2) / 254.0f);
}
