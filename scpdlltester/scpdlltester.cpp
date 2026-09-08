#include <Windows.h>

#include <algorithm>
#include <atomic>
#include <chrono>
#include <cstdint>
#include <iomanip>
#include <iostream>
#include <numeric>
#include <string>
#include <thread>
#include <vector>

#include "ReportMapper.h"
#include "XInputBridge.h"

namespace
{
	int g_passed = 0;
	int g_failed = 0;

	void Check(bool condition, const char* expression, int line)
	{
		if (condition)
		{
			++g_passed;
			return;
		}

		++g_failed;
		std::cerr << "FAIL (" << line << "): " << expression << '\n';
	}

	DS3_RAW_INPUT_REPORT MakeIdleReport()
	{
		DS3_RAW_INPUT_REPORT report{};
		report.ReportId = 0x01;
		report.LeftThumbX = 0x80;
		report.LeftThumbY = 0x80;
		report.RightThumbX = 0x80;
		report.RightThumbY = 0x80;
		report.BatteryStatus = 0x05;
		return report;
	}

	double TicksToNanos(LARGE_INTEGER start, LARGE_INTEGER end, LARGE_INTEGER frequency)
	{
		return (static_cast<double>(end.QuadPart - start.QuadPart) * 1'000'000'000.0) /
			static_cast<double>(frequency.QuadPart);
	}

	int RunSelfTests()
	{
		std::cout << "XInputBridge self-test\n";

		{
			const SHORT minAxis = ReportMapper::ScaleDsToXi(0x00, FALSE);
			const SHORT center = ReportMapper::ScaleDsToXi(0x80, FALSE);
			const SHORT maxAxis = ReportMapper::ScaleDsToXi(0xFF, FALSE);
			Check(minAxis == -32767, "ScaleDsToXi min", __LINE__);
			Check(center == 0, "ScaleDsToXi center", __LINE__);
			Check(maxAxis == 32767, "ScaleDsToXi max", __LINE__);
			Check(ReportMapper::ScaleDsToXi(0x00, TRUE) == 32767, "ScaleDsToXi invert min", __LINE__);
		}

		{
			Check(ReportMapper::ToAxis(0x7F) == 0.0f, "ToAxis center", __LINE__);
			Check(ReportMapper::ToAxis(0x00) <= -1.0f + 0.01f, "ToAxis min", __LINE__);
			Check(ReportMapper::ToAxis(0xFF) >= 1.0f - 0.01f, "ToAxis max", __LINE__);
			Check(!IS_OUTSIDE_DZ(0x80), "dead zone center", __LINE__);
			Check(!IS_OUTSIDE_DZ(0x80 + 9), "dead zone inner edge", __LINE__);
			Check(IS_OUTSIDE_DZ(0x80 + 10), "dead zone outer edge", __LINE__);
			Check(IS_OUTSIDE_DZ(0x00), "dead zone left", __LINE__);
		}

		{
			DS3_RAW_INPUT_REPORT report = MakeIdleReport();
			report.Buttons.bButtons[0] = 0x10;
			report.Buttons.Individual.Cross = 1;
			report.Buttons.Individual.PS = 1;
			report.Pressure.Values.L2 = 0x40;
			report.LeftThumbX = 0x00;

			XINPUT_GAMEPAD gamepad{};
			ReportMapper::MapDs3ToXInputGamepad(report, gamepad, false);
			Check((gamepad.wButtons & XINPUT_GAMEPAD_DPAD_UP) != 0, "dpad up", __LINE__);
			Check((gamepad.wButtons & XINPUT_GAMEPAD_A) != 0, "cross -> A", __LINE__);
			Check((gamepad.wButtons & XINPUT_GAMEPAD_GUIDE) == 0, "GetState hides Guide", __LINE__);
			Check(gamepad.bLeftTrigger == 0x40, "L2 trigger", __LINE__);
			Check(gamepad.sThumbLX == -32767, "left stick scaled", __LINE__);

			ReportMapper::MapDs3ToXInputGamepad(report, gamepad, true);
			Check((gamepad.wButtons & XINPUT_GAMEPAD_GUIDE) != 0, "GetStateEx reports Guide only when PS is down", __LINE__);

			report.Buttons.Individual.PS = 0;
			ReportMapper::MapDs3ToXInputGamepad(report, gamepad, true);
			Check((gamepad.wButtons & XINPUT_GAMEPAD_GUIDE) == 0, "GetStateEx omits Guide when PS is up", __LINE__);
		}

		{
			DS3_RAW_INPUT_REPORT report = MakeIdleReport();
			report.Pressure.Values.Circle = 0xFF;
			report.Buttons.Individual.Start = 1;
			SCP_EXTN ext{};
			ReportMapper::MapDs3ToExtended(report, ext);
			Check(ext.SCP_C == 1.0f, "extended circle pressure", __LINE__);
			Check(ext.SCP_START == 1.0f, "extended start", __LINE__);
			Check(ext.SCP_LX == 0.0f, "extended stick stays centered in dead zone", __LINE__);
		}

		{
			DWORD userIndex = 0xFFFFFFFF;
			Check(!ReportMapper::MapXusbLedStateToUserIndex(0, userIndex), "LED all-off is invalid", __LINE__);
			Check(ReportMapper::MapXusbLedStateToUserIndex(6, userIndex) && userIndex == 0, "LED 1 on -> user 0", __LINE__);
			Check(ReportMapper::MapXusbLedStateToUserIndex(9, userIndex) && userIndex == 3, "LED 4 on -> user 3", __LINE__);
			Check(!ReportMapper::MapXusbLedStateToUserIndex(16, userIndex), "LED 16 is out of range", __LINE__);
			Check(!ReportMapper::MapXusbLedStateToUserIndex(255, userIndex), "LED 255 is out of range", __LINE__);
		}

		{
			DS3_RAW_INPUT_REPORT report = MakeIdleReport();
			DS3_RAW_INPUT_REPORT last{};
			DWORD packet = 0;
			ReportMapper::UpdateSyntheticPacketNumber(report, last, packet);
			Check(packet == 1, "first report increments packet", __LINE__);
			ReportMapper::UpdateSyntheticPacketNumber(report, last, packet);
			Check(packet == 1, "unchanged report keeps packet", __LINE__);
			report.Buttons.Individual.Start = 1;
			ReportMapper::UpdateSyntheticPacketNumber(report, last, packet);
			Check(packet == 2, "changed report increments packet", __LINE__);
		}

		{
			ReportMapper::SlotView slots[8]{};
			slots[0] = { XI_DEVICE_TYPE_DS3, INVALID_X_INPUT_USER_ID };
			slots[1] = { XI_DEVICE_TYPE_XUSB, 3 };

			Check(ReportMapper::FindDs3ByUserIndex(slots, 8, 0) == &slots[0], "DS3 occupies its slot index", __LINE__);
			Check(ReportMapper::FindDs3ByUserIndex(slots, 8, 1) == nullptr, "XUSB slot is not a DS3 user index", __LINE__);
			Check(ReportMapper::FindXusbByUserIndex(slots, 8, 3) == &slots[1], "XUSB is found by real user index", __LINE__);
			Check(ReportMapper::FindXusbByUserIndex(slots, 8, 1) == nullptr, "XUSB is not found by storage slot", __LINE__);
			Check(ReportMapper::FindFreeSlotIndex(slots, 8) == 2, "next free slot is 2", __LINE__);
		}

		{
			Check(ReportMapper::IsValidDs3FeatureRead(ReportMapper::kMinDs3FeatureReportBytes), "min feature length accepted", __LINE__);
			Check(!ReportMapper::IsValidDs3FeatureRead(0), "zero-length feature rejected", __LINE__);
			Check(!ReportMapper::IsValidDs3FeatureRead(-1), "HID error rejected", __LINE__);
		}

		{
			XINPUT_STATE state{};
			const DWORD pad0 = XInputGetState(0, &state);
			Check(pad0 == ERROR_SUCCESS || pad0 == ERROR_DEVICE_NOT_CONNECTED, "GetState(0) returns a valid XInput code", __LINE__);
			Check(XInputGetState(255, &state) == ERROR_DEVICE_NOT_CONNECTED, "invalid index GetState", __LINE__);
			Check(XInputGetState(0, nullptr) == ERROR_DEVICE_NOT_CONNECTED, "null GetState", __LINE__);
			const DWORD pad0Ex = XInputGetStateEx(0, &state);
			Check(pad0Ex == ERROR_SUCCESS || pad0Ex == ERROR_DEVICE_NOT_CONNECTED, "GetStateEx(0) returns a valid XInput code", __LINE__);
			Check(XInputSetState(0, nullptr) == ERROR_DEVICE_NOT_CONNECTED, "null SetState", __LINE__);

			XINPUT_CAPABILITIES caps{};
			const DWORD capsStatus = XInputGetCapabilities(0, 0x00000002, &caps);
			Check(capsStatus == ERROR_BAD_ARGUMENTS, "invalid capability flags", __LINE__);
		}

		{
			HMODULE module = nullptr;
			Check(GetModuleHandleExW(
					GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
					reinterpret_cast<LPCWSTR>(&XInputGetState),
					&module) != FALSE,
				"resolve loaded XInput1_3 module", __LINE__);

			if (module)
			{
				Check(GetProcAddress(module, MAKEINTRESOURCEA(2)) == reinterpret_cast<FARPROC>(XInputGetState), "ordinal 2", __LINE__);
				Check(GetProcAddress(module, MAKEINTRESOURCEA(3)) == reinterpret_cast<FARPROC>(XInputSetState), "ordinal 3", __LINE__);
				Check(GetProcAddress(module, MAKEINTRESOURCEA(4)) == reinterpret_cast<FARPROC>(XInputGetCapabilities), "ordinal 4", __LINE__);
				Check(GetProcAddress(module, MAKEINTRESOURCEA(100)) == reinterpret_cast<FARPROC>(XInputGetStateEx), "ordinal 100", __LINE__);
				Check(GetProcAddress(module, MAKEINTRESOURCEA(200)) == reinterpret_cast<FARPROC>(XInputGetExtended), "ordinal 200", __LINE__);
			}
		}

		{
			HMODULE module = nullptr;
			GetModuleHandleExW(
				GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
				reinterpret_cast<LPCWSTR>(&XInputGetState),
				&module);

			if (module)
			{
				wchar_t path[MAX_PATH]{};
				GetModuleFileNameW(module, path, MAX_PATH);
				const HMODULE extra = LoadLibraryW(path);
				Check(extra != nullptr, "extra LoadLibrary of proxy DLL", __LINE__);
				if (extra)
				{
					XINPUT_STATE state{};
					const auto getState = reinterpret_cast<decltype(XInputGetState)*>(GetProcAddress(extra, "XInputGetState"));
					Check(getState != nullptr && getState(1, &state) == ERROR_DEVICE_NOT_CONNECTED, "extra refcount GetState", __LINE__);
					Check(FreeLibrary(extra) != FALSE, "FreeLibrary extra refcount", __LINE__);
				}
			}
		}

		{
			constexpr int threadCount = 4;
			constexpr int iterations = 250;
			std::atomic<int> valid{ 0 };
			std::vector<std::thread> threads;
			threads.reserve(threadCount);

			for (int t = 0; t < threadCount; ++t)
			{
				threads.emplace_back([iterations, &valid, t]
				{
					for (int i = 0; i < iterations; ++i)
					{
						XINPUT_STATE state{};
						const DWORD status = XInputGetState(static_cast<DWORD>(t), &state);
						if (status == ERROR_DEVICE_NOT_CONNECTED || status == ERROR_SUCCESS)
							valid.fetch_add(1);
					}
				});
			}

			for (auto& thread : threads)
				thread.join();

			Check(valid.load() == threadCount * iterations, "concurrent GetState stays within XInput status codes", __LINE__);
		}

		std::cout << g_passed << " passed, " << g_failed << " failed\n";
		return g_failed == 0 ? 0 : 1;
	}

	int RunBenchmarks()
	{
		constexpr int iterations = 20000;
		LARGE_INTEGER frequency{};
		QueryPerformanceFrequency(&frequency);

		auto summarize = [&](const char* name, const std::vector<double>& samples)
		{
			std::vector<double> sorted = samples;
			std::ranges::sort(sorted);
			const double median = sorted[sorted.size() / 2];
			const double p95 = sorted[static_cast<size_t>(sorted.size() * 0.95)];
			const double p99 = sorted[static_cast<size_t>(sorted.size() * 0.99)];
			const double mean = std::accumulate(sorted.begin(), sorted.end(), 0.0) / static_cast<double>(sorted.size());
			std::cout << name
				<< " mean_ns=" << static_cast<int>(mean)
				<< " median_ns=" << static_cast<int>(median)
				<< " p95_ns=" << static_cast<int>(p95)
				<< " p99_ns=" << static_cast<int>(p99)
				<< '\n';
		};

		{
			XINPUT_STATE state{};
			const DWORD warmStatus = XInputGetState(0, &state);

			std::vector<double> samples;
			samples.reserve(iterations);
			for (int i = 0; i < iterations; ++i)
			{
				LARGE_INTEGER start{}, end{};
				QueryPerformanceCounter(&start);
				(void)XInputGetState(0, &state);
				QueryPerformanceCounter(&end);
				samples.push_back(TicksToNanos(start, end, frequency));
			}
			summarize(warmStatus == ERROR_SUCCESS ? "warm_getstate_slot0" : "warm_getstate_nodevice", samples);
		}

		{
			std::vector<double> samples;
			samples.reserve(iterations);
			DS3_RAW_INPUT_REPORT report = MakeIdleReport();
			report.Buttons.Individual.Cross = 1;
			report.LeftThumbX = 0x10;

			for (int i = 0; i < iterations; ++i)
			{
				XINPUT_GAMEPAD gamepad{};
				LARGE_INTEGER start{}, end{};
				QueryPerformanceCounter(&start);
				ReportMapper::MapDs3ToXInputGamepad(report, gamepad, true);
				QueryPerformanceCounter(&end);
				samples.push_back(TicksToNanos(start, end, frequency));
			}
			summarize("map_ds3_to_xinput", samples);
		}

		{
			std::vector<double> samples;
			samples.reserve(iterations);
			DS3_RAW_INPUT_REPORT report = MakeIdleReport();
			DS3_RAW_INPUT_REPORT last{};
			DWORD packet = 0;

			for (int i = 0; i < iterations; ++i)
			{
				report.Buttons.Individual.Start = static_cast<UCHAR>(i & 1);
				LARGE_INTEGER start{}, end{};
				QueryPerformanceCounter(&start);
				ReportMapper::UpdateSyntheticPacketNumber(report, last, packet);
				QueryPerformanceCounter(&end);
				samples.push_back(TicksToNanos(start, end, frequency));
			}
			summarize("packet_number_update", samples);
		}

		{
			constexpr int threadCount = 4;
			constexpr int perThread = 5000;
			std::atomic<long long> totalNs{ 0 };
			std::vector<std::thread> threads;

			const auto wallStart = std::chrono::steady_clock::now();
			for (int t = 0; t < threadCount; ++t)
			{
				threads.emplace_back([perThread, &totalNs, t]
				{
					XINPUT_STATE state{};
					LARGE_INTEGER frequency{};
					QueryPerformanceFrequency(&frequency);
					for (int i = 0; i < perThread; ++i)
					{
						LARGE_INTEGER start{}, end{};
						QueryPerformanceCounter(&start);
						(void)XInputGetState(static_cast<DWORD>(t), &state);
						QueryPerformanceCounter(&end);
						totalNs.fetch_add(static_cast<long long>(TicksToNanos(start, end, frequency)));
					}
				});
			}

			for (auto& thread : threads)
				thread.join();

			const auto wallEnd = std::chrono::steady_clock::now();
			const auto wallNs = std::chrono::duration_cast<std::chrono::nanoseconds>(wallEnd - wallStart).count();
			const double calls = static_cast<double>(threadCount * perThread);
			std::cout << "concurrent_getstate_nodevice mean_ns="
				<< static_cast<int>(static_cast<double>(totalNs.load()) / calls)
				<< " throughput_calls_per_s="
				<< static_cast<int>(calls / (static_cast<double>(wallNs) / 1'000'000'000.0))
				<< '\n';
		}

		return 0;
	}

	int RunHardwareTests()
	{
		std::cout << "XInputBridge hardware test (requires an SXS DS3)\n";

		DWORD ds3Index = 0xFFFFFFFF;
		int connectedCount = 0;
		int xusbCount = 0;

		for (DWORD i = 0; i < MAX_PLAYER_COUNT; ++i)
		{
			XINPUT_STATE state{};
			const DWORD getState = XInputGetState(i, &state);
			SCP_EXTN ext{};
			const DWORD extStatus = XInputGetExtended(i, &ext);

			if (getState != ERROR_SUCCESS)
				continue;

			++connectedCount;
			if (extStatus == ERROR_SUCCESS)
			{
				if (ds3Index == 0xFFFFFFFF)
					ds3Index = i;
				std::cout << "slot " << i << " DS3 packet=" << state.dwPacketNumber
					<< " buttons=0x" << std::hex << state.Gamepad.wButtons << std::dec << '\n';
			}
			else
			{
				++xusbCount;
				std::cout << "slot " << i << " XUSB packet=" << state.dwPacketNumber << '\n';
			}
		}

		Check(connectedCount > 0, "at least one XInput pad is connected", __LINE__);
		Check(ds3Index != 0xFFFFFFFF, "at least one SXS DS3 answers XInputGetExtended", __LINE__);

		if (ds3Index == 0xFFFFFFFF)
		{
			std::cout << g_passed << " passed, " << g_failed << " failed\n";
			return 1;
		}

		XINPUT_STATE state{};
		XINPUT_STATE stateEx{};
		Check(XInputGetState(ds3Index, &state) == ERROR_SUCCESS, "GetState DS3", __LINE__);
		Check(XInputGetStateEx(ds3Index, &stateEx) == ERROR_SUCCESS, "GetStateEx DS3", __LINE__);
		Check((state.Gamepad.wButtons & XINPUT_GAMEPAD_GUIDE) == 0, "GetState never reports Guide", __LINE__);
		std::cout << "GetStateEx Guide="
			<< ((stateEx.Gamepad.wButtons & XINPUT_GAMEPAD_GUIDE) != 0 ? 1 : 0)
			<< " (1 only if PS is held)\n";

		XINPUT_CAPABILITIES caps{};
		Check(XInputGetCapabilities(ds3Index, 0, &caps) == ERROR_SUCCESS, "GetCapabilities DS3", __LINE__);
		Check(caps.Type == XINPUT_DEVTYPE_GAMEPAD, "caps type gamepad", __LINE__);
		Check(caps.SubType == XINPUT_DEVSUBTYPE_GAMEPAD, "caps subtype gamepad", __LINE__);
		Check((caps.Flags & XINPUT_CAPS_FFB_SUPPORTED) != 0, "caps FFB", __LINE__);

		SCP_EXTN ext{};
		Check(XInputGetExtended(ds3Index, &ext) == ERROR_SUCCESS, "GetExtended DS3", __LINE__);
		Check(ext.SCP_LX >= -1.0f && ext.SCP_LX <= 1.0f, "extended LX in range", __LINE__);
		Check(ext.SCP_LY >= -1.0f && ext.SCP_LY <= 1.0f, "extended LY in range", __LINE__);
		Check(ext.SCP_PS == 0.0f || ext.SCP_PS == 1.0f, "extended PS is 0 or 1", __LINE__);
		const bool guideEx = (stateEx.Gamepad.wButtons & XINPUT_GAMEPAD_GUIDE) != 0;
		Check((ext.SCP_PS >= 0.5f) == guideEx, "GetExtended PS matches GetStateEx Guide", __LINE__);

		XINPUT_STATE first{}, second{};
		Check(XInputGetState(ds3Index, &first) == ERROR_SUCCESS, "packet sample 1", __LINE__);
		Sleep(50);
		Check(XInputGetState(ds3Index, &second) == ERROR_SUCCESS, "packet sample 2", __LINE__);
		Check(second.dwPacketNumber >= first.dwPacketNumber, "packet number is monotonic", __LINE__);

		XINPUT_VIBRATION rumble{ 20000, 20000 };
		Check(XInputSetState(ds3Index, &rumble) == ERROR_SUCCESS, "SetState rumble on", __LINE__);
		Sleep(250);
		rumble = {};
		Check(XInputSetState(ds3Index, &rumble) == ERROR_SUCCESS, "SetState rumble off", __LINE__);

		{
			constexpr int iterations = 2000;
			LARGE_INTEGER frequency{};
			QueryPerformanceFrequency(&frequency);
			XINPUT_STATE poll{};
			(void)XInputGetState(ds3Index, &poll);

			std::vector<double> getStateSamples;
			std::vector<double> getExtendedSamples;
			getStateSamples.reserve(iterations);
			getExtendedSamples.reserve(iterations);

			for (int i = 0; i < iterations; ++i)
			{
				LARGE_INTEGER start{}, end{};
				QueryPerformanceCounter(&start);
				(void)XInputGetState(ds3Index, &poll);
				QueryPerformanceCounter(&end);
				getStateSamples.push_back(TicksToNanos(start, end, frequency));
			}

			SCP_EXTN pollExt{};
			for (int i = 0; i < iterations; ++i)
			{
				LARGE_INTEGER start{}, end{};
				QueryPerformanceCounter(&start);
				(void)XInputGetExtended(ds3Index, &pollExt);
				QueryPerformanceCounter(&end);
				getExtendedSamples.push_back(TicksToNanos(start, end, frequency));
			}

			auto summarize = [](const char* name, const std::vector<double>& samples)
			{
				std::vector<double> sorted = samples;
				std::ranges::sort(sorted);
				const double median = sorted[sorted.size() / 2];
				const double p95 = sorted[static_cast<size_t>(sorted.size() * 0.95)];
				const double p99 = sorted[static_cast<size_t>(sorted.size() * 0.99)];
				const double mean = std::accumulate(sorted.begin(), sorted.end(), 0.0) /
					static_cast<double>(sorted.size());
				std::cout << name
					<< " mean_ns=" << static_cast<int>(mean)
					<< " median_ns=" << static_cast<int>(median)
					<< " p95_ns=" << static_cast<int>(p95)
					<< " p99_ns=" << static_cast<int>(p99)
					<< '\n';
			};

			summarize("warm_getstate_ds3", getStateSamples);
			summarize("warm_getextended_ds3", getExtendedSamples);
		}

		{
			constexpr int threadCount = 4;
			constexpr int iterations = 250;
			std::atomic<int> valid{ 0 };
			std::vector<std::thread> threads;
			const auto wallStart = std::chrono::steady_clock::now();

			for (int t = 0; t < threadCount; ++t)
			{
				threads.emplace_back([iterations, &valid, ds3Index]
				{
					for (int i = 0; i < iterations; ++i)
					{
						XINPUT_STATE poll{};
						if (XInputGetState(ds3Index, &poll) == ERROR_SUCCESS)
							valid.fetch_add(1);
					}
				});
			}

			for (auto& thread : threads)
				thread.join();

			const auto wallNs = std::chrono::duration_cast<std::chrono::nanoseconds>(
				std::chrono::steady_clock::now() - wallStart).count();
			const double calls = static_cast<double>(threadCount * iterations);
			Check(valid.load() == threadCount * iterations, "concurrent GetState on DS3 stays connected", __LINE__);
			std::cout << "concurrent_getstate_ds3 mean_ns="
				<< static_cast<int>(static_cast<double>(wallNs) / calls)
				<< " throughput_calls_per_s="
				<< static_cast<int>(calls / (static_cast<double>(wallNs) / 1'000'000'000.0))
				<< '\n';
		}

		{
			HMODULE module = nullptr;
			GetModuleHandleExW(
				GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
				reinterpret_cast<LPCWSTR>(&XInputGetState),
				&module);
			Check(module != nullptr, "resolve loaded proxy for reload cycles", __LINE__);

			if (module)
			{
				wchar_t path[MAX_PATH]{};
				GetModuleFileNameW(module, path, MAX_PATH);
				int reloadOk = 0;
				for (int i = 0; i < 20; ++i)
				{
					const HMODULE extra = LoadLibraryW(path);
					if (!extra)
						continue;
					const auto getState = reinterpret_cast<decltype(XInputGetState)*>(
						GetProcAddress(extra, "XInputGetState"));
					XINPUT_STATE poll{};
					if (getState && getState(ds3Index, &poll) == ERROR_SUCCESS && FreeLibrary(extra))
						++reloadOk;
					else if (extra)
						FreeLibrary(extra);
				}
				Check(reloadOk == 20, "20 LoadLibrary/GetState/FreeLibrary cycles on DS3", __LINE__);
			}
		}

		std::cout << "connected=" << connectedCount << " ds3=" << (connectedCount - xusbCount)
			<< " xusb=" << xusbCount << " tested_slot=" << ds3Index << '\n';
		std::cout << g_passed << " passed, " << g_failed << " failed\n";
		return g_failed == 0 ? 0 : 1;
	}

	int RunPollLoop()
	{
		XINPUT_STATE state{};

		while (true)
		{
			const DWORD ret = XInputGetState(0, &state);

			if (ret == ERROR_SUCCESS)
			{
				std::cout << '\r' << "Pad 0 connected, packet counter: " << std::setw(5) << state.dwPacketNumber;
			}
			else if (ret == ERROR_DEVICE_NOT_CONNECTED)
			{
				std::cout << '\r' << "Pad 0 not connected" << std::setfill(' ') << std::setw(30);
			}

			Sleep(20);
		}
	}
}

int main(int argc, char** argv)
{
	std::string mode = "poll";
	if (argc > 1)
		mode = argv[1];

	if (mode == "--self-test" || mode == "self-test")
		return RunSelfTests();

	if (mode == "--bench" || mode == "bench")
		return RunBenchmarks();

	if (mode == "--hw-test" || mode == "hw-test")
		return RunHardwareTests();

	if (mode == "--help" || mode == "-h")
	{
		std::cout << "scpdlltester [--self-test|--bench|--hw-test|--poll]\n";
		return 0;
	}

	return RunPollLoop();
}
